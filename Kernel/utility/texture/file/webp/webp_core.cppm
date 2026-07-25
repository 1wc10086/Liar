module;
#include <cstddef>
#include <cstdint>
#include <span>
#include <string_view>

export module utility.texture.file.webp.webp_core;

export namespace texture::webp {

using bytes = std::span<const uint8_t>;
using mutable_bytes = std::span<uint8_t>;

struct Info { uint32_t width{}; uint32_t height{}; uint32_t flags{}; };
struct EncodeOptions { float quality{75}; bool lossless{}; int32_t method{4}; };

class Animation {
public:
    Animation() = default;
    explicit Animation(bytes input) noexcept;
    Animation(const Animation&) = delete;
    Animation& operator=(const Animation&) = delete;
    Animation(Animation&&) noexcept;
    Animation& operator=(Animation&&) noexcept;
    ~Animation();
    [[nodiscard]] explicit operator bool() const noexcept { return handle_ != nullptr; }
    [[nodiscard]] bool info(Info& out, uint32_t& frames, uint32_t& loop_count, uint32_t& background) const noexcept;
    [[nodiscard]] bool next(mutable_bytes rgba, uint32_t& timestamp) noexcept;
    void reset() noexcept;
private:
    void* handle_{};
};

[[nodiscard]] bool loaded() noexcept;
[[nodiscard]] uint32_t abi_version() noexcept;
[[nodiscard]] std::string_view library_path() noexcept;
[[nodiscard]] std::string_view load_error() noexcept;
[[nodiscard]] bool info(bytes input, Info& out) noexcept;
[[nodiscard]] bool decode_rgba(bytes input, mutable_bytes output, uint32_t width, uint32_t height, uint32_t stride = 0) noexcept;
[[nodiscard]] bool encode_rgba(bytes input, uint32_t width, uint32_t height, uint32_t stride, EncodeOptions options, mutable_bytes output, size_t& written) noexcept;

}
