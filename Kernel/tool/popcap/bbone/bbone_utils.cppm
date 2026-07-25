module;
#include <algorithm>
#include <cctype>
#include <cstdint>
#include <span>
#include <string>
#include <string_view>
#include <vector>
export module tool.popcap.bbone.utils;
import utility.binary.unified_binary_stream;
import utility.json;

export namespace BBone::Detail {

using Stream = UnifiedBinaryStream;

[[nodiscard]] inline std::string readUtf(Stream& bs) {
    const uint16_t n = bs.readUInt16();
    if (bs.hasErrorOccurred() || n == 0) return {};
    auto bytes = bs.readSpan(n);
    return {reinterpret_cast<const char*>(bytes.data()), bytes.size()};
}

inline void writeUtf(Stream& bs, std::string_view s) {
    bs.writeUInt16(static_cast<uint16_t>(s.size()));
    bs.writeBytes(reinterpret_cast<const uint8_t*>(s.data()), s.size());
}

[[nodiscard]] inline std::string latin1ToUtf8(std::span<const uint8_t> bytes) {
    std::string out;
    out.reserve(bytes.size());
    for (const uint8_t c : bytes) {
        if (c < 0x80u) {
            out += static_cast<char>(c);
        } else {
            out += static_cast<char>(0xC0u | (c >> 6u));
            out += static_cast<char>(0x80u | (c & 0x3Fu));
        }
    }
    return out;
}

[[nodiscard]] inline float getFloat(json::Value obj, std::string_view key, float def = 0.0f) noexcept {
    auto v = obj ? obj.obj_get(key) : json::Value{};
    return v && v.is_num() ? static_cast<float>(v.get_num()) : def;
}

[[nodiscard]] inline int getInt(json::Value obj, std::string_view key, int def = 0) noexcept {
    auto v = obj ? obj.obj_get(key) : json::Value{};
    if (!v) return def;
    if (v.is_uint()) return static_cast<int>(v.get_uint());
    if (v.is_int()) return static_cast<int>(v.get_sint());
    return def;
}

[[nodiscard]] inline int nextPowerOfTwo(int v) noexcept {
    if (v <= 1) return 1;
    int r = 1;
    while (r < v) r <<= 1;
    return r;
}

[[nodiscard]] inline std::string sanitize(std::string s) {
    std::ranges::replace(s, '.', '_');
    return s;
}

[[nodiscard]] inline std::string lowerAscii(std::string s) {
    std::ranges::transform(s, s.begin(), [](unsigned char c) { return static_cast<char>(std::tolower(c)); });
    return s;
}

}
