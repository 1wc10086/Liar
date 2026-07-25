module;
#include <array>
#include <cstddef>
#include <cstdint>
#include <cstdio>
#include <dlfcn.h>
#include <mutex>
#include <string_view>
#include <utility>

module utility.compression.brotli.brotli_core;

namespace brotli_ns {
namespace {

struct Api {
    using version_t = uint32_t (*)();
    using bound_t = size_t (*)(size_t);
    using create_t = void* (*)();
    using destroy_t = void (*)(void*);
    using set_parameter_t = int32_t (*)(void*, int32_t, uint32_t);
    using bool_state_t = int32_t (*)(void*);
    using error_string_t = const char* (*)(int32_t);
    using error_code_t = int32_t (*)(void*);
    using compress_t = size_t (*)(int32_t, int32_t, int32_t, size_t, const uint8_t*, size_t*, uint8_t*);
    using compress_stream_t = size_t (*)(void*, int32_t, size_t*, const uint8_t**, size_t*, uint8_t**, size_t*);
    using decompress_t = size_t (*)(size_t, const uint8_t*, size_t*, uint8_t*);
    using decompress_stream_t = size_t (*)(void*, size_t*, const uint8_t**, size_t*, uint8_t**, size_t*);

    void* lib{};
    const char* path{};
    const char* error{};
    version_t encoder_version{};
    version_t decoder_version{};
    bound_t compress_bound{};
    create_t encoder_create{};
    destroy_t encoder_destroy{};
    create_t decoder_create{};
    destroy_t decoder_destroy{};
    set_parameter_t encoder_set_parameter{};
    set_parameter_t decoder_set_parameter{};
    bool_state_t encoder_is_finished{};
    bool_state_t encoder_has_more_output{};
    bool_state_t decoder_is_used{};
    bool_state_t decoder_is_finished{};
    error_code_t decoder_get_error_code{};
    error_string_t decoder_error_string{};
    compress_t compress{};
    compress_stream_t compress_stream{};
    decompress_t decompress{};
    decompress_stream_t decompress_stream{};

