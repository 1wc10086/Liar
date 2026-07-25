module;
#include <charconv>
#include <cstdint>
#include <optional>
#include <span>
#include <string>
#include <string_view>
#include <utility>
#include <vector>
export module tool.popcap.reanim.definition;
export import tool.popcap.reanim.core;
import tool.popcap.reanim.utils;
import utility.binary.unified_binary_stream;
import utility.json;
import utility.xml.xml;

export namespace Reanim {

enum class ImageField : uint8_t { String, Int32 };

struct BinaryLayout {
    uint32_t magic = 0;
    size_t tracksOffset = 8;
    size_t fpsPad = 4;
    int32_t trackStrideMarker = 0x0C;
    size_t countEntryPadBefore = 8;
    size_t countEntryPadAfter = 0;
    int32_t transformMarker = 0x2C;
    size_t transformPad = 12;
    UnifiedBinaryStream::Endian endian = UnifiedBinaryStream::Endian::Little;
    ImageField imageField = ImageField::String;
    bool extendedStrings = false;
    size_t trackHeaderZeros = 2;
    size_t trackHeaderTailZeros = 0;
    size_t headerPrefixZeros = 0;
    size_t headerMidZeros = 1;
};

inline constexpr BinaryLayout kLayoutPC{
    .magic = kPcMagic,
    .tracksOffset = 8,
    .fpsPad = 4,
    .trackStrideMarker = 0x0C,
    .countEntryPadBefore = 8,
    .countEntryPadAfter = 0,
    .transformMarker = 0x2C,
    .transformPad = 12,
    .endian = UnifiedBinaryStream::Endian::Little,
    .imageField = ImageField::String,
    .extendedStrings = false,
    .trackHeaderZeros = 2,
    .trackHeaderTailZeros = 0,
    .headerPrefixZeros = 1,
    .headerMidZeros = 1,
};

inline constexpr BinaryLayout kLayoutPhone32{
    .magic = kPhone32Magic,
    .tracksOffset = 8,
    .fpsPad = 4,
    .trackStrideMarker = 0x10,
    .countEntryPadBefore = 12,
    .countEntryPadAfter = 0,
    .transformMarker = 0x2C,
    .transformPad = 12,
    .endian = UnifiedBinaryStream::Endian::Little,
    .imageField = ImageField::Int32,
    .extendedStrings = false,
    .trackHeaderZeros = 3,
    .trackHeaderTailZeros = 0,
    .headerPrefixZeros = 1,
    .headerMidZeros = 1,
};

inline constexpr BinaryLayout kLayoutPhone64{
    .magic = kPhone64Magic,
    .tracksOffset = 12,
    .fpsPad = 8,
    .trackStrideMarker = 0x20,
    .countEntryPadBefore = 24,
    .countEntryPadAfter = 4,
    .transformMarker = 0x38,
    .transformPad = 24,
    .endian = UnifiedBinaryStream::Endian::Little,
    .imageField = ImageField::Int32,
    .extendedStrings = false,
    .trackHeaderZeros = 6,
    .trackHeaderTailZeros = 1,
    .headerPrefixZeros = 2,
    .headerMidZeros = 2,
};

inline constexpr BinaryLayout kLayoutTV{
    .magic = 0,
    .tracksOffset = 8,
    .fpsPad = 4,
    .trackStrideMarker = 0x14,
    .countEntryPadBefore = 12,
    .countEntryPadAfter = 4,
    .transformMarker = 0x30,
    .transformPad = 16,
    .endian = UnifiedBinaryStream::Endian::Little,
    .imageField = ImageField::String,
    .extendedStrings = true,
    .trackHeaderZeros = 3,
    .trackHeaderTailZeros = 1,
    .headerPrefixZeros = 2,
    .headerMidZeros = 1,
};

inline constexpr BinaryLayout kLayoutGameConsole{
    .magic = 0,
    .tracksOffset = 8,
    .fpsPad = 4,
    .trackStrideMarker = 0x0C,
    .countEntryPadBefore = 8,
    .countEntryPadAfter = 0,
    .transformMarker = 0x2C,
    .transformPad = 12,
    .endian = UnifiedBinaryStream::Endian::Big,
    .imageField = ImageField::String,
    .extendedStrings = false,
    .trackHeaderZeros = 2,
    .trackHeaderTailZeros = 0,
    .headerPrefixZeros = 2,
    .headerMidZeros = 1,
};

[[nodiscard]] inline const BinaryLayout& layoutOf(Format format) noexcept {
    switch (format) {
        case Format::Phone32: return kLayoutPhone32;
        case Format::Phone64: return kLayoutPhone64;
        case Format::TV: return kLayoutTV;
        case Format::GameConsole: return kLayoutGameConsole;
        default: return kLayoutPC;
    }
}

struct BinaryCodec {
    [[nodiscard]] static std::optional<Data> decode(std::span<const uint8_t> data, const BinaryLayout& L) {
        UnifiedBinaryStream bs(data, L.endian);
        Data reanim;
        bs.setPosition(L.tracksOffset);
        int32_t tracksNumber = bs.readInt32();
        if (bs.hasErrorOccurred() || !validTrackCount(tracksNumber)) return std::nullopt;
        reanim.fps = bs.readFloat32();
        skipBytes(bs, L.fpsPad);
        if (bs.readInt32() != L.trackStrideMarker) return std::nullopt;

        std::vector<int32_t> counts(static_cast<size_t>(tracksNumber));
        for (int32_t i = 0; i < tracksNumber; ++i) {
            skipBytes(bs, L.countEntryPadBefore);
            int32_t count = bs.readInt32();
            skipBytes(bs, L.countEntryPadAfter);
            if (bs.hasErrorOccurred() || !validTransformCount(count)) return std::nullopt;
            counts[static_cast<size_t>(i)] = count;
        }

        reanim.tracks.reserve(static_cast<size_t>(tracksNumber));
        for (int32_t i = 0; i < tracksNumber; ++i) {
            Track track;
            track.name = bs.readStringByInt32Head();
            if (bs.readInt32() != L.transformMarker) return std::nullopt;
            int32_t times = counts[static_cast<size_t>(i)];
            track.transforms.resize(static_cast<size_t>(times));
            for (int32_t j = 0; j < times; ++j) {
                readFloat8(bs, track.transforms[static_cast<size_t>(j)], kSentinel);
                skipBytes(bs, L.transformPad);
            }
            for (int32_t j = 0; j < times; ++j) {
                auto& t = track.transforms[static_cast<size_t>(j)];
                if (L.imageField == ImageField::Int32) {
                    int32_t ival = bs.readInt32();
                    if (ival != -1) t.iInt = ival;
                } else {
                    t.i = takeNonEmpty(bs.readStringByInt32Head());
                }
                if (L.extendedStrings) {
                    t.resource = takeNonEmpty(bs.readStringByInt32Head());
                    t.i2 = takeNonEmpty(bs.readStringByInt32Head());
                    t.resource2 = takeNonEmpty(bs.readStringByInt32Head());
                }
                t.font = takeNonEmpty(bs.readStringByInt32Head());
                t.text = takeNonEmpty(bs.readStringByInt32Head());
            }
            reanim.tracks.push_back(std::move(track));
        }
        return bs.hasErrorOccurred() ? std::nullopt : std::optional{std::move(reanim)};
    }

