module;
#include <array>
#include <charconv>
#include <cstdint>
#include <optional>
#include <string>
#include <string_view>
#include <vector>
export module tool.popcap.particles.utils;
import tool.popcap.particles.core;
import utility.binary.unified_binary_stream;

export namespace Particles::Utils {

inline constexpr std::array<std::string_view, 5> emitterTypes{"Circle", "Box", "BoxPath", "CirclePath", "CircleEvenSpacing"};
inline constexpr std::array<std::string_view, 14> curveTypes{"Constant", "Linear", "EaseIn", "EaseOut", "EaseInOut", "EaseInOutWeak", "FastInOut", "FastInOutWeak", "WeakFastInOut", "Bounce", "BounceFastMiddle", "BounceSlowMiddle", "SinWave", "EaseSinWave"};
inline constexpr std::array<std::string_view, 12> fieldTypes{"Invalid", "Friction", "Acceleration", "Attractor", "MaxVelocity", "Velocity", "Position", "SystemPosition", "GroundConstraint", "Shake", "Circle", "Away"};

template<size_t N>
[[nodiscard]] inline std::optional<int32_t> indexOf(std::string_view value, const std::array<std::string_view, N>& values) noexcept {
    for (size_t i = 0; i < N; ++i) if (value == values[i]) return static_cast<int32_t>(i);
    return std::nullopt;
}

[[nodiscard]] inline Format format(std::string_view value) noexcept {
    if (value == "Phone32") return Format::Phone32;
    if (value == "Phone64") return Format::Phone64;
    if (value == "TV") return Format::TV;
    if (value == "GameConsole") return Format::GameConsole;
    if (value == "WP") return Format::WP;
    if (value == "JSON") return Format::JSON;
    if (value == "XML") return Format::XML;
    return Format::PC;
}

[[nodiscard]] inline int32_t imageId(std::string_view image) noexcept {
    if (image.empty()) return -1;
    int32_t value{};
    const auto [ptr, ec] = std::from_chars(image.data(), image.data() + image.size(), value);
    if (ec == std::errc{} && ptr == image.data() + image.size()) return value;
    uint64_t hash = 0xcbf29ce484222325ULL;
    for (const auto c : image) { hash ^= static_cast<uint8_t>(c); hash *= 0x100000001b3ULL; }
    const auto result = static_cast<int32_t>(hash & 0x7ffffffeU);
    return result == -1 ? 0 : result;
}

[[nodiscard]] inline std::string imageName(int32_t id) {
    return id == -1 ? std::string{} : std::to_string(id);
}

inline void writeTrackNodes(UnifiedBinaryStream& stream, const std::vector<TrackNode>& nodes) {
    stream.writeInt32(static_cast<int32_t>(nodes.size()));
    for (const auto& node : nodes) {
        stream.writeFloat32(node.time);
        stream.writeFloat32(node.lowValue.value_or(0.0F));
        stream.writeFloat32(node.highValue.value_or(0.0F));
        stream.writeInt32(node.curveType.value_or(1));
        stream.writeInt32(node.distribution.value_or(1));
    }
}

[[nodiscard]] inline std::vector<TrackNode> readTrackNodes(UnifiedBinaryStream& stream) {
    const auto count = stream.readInt32();
    if (count <= 0 || stream.hasErrorOccurred()) return {};
    std::vector<TrackNode> nodes(static_cast<size_t>(count));
    for (auto& node : nodes) {
        node.time = stream.readFloat32();
        if (const auto value = stream.readFloat32(); value != 0.0F) node.lowValue = value;
        if (const auto value = stream.readFloat32(); value != 0.0F) node.highValue = value;
        if (const auto value = stream.readInt32(); value != 1) node.curveType = value;
        if (const auto value = stream.readInt32(); value != 1) node.distribution = value;
    }
    return nodes;
}

inline void writeFields(UnifiedBinaryStream& stream, const std::vector<Field>& fields, int32_t marker, int32_t padding) {
    stream.writeInt32(marker);
    for (const auto& field : fields) {
        stream.writeInt32(field.type.value_or(0));
        for (int32_t i = 0; i < padding; ++i) stream.writeInt32(0);
    }
    for (const auto& field : fields) { writeTrackNodes(stream, field.x); writeTrackNodes(stream, field.y); }
}

inline void readFields(UnifiedBinaryStream& stream, std::vector<Field>& fields, size_t skip) {
    static_cast<void>(stream.readInt32());
    for (auto& field : fields) {
        if (const auto type = stream.readInt32(); type != 0) field.type = type;
        stream.setPosition(stream.getPosition() + skip);
    }
    for (auto& field : fields) { field.x = readTrackNodes(stream); field.y = readTrackNodes(stream); }
}

}
