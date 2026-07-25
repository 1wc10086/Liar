module;
#include <cstdint>

export module tool.popcap.dz.core;

export namespace Dz {

enum class CompressFlags : uint16_t {
    COMBUF = 1,
    DZ = 4,
    ZLIB = 8,
    BZIP = 16,
    MP3 = 32,
    JPEG = 64,
    ZERO = 128,
    STORE = 256,
    LZMA = 512,
    RANDOMACCESS = 1024,
};

[[nodiscard]] inline constexpr auto operator&(CompressFlags a, CompressFlags b) noexcept {
    return static_cast<CompressFlags>(static_cast<uint16_t>(a) & static_cast<uint16_t>(b));
}

[[nodiscard]] inline constexpr auto operator|(CompressFlags a, CompressFlags b) noexcept {
    return static_cast<CompressFlags>(static_cast<uint16_t>(a) | static_cast<uint16_t>(b));
}

[[nodiscard]] inline constexpr auto operator~(CompressFlags a) noexcept {
    return static_cast<CompressFlags>(~static_cast<uint16_t>(a));
}

inline constexpr CompressFlags& operator|=(CompressFlags& a, CompressFlags b) noexcept { return a = a | b; }
inline constexpr CompressFlags& operator&=(CompressFlags& a, CompressFlags b) noexcept { return a = a & b; }

[[nodiscard]] inline constexpr bool hasFlag(CompressFlags value, CompressFlags flag) noexcept {
    return (static_cast<uint16_t>(value) & static_cast<uint16_t>(flag)) != 0;
}

}
