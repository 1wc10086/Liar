module;
#include <algorithm>
#include <cstdint>
#include <cstring>
#include <span>
#include <vector>
#if defined(__aarch64__)
#include <arm_neon.h>
#endif
export module tool.popcap.rsb.rsb_utils;
import tool.popcap.rsb.rsb_core;
import utility.zlib.zlib_compress;
import utility.zlib.zlib_uncompress;

export namespace Rsb {

[[nodiscard]] inline std::vector<uint8_t> zlibDecompress(std::span<const uint8_t> data) {
    auto result = zlib_ns::Decompressor::decompress(data, 0);
    return result ? std::move(*result) : std::vector<uint8_t>{};
}

[[nodiscard]] inline std::vector<uint8_t> zlibCompress(std::span<const uint8_t> data, int level = 9) {
    auto result = zlib_ns::Compressor::compress(data, level);
    return result ? std::move(*result) : std::vector<uint8_t>{};
}

[[nodiscard]] inline std::vector<uint8_t> emptyZlibData(int level) {
    uint8_t head = (level == 1) ? 0x01 : ((level == 9) ? 0x9C : 0xDA);
    return {0x78, head, 0x03, 0x00, 0x00, 0x00, 0x00, 0x01};
}

[[nodiscard]] inline std::vector<uint8_t> smfDecompress(std::span<const uint8_t> data) {
    if (data.size() < 8) return {};
    auto inner = data.subspan(8);
    return zlibDecompress(inner);
}

[[nodiscard]] inline std::vector<uint8_t> readCanonicalListBytes(
    const uint8_t* base,
    size_t totalSize,
    size_t absoluteOffset,
    uint32_t byteLength,
    bool bigEndian
) {
    if (base == nullptr || byteLength == 0 || absoluteOffset >= totalSize) return {};
    size_t avail = std::min<size_t>(byteLength, totalSize - absoluteOffset);
    avail -= (avail % 4);
    if (avail == 0) return {};

    std::vector<uint8_t> out(avail);
    if (!bigEndian) {
        std::memcpy(out.data(), base + absoluteOffset, avail);
        return out;
    }

#if defined(__aarch64__)
    size_t i = 0;
    for (; i + 16 <= avail; i += 16) {
        uint8x16_t data = vld1q_u8(base + absoluteOffset + i);
        uint8x16_t swapped = vrev32q_u8(data);
        vst1q_u8(out.data() + i, swapped);
    }
    for (; i < avail; i += 4) {
        uint32_t v;
        std::memcpy(&v, base + absoluteOffset + i, 4);
        v = __builtin_bswap32(v);
        std::memcpy(out.data() + i, &v, 4);
    }
#else
    for (size_t i = 0; i < avail; i += 4) {
        uint32_t v;
        std::memcpy(&v, base + absoluteOffset + i, 4);
        v = __builtin_bswap32(v);
        std::memcpy(out.data() + i, &v, 4);
    }
#endif
    return out;
}

[[nodiscard]] inline uint32_t safeClampCountByRegion(
    size_t totalSize,
    uint32_t begin,
    uint32_t count,
    uint32_t each
) noexcept {
    if (each == 0 || begin >= totalSize) return 0;
    size_t maxCount = (totalSize - static_cast<size_t>(begin)) / static_cast<size_t>(each);
    return static_cast<uint32_t>(std::min<size_t>(count, maxCount));
}

}