module;
#include <cstddef>
#include <cstdint>
#include <optional>
#include <span>
#include <utility>
#include <vector>

export module utility.compression.heatshrink.heatshrink_core;

export namespace heatshrink_ns {
using byte = uint8_t;
using view_type = std::span<const byte>;
using buffer_type = std::vector<byte>;
class Decoder {
public:
    Decoder(uint16_t input_buffer_size, uint8_t window_bits, uint8_t lookahead_bits) noexcept;
    Decoder(const Decoder&) = delete;
    Decoder& operator=(const Decoder&) = delete;
    Decoder(Decoder&& other) noexcept : handle_(std::exchange(other.handle_, nullptr)) {}
    Decoder& operator=(Decoder&& other) noexcept { if (this != &other) { reset(); handle_ = std::exchange(other.handle_, nullptr); } return *this; }
    ~Decoder();
    [[nodiscard]] explicit operator bool() const noexcept { return handle_; }
    [[nodiscard]] int32_t sink(view_type input, size_t& consumed) noexcept;
    [[nodiscard]] int32_t poll(std::span<byte> output, size_t& written) noexcept;
    [[nodiscard]] int32_t finish() noexcept;
    void reset() noexcept;
private:
    void* handle_{};
};
[[nodiscard]] bool loaded() noexcept;
[[nodiscard]] std::optional<buffer_type> compress(view_type input, uint8_t window_bits, uint8_t lookahead_bits);
}
