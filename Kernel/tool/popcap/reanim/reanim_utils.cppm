module;
#include <array>
#include <cstdint>
#include <optional>
#include <span>
#include <string>
#include <string_view>
#include <utility>
#include <vector>
export module tool.popcap.reanim.utils;
export import tool.popcap.reanim.core;
import utility.binary.unified_binary_stream;
import utility.zlib.zlib_compress;
import utility.zlib.zlib_uncompress;

export namespace Reanim {

[[nodiscard]] inline Format parseFormat(std::string_view fmt) noexcept {
    if (fmt == "Phone32") return Format::Phone32;
    if (fmt == "Phone64") return Format::Phone64;
    if (fmt == "TV") return Format::TV;
    if (fmt == "GameConsole") return Format::GameConsole;
    if (fmt == "WP") return Format::WP;
    return Format::PC;
}

[[nodiscard]] inline bool validTrackCount(int32_t n) noexcept {
    return n >= 0 && n <= 10000;
}

[[nodiscard]] inline bool validTransformCount(int32_t n) noexcept {
    return n >= 0 && n <= 100000;
}

[[nodiscard]] inline float optOr(const std::optional<float>& v, float sentinel) noexcept {
    return v.value_or(sentinel);
}

[[nodiscard]] inline std::optional<float> takeFloat(float v, float sentinel) noexcept {
    return v != sentinel ? std::optional{v} : std::nullopt;
}

[[nodiscard]] inline std::optional<std::string> takeNonEmpty(std::string s) {
    return s.empty() ? std::nullopt : std::optional{std::move(s)};
}

inline void writeZeros(UnifiedBinaryStream& bs, size_t n) {
    for (size_t i = 0; i < n; ++i) bs.writeInt32(0);
}

inline void skipBytes(UnifiedBinaryStream& bs, size_t n) {
    bs.setPosition(bs.getPosition() + n);
}

inline void writeFloat8(UnifiedBinaryStream& bs, const Transform& t, float sentinel) {
    bs.writeFloat32(optOr(t.x, sentinel));
    bs.writeFloat32(optOr(t.y, sentinel));
    bs.writeFloat32(optOr(t.kx, sentinel));
    bs.writeFloat32(optOr(t.ky, sentinel));
    bs.writeFloat32(optOr(t.sx, sentinel));
    bs.writeFloat32(optOr(t.sy, sentinel));
    bs.writeFloat32(optOr(t.f, sentinel));
    bs.writeFloat32(optOr(t.a, sentinel));
}

inline void readFloat8(UnifiedBinaryStream& bs, Transform& t, float sentinel) {
    t.x = takeFloat(bs.readFloat32(), sentinel);
    t.y = takeFloat(bs.readFloat32(), sentinel);
    t.kx = takeFloat(bs.readFloat32(), sentinel);
    t.ky = takeFloat(bs.readFloat32(), sentinel);
    t.sx = takeFloat(bs.readFloat32(), sentinel);
    t.sy = takeFloat(bs.readFloat32(), sentinel);
    t.f = takeFloat(bs.readFloat32(), sentinel);
    t.a = takeFloat(bs.readFloat32(), sentinel);
}

inline void writeUnicodeStr(UnifiedBinaryStream& bs, std::string_view s) {
    if (s.empty()) {
        bs.writeInt32(0);
        return;
    }
    bs.writeInt32(static_cast<int32_t>(s.size()));
    bs.writeStringUnicode(s);
}

[[nodiscard]] inline std::optional<std::string> readUnicodeStr(UnifiedBinaryStream& bs) {
    int32_t len = bs.readInt32();
    if (len <= 0) return std::nullopt;
    auto s = bs.readStringUnicode(static_cast<size_t>(len));
    return s.empty() ? std::nullopt : std::optional{std::move(s)};
}

[[nodiscard]] inline std::vector<uint8_t> maybeDecompress(std::span<const uint8_t> input, Format format, bool useZlib) {
    if (!useZlib || input.size() < 8) return {};
    uint32_t magic = UnifiedBinaryStream::bytesToUInt32(input.data());

    if (magic == kPopCapZlibMagicLE) {
        auto dec = zlib_ns::PopCapDecompressor::decompress(input, format == Format::Phone64);
        return dec ? std::move(*dec) : std::vector<uint8_t>{};
    }

    if (magic == kPopCapZlibMagicBE && format == Format::GameConsole) {
        UnifiedBinaryStream check(input, UnifiedBinaryStream::Endian::Big);
        check.setPosition(4);
        int32_t uncompSize = check.readInt32();
        if (check.hasErrorOccurred() || uncompSize <= 0 || uncompSize > 100 * 1024 * 1024)
            return {};
        auto dec = zlib_ns::Decompressor::decompress(input.subspan(8), static_cast<size_t>(uncompSize));
        return dec ? std::move(*dec) : std::vector<uint8_t>{};
    }
    return {};
}

[[nodiscard]] inline std::vector<uint8_t> maybeCompress(std::span<const uint8_t> uncompressed, Format format, bool useZlib) {
    if (!useZlib || format == Format::WP || uncompressed.empty())
        return {uncompressed.begin(), uncompressed.end()};

    if (format == Format::GameConsole) {
        UnifiedBinaryStream out(UnifiedBinaryStream::Mode::Write, UnifiedBinaryStream::Endian::Big);
        out.writeInt32(static_cast<int32_t>(kPopCapZlibMagicLE));
        out.writeInt32(static_cast<int32_t>(uncompressed.size()));
        auto comp = zlib_ns::Compressor::compress(uncompressed);
        if (!comp) return {uncompressed.begin(), uncompressed.end()};
        out.writeBytes(*comp);
        return out.toByteArray();
    }

    auto comp = zlib_ns::PopCapCompressor::compress(uncompressed, format == Format::Phone64);
    return comp ? std::move(*comp) : std::vector<uint8_t>{uncompressed.begin(), uncompressed.end()};
}

inline constexpr std::array<uint8_t, 6> kXnbMagic = {0x58, 0x4E, 0x42, 0x6D, 0x05, 0x00};
inline constexpr std::array<uint8_t, 38> kXnbInfo = {
    0x01, 0x1E, 0x53, 0x65, 0x78, 0x79, 0x2E, 0x54, 0x6F, 0x64,
    0x4C, 0x69, 0x62, 0x2E, 0x52, 0x65, 0x61, 0x6E, 0x69, 0x6D,
    0x52, 0x65, 0x61, 0x64, 0x65, 0x72, 0x2C, 0x20, 0x4C, 0x41,
    0x57, 0x4E, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01
};

}