    [[nodiscard]] static std::vector<uint8_t> encode(const Data& reanim, const BinaryLayout& L) {
        UnifiedBinaryStream bs(UnifiedBinaryStream::Mode::Write, L.endian);
        if (L.magic) bs.writeInt32(static_cast<int32_t>(L.magic));
        writeZeros(bs, L.headerPrefixZeros);
        bs.writeInt32(static_cast<int32_t>(reanim.tracks.size()));
        bs.writeFloat32(reanim.fps);
        writeZeros(bs, L.headerMidZeros);
        bs.writeInt32(L.trackStrideMarker);

        for (const auto& track : reanim.tracks) {
            writeZeros(bs, L.trackHeaderZeros);
            bs.writeInt32(static_cast<int32_t>(track.transforms.size()));
            writeZeros(bs, L.trackHeaderTailZeros);
        }

        for (const auto& track : reanim.tracks) {
            bs.writeStringByInt32Head(track.name);
            bs.writeInt32(L.transformMarker);
            for (const auto& t : track.transforms) {
                writeFloat8(bs, t, kSentinel);
                writeZeros(bs, L.transformPad / 4);
            }
            for (const auto& t : track.transforms) {
                if (L.imageField == ImageField::Int32) {
                    bs.writeInt32(t.iInt.value_or(-1));
                } else {
                    bs.writeStringByInt32Head(t.i.value_or(""));
                }
                if (L.extendedStrings) {
                    bs.writeStringByInt32Head(t.resource.value_or(""));
                    bs.writeStringByInt32Head(t.i2.value_or(""));
                    bs.writeStringByInt32Head(t.resource2.value_or(""));
                }
                bs.writeStringByInt32Head(t.font.value_or(""));
                bs.writeStringByInt32Head(t.text.value_or(""));
            }
        }
        return bs.toByteArray();
    }

