module;
#include <cstddef>
#include <cstdint>
#include <limits>
#include <memory>
#include <span>
#include <string_view>
#include <utility>
#include <vector>

export module utility.audio.audio_core;

export namespace audio_ns {

using bytes = std::span<const uint8_t>;
using pcm = std::vector<float>;

enum class Codec : uint32_t { wav = 1, mp3 = 2, vorbis = 3, xm = 4 };
inline constexpr uint64_t unknown_frames = std::numeric_limits<uint64_t>::max();

struct Info { uint32_t sample_rate{}; uint32_t channels{}; uint64_t frames{unknown_frames}; };
struct Chunk { Info info{}; pcm samples{}; };
struct AlacFrame { std::vector<uint8_t> cookie{}; std::vector<uint8_t> data{}; };

[[nodiscard]] bool loaded() noexcept;
[[nodiscard]] std::string_view library_path() noexcept;
[[nodiscard]] std::string_view load_error() noexcept;

class Decoder {
public:
    Decoder() = default;
    Decoder(const Decoder&) = delete;
    Decoder& operator=(const Decoder&) = delete;
    Decoder(Decoder&& other) noexcept;
    Decoder& operator=(Decoder&& other) noexcept;
    ~Decoder();
    [[nodiscard]] explicit operator bool() const noexcept;
    [[nodiscard]] Info info() const noexcept;
    [[nodiscard]] int64_t read(std::span<float> output) noexcept;
    [[nodiscard]] bool seek(uint64_t frame) noexcept;
private:
    friend std::unique_ptr<Decoder> open(Codec, bytes, uint32_t) noexcept;
    void* handle_{};
    Info info_{};
};

[[nodiscard]] std::unique_ptr<Decoder> open(Codec codec, bytes input, uint32_t sample_rate = 48000) noexcept;
[[nodiscard]] Chunk decode(Codec codec, bytes input, uint32_t frame_capacity, uint32_t sample_rate = 48000) noexcept;
[[nodiscard]] Chunk decode_flac(bytes input, uint32_t frame_capacity) noexcept;
[[nodiscard]] Chunk decode_wma(bytes input, uint32_t frame_capacity) noexcept;
[[nodiscard]] Chunk decode_aac(bytes input, uint32_t frame_capacity) noexcept;
[[nodiscard]] std::vector<int16_t> decode_alac_s16(bytes cookie, bytes input, uint32_t frame_capacity, Info& info) noexcept;
[[nodiscard]] AlacFrame encode_alac_s16(std::span<const int16_t> pcm, uint32_t sample_rate, uint32_t channels, uint32_t frame_length = 4096) noexcept;
[[nodiscard]] std::vector<uint8_t> encode_wav_f32(std::span<const float> pcm, uint32_t sample_rate, uint32_t channels) noexcept;
[[nodiscard]] std::vector<uint8_t> encode_flac_f32(std::span<const float> pcm, uint32_t sample_rate, uint32_t channels, uint32_t compression = 5) noexcept;

}
