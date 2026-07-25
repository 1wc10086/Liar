#include <curl/curl.h>
#include <cstddef>
#include <cstdint>
#include <new>
#include <string>
#include <string_view>
#include <unistd.h>
#include <utility>
#include <vector>

namespace {

struct Handle {
    CURL* easy{curl_easy_init()};
    curl_slist* headers{};
    std::string url;
    std::string method;
    std::string ca_path;
    std::vector<uint8_t> body;
    std::vector<uint8_t> response;
    std::vector<uint8_t> response_headers;
    CURLcode code{CURLE_OK};
    long status{};
    double elapsed{};

    ~Handle() {
        curl_slist_free_all(headers);
        curl_easy_cleanup(easy);
    }
};

using write_callback = int32_t (*)(void*, const uint8_t*, size_t);
using read_callback = int64_t (*)(void*, uint8_t*, size_t);
using progress_callback = int32_t (*)(void*, int64_t, int64_t, int64_t, int64_t);

struct StreamCallbacks {
    void* opaque{};
    write_callback write{};
    write_callback header{};
    read_callback read{};
    progress_callback progress{};
};

size_t append(char* data, size_t size, size_t count, void* opaque) noexcept {
    if (!opaque || (size && count && !data)) return 0;
    try {
        auto& output = *static_cast<std::vector<uint8_t>*>(opaque);
        const auto bytes = size * count;
        const auto* first = reinterpret_cast<const uint8_t*>(data);
        output.insert(output.end(), first, first + bytes);
        return bytes;
    } catch (...) { return 0; }
}

size_t stream_write(char* data, size_t size, size_t count, void* opaque) noexcept {
    auto* callbacks = static_cast<StreamCallbacks*>(opaque);
    const auto bytes = size * count;
    return callbacks && callbacks->write && callbacks->write(callbacks->opaque, reinterpret_cast<const uint8_t*>(data), bytes) != 0 ? bytes : 0;
}

size_t stream_header(char* data, size_t size, size_t count, void* opaque) noexcept {
    auto* callbacks = static_cast<StreamCallbacks*>(opaque);
    const auto bytes = size * count;
    return !callbacks || !callbacks->header || callbacks->header(callbacks->opaque, reinterpret_cast<const uint8_t*>(data), bytes) != 0 ? bytes : 0;
}

size_t stream_read(char* data, size_t size, size_t count, void* opaque) noexcept {
    auto* callbacks = static_cast<StreamCallbacks*>(opaque);
    if (!callbacks || !callbacks->read) return CURL_READFUNC_ABORT;
    const auto capacity = size * count;
    const auto read = callbacks->read(callbacks->opaque, reinterpret_cast<uint8_t*>(data), capacity);
    return read >= 0 && static_cast<uint64_t>(read) <= capacity ? static_cast<size_t>(read) : CURL_READFUNC_ABORT;
}

int stream_progress(void* opaque, curl_off_t download_total, curl_off_t download_now, curl_off_t upload_total, curl_off_t upload_now) noexcept {
    auto* callbacks = static_cast<StreamCallbacks*>(opaque);
    return callbacks && callbacks->progress && callbacks->progress(callbacks->opaque, download_total, download_now, upload_total, upload_now) == 0 ? 0 : 1;
}

bool set(CURL* easy, CURLoption option, const void* value) noexcept {
    return easy && curl_easy_setopt(easy, option, value) == CURLE_OK;
}

bool set(CURL* easy, CURLoption option, long value) noexcept {
    return easy && curl_easy_setopt(easy, option, value) == CURLE_OK;
}

bool set_off_t(CURL* easy, CURLoption option, curl_off_t value) noexcept {
    return easy && curl_easy_setopt(easy, option, value) == CURLE_OK;
}

bool configure_method(Handle& handle, std::string_view method) noexcept {
    if (method == "GET") return set(handle.easy, CURLOPT_NOBODY, 0L) && set(handle.easy, CURLOPT_POST, 0L) && set(handle.easy, CURLOPT_HTTPGET, 1L);
    if (method == "POST") return set(handle.easy, CURLOPT_NOBODY, 0L) && set(handle.easy, CURLOPT_POST, 1L);
    if (method == "HEAD") return set(handle.easy, CURLOPT_POST, 0L) && set(handle.easy, CURLOPT_NOBODY, 1L);
    return set(handle.easy, CURLOPT_NOBODY, 0L) && set(handle.easy, CURLOPT_POST, 0L) && set(handle.easy, CURLOPT_CUSTOMREQUEST, handle.method.c_str());
}

const char* android_ca_path() noexcept {
    constexpr const char* paths[]{"/system/etc/security/cacerts", "/apex/com.android.conscrypt/cacerts"};
    for (const auto* path : paths) if (access(path, R_OK | X_OK) == 0) return path;
    return nullptr;
}

}