    [[nodiscard]] static std::optional<Data> decode(std::span<const uint8_t> data, Format format) {
        return decode(data, layoutOf(format));
    }

    [[nodiscard]] static std::vector<uint8_t> encode(const Data& reanim, Format format) {
        return encode(reanim, layoutOf(format));
    }
};

struct WPCodec {
    [[nodiscard]] static std::optional<Data> decode(std::span<const uint8_t> data) {
        if (data.size() < kXnbMagic.size() + kXnbInfo.size() + 10) return std::nullopt;
        UnifiedBinaryStream bs(data);
        auto magic = bs.readBytes(kXnbMagic.size());
        if (magic.size() != kXnbMagic.size()) return std::nullopt;
        for (size_t i = 0; i < kXnbMagic.size(); ++i)
            if (magic[i] != kXnbMagic[i]) return std::nullopt;
        (void)bs.readInt32();
        auto info = bs.readBytes(kXnbInfo.size());
        if (info.size() != kXnbInfo.size()) return std::nullopt;
        for (size_t i = 0; i < kXnbInfo.size(); ++i)
            if (info[i] != kXnbInfo[i]) return std::nullopt;

        Data reanim;
        reanim.doScale = bs.readInt8();
        reanim.fps = bs.readFloat32();
        int32_t tracksNumber = bs.readInt32();
        if (bs.hasErrorOccurred() || !validTrackCount(tracksNumber)) return std::nullopt;

        reanim.tracks.reserve(static_cast<size_t>(tracksNumber));
        for (int32_t i = 0; i < tracksNumber; ++i) {
            Track track;
            int32_t nameLength = bs.readInt32();
            if (nameLength > 0) track.name = bs.readStringUnicode(static_cast<size_t>(nameLength));
            int32_t times = bs.readInt32();
            if (bs.hasErrorOccurred() || !validTransformCount(times)) return std::nullopt;
            track.transforms.reserve(static_cast<size_t>(times));
            for (int32_t j = 0; j < times; ++j) {
                Transform ts;
                if (bs.readUInt8() == 0) {
                    ts.font = readUnicodeStr(bs);
                    ts.i = readUnicodeStr(bs);
                    ts.text = readUnicodeStr(bs);
                    ts.a = takeFloat(bs.readFloat32(), kWpSentinel);
                    ts.f = takeFloat(bs.readFloat32(), kWpSentinel);
                    ts.sx = takeFloat(bs.readFloat32(), kWpSentinel);
                    ts.sy = takeFloat(bs.readFloat32(), kWpSentinel);
                    ts.kx = takeFloat(bs.readFloat32(), kWpSentinel);
                    ts.ky = takeFloat(bs.readFloat32(), kWpSentinel);
                    ts.x = takeFloat(bs.readFloat32(), kWpSentinel);
                    ts.y = takeFloat(bs.readFloat32(), kWpSentinel);
                }
                track.transforms.push_back(std::move(ts));
            }
            reanim.tracks.push_back(std::move(track));
        }
        return bs.hasErrorOccurred() ? std::nullopt : std::optional{std::move(reanim)};
    }

