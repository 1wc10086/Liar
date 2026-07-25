module;
#include <cstddef>
#include <cstdint>
#include <span>
#include <string_view>
#include <utility>

export module utility.differentiation.vcdiff.vcdiff_core;

export namespace vcdiff {

using byte = uint8_t;
using view_type = std::span<const byte>;
using mutable_view_type = std::span<byte>;

enum class Format : uint32_t { standard = 0, interleaved = 1, checksum = 2, json = 4 };

class Dictionary {
public:
    Dictionary() = default;
    Dictionary(view_type input) noexcept;
    Dictionary(const Dictionary&) = delete;
    Dictionary& operator=(const Dictionary&) = delete;
    Dictionary(Dictionary&& other) noexcept;
    Dictionary& operator=(Dictionary&& other) noexcept;
    ~Dictionary();

    [[nodiscard]] explicit operator bool() const noexcept { return handle_ != nullptr; }
    [[nodiscard]] void* raw() const noexcept { return handle_; }
    void reset() noexcept;

private:
    void* handle_{};
};

class Encoder {
public:
    Encoder() = default;
    Encoder(view_type dictionary, Format format = Format::standard, bool target_matching = true) noexcept;
    Encoder(const Encoder&) = delete;
    Encoder& operator=(const Encoder&) = delete;
    Encoder(Encoder&& other) noexcept;
    Encoder& operator=(Encoder&& other) noexcept;
    ~Encoder();

    [[nodiscard]] explicit operator bool() const noexcept { return handle_ != nullptr; }
    [[nodiscard]] bool start(mutable_view_type output, size_t& written) noexcept;
    [[nodiscard]] bool encode(view_type input, mutable_view_type output, size_t& written) noexcept;
    [[nodiscard]] bool finish(mutable_view_type output, size_t& written) noexcept;
    void reset() noexcept;

private:
    void* handle_{};
};

class Decoder {
public:
    Decoder() noexcept;
    Decoder(const Decoder&) = delete;
    Decoder& operator=(const Decoder&) = delete;
    Decoder(Decoder&& other) noexcept;
    Decoder& operator=(Decoder&& other) noexcept;
    ~Decoder();

    [[nodiscard]] explicit operator bool() const noexcept { return handle_ != nullptr; }
    [[nodiscard]] bool max_target_file_size(size_t size) noexcept;
    [[nodiscard]] bool max_target_window_size(size_t size) noexcept;
    void allow_target(bool allow) noexcept;
    [[nodiscard]] bool start(view_type dictionary) noexcept;
    [[nodiscard]] bool decode(view_type input, mutable_view_type output, size_t& written) noexcept;
    [[nodiscard]] bool finish() noexcept;
    void reset() noexcept;

private:
    void* handle_{};
};

[[nodiscard]] uint32_t abi_version() noexcept;
[[nodiscard]] bool loaded() noexcept;
[[nodiscard]] std::string_view library_path() noexcept;
[[nodiscard]] std::string_view load_error() noexcept;
[[nodiscard]] bool encode_to(view_type dictionary, view_type input, mutable_view_type output, size_t& written, Format format = Format::standard, bool target_matching = true) noexcept;
[[nodiscard]] bool decode_to(view_type dictionary, view_type input, mutable_view_type output, size_t& written, size_t max_target_file_size = 0, size_t max_target_window_size = 0, bool allow_target = true) noexcept;

}