    [[nodiscard]] bool ready() const noexcept {
        return lib && encoder_version && decoder_version && compress_bound && encoder_create && encoder_destroy && decoder_create && decoder_destroy && encoder_set_parameter && decoder_set_parameter && encoder_is_finished && encoder_has_more_output && decoder_is_finished && decoder_get_error_code && decoder_error_string && compress && compress_stream && decompress && decompress_stream;
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
    api.lib = dlopen("libbrotli.so", RTLD_NOW | RTLD_LOCAL);
    if (api.lib) api.path = "libbrotli.so";

    if (!api.lib) {
        Dl_info info{};
        if (dladdr(reinterpret_cast<const void*>(&init_api), &info) && info.dli_fname) {
            std::string_view path{info.dli_fname};
            if (const auto slash = path.rfind('/'); slash != std::string_view::npos && slash + 1 < fallback_path.size()) {
                const auto dir = path.substr(0, slash);
                if (dir.size() + 14 < fallback_path.size()) {
                    std::snprintf(fallback_path.data(), fallback_path.size(), "%.*s/libbrotli.so", static_cast<int>(dir.size()), dir.data());
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

    bind(api.encoder_version, "kbrotli_encoder_version");
    bind(api.decoder_version, "kbrotli_decoder_version");
    bind(api.compress_bound, "kbrotli_encoder_max_compressed_size");
    bind(api.encoder_create, "kbrotli_encoder_create_instance");
    bind(api.encoder_destroy, "kbrotli_encoder_destroy_instance");
    bind(api.decoder_create, "kbrotli_decoder_create_instance");
    bind(api.decoder_destroy, "kbrotli_decoder_destroy_instance");
    bind(api.encoder_set_parameter, "kbrotli_encoder_set_parameter");
    bind(api.decoder_set_parameter, "kbrotli_decoder_set_parameter");
    bind(api.encoder_is_finished, "kbrotli_encoder_is_finished");
    bind(api.encoder_has_more_output, "kbrotli_encoder_has_more_output");
    bind(api.decoder_is_used, "kbrotli_decoder_is_used");
    bind(api.decoder_is_finished, "kbrotli_decoder_is_finished");
    bind(api.decoder_get_error_code, "kbrotli_decoder_get_error_code");
    bind(api.decoder_error_string, "kbrotli_decoder_error_string");
    bind(api.compress, "kbrotli_compress");
    bind(api.compress_stream, "kbrotli_compress_stream");
    bind(api.decompress, "kbrotli_decompress");
    bind(api.decompress_stream, "kbrotli_decompress_stream");

    if (!api.ready()) api.error = "missing required kbrotli symbol";
}

Api& brotli() noexcept {
    std::call_once(api_once, init_api);
    return api;
}

}

uint32_t encoder_version() noexcept { auto& a = brotli(); return a.encoder_version ? a.encoder_version() : 0; }
uint32_t decoder_version() noexcept { auto& a = brotli(); return a.decoder_version ? a.decoder_version() : 0; }
bool loaded() noexcept { return brotli().ready(); }
std::string_view library_path() noexcept { auto& a = brotli(); return a.path ? std::string_view{a.path} : std::string_view{}; }
std::string_view load_error() noexcept { auto& a = brotli(); return a.error ? std::string_view{a.error} : std::string_view{}; }
const char* decoder_error_string(int32_t code) noexcept { auto& a = brotli(); return a.decoder_error_string ? a.decoder_error_string(code) : "brotli bridge unavailable"; }
size_t compress_bound(size_t input_size) noexcept { auto& a = brotli(); return a.compress_bound ? a.compress_bound(input_size) : 0; }

bool compress_to(void* dst, size_t& dst_size, const void* src, size_t src_size, const Options& options) noexcept {
    auto& a = brotli();
    return a.compress && a.compress(options.quality, options.lgwin, options.mode, src_size, static_cast<const uint8_t*>(src), &dst_size, static_cast<uint8_t*>(dst)) != 0;
}

DecoderResult decompress_to(void* dst, size_t& dst_size, const void* src, size_t src_size) noexcept {
    auto& a = brotli();
    return a.decompress ? static_cast<DecoderResult>(a.decompress(src_size, static_cast<const uint8_t*>(src), &dst_size, static_cast<uint8_t*>(dst))) : DecoderResult::error;
}

CStream::CStream() noexcept { auto& a = brotli(); if (a.encoder_create) handle_ = a.encoder_create(); }
CStream::CStream(CStream&& other) noexcept : handle_(std::exchange(other.handle_, nullptr)) {}
CStream& CStream::operator=(CStream&& other) noexcept { if (this != &other) { reset(); handle_ = std::exchange(other.handle_, nullptr); } return *this; }
CStream::~CStream() { reset(); }
bool CStream::set_parameter(int32_t parameter, uint32_t value) noexcept { auto& a = brotli(); return handle_ && a.encoder_set_parameter && a.encoder_set_parameter(handle_, parameter, value) != 0; }
bool CStream::set_options(const Options& o) noexcept { return set_parameter(1, static_cast<uint32_t>(o.quality)) && set_parameter(2, static_cast<uint32_t>(o.lgwin)) && set_parameter(0, static_cast<uint32_t>(o.mode)); }
bool CStream::compress(BufferCursor& dst, ConstBufferCursor& src, EncoderOperation op, size_t* total_out) noexcept {
    auto& a = brotli();
    auto available_in = src.size - src.pos;
    auto* next_in = static_cast<const uint8_t*>(src.data) + src.pos;
    auto available_out = dst.size - dst.pos;
    auto* next_out = static_cast<uint8_t*>(dst.data) + dst.pos;
    const auto ok = handle_ && a.compress_stream && a.compress_stream(handle_, static_cast<int32_t>(op), &available_in, &next_in, &available_out, &next_out, total_out) != 0;
    src.pos = src.size - available_in;
    dst.pos = dst.size - available_out;
    return ok;
}
bool CStream::finished() const noexcept { auto& a = brotli(); return handle_ && a.encoder_is_finished && a.encoder_is_finished(handle_) != 0; }
bool CStream::has_more_output() const noexcept { auto& a = brotli(); return handle_ && a.encoder_has_more_output && a.encoder_has_more_output(handle_) != 0; }
void CStream::reset() noexcept { auto& a = brotli(); if (handle_ && a.encoder_destroy) a.encoder_destroy(std::exchange(handle_, nullptr)); else handle_ = nullptr; }

DStream::DStream() noexcept { auto& a = brotli(); if (a.decoder_create) handle_ = a.decoder_create(); }
DStream::DStream(DStream&& other) noexcept : handle_(std::exchange(other.handle_, nullptr)) {}
DStream& DStream::operator=(DStream&& other) noexcept { if (this != &other) { reset(); handle_ = std::exchange(other.handle_, nullptr); } return *this; }
DStream::~DStream() { reset(); }
bool DStream::set_parameter(int32_t parameter, uint32_t value) noexcept { auto& a = brotli(); return handle_ && a.decoder_set_parameter && a.decoder_set_parameter(handle_, parameter, value) != 0; }
DecoderResult DStream::decompress(BufferCursor& dst, ConstBufferCursor& src, size_t* total_out) noexcept {
    auto& a = brotli();
    auto available_in = src.size - src.pos;
    auto* next_in = static_cast<const uint8_t*>(src.data) + src.pos;
    auto available_out = dst.size - dst.pos;
    auto* next_out = static_cast<uint8_t*>(dst.data) + dst.pos;
    const auto ret = handle_ && a.decompress_stream ? static_cast<DecoderResult>(a.decompress_stream(handle_, &available_in, &next_in, &available_out, &next_out, total_out)) : DecoderResult::error;
    src.pos = src.size - available_in;
    dst.pos = dst.size - available_out;
    return ret;
}
bool DStream::used() const noexcept { auto& a = brotli(); return handle_ && a.decoder_is_used && a.decoder_is_used(handle_) != 0; }
bool DStream::finished() const noexcept { auto& a = brotli(); return handle_ && a.decoder_is_finished && a.decoder_is_finished(handle_) != 0; }
int32_t DStream::error_code_detail() const noexcept { auto& a = brotli(); return handle_ && a.decoder_get_error_code ? a.decoder_get_error_code(handle_) : -20; }
void DStream::reset() noexcept { auto& a = brotli(); if (handle_ && a.decoder_destroy) a.decoder_destroy(std::exchange(handle_, nullptr)); else handle_ = nullptr; }

}