    [[nodiscard]] static std::vector<uint8_t> encode(const Data& reanim) {
        UnifiedBinaryStream bs;
        bs.writeBytes(kXnbMagic);
        size_t sizePos = bs.getPosition();
        bs.writeInt32(0);
        bs.writeBytes(kXnbInfo);
        bs.writeInt8(reanim.doScale.value_or(0));
        bs.writeFloat32(reanim.fps);
        bs.writeInt32(static_cast<int32_t>(reanim.tracks.size()));

        for (const auto& track : reanim.tracks) {
            writeUnicodeStr(bs, track.name);
            bs.writeInt32(static_cast<int32_t>(track.transforms.size()));
            for (const auto& t : track.transforms) {
                if (t.emptyWp()) {
                    bs.writeUInt8(1);
                    continue;
                }
                bs.writeUInt8(0);
                writeUnicodeStr(bs, t.font.value_or(""));
                writeUnicodeStr(bs, t.i.value_or(""));
                writeUnicodeStr(bs, t.text.value_or(""));
                bs.writeFloat32(optOr(t.a, kWpSentinel));
                bs.writeFloat32(optOr(t.f, kWpSentinel));
                bs.writeFloat32(optOr(t.sx, kWpSentinel));
                bs.writeFloat32(optOr(t.sy, kWpSentinel));
                bs.writeFloat32(optOr(t.kx, kWpSentinel));
                bs.writeFloat32(optOr(t.ky, kWpSentinel));
                bs.writeFloat32(optOr(t.x, kWpSentinel));
                bs.writeFloat32(optOr(t.y, kWpSentinel));
            }
        }

        auto result = bs.toByteArray();
        auto totalSize = static_cast<uint32_t>(result.size());
        result[sizePos] = static_cast<uint8_t>(totalSize);
        result[sizePos + 1] = static_cast<uint8_t>(totalSize >> 8);
        result[sizePos + 2] = static_cast<uint8_t>(totalSize >> 16);
        result[sizePos + 3] = static_cast<uint8_t>(totalSize >> 24);
        return result;
    }
};

struct JSONCodec {
    [[nodiscard]] static std::optional<Data> decode(std::span<const uint8_t> data) {
        if (data.empty()) return std::nullopt;
        auto doc = json::Document::parse({reinterpret_cast<const char*>(data.data()), data.size()});
        if (!doc) return std::nullopt;
        auto root = doc.root();
        if (!root.is_obj()) return std::nullopt;

        Data reanim;
        if (auto v = root.obj_get("doScale"); v.is_int())
            reanim.doScale = static_cast<int8_t>(v.get_sint());
        if (auto v = root.obj_get("fps"); v.is_num())
            reanim.fps = static_cast<float>(v.get_num());

        if (auto tracksVal = root.obj_get("tracks"); tracksVal.is_arr()) {
            for (auto trackJson : tracksVal.array()) {
                Track track;
                if (auto n = trackJson.obj_get("name"); n.is_str())
                    track.name.assign(n.get_str_view());
                if (auto transformsVal = trackJson.obj_get("transforms"); transformsVal.is_arr()) {
                    for (auto tJson : transformsVal.array()) {
                        Transform t;
                        auto num = [&](const char* k, std::optional<float>& out) {
                            if (auto v = tJson.obj_get(k); v.is_num()) out = static_cast<float>(v.get_num());
                        };
                        num("x", t.x); num("y", t.y); num("kx", t.kx); num("ky", t.ky);
                        num("sx", t.sx); num("sy", t.sy); num("f", t.f); num("a", t.a);
                        if (auto iVal = tJson.obj_get("i")) {
                            if (iVal.is_str()) t.i.emplace(iVal.get_str_view());
                            else if (iVal.is_int()) t.iInt = static_cast<int32_t>(iVal.get_sint());
                        }
                        auto str = [&](const char* k, std::optional<std::string>& out) {
                            if (auto v = tJson.obj_get(k); v.is_str()) out.emplace(v.get_str_view());
                        };
                        str("resource", t.resource);
                        str("i2", t.i2);
                        str("resource2", t.resource2);
                        str("font", t.font);
                        str("text", t.text);
                        track.transforms.push_back(std::move(t));
                    }
                }
                reanim.tracks.push_back(std::move(track));
            }
        }
        return reanim;
    }

