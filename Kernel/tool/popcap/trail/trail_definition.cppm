module;
#include <algorithm>
#include <array>
#include <charconv>
#include <cctype>
#include <cmath>
#include <cstdint>
#include <format>
#include <optional>
#include <span>
#include <sstream>
#include <string>
#include <string_view>
#include <utility>
#include <vector>
export module tool.popcap.trail.definition;
import tool.popcap.trail.core;
import tool.popcap.trail.utils;
import utility.binary.unified_binary_stream;
import utility.json;
import utility.xml.xml;
import utility.zlib.zlib_compress;
import utility.zlib.zlib_uncompress;

export namespace Trail::Definition {

namespace Detail {

[[nodiscard]] inline std::vector<TrackNode> readNodes(UnifiedBinaryStream& stream) {
    const auto count = stream.readInt32();
    if (count < 0 || stream.hasErrorOccurred()) return {};
    std::vector<TrackNode> nodes(static_cast<size_t>(count));
    for (auto& node : nodes) {
        node.time = stream.readFloat32();
        node.lowValue = stream.readFloat32();
        node.highValue = stream.readFloat32();
        node.curveType = stream.readInt32();
        node.distribution = stream.readInt32();
    }
    return stream.hasErrorOccurred() ? std::vector<TrackNode>{} : nodes;
}

inline void writeNodes(UnifiedBinaryStream& stream, std::span<const TrackNode> nodes) {
    stream.writeInt32(static_cast<int32_t>(nodes.size()));
    for (const auto& node : nodes) {
        stream.writeFloat32(node.time);
        stream.writeFloat32(node.lowValue);
        stream.writeFloat32(node.highValue);
        stream.writeInt32(node.curveType);
        stream.writeInt32(node.distribution);
    }
}

[[nodiscard]] inline std::vector<uint8_t> compress(const std::vector<uint8_t>& data, bool is64) {
    if (auto result = zlib_ns::PopCapCompressor::compress(data, is64)) return std::move(*result);
    return {};
}

[[nodiscard]] inline std::vector<uint8_t> finish(UnifiedBinaryStream& stream, bool compressed, bool is64 = false) {
    if (stream.hasErrorOccurred()) return {};
    return compressed ? compress(stream.getData(), is64) : stream.getData();
}

inline void writeHeader(UnifiedBinaryStream& stream, const Data& data, int32_t magic, size_t padding) {
    stream.writeInt32(magic);
    stream.writeInt32(0);
    stream.writeInt32(data.maxPoints);
    stream.writeFloat32(data.minPointDistance);
    stream.writeInt32(data.flags);
    for (size_t i = 0; i < padding; ++i) stream.writeInt32(0);
}

inline void readStandard(Data& data, UnifiedBinaryStream& stream, bool defaults) {
    const auto points = stream.readInt32();
    const auto distance = stream.readFloat32();
    data.maxPoints = defaults && points == 0 ? 2 : points;
    data.minPointDistance = defaults && distance == 0.0f ? 1.0f : distance;
    data.flags = stream.readInt32();
}

inline void readNodeFields(json::Value array, std::vector<TrackNode>& nodes) {
    if (!array || !array.is_arr()) return;
    nodes.reserve(array.arr_size());
    for (auto object : array.array()) {
        TrackNode node;
        if (auto value = object.obj_get("Time"); value && value.is_num()) node.time = static_cast<float>(value.get_num());
        if (auto value = object.obj_get("LowValue"); value && value.is_num()) node.lowValue = static_cast<float>(value.get_num());
        if (auto value = object.obj_get("HighValue"); value && value.is_num()) node.highValue = static_cast<float>(value.get_num());
        if (auto value = object.obj_get("CurveType"); value) node.curveType = value.is_str() ? curveValue(value.get_str_view()) : value.is_int() ? static_cast<int32_t>(value.get_sint()) : node.curveType;
        if (auto value = object.obj_get("Distribution"); value) node.distribution = value.is_str() ? curveValue(value.get_str_view()) : value.is_int() ? static_cast<int32_t>(value.get_sint()) : node.distribution;
        nodes.push_back(node);
    }
}

inline void writeNodeFields(json::MutDocument& document, json::MutValue parent, std::string_view name, std::span<const TrackNode> nodes) {
    auto array = document.mut_arr();
    for (const auto& node : nodes) {
        auto object = document.mut_obj();
        document.obj_add_real(object, "Time", node.time);
        document.obj_add_real(object, "LowValue", node.lowValue);
        document.obj_add_real(object, "HighValue", node.highValue);
        if (node.curveType >= 0 && static_cast<size_t>(node.curveType) < kCurveNames.size()) document.obj_add_str(object, "CurveType", curveName(node.curveType));
        else document.obj_add_int(object, "CurveType", node.curveType);
        if (node.distribution >= 0 && static_cast<size_t>(node.distribution) < kCurveNames.size()) document.obj_add_str(object, "Distribution", curveName(node.distribution));
        else document.obj_add_int(object, "Distribution", node.distribution);
        array.arr_append(object);
    }
    parent.obj_add(document.mut_str(name), array);
}

[[nodiscard]] inline std::string floatText(float value) {
    auto text = std::format("{}", value);
    if (text.starts_with("0.") && text.size() > 1) text.erase(0, 1);
    return text;
}

[[nodiscard]] inline std::vector<TrackNode> parseXmlNodes(std::string_view text) {
    std::vector<TrackNode> nodes;
    size_t pos{};
    const auto skipSpace = [&] { while (pos < text.size() && std::isspace(static_cast<unsigned char>(text[pos]))) ++pos; };
    const auto readToken = [&]() -> std::string_view { const auto begin = pos; while (pos < text.size() && !std::isspace(static_cast<unsigned char>(text[pos])) && text[pos] != ',' && text[pos] != ']') ++pos; return text.substr(begin, pos - begin); };
    const auto number = [](std::string_view token) { float value{}; std::from_chars(token.data(), token.data() + token.size(), value); return value; };
    while (pos < text.size()) {
        TrackNode node;
        skipSpace();
        if (pos == text.size()) break;
        if (text[pos] == '[') {
            ++pos;
            skipSpace();
            node.lowValue = number(readToken());
            skipSpace();
            if (pos < text.size() && text[pos] == ']') { node.highValue = node.lowValue; node.distribution = 0; ++pos; }
            else {
                const auto token = readToken();
                node.distribution = token.empty() ? 1 : (std::isalpha(static_cast<unsigned char>(token.front())) ? curveValue(token) : 1);
                if (!token.empty() && !std::isalpha(static_cast<unsigned char>(token.front()))) node.highValue = number(token);
                else { skipSpace(); node.highValue = number(readToken()); }
                while (pos < text.size() && text[pos] != ']') ++pos;
                if (pos < text.size()) ++pos;
            }
        } else { node.lowValue = node.highValue = number(readToken()); }
        skipSpace();
        if (pos < text.size() && text[pos] == ',') { ++pos; skipSpace(); node.time = number(readToken()) / 100.0f; }
        else node.time = nodes.empty() ? 0.0f : 1.0f;
        skipSpace();
        if (pos < text.size() && std::isalpha(static_cast<unsigned char>(text[pos]))) node.curveType = curveValue(readToken());
        nodes.push_back(node);
    }
    return nodes;
}

inline void writeXmlNodes(std::ostringstream& output, std::span<const TrackNode> nodes) {
    for (size_t i = 0; i < nodes.size(); ++i) {
        if (i) output << ' ';
        const auto& node = nodes[i];
        if (node.lowValue == node.highValue) {
            if (node.distribution == 0) output << '[' << floatText(node.lowValue) << ']';
            else if (node.distribution == 1) output << floatText(node.lowValue);
            else output << '[' << floatText(node.lowValue) << ' ' << (node.distribution >= 0 && static_cast<size_t>(node.distribution) < kCurveNames.size() ? std::string(curveName(node.distribution)) : std::format("TodCurves({})", node.distribution)) << ' ' << floatText(node.highValue) << ']';
        } else {
            output << '[' << floatText(node.lowValue);
            if (node.distribution != 1) output << ' ' << (node.distribution >= 0 && static_cast<size_t>(node.distribution) < kCurveNames.size() ? std::string(curveName(node.distribution)) : std::format("TodCurves({})", node.distribution));
            output << ' ' << floatText(node.highValue) << ']';
        }
        if (node.time != 0.0f && node.time != 1.0f) output << ',' << node.time * 100.0f;
        if (node.curveType != 1) output << ' ' << (node.curveType >= 0 && static_cast<size_t>(node.curveType) < kCurveNames.size() ? std::string(curveName(node.curveType)) : std::format("TodCurves({})", node.curveType));
    }
}

}

class GameConsoleCodec {
public:
    [[nodiscard]] static Data decode(std::span<const uint8_t> input, bool compressed = true) {
        auto data = compressed ? zlib_ns::PopCapDecompressor::decompress(input, false) : std::optional<std::vector<uint8_t>>{};
        const auto bytes = compressed ? (data ? std::span<const uint8_t>(*data) : std::span<const uint8_t>{}) : input;
        UnifiedBinaryStream stream(bytes, UnifiedBinaryStream::Endian::Big);
        Data trail; stream.setPosition(8); Detail::readStandard(trail, stream, false); stream.setPosition(stream.getPosition() + 40);
        trail.image = stream.readStringByInt32Head(); trail.widthOverLength = Detail::readNodes(stream); trail.widthOverTime = Detail::readNodes(stream); trail.alphaOverLength = Detail::readNodes(stream); trail.alphaOverTime = Detail::readNodes(stream); trail.duration = Detail::readNodes(stream);
        return stream.hasErrorOccurred() ? Data{} : trail;
    }
    [[nodiscard]] static std::vector<uint8_t> encode(const Data& trail, bool compressed = true) {
        UnifiedBinaryStream stream(UnifiedBinaryStream::Mode::Write, UnifiedBinaryStream::Endian::Big); Detail::writeHeader(stream, trail, 0, 10); stream.writeStringByInt32Head(trail.image); Detail::writeNodes(stream, trail.widthOverLength); Detail::writeNodes(stream, trail.widthOverTime); Detail::writeNodes(stream, trail.alphaOverLength); Detail::writeNodes(stream, trail.alphaOverTime); Detail::writeNodes(stream, trail.duration); return Detail::finish(stream, compressed);
    }
};

class PCCodec {
public:
    [[nodiscard]] static Data decode(std::span<const uint8_t> input, bool compressed = true) {
        auto data = compressed ? zlib_ns::PopCapDecompressor::decompress(input, false) : std::optional<std::vector<uint8_t>>{}; const auto bytes = compressed ? (data ? std::span<const uint8_t>(*data) : std::span<const uint8_t>{}) : input;
        UnifiedBinaryStream stream(bytes); Data trail; stream.setPosition(8); Detail::readStandard(trail, stream, true); stream.setPosition(stream.getPosition() + 40); trail.image = stream.readStringByInt32Head(); trail.widthOverLength = Detail::readNodes(stream); trail.widthOverTime = Detail::readNodes(stream); trail.alphaOverLength = Detail::readNodes(stream); trail.alphaOverTime = Detail::readNodes(stream); trail.duration = Detail::readNodes(stream); return stream.hasErrorOccurred() ? Data{} : trail;
    }
    [[nodiscard]] static std::vector<uint8_t> encode(const Data& trail, bool compressed = true) {
        UnifiedBinaryStream stream; Detail::writeHeader(stream, trail, -1416928589, 10); stream.writeStringByInt32Head(trail.image); Detail::writeNodes(stream, trail.widthOverLength); Detail::writeNodes(stream, trail.widthOverTime); Detail::writeNodes(stream, trail.alphaOverLength); Detail::writeNodes(stream, trail.alphaOverTime); Detail::writeNodes(stream, trail.duration); return Detail::finish(stream, compressed);
    }
};

class Phone32Codec {
public:
    [[nodiscard]] static Data decode(std::span<const uint8_t> input, bool compressed = true) {
        auto data = compressed ? zlib_ns::PopCapDecompressor::decompress(input, false) : std::optional<std::vector<uint8_t>>{}; const auto bytes = compressed ? (data ? std::span<const uint8_t>(*data) : std::span<const uint8_t>{}) : input;
        UnifiedBinaryStream stream(bytes); Data trail; stream.setPosition(8); Detail::readStandard(trail, stream, true); stream.setPosition(stream.getPosition() + 40); if (const auto id = stream.readInt32(); id != -1) trail.image = ImageMap::fromId(id); trail.widthOverLength = Detail::readNodes(stream); trail.widthOverTime = Detail::readNodes(stream); trail.alphaOverLength = Detail::readNodes(stream); trail.alphaOverTime = Detail::readNodes(stream); trail.duration = Detail::readNodes(stream); return stream.hasErrorOccurred() ? Data{} : trail;
    }
    [[nodiscard]] static std::vector<uint8_t> encode(const Data& trail, bool compressed = true) {
        UnifiedBinaryStream stream; Detail::writeHeader(stream, trail, -1416928589, 10); stream.writeInt32(ImageMap::toId(trail.image)); Detail::writeNodes(stream, trail.widthOverLength); Detail::writeNodes(stream, trail.widthOverTime); Detail::writeNodes(stream, trail.alphaOverLength); Detail::writeNodes(stream, trail.alphaOverTime); Detail::writeNodes(stream, trail.duration); return Detail::finish(stream, compressed);
    }
};

class Phone64Codec {
public:
    [[nodiscard]] static Data decode(std::span<const uint8_t> input, bool compressed = true) {
        auto data = compressed ? zlib_ns::PopCapDecompressor::decompress(input, true) : std::optional<std::vector<uint8_t>>{}; const auto bytes = compressed ? (data ? std::span<const uint8_t>(*data) : std::span<const uint8_t>{}) : input;
        UnifiedBinaryStream stream(bytes); Data trail; stream.setPosition(12); Detail::readStandard(trail, stream, true); stream.setPosition(stream.getPosition() + 84); if (const auto id = stream.readInt32(); id != -1) trail.image = ImageMap::fromId(id); trail.widthOverLength = Detail::readNodes(stream); trail.widthOverTime = Detail::readNodes(stream); trail.alphaOverLength = Detail::readNodes(stream); trail.alphaOverTime = Detail::readNodes(stream); trail.duration = Detail::readNodes(stream); return stream.hasErrorOccurred() ? Data{} : trail;
    }
    [[nodiscard]] static std::vector<uint8_t> encode(const Data& trail, bool compressed = true) {
        UnifiedBinaryStream stream; stream.writeInt32(-2071413752); stream.writeInt32(0); stream.writeInt32(0); stream.writeInt32(trail.maxPoints); stream.writeFloat32(trail.minPointDistance); stream.writeInt32(trail.flags); stream.writeInt32(0); for (int i = 0; i < 20; ++i) stream.writeInt32(0); stream.writeInt32(ImageMap::toId(trail.image)); Detail::writeNodes(stream, trail.widthOverLength); Detail::writeNodes(stream, trail.widthOverTime); Detail::writeNodes(stream, trail.alphaOverLength); Detail::writeNodes(stream, trail.alphaOverTime); Detail::writeNodes(stream, trail.duration); return Detail::finish(stream, compressed, true);
    }
};

class TVCodec {
public:
    [[nodiscard]] static Data decode(std::span<const uint8_t> input, bool compressed = true) {
        auto data = compressed ? zlib_ns::PopCapDecompressor::decompress(input, false) : std::optional<std::vector<uint8_t>>{}; const auto bytes = compressed ? (data ? std::span<const uint8_t>(*data) : std::span<const uint8_t>{}) : input;
        UnifiedBinaryStream stream(bytes); Data trail; stream.setPosition(8); Detail::readStandard(trail, stream, false); stream.setPosition(stream.getPosition() + 40); trail.image = stream.readStringByInt32Head(); trail.imageResource = stream.readStringByInt32Head(); trail.widthOverLength = Detail::readNodes(stream); trail.widthOverTime = Detail::readNodes(stream); trail.alphaOverLength = Detail::readNodes(stream); trail.alphaOverTime = Detail::readNodes(stream); trail.duration = Detail::readNodes(stream); return stream.hasErrorOccurred() ? Data{} : trail;
    }
    [[nodiscard]] static std::vector<uint8_t> encode(const Data& trail, bool compressed = true) {
        UnifiedBinaryStream stream; Detail::writeHeader(stream, trail, -1416928589, 10); stream.writeStringByInt32Head(trail.image); stream.writeStringByInt32Head(trail.imageResource); Detail::writeNodes(stream, trail.widthOverLength); Detail::writeNodes(stream, trail.widthOverTime); Detail::writeNodes(stream, trail.alphaOverLength); Detail::writeNodes(stream, trail.alphaOverTime); Detail::writeNodes(stream, trail.duration); return Detail::finish(stream, compressed);
    }
};

class WPCodec {
    inline static constexpr std::array<uint8_t, 6> kMagic{0x58, 0x4E, 0x42, 0x6D, 0x05, 0x00};
    inline static constexpr std::array<uint8_t, 37> kInfo{0x01, 0x1D, 0x53, 0x65, 0x78, 0x79, 0x2E, 0x54, 0x6F, 0x64, 0x4C, 0x69, 0x62, 0x2E, 0x54, 0x72, 0x61, 0x69, 0x6C, 0x52, 0x65, 0x61, 0x64, 0x65, 0x72, 0x2C, 0x20, 0x4C, 0x41, 0x57, 0x4E, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01};
    [[nodiscard]] static std::vector<TrackNode> readNodes(UnifiedBinaryStream& stream) { const auto count = stream.readInt32(); if (count < 0 || stream.hasErrorOccurred()) return {}; std::vector<TrackNode> nodes(static_cast<size_t>(count)); for (auto& node : nodes) { node.curveType = stream.readInt32(); node.distribution = stream.readInt32(); node.highValue = static_cast<float>(stream.readDouble()); node.lowValue = static_cast<float>(stream.readDouble()); node.time = static_cast<float>(stream.readDouble()); } return stream.hasErrorOccurred() ? std::vector<TrackNode>{} : nodes; }
    static void writeNodes(UnifiedBinaryStream& stream, std::span<const TrackNode> nodes) { stream.writeInt32(static_cast<int32_t>(nodes.size())); for (const auto& node : nodes) { stream.writeInt32(node.curveType); stream.writeInt32(node.distribution); stream.writeDouble(node.highValue); stream.writeDouble(node.lowValue); stream.writeDouble(node.time); } }
public:
    [[nodiscard]] static Data decode(std::span<const uint8_t> input) {
        try { UnifiedBinaryStream stream(input); stream.verifyBytes(std::span{kMagic}); (void)stream.readInt32(); stream.verifyBytes(std::span{kInfo}); Data trail; trail.image = stream.readStringByVarInt32Head(); trail.maxPoints = stream.readInt32(); trail.minPointDistance = static_cast<float>(stream.readDouble()); trail.flags = stream.readInt32(); trail.duration = readNodes(stream); trail.widthOverLength = readNodes(stream); trail.widthOverTime = readNodes(stream); trail.alphaOverLength = readNodes(stream); trail.alphaOverTime = readNodes(stream); return stream.hasErrorOccurred() ? Data{} : trail; } catch (...) { return {}; }
    }
    [[nodiscard]] static std::vector<uint8_t> encode(const Data& trail) {
        UnifiedBinaryStream stream; stream.writeBytes(std::span{kMagic}); const auto sizePosition = stream.getPosition(); stream.writeInt32(0); stream.writeBytes(std::span{kInfo}); stream.writeStringByVarInt32Head(trail.image); stream.writeInt32(trail.maxPoints); stream.writeDouble(trail.minPointDistance); stream.writeInt32(trail.flags); writeNodes(stream, trail.duration); writeNodes(stream, trail.widthOverLength); writeNodes(stream, trail.widthOverTime); writeNodes(stream, trail.alphaOverLength); writeNodes(stream, trail.alphaOverTime); auto data = stream.toByteArray(); UnifiedBinaryStream::int32ToBytes(static_cast<int32_t>(data.size()), data.data() + sizePosition); return data;
    }
};

class JsonCodec {
public:
    [[nodiscard]] static Data decode(std::string_view input) {
        auto document = json::Document::parse(input); if (!document || !document.root().is_obj()) return {}; auto root = document.root(); Data trail;
        if (auto value = root.obj_get("MaxPoints"); value && value.is_int()) trail.maxPoints = static_cast<int32_t>(value.get_sint());
        if (auto value = root.obj_get("MinPointDistance"); value && value.is_num()) trail.minPointDistance = static_cast<float>(value.get_num());
        if (auto value = root.obj_get("Loops"); value && value.is_bool()) trail.flags = value.get_bool() ? 1 : 0;
        if (auto value = root.obj_get("Image"); value) { if (value.is_str()) trail.image = value.get_str_view(); else if (value.is_int()) trail.image = std::to_string(value.get_sint()); }
        if (auto value = root.obj_get("ImageResource"); value && value.is_str()) trail.imageResource = value.get_str_view();
        Detail::readNodeFields(root.obj_get("WidthOverLength"), trail.widthOverLength); Detail::readNodeFields(root.obj_get("WidthOverTime"), trail.widthOverTime); Detail::readNodeFields(root.obj_get("AlphaOverLength"), trail.alphaOverLength); Detail::readNodeFields(root.obj_get("AlphaOverTime"), trail.alphaOverTime); Detail::readNodeFields(root.obj_get("TrailDuration"), trail.duration); return trail;
    }
    [[nodiscard]] static std::string encode(const Data& trail) {
        json::MutDocument document; auto root = document.mut_obj(); document.set_root(root); if (trail.maxPoints != 2) document.obj_add_int(root, "MaxPoints", trail.maxPoints); if (trail.minPointDistance != 1.0f) document.obj_add_real(root, "MinPointDistance", trail.minPointDistance); if (trail.flags & 1) document.obj_add_bool(root, "Loops", true); if (!trail.image.empty()) { int32_t id{}; const auto [ptr, ec] = std::from_chars(trail.image.data(), trail.image.data() + trail.image.size(), id); if (ec == std::errc{} && ptr == trail.image.data() + trail.image.size()) document.obj_add_int(root, "Image", id); else document.obj_add_str(root, "Image", trail.image); } if (!trail.imageResource.empty()) document.obj_add_str(root, "ImageResource", trail.imageResource); if (!trail.widthOverLength.empty()) Detail::writeNodeFields(document, root, "WidthOverLength", trail.widthOverLength); if (!trail.widthOverTime.empty()) Detail::writeNodeFields(document, root, "WidthOverTime", trail.widthOverTime); if (!trail.alphaOverLength.empty()) Detail::writeNodeFields(document, root, "AlphaOverLength", trail.alphaOverLength); if (!trail.alphaOverTime.empty()) Detail::writeNodeFields(document, root, "AlphaOverTime", trail.alphaOverTime); if (!trail.duration.empty()) Detail::writeNodeFields(document, root, "TrailDuration", trail.duration); return document.write(json::WriteFlag::Pretty);
    }
};

class XmlCodec {
public:
    [[nodiscard]] static Data decode(std::string_view input) {
        auto document = xml::Document::parse(input); if (!document) return {}; Data trail;
        for (auto node : document->children()) { const auto name = node.name(); const auto value = node.text(); if (name == "MaxPoints") { std::from_chars(value.data(), value.data() + value.size(), trail.maxPoints); } else if (name == "MinPointDistance") { std::from_chars(value.data(), value.data() + value.size(), trail.minPointDistance); } else if (name == "Loops") trail.flags = value == "1" ? 1 : 0; else if (name == "Image") trail.image = value; else if (name == "ImageResource") trail.imageResource = value; else if (name == "WidthOverLength") trail.widthOverLength = Detail::parseXmlNodes(value); else if (name == "WidthOverTime") trail.widthOverTime = Detail::parseXmlNodes(value); else if (name == "AlphaOverLength") trail.alphaOverLength = Detail::parseXmlNodes(value); else if (name == "AlphaOverTime") trail.alphaOverTime = Detail::parseXmlNodes(value); else if (name == "TrailDuration") trail.duration = Detail::parseXmlNodes(value); }
        return trail;
    }
    [[nodiscard]] static std::string encode(const Data& trail) {
        std::ostringstream output; if (!trail.image.empty()) output << "<Image>" << trail.image << "</Image>\n"; if (trail.maxPoints != 2) output << "<MaxPoints>" << trail.maxPoints << "</MaxPoints>\n"; if (trail.flags & 1) output << "<Loops>1</Loops>\n"; if (trail.minPointDistance != 1.0f) output << "<MinPointDistance>" << Detail::floatText(trail.minPointDistance) << "</MinPointDistance>\n"; const auto write = [&](std::string_view name, std::span<const TrackNode> nodes) { if (!nodes.empty()) { output << '<' << name << '>'; Detail::writeXmlNodes(output, nodes); output << "</" << name << ">\n"; } }; write("WidthOverLength", trail.widthOverLength); write("WidthOverTime", trail.widthOverTime); write("AlphaOverLength", trail.alphaOverLength); write("AlphaOverTime", trail.alphaOverTime); write("TrailDuration", trail.duration); return std::move(output).str();
    }
};

}
