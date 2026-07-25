module;
#include <cstddef>
#include <cstdint>
#include <span>
#include <string_view>

export module utility.texture.file.jpeg.jpeg_core;

export namespace texture::jpeg {
using bytes = std::span<const uint8_t>;
using mutable_bytes = std::span<uint8_t>;
inline constexpr int32_t pixel_rgb = 0;
inline constexpr int32_t pixel_rgba = 7;
inline constexpr int32_t pixel_bgr = 1;
inline constexpr int32_t pixel_bgra = 8;
struct Info { uint32_t width{}; uint32_t height{}; int32_t subsampling{}; int32_t colorspace{}; };
struct EncodeOptions { int32_t subsampling{}; int32_t quality{90}; int32_t flags{}; };
[[nodiscard]] bool loaded() noexcept;
[[nodiscard]] uint32_t abi_version() noexcept;
[[nodiscard]] std::string_view library_path() noexcept;
[[nodiscard]] std::string_view load_error() noexcept;
[[nodiscard]] bool info(bytes input, Info& out) noexcept;
[[nodiscard]] bool decode(bytes input, mutable_bytes output, uint32_t width, uint32_t height, uint32_t stride, int32_t pixel_format = pixel_rgba, int32_t flags = 0) noexcept;
[[nodiscard]] bool encode(bytes input, uint32_t width, uint32_t height, uint32_t stride, int32_t pixel_format, EncodeOptions options, mutable_bytes output, size_t& written) noexcept;
}