    [[nodiscard]] static std::string encode(const Data& reanim) {
        json::MutDocument doc;
        auto root = doc.mut_obj();
        doc.set_root(root);
        if (reanim.doScale) doc.obj_add_int(root, "doScale", *reanim.doScale);
        doc.obj_add_real(root, "fps", reanim.fps);

        auto tracksArray = doc.mut_arr();
        for (const auto& track : reanim.tracks) {
            auto trackObj = doc.mut_obj();
            if (!track.name.empty()) doc.obj_add_str(trackObj, "name", track.name);
            auto transformsArray = doc.mut_arr();
            for (const auto& t : track.transforms) {
                auto tObj = doc.mut_obj();
                if (t.x) doc.obj_add_real(tObj, "x", *t.x);
                if (t.y) doc.obj_add_real(tObj, "y", *t.y);
                if (t.kx) doc.obj_add_real(tObj, "kx", *t.kx);
                if (t.ky) doc.obj_add_real(tObj, "ky", *t.ky);
                if (t.sx) doc.obj_add_real(tObj, "sx", *t.sx);
                if (t.sy) doc.obj_add_real(tObj, "sy", *t.sy);
                if (t.f) doc.obj_add_real(tObj, "f", *t.f);
                if (t.a) doc.obj_add_real(tObj, "a", *t.a);
                if (t.i) doc.obj_add_str(tObj, "i", *t.i);
                else if (t.iInt) doc.obj_add_int(tObj, "i", *t.iInt);
                if (t.resource) doc.obj_add_str(tObj, "resource", *t.resource);
                if (t.i2) doc.obj_add_str(tObj, "i2", *t.i2);
                if (t.resource2) doc.obj_add_str(tObj, "resource2", *t.resource2);
                if (t.font) doc.obj_add_str(tObj, "font", *t.font);
                if (t.text) doc.obj_add_str(tObj, "text", *t.text);
                transformsArray.arr_append(tObj);
            }
            trackObj.obj_add(doc.mut_str("transforms"), transformsArray);
            tracksArray.arr_append(trackObj);
        }
        root.obj_add(doc.mut_str("tracks"), tracksArray);
        return doc.write(json::WriteFlag::Pretty);
    }
};

struct XMLCodec {
    [[nodiscard]] static std::optional<Data> decode(std::span<const uint8_t> data) {
        if (data.empty()) return std::nullopt;
        std::string wrapped;
        wrapped.reserve(data.size() + 64);
        wrapped.append("<?xml version=\"1.0\" encoding=\"utf-8\"?><root>");
        wrapped.append(reinterpret_cast<const char*>(data.data()), data.size());
        wrapped.append("</root>");

        auto docOpt = xml::Document::parse(wrapped);
        if (!docOpt) return std::nullopt;
        xml::Node root = docOpt->root().child("root");
        if (!root) return std::nullopt;

        Data reanim;
        for (xml::Node node : root.children()) {
            auto nodeName = node.name();
            if (nodeName == "doScale") {
                int v = 0;
                auto tv = node.text();
                if (auto r = std::from_chars(tv.data(), tv.data() + tv.size(), v); r.ec == std::errc{})
                    reanim.doScale = static_cast<int8_t>(v);
            } else if (nodeName == "fps") {
                float v = 12.0f;
                auto tv = node.text();
                if (auto r = std::from_chars(tv.data(), tv.data() + tv.size(), v); r.ec == std::errc{})
                    reanim.fps = v;
            } else if (nodeName == "track") {
                Track track;
                for (xml::Node trackChild : node.children()) {
                    auto trackChildName = trackChild.name();
                    if (trackChildName == "name") {
                        track.name.assign(trackChild.text());
                    } else if (trackChildName == "t") {
                        Transform t;
                        for (xml::Node attr : trackChild.children()) {
                            auto attrName = attr.name();
                            auto attrValue = attr.text();
                            if (attrValue.empty()) continue;
                            auto parseF = [&](std::optional<float>& out) {
                                float v = 0;
                                if (auto r = std::from_chars(attrValue.data(), attrValue.data() + attrValue.size(), v); r.ec == std::errc{})
                                    out = v;
                            };
                            if (attrName == "x") parseF(t.x);
                            else if (attrName == "y") parseF(t.y);
                            else if (attrName == "kx") parseF(t.kx);
                            else if (attrName == "ky") parseF(t.ky);
                            else if (attrName == "sx") parseF(t.sx);
                            else if (attrName == "sy") parseF(t.sy);
                            else if (attrName == "f") parseF(t.f);
                            else if (attrName == "a") parseF(t.a);
                            else if (attrName == "i") t.i.emplace(attrValue);
                            else if (attrName == "resource") t.resource.emplace(attrValue);
                            else if (attrName == "i2") t.i2.emplace(attrValue);
                            else if (attrName == "resource2") t.resource2.emplace(attrValue);
                            else if (attrName == "font") t.font.emplace(attrValue);
                            else if (attrName == "text") t.text.emplace(attrValue);
                        }
                        track.transforms.push_back(std::move(t));
                    }
                }
                reanim.tracks.push_back(std::move(track));
            }
        }
        return reanim;
    }

