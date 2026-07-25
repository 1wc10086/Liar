module;
#include <algorithm>
#include <array>
#include <cstddef>
#include <cstdint>
#include <cstdio>
#include <dlfcn.h>
#include <mutex>
#include <new>
#include <string_view>
#include <utility>

module utility.compression.zstd.zstd_core;

namespace zstd_ns {
namespace {

struct Api {
    using abi_version_t = uint32_t (*)();
    using version_number_t = uint32_t (*)();
    using version_string_t = const char* (*)();
    using content_size_const_t = uint64_t (*)();
    using is_error_t = uint32_t (*)(size_t);
    using error_name_t = const char* (*)(size_t);
    using compress_bound_t = size_t (*)(size_t);
    using frame_content_size_t = uint64_t (*)(const void*, size_t);
    using frame_header_size_t = uint32_t (*)(const void*, size_t, size_t*);
    using dict_id_t = uint32_t (*)(const void*, size_t);
    using compress_t = size_t (*)(void*, size_t, const void*, size_t, int32_t);
    using decompress_t = size_t (*)(void*, size_t, const void*, size_t);
    using compress_using_dict_t = size_t (*)(void*, size_t, const void*, size_t, const void*, size_t, int32_t);
    using decompress_using_dict_t = size_t (*)(void*, size_t, const void*, size_t, const void*, size_t);
    using create_cdict_t = void* (*)(const void*, size_t, int32_t);
    using free_cdict_t = void (*)(void*);
    using create_ddict_t = void* (*)(const void*, size_t);
    using free_ddict_t = void (*)(void*);
    using compress_cdict_t = size_t (*)(void*, void*, size_t, const void*, size_t);
    using decompress_ddict_t = size_t (*)(void*, void*, size_t, const void*, size_t);
    using train_dictionary_t = size_t (*)(void*, size_t, const void*, const size_t*, uint32_t);
    using train_samples_bound_t = size_t (*)(const size_t*, uint32_t);
    using compress_advanced_t = size_t (*)(void*, size_t, const void*, size_t, int32_t, int32_t, int32_t, int32_t, int32_t, int32_t, int32_t, int32_t, int32_t, int32_t, int32_t);
    using create_stream_t = void* (*)();
    using free_stream_t = void (*)(void*);
    using cstream_init_t = size_t (*)(void*, int32_t);
    using cstream_set_parameter_t = size_t (*)(void*, int32_t, int32_t);
    using cstream_load_cdict_t = size_t (*)(void*, void*);
    using cstream_compress_t = size_t (*)(void*, void*, size_t, size_t*, const void*, size_t, size_t*);
    using cstream_flush_t = size_t (*)(void*, void*, size_t, size_t*);
    using cstream_end_t = size_t (*)(void*, void*, size_t, size_t*);
    using dstream_init_t = size_t (*)(void*);
    using dstream_set_parameter_t = size_t (*)(void*, int32_t, int32_t);
    using dstream_load_ddict_t = size_t (*)(void*, void*);
    using dstream_decompress_t = size_t (*)(void*, void*, size_t, size_t*, const void*, size_t, size_t*);

    void* lib{};
    const char* path{};
    const char* error{};
    abi_version_t abi_version{};
    version_number_t version_number{};
    version_string_t version_string{};
    content_size_const_t content_size_unknown{};
    content_size_const_t content_size_error{};
    is_error_t is_error{};
    error_name_t error_name{};
    compress_bound_t compress_bound{};
    frame_content_size_t frame_content_size{};
    frame_content_size_t find_decompressed_size{};
    frame_header_size_t frame_header_size{};
    dict_id_t dict_id_from_dict{};
    dict_id_t dict_id_from_frame{};
    compress_t compress{};
    decompress_t decompress{};
    compress_using_dict_t compress_using_dict{};
    decompress_using_dict_t decompress_using_dict{};
    create_cdict_t create_cdict{};
    free_cdict_t free_cdict{};
    create_ddict_t create_ddict{};
    free_ddict_t free_ddict{};
    compress_cdict_t compress_cdict{};
    decompress_ddict_t decompress_ddict{};
    train_dictionary_t train_dictionary{};
    train_samples_bound_t train_samples_bound{};
    compress_advanced_t compress_advanced{};
    create_stream_t create_cstream{};
    free_stream_t free_cstream{};
    cstream_init_t cstream_init{};
    cstream_set_parameter_t cstream_set_parameter{};
    cstream_load_cdict_t cstream_load_cdict{};
    cstream_compress_t cstream_compress{};
    cstream_flush_t cstream_flush{};
    cstream_end_t cstream_end{};
    create_stream_t create_dstream{};
    free_stream_t free_dstream{};
    dstream_init_t dstream_init{};
    dstream_set_parameter_t dstream_set_parameter{};
    dstream_load_ddict_t dstream_load_ddict{};
    dstream_decompress_t dstream_decompress{};

