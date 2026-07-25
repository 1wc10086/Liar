#include <zip.h>

#include <algorithm>
#include <cstddef>
#include <cstdint>
#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <limits>
#include <memory>
#include <new>
#include <vector>

extern "C" {

struct kzip_entry_input {
    const char* name;
    const void* data;
    uint64_t size;
    uint32_t directory;
};

struct kzip_entry_info {
    const char* name;
    uint64_t size;
    uint64_t compressed_size;
    uint16_t method;
    uint16_t encryption;
    uint32_t directory;
};

}

#define KZIP_API __attribute__((visibility("default")))

namespace {

struct Output {
    std::vector<std::byte> data;
    uint64_t position{};
    zip_error_t error;

    Output() { zip_error_init(&error); }
    ~Output() { zip_error_fini(&error); }
};

zip_int64_t output_source(void* state, void* data, zip_uint64_t length, zip_source_cmd_t command) {
    auto& out = *static_cast<Output*>(state);
    switch (command) {
    case ZIP_SOURCE_OPEN: out.position = 0; return 0;
    case ZIP_SOURCE_CLOSE: return 0;
    case ZIP_SOURCE_READ: {
        const auto available = out.position < out.data.size() ? out.data.size() - static_cast<size_t>(out.position) : 0;
        const auto read = std::min<size_t>(available, static_cast<size_t>(length));
        if (read) std::memcpy(data, out.data.data() + out.position, read);
        out.position += read;
        return static_cast<zip_int64_t>(read);
    }
    case ZIP_SOURCE_TELL: return static_cast<zip_int64_t>(out.position);
    case ZIP_SOURCE_SEEK: {
        if (length < sizeof(zip_source_args_seek_t)) return -1;
        const auto& args = *static_cast<const zip_source_args_seek_t*>(data);
        const auto base = args.whence == SEEK_SET ? 0ll : args.whence == SEEK_CUR ? static_cast<long long>(out.position) : args.whence == SEEK_END ? static_cast<long long>(out.data.size()) : -1ll;
        if (base < 0 || args.offset < -base) return -1;
        out.position = static_cast<uint64_t>(base + args.offset);
        return 0;
    }
    case ZIP_SOURCE_BEGIN_WRITE: out.position = 0; out.data.clear(); return 0;
    case ZIP_SOURCE_WRITE: {
        if (length > std::numeric_limits<size_t>::max() || out.position > std::numeric_limits<size_t>::max() - length) return -1;
        const auto end = static_cast<size_t>(out.position + length);
        if (end > out.data.size()) out.data.resize(end);
        std::memcpy(out.data.data() + out.position, data, static_cast<size_t>(length));
        out.position += length;
        return static_cast<zip_int64_t>(length);
    }
    case ZIP_SOURCE_TELL_WRITE: return static_cast<zip_int64_t>(out.position);
    case ZIP_SOURCE_SEEK_WRITE: {
        if (length < sizeof(zip_source_args_seek_t)) return -1;
        const auto& args = *static_cast<const zip_source_args_seek_t*>(data);
        const auto base = args.whence == SEEK_SET ? 0ll : args.whence == SEEK_CUR ? static_cast<long long>(out.position) : args.whence == SEEK_END ? static_cast<long long>(out.data.size()) : -1ll;
        if (base < 0 || args.offset < -base) return -1;
        out.position = static_cast<uint64_t>(base + args.offset);
        return 0;
    }
    case ZIP_SOURCE_COMMIT_WRITE: return 0;
    case ZIP_SOURCE_ROLLBACK_WRITE: out.data.clear(); out.position = 0; return 0;
    case ZIP_SOURCE_REMOVE: out.data.clear(); out.position = 0; return 0;
    case ZIP_SOURCE_STAT: {
        zip_stat_t st;
        zip_stat_init(&st);
        st.valid = ZIP_STAT_SIZE;
        st.size = out.data.size();
        if (length < sizeof(st)) return -1;
        std::memcpy(data, &st, sizeof(st));
        return sizeof(st);
    }
    case ZIP_SOURCE_ERROR: {
        if (length < sizeof(int) * 2) return -1;
        std::memset(data, 0, sizeof(int) * 2);
        return sizeof(int) * 2;
    }
    case ZIP_SOURCE_SUPPORTS: return ZIP_SOURCE_SUPPORTS_WRITABLE;
    case ZIP_SOURCE_FREE: return 0;
    default: return -1;
    }
}

struct Archive { zip_t* value{}; };
struct File { zip_file_t* value{}; };

}

