module;
#include <cstdint>
#include <span>
#include <string_view>

export module utility.texture.file.stb.stb_core;

export namespace texture::stb {
using bytes = std::span<const uint8_t>;
using mutable_bytes = std::span<uint8_t>;
using floats = std::span<const float>;
using mutable_floats = std::span<float>;
enum class Layout : int32_t { one = 1, two = 2, rgb = 3, bgr = 4, rgba = 5, bgra = 6, argb = 7, abgr = 8, rgba_premultiplied = 11, bgra_premultiplied = 12 };
[[nodiscard]] bool loaded() noexcept;
[[nodiscard]] uint32_t abi_version() noexcept;
[[nodiscard]] std::string_view library_path() noexcept;
[[nodiscard]] std::string_view load_error() noexcept;
[[nodiscard]] bool resize_u8(bytes input, uint32_t input_width, uint32_t input_height, uint32_t input_stride, mutable_bytes output, uint32_t output_width, uint32_t output_height, uint32_t output_stride, Layout layout = Layout::rgba, bool srgb = false) noexcept;
[[nodiscard]] bool resize_f32(floats input, uint32_t input_width, uint32_t input_height, uint32_t input_stride, mutable_floats output, uint32_t output_width, uint32_t output_height, uint32_t output_stride, Layout layout = Layout::rgba) noexcept;
}