    [[nodiscard]] static std::string encode(const Data& reanim) {
        std::string out;
        out.reserve(4096);
        auto appendNum = [&](std::string_view tag, float v) {
            out.push_back('<'); out.append(tag); out.push_back('>');
            char buf[64];
            auto r = std::to_chars(buf, buf + sizeof(buf), v);
            out.append(buf, r.ptr);
            out.append("</"); out.append(tag); out.push_back('>');
        };
        auto appendStr = [&](std::string_view tag, std::string_view v) {
            out.push_back('<'); out.append(tag); out.push_back('>');
            out.append(v);
            out.append("</"); out.append(tag); out.push_back('>');
        };

        if (reanim.doScale) {
            out.append("<doScale>");
            char buf[16];
            auto r = std::to_chars(buf, buf + sizeof(buf), static_cast<int>(*reanim.doScale));
            out.append(buf, r.ptr);
            out.append("</doScale>\n");
        }
        out.append("<fps>");
        {
            char buf[64];
            auto r = std::to_chars(buf, buf + sizeof(buf), reanim.fps);
            out.append(buf, r.ptr);
        }
        out.append("</fps>\n");

        for (const auto& track : reanim.tracks) {
            out.append("<track>\n");
            if (!track.name.empty()) {
                appendStr("name", track.name);
                out.push_back('\n');
            }
            for (const auto& t : track.transforms) {
                out.append("<t>");
                if (t.x) appendNum("x", *t.x);
                if (t.y) appendNum("y", *t.y);
                if (t.kx) appendNum("kx", *t.kx);
                if (t.ky) appendNum("ky", *t.ky);
                if (t.sx) appendNum("sx", *t.sx);
                if (t.sy) appendNum("sy", *t.sy);
                if (t.f) appendNum("f", *t.f);
                if (t.a) appendNum("a", *t.a);
                if (t.i) appendStr("i", *t.i);
                if (t.resource) appendStr("resource", *t.resource);
                if (t.i2) appendStr("i2", *t.i2);
                if (t.resource2) appendStr("resource2", *t.resource2);
                if (t.font) appendStr("font", *t.font);
                if (t.text) appendStr("text", *t.text);
                out.append("</t>\n");
            }
            out.append("</track>\n");
        }
        return out;
    }
};

[[nodiscard]] inline std::optional<Data> decodeBinary(std::span<const uint8_t> data, Format format) {
    if (format == Format::WP) return WPCodec::decode(data);
    return BinaryCodec::decode(data, format);
}

[[nodiscard]] inline std::vector<uint8_t> encodeBinary(const Data& reanim, Format format) {
    if (format == Format::WP) return WPCodec::encode(reanim);
    return BinaryCodec::encode(reanim, format);
}

}