extern "C" {

KZIP_API uint32_t kzip_abi_version() { return 1; }
KZIP_API const char* kzip_version_string() { return zip_libzip_version(); }
KZIP_API void kzip_free(void* data) { std::free(data); }

KZIP_API int32_t kzip_compress(const kzip_entry_input* entries, uint64_t count, int32_t level, const char* password, void** result, uint64_t* result_size) {
    if (!result || !result_size || (count && !entries)) return ZIP_ER_INVAL;
    *result = nullptr;
    *result_size = 0;
    auto* output = new (std::nothrow) Output;
    if (!output) return ZIP_ER_MEMORY;
    auto* source = zip_source_function_create(output_source, output, nullptr);
    if (!source) { delete output; return ZIP_ER_MEMORY; }
    auto* archive = zip_open_from_source(source, ZIP_CREATE | ZIP_TRUNCATE, nullptr);
    if (!archive) { zip_source_free(source); delete output; return ZIP_ER_OPEN; }
    for (uint64_t i = 0; i < count; ++i) {
        const auto& entry = entries[i];
        if (!entry.name || (!entry.directory && entry.size && !entry.data)) { zip_discard(archive); delete output; return ZIP_ER_INVAL; }
        zip_int64_t index;
        if (entry.directory) index = zip_dir_add(archive, entry.name, ZIP_FL_ENC_UTF_8);
        else {
            auto* input = zip_source_buffer(archive, entry.data, entry.size, 0);
            if (!input) { zip_discard(archive); delete output; return ZIP_ER_MEMORY; }
            index = zip_file_add(archive, entry.name, input, ZIP_FL_ENC_UTF_8);
            if (index >= 0 && zip_set_file_compression(archive, static_cast<zip_uint64_t>(index), ZIP_CM_DEFLATE, std::clamp(level, 0, 9)) < 0) index = -1;
            if (index >= 0 && password && *password && zip_file_set_encryption(archive, static_cast<zip_uint64_t>(index), ZIP_EM_TRAD_PKWARE, password) < 0) index = -1;
        }
        if (index < 0) {
            const auto error = zip_error_code_zip(zip_get_error(archive));
            zip_discard(archive);
            delete output;
            return error;
        }
    }
    if (zip_close(archive) < 0) { zip_discard(archive); delete output; return ZIP_ER_CLOSE; }
    if (output->data.size() > std::numeric_limits<uint64_t>::max()) { delete output; return ZIP_ER_MEMORY; }
    auto* memory = std::malloc(output->data.size());
    if (!memory && !output->data.empty()) { delete output; return ZIP_ER_MEMORY; }
    std::memcpy(memory, output->data.data(), output->data.size());
    *result = memory;
    *result_size = output->data.size();
    delete output;
    return ZIP_ER_OK;
}

KZIP_API void* kzip_open(const void* data, uint64_t size, const char* password) {
    if ((size && !data) || size > std::numeric_limits<zip_uint64_t>::max()) return nullptr;
    zip_error_t error;
    zip_error_init(&error);
    auto* source = zip_source_buffer_create(data, size, 0, &error);
    if (!source) { zip_error_fini(&error); return nullptr; }
    auto* value = zip_open_from_source(source, ZIP_RDONLY, &error);
    zip_error_fini(&error);
    if (!value) { zip_source_free(source); return nullptr; }
    if (password && *password && zip_set_default_password(value, password) < 0) { zip_close(value); return nullptr; }
    auto* archive = new (std::nothrow) Archive{value};
    if (!archive) { zip_close(value); return nullptr; }
    return archive;
}

KZIP_API void kzip_close(void* handle) { if (auto* archive = static_cast<Archive*>(handle)) { zip_close(archive->value); delete archive; } }
KZIP_API uint64_t kzip_entry_count(void* handle) { auto* archive = static_cast<Archive*>(handle); return archive ? zip_get_num_entries(archive->value, 0) : 0; }
KZIP_API int32_t kzip_entry_info(void* handle, uint64_t index, kzip_entry_info* out) {
    auto* archive = static_cast<Archive*>(handle);
    if (!archive || !out) return ZIP_ER_INVAL;
    zip_stat_t st;
    zip_stat_init(&st);
    if (zip_stat_index(archive->value, index, ZIP_FL_ENC_GUESS, &st) < 0) return zip_error_code_zip(zip_get_error(archive->value));
    out->name = st.name;
    out->size = st.size;
    out->compressed_size = st.comp_size;
    out->method = st.comp_method;
    out->encryption = st.encryption_method;
    out->directory = st.name && std::strlen(st.name) && st.name[std::strlen(st.name) - 1] == '/';
    return ZIP_ER_OK;
}
KZIP_API void* kzip_entry_open(void* handle, uint64_t index, const char* password) {
    auto* archive = static_cast<Archive*>(handle);
    if (!archive) return nullptr;
    auto* value = password && *password ? zip_fopen_index_encrypted(archive->value, index, 0, password) : zip_fopen_index(archive->value, index, 0);
    auto* file = value ? new (std::nothrow) File{value} : nullptr;
    if (!file && value) zip_fclose(value);
    return file;
}
KZIP_API int64_t kzip_entry_read(void* handle, void* output, uint64_t size) {
    auto* file = static_cast<File*>(handle);
    return file && (output || !size) ? zip_fread(file->value, output, size) : -1;
}
KZIP_API void kzip_entry_close(void* handle) { if (auto* file = static_cast<File*>(handle)) { zip_fclose(file->value); delete file; } }
KZIP_API int32_t kzip_pkware_supported() { return zip_encryption_method_supported(ZIP_EM_TRAD_PKWARE, 1); }

}