    [[nodiscard]] bool ready() const noexcept {
        return lib && abi_version && version_number && version_string && is_error && error_name && compress_bound && frame_content_size && find_decompressed_size && frame_header_size && dict_id_from_dict && dict_id_from_frame && compress && decompress;
    }
};

Api api;
std::once_flag api_once;

std::array<char, 4096> fallback_path{};

template <class T>
void bind(T& out, const char* name) noexcept {
    out = reinterpret_cast<T>(dlsym(api.lib, name));
}

void init_api() noexcept {
    constexpr const char* names[] = {"libzstd.so", "libzstd.so.1"};
    for (auto name : names) {
        api.lib = dlopen(name, RTLD_NOW | RTLD_LOCAL);
        if (api.lib) {
            api.path = name;
            break;
        }
    }

    if (!api.lib) {
        Dl_info info{};
        if (dladdr(reinterpret_cast<const void*>(&init_api), &info) && info.dli_fname) {
            std::string_view path{info.dli_fname};
            if (const auto slash = path.rfind('/'); slash != std::string_view::npos && slash + 1 < fallback_path.size()) {
                const auto dir = path.substr(0, slash);
                if (dir.size() + 12 < fallback_path.size()) {
                    std::snprintf(fallback_path.data(), fallback_path.size(), "%.*s/libzstd.so", static_cast<int>(dir.size()), dir.data());
                    api.lib = dlopen(fallback_path.data(), RTLD_NOW | RTLD_LOCAL);
                    if (api.lib) api.path = fallback_path.data();
                }
            }
        }
    }

    if (!api.lib) {
        api.error = dlerror();
        return;
    }

    bind(api.abi_version, "kzstd_abi_version");
    bind(api.version_number, "kzstd_version_number");
    bind(api.version_string, "kzstd_version_string");
    bind(api.content_size_unknown, "kzstd_content_size_unknown");
    bind(api.content_size_error, "kzstd_content_size_error");
    bind(api.is_error, "kzstd_is_error");
    bind(api.error_name, "kzstd_error_name");
    bind(api.compress_bound, "kzstd_compress_bound");
    bind(api.frame_content_size, "kzstd_frame_content_size");
    bind(api.find_decompressed_size, "kzstd_find_decompressed_size");
    bind(api.frame_header_size, "kzstd_frame_header_size");
    bind(api.dict_id_from_dict, "kzstd_get_dict_id_from_dict");
    bind(api.dict_id_from_frame, "kzstd_get_dict_id_from_frame");
    bind(api.compress, "kzstd_compress");
    bind(api.decompress, "kzstd_decompress");
    bind(api.compress_using_dict, "kzstd_compress_using_dict");
    bind(api.decompress_using_dict, "kzstd_decompress_using_dict");
    bind(api.create_cdict, "kzstd_create_cdict");
    bind(api.free_cdict, "kzstd_free_cdict");
    bind(api.create_ddict, "kzstd_create_ddict");
    bind(api.free_ddict, "kzstd_free_ddict");
    bind(api.compress_cdict, "kzstd_compress_cdict");
    bind(api.decompress_ddict, "kzstd_decompress_ddict");
    bind(api.train_dictionary, "kzstd_train_dictionary");
    bind(api.train_samples_bound, "kzstd_get_train_samples_bound");
    bind(api.compress_advanced, "kzstd_compress_advanced");
    bind(api.create_cstream, "kzstd_create_cstream");
    bind(api.free_cstream, "kzstd_free_cstream");
    bind(api.cstream_init, "kzstd_cstream_init");
    bind(api.cstream_set_parameter, "kzstd_cstream_set_parameter");
    bind(api.cstream_load_cdict, "kzstd_cstream_load_cdict");
    bind(api.cstream_compress, "kzstd_cstream_compress");
    bind(api.cstream_flush, "kzstd_cstream_flush");
    bind(api.cstream_end, "kzstd_cstream_end");
    bind(api.create_dstream, "kzstd_create_dstream");
    bind(api.free_dstream, "kzstd_free_dstream");
    bind(api.dstream_init, "kzstd_dstream_init");
    bind(api.dstream_set_parameter, "kzstd_dstream_set_parameter");
    bind(api.dstream_load_ddict, "kzstd_dstream_load_ddict");
    bind(api.dstream_decompress, "kzstd_dstream_decompress");

    if (!api.ready()) api.error = "missing required kzstd symbol";
}

Api& zstd() noexcept {
    std::call_once(api_once, init_api);
    return api;
}

size_t missing() noexcept {
    return static_cast<size_t>(-1);
}

}

uint32_t abi_version() noexcept { auto& a = zstd(); return a.abi_version ? a.abi_version() : 0; }
uint32_t version_number() noexcept { auto& a = zstd(); return a.version_number ? a.version_number() : 0; }
std::string_view version_string() noexcept { auto& a = zstd(); return a.version_string ? std::string_view{a.version_string()} : std::string_view{}; }
bool loaded() noexcept { return zstd().ready(); }
std::string_view library_path() noexcept { auto& a = zstd(); return a.path ? std::string_view{a.path} : std::string_view{}; }
std::string_view load_error() noexcept { auto& a = zstd(); return a.error ? std::string_view{a.error} : std::string_view{}; }
bool is_error(size_t code) noexcept { auto& a = zstd(); return !a.is_error || a.is_error(code) != 0; }
const char* error_name(size_t code) noexcept { auto& a = zstd(); return a.error_name ? a.error_name(code) : "zstd bridge unavailable"; }
size_t compress_bound(size_t src_size) noexcept { auto& a = zstd(); return a.compress_bound ? a.compress_bound(src_size) : 0; }
uint64_t frame_content_size(const void* src, size_t src_size) noexcept { auto& a = zstd(); return a.frame_content_size ? a.frame_content_size(src, src_size) : content_size_error; }
uint64_t find_decompressed_size(const void* src, size_t src_size) noexcept { auto& a = zstd(); return a.find_decompressed_size ? a.find_decompressed_size(src, src_size) : content_size_error; }
bool frame_header_size(const void* src, size_t src_size, size_t& out_size) noexcept { auto& a = zstd(); return a.frame_header_size && a.frame_header_size(src, src_size, &out_size) != 0; }
uint32_t dict_id_from_dict(const void* dict, size_t dict_size) noexcept { auto& a = zstd(); return a.dict_id_from_dict ? a.dict_id_from_dict(dict, dict_size) : 0; }
uint32_t dict_id_from_frame(const void* src, size_t src_size) noexcept { auto& a = zstd(); return a.dict_id_from_frame ? a.dict_id_from_frame(src, src_size) : 0; }
size_t compress_to(void* dst, size_t dst_capacity, const void* src, size_t src_size, int32_t level) noexcept { auto& a = zstd(); return a.compress ? a.compress(dst, dst_capacity, src, src_size, level) : missing(); }
size_t decompress_to(void* dst, size_t dst_capacity, const void* src, size_t src_size) noexcept { auto& a = zstd(); return a.decompress ? a.decompress(dst, dst_capacity, src, src_size) : missing(); }
size_t compress_using_dict_to(void* dst, size_t dst_capacity, const void* src, size_t src_size, const void* dict, size_t dict_size, int32_t level) noexcept { auto& a = zstd(); return a.compress_using_dict ? a.compress_using_dict(dst, dst_capacity, src, src_size, dict, dict_size, level) : missing(); }
size_t decompress_using_dict_to(void* dst, size_t dst_capacity, const void* src, size_t src_size, const void* dict, size_t dict_size) noexcept { auto& a = zstd(); return a.decompress_using_dict ? a.decompress_using_dict(dst, dst_capacity, src, src_size, dict, dict_size) : missing(); }
size_t train_dictionary_to(void* dst, size_t dst_capacity, const void* samples, const size_t* sample_sizes, uint32_t sample_count) noexcept { auto& a = zstd(); return a.train_dictionary ? a.train_dictionary(dst, dst_capacity, samples, sample_sizes, sample_count) : missing(); }
size_t train_samples_bound(const size_t* sample_sizes, uint32_t sample_count) noexcept { auto& a = zstd(); return a.train_samples_bound ? a.train_samples_bound(sample_sizes, sample_count) : 0; }
size_t compress_advanced_to(void* dst, size_t dst_capacity, const void* src, size_t src_size, const AdvancedOptions& o) noexcept {
    auto& a = zstd();
    return a.compress_advanced ? a.compress_advanced(dst, dst_capacity, src, src_size, o.level, o.checksum, o.content_size, o.workers, o.window_log, o.chain_log, o.hash_log, o.search_log, o.min_match, o.target_length, o.strategy) : missing();
}

CDict::CDict(const void* dict, size_t dict_size, int32_t level) noexcept { auto& a = zstd(); if (a.create_cdict) handle_ = a.create_cdict(dict, dict_size, level); }
CDict::CDict(CDict&& other) noexcept : handle_(std::exchange(other.handle_, nullptr)) {}
CDict& CDict::operator=(CDict&& other) noexcept { if (this != &other) { reset(); handle_ = std::exchange(other.handle_, nullptr); } return *this; }
CDict::~CDict() { reset(); }
void CDict::reset() noexcept { auto& a = zstd(); if (handle_ && a.free_cdict) a.free_cdict(std::exchange(handle_, nullptr)); else handle_ = nullptr; }

DDict::DDict(const void* dict, size_t dict_size) noexcept { auto& a = zstd(); if (a.create_ddict) handle_ = a.create_ddict(dict, dict_size); }
DDict::DDict(DDict&& other) noexcept : handle_(std::exchange(other.handle_, nullptr)) {}
DDict& DDict::operator=(DDict&& other) noexcept { if (this != &other) { reset(); handle_ = std::exchange(other.handle_, nullptr); } return *this; }
DDict::~DDict() { reset(); }
void DDict::reset() noexcept { auto& a = zstd(); if (handle_ && a.free_ddict) a.free_ddict(std::exchange(handle_, nullptr)); else handle_ = nullptr; }

CStream::CStream() noexcept { auto& a = zstd(); if (a.create_cstream) handle_ = a.create_cstream(); }
CStream::CStream(CStream&& other) noexcept : handle_(std::exchange(other.handle_, nullptr)) {}
CStream& CStream::operator=(CStream&& other) noexcept { if (this != &other) { reset(); handle_ = std::exchange(other.handle_, nullptr); } return *this; }
CStream::~CStream() { reset(); }
size_t CStream::init(int32_t level) noexcept { auto& a = zstd(); return handle_ && a.cstream_init ? a.cstream_init(handle_, level) : missing(); }
size_t CStream::set_parameter(int32_t parameter, int32_t value) noexcept { auto& a = zstd(); return handle_ && a.cstream_set_parameter ? a.cstream_set_parameter(handle_, parameter, value) : missing(); }
size_t CStream::load(const CDict& dict) noexcept { auto& a = zstd(); return handle_ && dict.raw() && a.cstream_load_cdict ? a.cstream_load_cdict(handle_, dict.raw()) : missing(); }
size_t CStream::compress(BufferCursor& dst, ConstBufferCursor& src) noexcept { auto& a = zstd(); return handle_ && a.cstream_compress ? a.cstream_compress(handle_, dst.data, dst.size, &dst.pos, src.data, src.size, &src.pos) : missing(); }
size_t CStream::flush(BufferCursor& dst) noexcept { auto& a = zstd(); return handle_ && a.cstream_flush ? a.cstream_flush(handle_, dst.data, dst.size, &dst.pos) : missing(); }
size_t CStream::end(BufferCursor& dst) noexcept { auto& a = zstd(); return handle_ && a.cstream_end ? a.cstream_end(handle_, dst.data, dst.size, &dst.pos) : missing(); }
void CStream::reset() noexcept { auto& a = zstd(); if (handle_ && a.free_cstream) a.free_cstream(std::exchange(handle_, nullptr)); else handle_ = nullptr; }

DStream::DStream() noexcept { auto& a = zstd(); if (a.create_dstream) handle_ = a.create_dstream(); }
DStream::DStream(DStream&& other) noexcept : handle_(std::exchange(other.handle_, nullptr)) {}
DStream& DStream::operator=(DStream&& other) noexcept { if (this != &other) { reset(); handle_ = std::exchange(other.handle_, nullptr); } return *this; }
DStream::~DStream() { reset(); }
size_t DStream::init() noexcept { auto& a = zstd(); return handle_ && a.dstream_init ? a.dstream_init(handle_) : missing(); }
size_t DStream::set_parameter(int32_t parameter, int32_t value) noexcept { auto& a = zstd(); return handle_ && a.dstream_set_parameter ? a.dstream_set_parameter(handle_, parameter, value) : missing(); }
size_t DStream::load(const DDict& dict) noexcept { auto& a = zstd(); return handle_ && dict.raw() && a.dstream_load_ddict ? a.dstream_load_ddict(handle_, dict.raw()) : missing(); }
size_t DStream::decompress(BufferCursor& dst, ConstBufferCursor& src) noexcept { auto& a = zstd(); return handle_ && a.dstream_decompress ? a.dstream_decompress(handle_, dst.data, dst.size, &dst.pos, src.data, src.size, &src.pos) : missing(); }
void DStream::reset() noexcept { auto& a = zstd(); if (handle_ && a.free_dstream) a.free_dstream(std::exchange(handle_, nullptr)); else handle_ = nullptr; }

}
