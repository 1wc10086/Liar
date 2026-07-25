module;
#include <array>
#include <cstddef>
#include <cstdint>
#include <cstdio>
#include <dlfcn.h>
#include <mutex>
#include <string_view>
#include <utility>
#include <vector>

module utility.requests.cpr.cpr_core;

namespace cpr_ns {
namespace {

struct Api {
    using abi_t = uint32_t (*)(); using version_t = const char* (*)(); using init_t = int32_t (*)(); using create_t = void* (*)(); using destroy_t = void (*)(void*); using reset_t = void (*)(void*);
    using set_url_t = int32_t (*)(void*, const char*, size_t); using set_method_t = set_url_t; using set_headers_t = int32_t (*)(void*, const char* const*, const size_t*, const char* const*, const size_t*, size_t);
    using set_body_t = int32_t (*)(void*, const byte*, size_t); using set_integer_t = int32_t (*)(void*, int32_t, int64_t); using perform_t = int32_t (*)(void*); using stream_callback_t = int32_t (*)(void*, const byte*, size_t); using stream_read_t = int64_t (*)(void*, byte*, size_t); using stream_progress_t = int32_t (*)(void*, int64_t, int64_t, int64_t, int64_t); using perform_stream_t = int32_t (*)(void*, void*, stream_callback_t, stream_callback_t, stream_read_t, stream_progress_t, int64_t); using get_long_t = int64_t (*)(void*, int32_t); using get_double_t = double (*)(void*, int32_t);
    using view_t = const byte* (*)(void*, size_t*); using response_long_t = int64_t (*)(void*); using response_double_t = double (*)(void*); using error_t = const char* (*)(int32_t);
    void* library{}; const char* path{}; const char* load_error{}; abi_t abi{}; version_t version{}; init_t init{}; create_t create{}; destroy_t destroy{}; reset_t reset{}; set_url_t set_url{}; set_method_t set_method{}; set_headers_t set_headers{}; set_body_t set_body{}; set_integer_t set_long{}; set_integer_t set_off_t{}; perform_t perform{}; perform_stream_t perform_stream{}; get_long_t get_long{}; get_double_t get_double{}; view_t body{}; view_t headers{}; response_long_t status{}; response_double_t elapsed{}; error_t error{};
    [[nodiscard]] bool ready() const noexcept { return library && abi && version && init && create && destroy && reset && set_url && set_method && set_headers && set_body && set_long && set_off_t && perform && get_long && get_double && body && headers && status && elapsed && error; }
};

Api api; std::once_flag once; std::array<char, 4096> fallback{};
template <class T> void bind(T& destination, const char* name) noexcept { destination = reinterpret_cast<T>(dlsym(api.library, name)); }
void initialize() noexcept {
    api.library = dlopen("libcpr.so", RTLD_NOW | RTLD_LOCAL); if (api.library) api.path = "libcpr.so";
    if (!api.library) { Dl_info info{}; if (dladdr(reinterpret_cast<const void*>(&initialize), &info) && info.dli_fname) { const std::string_view path{info.dli_fname}; if (const auto slash = path.rfind('/'); slash != path.npos && slash + sizeof("/libcpr.so") <= fallback.size()) { const auto directory = path.substr(0, slash); std::snprintf(fallback.data(), fallback.size(), "%.*s/libcpr.so", static_cast<int>(directory.size()), directory.data()); api.library = dlopen(fallback.data(), RTLD_NOW | RTLD_LOCAL); if (api.library) api.path = fallback.data(); } } }
    if (!api.library) { api.load_error = dlerror(); return; }
    bind(api.abi, "kcpr_abi_version"); bind(api.version, "kcpr_version"); bind(api.init, "kcpr_global_init"); bind(api.create, "kcpr_easy_create"); bind(api.destroy, "kcpr_easy_destroy"); bind(api.reset, "kcpr_easy_reset"); bind(api.set_url, "kcpr_easy_set_url"); bind(api.set_method, "kcpr_easy_set_method"); bind(api.set_headers, "kcpr_easy_set_headers"); bind(api.set_body, "kcpr_easy_set_body"); bind(api.set_long, "kcpr_easy_set_long"); bind(api.set_off_t, "kcpr_easy_set_off_t"); bind(api.perform, "kcpr_easy_perform"); bind(api.get_long, "kcpr_easy_get_long"); bind(api.get_double, "kcpr_easy_get_double"); bind(api.body, "kcpr_response_body"); bind(api.headers, "kcpr_response_headers"); bind(api.status, "kcpr_response_status"); bind(api.elapsed, "kcpr_response_elapsed"); bind(api.error, "kcpr_error");
    if (!api.ready()) { api.load_error = "missing required cpr symbols"; return; } if (api.abi() < 1 || api.abi() > 2) { api.load_error = "unsupported cpr ABI"; return; } if (api.abi() >= 2) bind(api.perform_stream, "kcpr_easy_perform_stream"); if (api.init() != 0) { api.load_error = "curl global initialization failed"; return; }
}
Api& instance() noexcept { std::call_once(once, initialize); return api; }
constexpr int32_t curlopt_timeout_ms = 155; constexpr int32_t curlopt_connecttimeout_ms = 156; constexpr int32_t curlopt_followlocation = 52; constexpr int32_t curlopt_ssl_verifypeer = 64;

}

std::string_view Response::error() const noexcept { return cpr_ns::error(code); }
Easy::Easy() noexcept { auto& a = instance(); if (a.ready()) handle_ = a.create(); }
Easy::Easy(Easy&& other) noexcept : handle_(std::exchange(other.handle_, nullptr)) {}
Easy& Easy::operator=(Easy&& other) noexcept { if (this != &other) { if (handle_) instance().destroy(handle_); handle_ = std::exchange(other.handle_, nullptr); } return *this; }
Easy::~Easy() { if (handle_) instance().destroy(handle_); }
Easy::operator bool() const noexcept { return handle_ != nullptr; }
bool Easy::set_long(int32_t option, int64_t value) noexcept { auto& a = instance(); return handle_ && a.set_long(handle_, option, value); }
bool Easy::set_off_t(int32_t option, int64_t value) noexcept { auto& a = instance(); return handle_ && a.set_off_t(handle_, option, value); }
int64_t Easy::get_long(int32_t info) noexcept { auto& a = instance(); return handle_ && a.get_long ? a.get_long(handle_, info) : 0; }
double Easy::get_double(int32_t info) noexcept { auto& a = instance(); return handle_ && a.get_double ? a.get_double(handle_, info) : 0; }
void Easy::reset() noexcept { if (handle_) instance().reset(handle_); }
Response Easy::perform(const Request& request) {
    Response response; auto& a = instance(); if (!handle_ || !a.ready() || request.url.empty()) { response.code = -1; return response; }
    std::vector<const char*> names(request.headers.size()), values(request.headers.size()); std::vector<size_t> name_sizes(request.headers.size()), value_sizes(request.headers.size());
    for (size_t index{}; index < request.headers.size(); ++index) { names[index] = request.headers[index].name.data(); values[index] = request.headers[index].value.data(); name_sizes[index] = request.headers[index].name.size(); value_sizes[index] = request.headers[index].value.size(); }
    if (!a.set_url(handle_, request.url.data(), request.url.size()) || !a.set_method(handle_, request.method.data(), request.method.size()) || !a.set_headers(handle_, names.data(), name_sizes.data(), values.data(), value_sizes.data(), names.size()) || !a.set_body(handle_, request.body.data(), request.body.size())) { response.code = -1; return response; }
    if (request.timeout_ms > 0) a.set_long(handle_, curlopt_timeout_ms, request.timeout_ms); if (request.connect_timeout_ms > 0) a.set_long(handle_, curlopt_connecttimeout_ms, request.connect_timeout_ms); a.set_long(handle_, curlopt_followlocation, request.follow_redirects); a.set_long(handle_, curlopt_ssl_verifypeer, request.verify_peer);
    response.code = a.perform(handle_); response.status = a.status(handle_); response.elapsed = a.elapsed(handle_); size_t size{}; if (const auto* data = a.body(handle_, &size)) response.body.assign(data, data + size); if (const auto* data = a.headers(handle_, &size)) response.headers.assign(data, data + size); return response;
}

Response Easy::perform_stream(const Request& request, StreamCallbacks& callbacks) {
    Response response;
    auto& a = instance();
    if (!handle_ || !a.ready() || !a.perform_stream || request.url.empty() || !callbacks.write) { response.code = -1; return response; }
    std::vector<const char*> names(request.headers.size()), values(request.headers.size()); std::vector<size_t> name_sizes(request.headers.size()), value_sizes(request.headers.size());
    for (size_t index{}; index < request.headers.size(); ++index) { names[index] = request.headers[index].name.data(); values[index] = request.headers[index].value.data(); name_sizes[index] = request.headers[index].name.size(); value_sizes[index] = request.headers[index].value.size(); }
    if (!a.set_url(handle_, request.url.data(), request.url.size()) || !a.set_method(handle_, request.method.data(), request.method.size()) || !a.set_headers(handle_, names.data(), name_sizes.data(), values.data(), value_sizes.data(), names.size()) || (!callbacks.read && !a.set_body(handle_, request.body.data(), request.body.size()))) { response.code = -1; return response; }
    if (request.timeout_ms > 0) a.set_long(handle_, curlopt_timeout_ms, request.timeout_ms); if (request.connect_timeout_ms > 0) a.set_long(handle_, curlopt_connecttimeout_ms, request.connect_timeout_ms); a.set_long(handle_, curlopt_followlocation, request.follow_redirects); a.set_long(handle_, curlopt_ssl_verifypeer, request.verify_peer);
    struct CallbackState { StreamCallbacks* callbacks; } state{&callbacks};
    const auto write = [](void* opaque, const byte* data, size_t size) -> int32_t { auto& callbacks = *static_cast<CallbackState*>(opaque)->callbacks; return callbacks.write && callbacks.write({data, size}) ? 1 : 0; };
    const auto header = [](void* opaque, const byte* data, size_t size) -> int32_t { auto& callbacks = *static_cast<CallbackState*>(opaque)->callbacks; return !callbacks.header || callbacks.header({data, size}) ? 1 : 0; };
    const auto read = [](void* opaque, byte* data, size_t size) -> int64_t { auto& callbacks = *static_cast<CallbackState*>(opaque)->callbacks; return callbacks.read ? static_cast<int64_t>(callbacks.read({data, size})) : -1; };
    const auto progress = [](void* opaque, int64_t dt, int64_t dn, int64_t ut, int64_t un) -> int32_t { auto& callbacks = *static_cast<CallbackState*>(opaque)->callbacks; return !callbacks.progress || callbacks.progress(dt, dn, ut, un) ? 0 : 1; };
    response.code = a.perform_stream(handle_, &state, write, header, callbacks.read ? read : nullptr, callbacks.progress ? progress : nullptr, callbacks.upload_size);
    response.status = a.status(handle_); response.elapsed = a.elapsed(handle_); return response;
}
bool loaded() noexcept { return instance().ready(); }
uint32_t abi_version() noexcept { auto& a = instance(); return a.abi ? a.abi() : 0; }
std::string_view version() noexcept { auto& a = instance(); return a.version ? a.version() : ""; }
std::string_view library_path() noexcept { auto& a = instance(); return a.path ? a.path : ""; }
std::string_view load_error() noexcept { auto& a = instance(); return a.load_error ? a.load_error : ""; }
std::string_view error(int32_t code) noexcept { auto& a = instance(); return a.error ? a.error(code) : "cpr bridge unavailable"; }
Response request(const Request& request) { Easy easy; return easy.perform(request); }
Response request_stream(const Request& request, StreamCallbacks& callbacks) { Easy easy; return easy.perform_stream(request, callbacks); }

}
