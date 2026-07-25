module;
#include <array>
#include <cstddef>
#include <cstdint>
#include <cstdio>
#include <dlfcn.h>
#include <mutex>
#include <string_view>
#include <utility>

module utility.texture.file.webp.webp_core;

namespace texture::webp {
namespace {
struct Api {
    using abi_t = uint32_t (*)(); using info_t = int (*)(const uint8_t*, size_t, uint32_t*, uint32_t*, uint32_t*); using decode_t = int (*)(const uint8_t*, size_t, uint8_t*, size_t, uint32_t, uint32_t, uint32_t); using encode_t = int (*)(const uint8_t*, uint32_t, uint32_t, uint32_t, float, int, int, uint8_t*, size_t, size_t*); using animation_create_t = void* (*)(const uint8_t*, size_t); using animation_destroy_t = void (*)(void*); using animation_info_t = int (*)(void*, uint32_t*, uint32_t*, uint32_t*, uint32_t*, uint32_t*); using animation_next_t = int (*)(void*, uint8_t*, size_t, uint32_t*); using animation_reset_t = void (*)(void*);
    void* library{}; const char* path{}; const char* error{}; abi_t abi{}; info_t info{}; decode_t decode{}; encode_t encode{}; animation_create_t animation_create{}; animation_destroy_t animation_destroy{}; animation_info_t animation_info{}; animation_next_t animation_next{}; animation_reset_t animation_reset{};
    [[nodiscard]] bool ready() const noexcept { return library && abi && info && decode && encode && animation_create && animation_destroy && animation_info && animation_next && animation_reset; }
} api;
std::once_flag once; std::array<char, 4096> fallback{};
template<class T> void bind(T& out, const char* name) noexcept { out = reinterpret_cast<T>(dlsym(api.library, name)); }
void initialize() noexcept {
    api.library = dlopen("libtexture.so", RTLD_NOW | RTLD_LOCAL); if (api.library) api.path = "libtexture.so";
    if (!api.library) { Dl_info info{}; if (dladdr(reinterpret_cast<const void*>(&initialize), &info) && info.dli_fname) { const std::string_view path{info.dli_fname}; if (const auto slash = path.rfind('/'); slash != std::string_view::npos && slash + sizeof("/libtexture.so") <= fallback.size()) { const auto dir = path.substr(0, slash); std::snprintf(fallback.data(), fallback.size(), "%.*s/libtexture.so", static_cast<int>(dir.size()), dir.data()); api.library = dlopen(fallback.data(), RTLD_NOW | RTLD_LOCAL); if (api.library) api.path = fallback.data(); } } }
    if (!api.library) { api.error = dlerror(); return; }
    bind(api.abi, "ktx_abi_version"); bind(api.info, "ktx_webp_info"); bind(api.decode, "ktx_webp_decode_rgba"); bind(api.encode, "ktx_webp_encode_rgba"); bind(api.animation_create, "ktx_webp_animation_create"); bind(api.animation_destroy, "ktx_webp_animation_destroy"); bind(api.animation_info, "ktx_webp_animation_info"); bind(api.animation_next, "ktx_webp_animation_next_rgba"); bind(api.animation_reset, "ktx_webp_animation_reset"); if (!api.ready()) api.error = "missing required texture WebP symbol";
}
Api& instance() noexcept { std::call_once(once, initialize); return api; }
}
bool loaded() noexcept { return instance().ready(); } uint32_t abi_version() noexcept { auto& a = instance(); return a.abi ? a.abi() : 0; } std::string_view library_path() noexcept { auto& a = instance(); return a.path ? a.path : ""; } std::string_view load_error() noexcept { auto& a = instance(); return a.error ? a.error : ""; }
bool info(bytes input, Info& out) noexcept { auto& a = instance(); return a.info && a.info(input.data(), input.size(), &out.width, &out.height, &out.flags); }
bool decode_rgba(bytes input, mutable_bytes output, uint32_t width, uint32_t height, uint32_t stride) noexcept { auto& a = instance(); return a.decode && a.decode(input.data(), input.size(), output.data(), output.size(), width, height, stride ? stride : width * 4); }
bool encode_rgba(bytes input, uint32_t width, uint32_t height, uint32_t stride, EncodeOptions o, mutable_bytes output, size_t& written) noexcept { auto& a = instance(); return a.encode && a.encode(input.data(), width, height, stride ? stride : width * 4, o.quality, o.lossless, o.method, output.data(), output.size(), &written); }
Animation::Animation(bytes input) noexcept { auto& a = instance(); if (a.animation_create) handle_ = a.animation_create(input.data(), input.size()); } Animation::Animation(Animation&& other) noexcept : handle_(std::exchange(other.handle_, nullptr)) {} Animation& Animation::operator=(Animation&& other) noexcept { if (this != &other) { auto& a = instance(); if (handle_ && a.animation_destroy) a.animation_destroy(handle_); handle_ = std::exchange(other.handle_, nullptr); } return *this; } Animation::~Animation() { auto& a = instance(); if (handle_ && a.animation_destroy) a.animation_destroy(std::exchange(handle_, nullptr)); } bool Animation::info(Info& out, uint32_t& frames, uint32_t& loop_count, uint32_t& background) const noexcept { auto& a = instance(); return handle_ && a.animation_info && a.animation_info(handle_, &out.width, &out.height, &frames, &loop_count, &background); } bool Animation::next(mutable_bytes rgba, uint32_t& timestamp) noexcept { auto& a = instance(); return handle_ && a.animation_next && a.animation_next(handle_, rgba.data(), rgba.size(), &timestamp); } void Animation::reset() noexcept { auto& a = instance(); if (handle_ && a.animation_reset) a.animation_reset(handle_); }
}
