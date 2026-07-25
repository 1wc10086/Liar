module;
#include <cstddef>
#include <cstdint>
#include <optional>
#include <span>
#include <string_view>
#include <vector>

export module utility.compression.ncomp_core;

export namespace ncomp_ns {

using byte = uint8_t;
using buffer_type = std::vector<byte>;
using view_type = std::span<const byte>;

enum class Algorithm : uint8_t { fastlz, lzf, bsc, zpaq, lzw, lzfse, heatshrink };

[[nodiscard]] bool loaded() noexcept;
[[nodiscard]] std::string_view library_path() noexcept;
[[nodiscard]] std::string_view load_error() noexcept;
[[nodiscard]] int64_t decompress_to(Algorithm algorithm, std::span<byte> output, view_type input) noexcept;
[[nodiscard]] int64_t compress_to(Algorithm algorithm, std::span<byte> output, view_type input, int32_t level = 3) noexcept;

[[nodiscard]] std::optional<buffer_type> decompress(Algorithm algorithm, view_type input, size_t expected_size = 0);
[[nodiscard]] std::optional<buffer_type> compress(Algorithm algorithm, view_type input, int32_t level = 3);

}
