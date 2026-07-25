module;
#include <array>
#include <cstddef>
#include <cstdint>
#include <cstdio>
#include <dlfcn.h>
#include <mutex>
#include <string_view>

module utility.texture.file.jpeg.jpeg_core;

namespace texture::jpeg {
namespace {
struct Api { using abi_t = uint32_t (*)(); using info_t = int (*)(const uint8_t*, size_t, uint32_t*, uint32_t*, int*, int*); using decode_t = int (*)(const uint8_t*, size_t, uint8_t*, size_t, uint32_t, uint32_t, uint32_t, int, int); using encode_t = int (*)(const uint8_t*, uint32_t, uint32_t, uint32_t, int, uint8_t*, size_t, size_t*, int, int, int); void* library{}; const char* path{}; const char* error{}; abi_t abi{}; info_t info{}; decode_t decode{}; encode_t encode{}; [[nodiscard]] bool ready() const noexcept { return library && abi && info && decode && encode; } } api;
std::once_flag once; std::array<char, 4096> fallback{}; template<class T> void bind(T& out, const char* name) noexcept { out = reinterpret_cast<T>(dlsym(api.library, name)); }
void initialize() noexcept { api.library = dlopen("libtexture.so", RTLD_NOW | RTLD_LOCAL); if (api.library) api.path = "libtexture.so"; if (!api.library) { Dl_info info{}; if (dladdr(reinterpret_cast<const void*>(&initialize), &info) && info.dli_fname) { const std::string_view path{info.dli_fname}; if (const auto slash = path.rfind('/'); slash != std::string_view::npos && slash + sizeof("/libtexture.so") <= fallback.size()) { const auto dir = path.substr(0, slash); std::snprintf(fallback.data(), fallback.size(), "%.*s/libtexture.so", static_cast<int>(dir.size()), dir.data()); api.library = dlopen(fallback.data(), RTLD_NOW | RTLD_LOCAL); if (api.library) api.path = fallback.data(); } } } if (!api.library) { api.error = dlerror(); return; } bind(api.abi, "ktx_abi_version"); bind(api.info, "ktx_jpeg_info"); bind(api.decode, "ktx_jpeg_decode"); bind(api.encode, "ktx_jpeg_encode"); if (!api.ready()) api.error = "missing required texture JPEG symbol"; }
Api& instance() noexcept { std::call_once(once, initialize); return api; }
}
bool loaded() noexcept { return instance().ready(); } uint32_t abi_version() noexcept { auto& a = instance(); return a.abi ? a.abi() : 0; } std::string_view library_path() noexcept { auto& a = instance(); return a.path ? a.path : ""; } std::string_view load_error() noexcept { auto& a = instance(); return a.error ? a.error : ""; } bool info(bytes input, Info& out) noexcept { auto& a = instance(); return a.info && a.info(input.data(), input.size(), &out.width, &out.height, &out.subsampling, &out.colorspace); } bool decode(bytes input, mutable_bytes output, uint32_t width, uint32_t height, uint32_t stride, int32_t format, int32_t flags) noexcept { auto& a = instance(); return a.decode && a.decode(input.data(), input.size(), output.data(), output.size(), width, height, stride, format, flags); } bool encode(bytes input, uint32_t width, uint32_t height, uint32_t stride, int32_t format, EncodeOptions o, mutable_bytes output, size_t& written) noexcept { auto& a = instance(); return a.encode && a.encode(input.data(), width, height, stride, format, output.data(), output.size(), &written, o.subsampling, o.quality, o.flags); }
}
