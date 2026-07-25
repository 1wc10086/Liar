module;
#include <cstddef>
#include <cstdint>
#include <span>
#include <string_view>
#include <vector>

export module utility.encoding.iconv.iconv_core;

export namespace iconv_ns {

using byte = uint8_t;
using buffer_type = std::vector<byte>;
using view_type = std::span<const byte>;

struct ConversionResult {
    int error{};
    size_t input_consumed{};
    size_t output_written{};

    [[nodiscard]] explicit operator bool() const noexcept { return error == 0; }
};

[[nodiscard]] bool loaded() noexcept;
[[nodiscard]] std::string_view library_path() noexcept;
[[nodiscard]] std::string_view load_error() noexcept;
[[nodiscard]] ConversionResult convert_to(const char* to_encoding, const char* from_encoding, view_type input, std::span<byte> output) noexcept;
[[nodiscard]] ConversionResult convert_to(std::string_view to_encoding, std::string_view from_encoding, view_type input, std::span<byte> output) noexcept;

}
