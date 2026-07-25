module;
#include <array>
#include <cstddef>
#include <cstdint>
#include <cstdio>
#include <dlfcn.h>
#include <mutex>
#include <string_view>
#include <utility>

module utility.compression.xz.xz_core;

namespace xz_ns {
namespace {

struct NativeCursor {
    const void* src;
    size_t src_size;
    size_t src_pos;
    void* dst;
    size_t dst_size;
    size_t dst_pos;
};

struct Api {
    using abi_version_t = uint32_t (*)();
    using version_number_t = uint32_t (*)();
    using version_string_t = const char* (*)();
    using error_name_t = const char* (*)(uint32_t);
    using memusage_decoder_t = uint64_t (*)(uint64_t, uint32_t, const void*, size_t);
    using memusage_preset_t = uint64_t (*)(uint32_t);
    using bound_t = size_t (*)(size_t);
    using encode_t = uint32_t (*)(void*, size_t, size_t*, const void*, size_t, uint32_t, uint32_t);
    using decode_t = uint32_t (*)(void*, size_t, size_t*, const void*, size_t, size_t*, uint64_t, uint32_t);
    using encoder_create_t = void* (*)(uint32_t, uint32_t);
    using decoder_create_t = void* (*)(uint64_t, uint32_t);
    using destroy_t = void (*)(void*);
    using code_t = uint32_t (*)(void*, NativeCursor*, uint32_t);
    using total_t = uint64_t (*)(void*);

    void* lib{};
    const char* path{};
    const char* error{};
    abi_version_t abi_version{};
    version_number_t version_number{};
    version_string_t version_string{};
    error_name_t error_name{};
    memusage_decoder_t memusage_decoder{};
    memusage_preset_t easy_encoder_memusage{};
    memusage_preset_t easy_decoder_memusage{};
    bound_t compress_bound{};
    encode_t encode{};
    decode_t decode{};
    encoder_create_t encoder_create{};
    decoder_create_t decoder_create{};
    destroy_t destroy{};
    code_t code{};
    total_t total_in{};
    total_t total_out{};

