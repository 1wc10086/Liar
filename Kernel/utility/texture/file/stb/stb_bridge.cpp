module;
#include <array>
#include <cstddef>
#include <cstdint>
#include <cstdio>
#include <dlfcn.h>
#include <mutex>
#include <string_view>

module utility.texture.file.stb.stb_core;

namespace texture::stb {
namespace {
struct Api { using abi_t = uint32_t (*)(); using resize_u8_t = int (*)(const uint8_t*, uint32_t, uint32_t, uint32_t, uint8_t*, uint32_t, uint32_t, uint32_t, int, int); using resize_f32_t = int (*)(const float*, uint32_t, uint32_t, uint32_t, float*, uint32_t, uint32_t, uint32_t, int); void* library{}; const char* path{}; const char* error{}; abi_t abi{}; resize_u8_t resize_u8{}; resize_f32_t resize_f32{}; [[nodiscard]] bool ready() const noexcept { return library && abi && resize_u8 && resize_f32; } } api;
std::once_flag once; std::array<char, 4096> fallback{}; template<class T> void bind(T& out, const char* name) noexcept { out = reinterpret_cast<T>(dlsym(api.library, name)); }
void initialize() noexcept { api.library = dlopen("libtexture.so", RTLD_NOW | RTLD_LOCAL); if (api.library) api.path = "libtexture.so"; if (!api.library) { Dl_info info{}; if (dladdr(reinterpret_cast<const void*>(&initialize), &info) && info.dli_fname) { const std::string_view path{info.dli_fname}; if (const auto slash = path.rfind('/'); slash != std::string_view::npos && slash + sizeof("/libtexture.so") <= fallback.size()) { const auto dir = path.substr(0, slash); std::snprintf(fallback.data(), fallback.size(), "%.*s/libtexture.so", static_cast<int>(dir.size()), dir.data()); api.library = dlopen(fallback.data(), RTLD_NOW | RTLD_LOCAL); if (api.library) api.path = fallback.data(); } } } if (!api.library) { api.error = dlerror(); return; } bind(api.abi, "ktx_abi_version"); bind(api.resize_u8, "ktx_stb_resize_u8"); bind(api.resize_f32, "ktx_stb_resize_f32"); if (!api.ready()) api.error = "missing required texture stb symbol"; }
Api& instance() noexcept { std::call_once(once, initialize); return api; }
}
bool loaded() noexcept { return instance().ready(); } uint32_t abi_version() noexcept { auto& a = instance(); return a.abi ? a.abi() : 0; } std::string_view library_path() noexcept { auto& a = instance(); return a.path ? a.path : ""; } std::string_view load_error() noexcept { auto& a = instance(); return a.error ? a.error : ""; } bool resize_u8(bytes input, uint32_t iw, uint32_t ih, uint32_t is, mutable_bytes output, uint32_t ow, uint32_t oh, uint32_t os, Layout layout, bool srgb) noexcept { auto& a = instance(); return a.resize_u8 && a.resize_u8(input.data(), iw, ih, is, output.data(), ow, oh, os, static_cast<int>(layout), srgb); } bool resize_f32(floats input, uint32_t iw, uint32_t ih, uint32_t is, mutable_floats output, uint32_t ow, uint32_t oh, uint32_t os, Layout layout) noexcept { auto& a = instance(); return a.resize_f32 && a.resize_f32(input.data(), iw, ih, is, output.data(), ow, oh, os, static_cast<int>(layout)); }
}