extern "C" {

uint32_t kcpr_abi_version() noexcept { return 2; }
const char* kcpr_version() noexcept { return curl_version(); }
int32_t kcpr_global_init() noexcept { return static_cast<int32_t>(curl_global_init(CURL_GLOBAL_DEFAULT)); }
void kcpr_global_cleanup() noexcept { curl_global_cleanup(); }
void* kcpr_easy_create() noexcept { return new (std::nothrow) Handle; }
void kcpr_easy_destroy(void* value) noexcept { delete static_cast<Handle*>(value); }

void kcpr_easy_reset(void* value) noexcept {
    auto* handle = static_cast<Handle*>(value);
    if (!handle || !handle->easy) return;
    curl_easy_reset(handle->easy);
    curl_slist_free_all(std::exchange(handle->headers, nullptr));
    handle->url.clear();
    handle->method.clear();
    handle->ca_path.clear();
    handle->body.clear();
    handle->response.clear();
    handle->response_headers.clear();
    handle->code = CURLE_OK;
    handle->status = 0;
    handle->elapsed = 0;
}

int32_t kcpr_easy_set_url(void* value, const char* data, size_t size) noexcept {
    auto* handle = static_cast<Handle*>(value);
    if (!handle || !data) return 0;
    handle->url.assign(data, size);
    return set(handle->easy, CURLOPT_URL, handle->url.c_str());
}

int32_t kcpr_easy_set_method(void* value, const char* data, size_t size) noexcept {
    auto* handle = static_cast<Handle*>(value);
    if (!handle || !data) return 0;
    handle->method.assign(data, size);
    const std::string_view method{handle->method};
    return configure_method(*handle, method);
}

int32_t kcpr_easy_set_headers(void* value, const char* const* names, const size_t* name_sizes, const char* const* values, const size_t* value_sizes, size_t count) noexcept {
    auto* handle = static_cast<Handle*>(value);
    if (!handle) return 0;
    curl_slist_free_all(std::exchange(handle->headers, nullptr));
    for (size_t index{}; index < count; ++index) {
        if (!names[index] || !values[index]) return 0;
        std::string line;
        line.reserve(name_sizes[index] + value_sizes[index] + 2);
        line.append(names[index], name_sizes[index]);
        line.append(": ");
        line.append(values[index], value_sizes[index]);
        auto* next = curl_slist_append(handle->headers, line.c_str());
        if (!next) return 0;
        handle->headers = next;
    }
    return set(handle->easy, CURLOPT_HTTPHEADER, handle->headers);
}

int32_t kcpr_easy_set_body(void* value, const uint8_t* data, size_t size) noexcept {
    auto* handle = static_cast<Handle*>(value);
    if (!handle || (size && !data)) return 0;
    return set(handle->easy, CURLOPT_POSTFIELDS, size ? static_cast<const void*>(data) : static_cast<const void*>("")) && set_off_t(handle->easy, CURLOPT_POSTFIELDSIZE_LARGE, static_cast<curl_off_t>(size));
}

int32_t kcpr_easy_set_long(void* value, int32_t option, int64_t parameter) noexcept {
    const auto* handle = static_cast<Handle*>(value);
    return handle && set(handle->easy, static_cast<CURLoption>(option), static_cast<long>(parameter));
}

int32_t kcpr_easy_set_off_t(void* value, int32_t option, int64_t parameter) noexcept {
    const auto* handle = static_cast<Handle*>(value);
    return handle && set_off_t(handle->easy, static_cast<CURLoption>(option), static_cast<curl_off_t>(parameter));
}

int32_t kcpr_easy_set_string(void* value, int32_t option, const char* data, size_t size) noexcept {
    auto* handle = static_cast<Handle*>(value);
    if (!handle || !data) return 0;
    handle->body.assign(reinterpret_cast<const uint8_t*>(data), reinterpret_cast<const uint8_t*>(data) + size);
    handle->body.push_back(0);
    return set(handle->easy, static_cast<CURLoption>(option), reinterpret_cast<const char*>(handle->body.data()));
}

int32_t kcpr_easy_perform(void* value) noexcept {
    auto* handle = static_cast<Handle*>(value);
    if (!handle || !handle->easy) return static_cast<int32_t>(CURLE_FAILED_INIT);
    handle->response.clear();
    handle->response_headers.clear();
    curl_easy_setopt(handle->easy, CURLOPT_WRITEFUNCTION, &append);
    curl_easy_setopt(handle->easy, CURLOPT_WRITEDATA, &handle->response);
    curl_easy_setopt(handle->easy, CURLOPT_HEADERFUNCTION, &append);
    curl_easy_setopt(handle->easy, CURLOPT_HEADERDATA, &handle->response_headers);
    if (const auto* path = android_ca_path()) {
        handle->ca_path = path;
        curl_easy_setopt(handle->easy, CURLOPT_CAPATH, handle->ca_path.c_str());
    } else curl_easy_setopt(handle->easy, CURLOPT_CAPATH, nullptr);
    curl_easy_setopt(handle->easy, CURLOPT_SSL_VERIFYHOST, 2L);
    handle->code = curl_easy_perform(handle->easy);
    curl_easy_getinfo(handle->easy, CURLINFO_RESPONSE_CODE, &handle->status);
    curl_easy_getinfo(handle->easy, CURLINFO_TOTAL_TIME, &handle->elapsed);
    return static_cast<int32_t>(handle->code);
}

int32_t kcpr_easy_perform_stream(void* value, void* opaque, write_callback write, write_callback header, read_callback read, progress_callback progress, int64_t upload_size) noexcept {
    auto* handle = static_cast<Handle*>(value);
    if (!handle || !handle->easy || !write) return static_cast<int32_t>(CURLE_FAILED_INIT);
    StreamCallbacks callbacks{opaque, write, header, read, progress};
    handle->response.clear();
    handle->response_headers.clear();
    curl_easy_setopt(handle->easy, CURLOPT_WRITEFUNCTION, &stream_write);
    curl_easy_setopt(handle->easy, CURLOPT_WRITEDATA, &callbacks);
    curl_easy_setopt(handle->easy, CURLOPT_HEADERFUNCTION, &stream_header);
    curl_easy_setopt(handle->easy, CURLOPT_HEADERDATA, &callbacks);
    curl_easy_setopt(handle->easy, CURLOPT_XFERINFOFUNCTION, &stream_progress);
    curl_easy_setopt(handle->easy, CURLOPT_XFERINFODATA, &callbacks);
    curl_easy_setopt(handle->easy, CURLOPT_NOPROGRESS, progress ? 0L : 1L);
    if (read) {
        curl_easy_setopt(handle->easy, CURLOPT_READFUNCTION, &stream_read);
        curl_easy_setopt(handle->easy, CURLOPT_READDATA, &callbacks);
        curl_easy_setopt(handle->easy, CURLOPT_POSTFIELDS, nullptr);
        curl_easy_setopt(handle->easy, CURLOPT_POSTFIELDSIZE_LARGE, static_cast<curl_off_t>(upload_size));
        if (handle->method == "PUT") {
            curl_easy_setopt(handle->easy, CURLOPT_UPLOAD, 1L);
            curl_easy_setopt(handle->easy, CURLOPT_INFILESIZE_LARGE, static_cast<curl_off_t>(upload_size));
        } else if (handle->method == "POST") {
            curl_easy_setopt(handle->easy, CURLOPT_POST, 1L);
        }
    }
    if (const auto* path = android_ca_path()) {
        handle->ca_path = path;
        curl_easy_setopt(handle->easy, CURLOPT_CAPATH, handle->ca_path.c_str());
    } else curl_easy_setopt(handle->easy, CURLOPT_CAPATH, nullptr);
    curl_easy_setopt(handle->easy, CURLOPT_SSL_VERIFYHOST, 2L);
    handle->code = curl_easy_perform(handle->easy);
    curl_easy_getinfo(handle->easy, CURLINFO_RESPONSE_CODE, &handle->status);
    curl_easy_getinfo(handle->easy, CURLINFO_TOTAL_TIME, &handle->elapsed);
    return static_cast<int32_t>(handle->code);
}

int64_t kcpr_easy_get_long(void* value, int32_t info) noexcept {
    auto* handle = static_cast<Handle*>(value);
    long result{};
    return handle && curl_easy_getinfo(handle->easy, static_cast<CURLINFO>(info), &result) == CURLE_OK ? result : 0;
}

double kcpr_easy_get_double(void* value, int32_t info) noexcept {
    auto* handle = static_cast<Handle*>(value);
    double result{};
    return handle && curl_easy_getinfo(handle->easy, static_cast<CURLINFO>(info), &result) == CURLE_OK ? result : 0;
}

const uint8_t* kcpr_response_body(void* value, size_t* size) noexcept {
    auto* handle = static_cast<Handle*>(value);
    if (size) *size = handle ? handle->response.size() : 0;
    return handle && !handle->response.empty() ? handle->response.data() : nullptr;
}

const uint8_t* kcpr_response_headers(void* value, size_t* size) noexcept {
    auto* handle = static_cast<Handle*>(value);
    if (size) *size = handle ? handle->response_headers.size() : 0;
    return handle && !handle->response_headers.empty() ? handle->response_headers.data() : nullptr;
}

int64_t kcpr_response_status(void* value) noexcept { const auto* handle = static_cast<Handle*>(value); return handle ? handle->status : 0; }
double kcpr_response_elapsed(void* value) noexcept { const auto* handle = static_cast<Handle*>(value); return handle ? handle->elapsed : 0; }
const char* kcpr_error(int32_t code) noexcept { return curl_easy_strerror(static_cast<CURLcode>(code)); }

}