    [[nodiscard]] bool ready() const noexcept {
        return lib && abi_version && version_number && version_string && error_name && memusage_decoder && easy_encoder_memusage && easy_decoder_memusage && compress_bound && encode && decode && encoder_create && decoder_create && destroy && code && total_in && total_out;
    }
};

Api api;
std::once_flag api_once;
std::array<char, 4096> fallback_path{};

template <class T>
void bind(T& out, const char* name) noexcept { out = reinterpret_cast<T>(dlsym(api.lib, name)); }

void init_api() noexcept {
    api.lib = dlopen("libxz.so", RTLD_NOW | RTLD_LOCAL);
    if (api.lib) api.path = "libxz.so";

    if (!api.lib) {
        Dl_info info{};
        if (dladdr(reinterpret_cast<const void*>(&init_api), &info) && info.dli_fname) {
            std::string_view path{info.dli_fname};
            if (const auto slash = path.rfind('/'); slash != std::string_view::npos && slash + 1 < fallback_path.size()) {
                const auto dir = path.substr(0, slash);
                if (dir.size() + 10 < fallback_path.size()) {
                    std::snprintf(fallback_path.data(), fallback_path.size(), "%.*s/libxz.so", static_cast<int>(dir.size()), dir.data());
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

    bind(api.abi_version, "kxz_abi_version");
    bind(api.version_number, "kxz_version_number");
    bind(api.version_string, "kxz_version_string");
    bind(api.error_name, "kxz_error_name");
    bind(api.memusage_decoder, "kxz_memusage_decoder");
    bind(api.easy_encoder_memusage, "kxz_easy_encoder_memusage");
    bind(api.easy_decoder_memusage, "kxz_easy_decoder_memusage");
    bind(api.compress_bound, "kxz_stream_buffer_bound");
    bind(api.encode, "kxz_stream_buffer_encode");
    bind(api.decode, "kxz_stream_buffer_decode");
    bind(api.encoder_create, "kxz_encoder_create");
    bind(api.decoder_create, "kxz_decoder_create");
    bind(api.destroy, "kxz_stream_destroy");
    bind(api.code, "kxz_stream_code");
    bind(api.total_in, "kxz_stream_total_in");
    bind(api.total_out, "kxz_stream_total_out");

    if (!api.ready()) api.error = "missing required kxz symbol";
}

Api& xz() noexcept {
    std::call_once(api_once, init_api);
    return api;
}

Code unavailable() noexcept { return Code::prog_error; }

}

uint32_t abi_version() noexcept { auto& a = xz(); return a.abi_version ? a.abi_version() : 0; }
uint32_t version_number() noexcept { auto& a = xz(); return a.version_number ? a.version_number() : 0; }
std::string_view version_string() noexcept { auto& a = xz(); return a.version_string ? std::string_view{a.version_string()} : std::string_view{}; }
bool loaded() noexcept { return xz().ready(); }
std::string_view library_path() noexcept { auto& a = xz(); return a.path ? std::string_view{a.path} : std::string_view{}; }
std::string_view load_error() noexcept { auto& a = xz(); return a.error ? std::string_view{a.error} : std::string_view{}; }
const char* error_name(Code code) noexcept { auto& a = xz(); return a.error_name ? a.error_name(static_cast<uint32_t>(code)) : "xz bridge unavailable"; }
uint64_t memusage_decoder(view_type input, uint64_t memlimit, uint32_t flags) noexcept { auto& a = xz(); return a.memusage_decoder ? a.memusage_decoder(memlimit, flags, input.data(), input.size()) : 0; }
uint64_t easy_encoder_memusage(uint32_t preset) noexcept { auto& a = xz(); return a.easy_encoder_memusage ? a.easy_encoder_memusage(preset) : 0; }
uint64_t easy_decoder_memusage(uint32_t preset) noexcept { auto& a = xz(); return a.easy_decoder_memusage ? a.easy_decoder_memusage(preset) : 0; }
size_t compress_bound(size_t src_size) noexcept { auto& a = xz(); return a.compress_bound ? a.compress_bound(src_size) : 0; }
Code compress_to(void* dst, size_t dst_capacity, size_t& dst_pos, const void* src, size_t src_size, const Options& o) noexcept { auto& a = xz(); return a.encode ? static_cast<Code>(a.encode(dst, dst_capacity, &dst_pos, src, src_size, o.preset, o.check)) : unavailable(); }
Code decompress_to(void* dst, size_t dst_capacity, size_t& dst_pos, const void* src, size_t src_size, size_t& src_pos, uint64_t memlimit, uint32_t flags) noexcept { auto& a = xz(); return a.decode ? static_cast<Code>(a.decode(dst, dst_capacity, &dst_pos, src, src_size, &src_pos, memlimit, flags)) : unavailable(); }

CStream::CStream(const Options& options) noexcept { auto& a = xz(); if (a.encoder_create) handle_ = a.encoder_create(options.preset, options.check); }
CStream::CStream(CStream&& other) noexcept : handle_(std::exchange(other.handle_, nullptr)) {}
CStream& CStream::operator=(CStream&& other) noexcept { if (this != &other) { reset(); handle_ = std::exchange(other.handle_, nullptr); } return *this; }
CStream::~CStream() { reset(); }
Code CStream::code(BufferCursor& dst, ConstBufferCursor& src, Action action) noexcept { auto& a = xz(); NativeCursor cursor{src.data, src.size, src.pos, dst.data, dst.size, dst.pos}; const auto ret = handle_ && a.code ? static_cast<Code>(a.code(handle_, &cursor, static_cast<uint32_t>(action))) : unavailable(); src.pos = cursor.src_pos; dst.pos = cursor.dst_pos; return ret; }
uint64_t CStream::total_in() const noexcept { auto& a = xz(); return handle_ && a.total_in ? a.total_in(handle_) : 0; }
uint64_t CStream::total_out() const noexcept { auto& a = xz(); return handle_ && a.total_out ? a.total_out(handle_) : 0; }
void CStream::reset() noexcept { auto& a = xz(); if (handle_ && a.destroy) a.destroy(std::exchange(handle_, nullptr)); else handle_ = nullptr; }

DStream::DStream(uint64_t memlimit, uint32_t flags) noexcept { auto& a = xz(); if (a.decoder_create) handle_ = a.decoder_create(memlimit, flags); }
DStream::DStream(DStream&& other) noexcept : handle_(std::exchange(other.handle_, nullptr)) {}
DStream& DStream::operator=(DStream&& other) noexcept { if (this != &other) { reset(); handle_ = std::exchange(other.handle_, nullptr); } return *this; }
DStream::~DStream() { reset(); }
Code DStream::code(BufferCursor& dst, ConstBufferCursor& src, Action action) noexcept { auto& a = xz(); NativeCursor cursor{src.data, src.size, src.pos, dst.data, dst.size, dst.pos}; const auto ret = handle_ && a.code ? static_cast<Code>(a.code(handle_, &cursor, static_cast<uint32_t>(action))) : unavailable(); src.pos = cursor.src_pos; dst.pos = cursor.dst_pos; return ret; }
uint64_t DStream::total_in() const noexcept { auto& a = xz(); return handle_ && a.total_in ? a.total_in(handle_) : 0; }
uint64_t DStream::total_out() const noexcept { auto& a = xz(); return handle_ && a.total_out ? a.total_out(handle_) : 0; }
void DStream::reset() noexcept { auto& a = xz(); if (handle_ && a.destroy) a.destroy(std::exchange(handle_, nullptr)); else handle_ = nullptr; }

}
