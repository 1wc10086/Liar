module;
#include <array>
#include <cstdlib>
#include <charconv>
#include <cstdint>
#include <optional>
#include <span>
#include <stdexcept>
#include <string>
#include <string_view>
#include <utility>
#include <vector>
export module tool.popcap.particles.decode;
import tool.popcap.particles.core;
import tool.popcap.particles.utils;
import tool.popcap.particles.definition;
import utility.binary.unified_binary_stream;
import utility.json;
import utility.xml.xml;
import tool.popcap.particles.encode;

export namespace Particles {

namespace Detail {
[[nodiscard]] inline std::optional<int32_t> namedValue(std::string_view value, const auto& names, std::string_view wrapper) {
    if (auto index = Utils::indexOf(value, names)) return index;
    if (value.starts_with(wrapper) && value.ends_with(')')) {
        int32_t result{};
        const auto body = value.substr(wrapper.size(), value.size() - wrapper.size() - 1);
        const auto [ptr, ec] = std::from_chars(body.data(), body.data() + body.size(), result);
        if (ec == std::errc{} && ptr == body.data() + body.size()) return result;
    }
    return std::nullopt;
}
[[nodiscard]] inline std::vector<TrackNode> xmlTracks(std::string_view text) {
    std::vector<TrackNode> nodes;
    auto number = [&](size_t& pos) { char* end{}; const auto value = std::strtof(text.data() + pos, &end); pos = static_cast<size_t>(end - text.data()); return value; };
    auto word = [&](size_t& pos) { const auto start = pos; while (pos < text.size() && text[pos] != ' ' && text[pos] != ',' && text[pos] != ']') ++pos; return text.substr(start, pos - start); };
    for (size_t pos = 0; pos < text.size();) {
        TrackNode node;
        if (text[pos] == '[') {
            ++pos; const auto low = number(pos); node.lowValue = low;
            if (pos < text.size() && text[pos] == ']') { node.highValue = low; node.distribution = 0; ++pos; }
            else {
                if (pos < text.size() && text[pos] == ' ') ++pos;
                const auto token = word(pos);
                if (auto dist = namedValue(token, Utils::curveTypes, "TodCurves(")) { node.distribution = *dist; if (pos < text.size() && text[pos] == ' ') ++pos; }
                else { node.distribution = 1; pos -= token.size(); }
                node.highValue = number(pos); if (pos < text.size() && text[pos] == ']') ++pos;
            }
        } else {
            node.lowValue = number(pos); node.highValue = node.lowValue; node.distribution = 1;
        }
        node.time = -10000.0F;
        if (pos < text.size() && text[pos] == ',') { ++pos; node.time = number(pos); }
        if (pos < text.size() && text[pos] == ' ') {
            ++pos; const auto token = word(pos); node.curveType = namedValue(token, Utils::curveTypes, "TodCurves(").value_or(1);
        } else node.curveType = 1;
        while (pos < text.size() && text[pos] == ' ') ++pos;
        nodes.push_back(std::move(node));
    }
    if (nodes.empty()) return nodes;
    if (nodes.front().time < -1000.0F) nodes.front().time = 0;
    if (nodes.size() != 1 && nodes.back().time < -1000.0F) nodes.back().time = 100;
    float last{}, delta{};
    for (size_t i = 0; i < nodes.size(); ++i) {
        if (nodes[i].time >= -1000.0F) { last = nodes[i].time; for (size_t next = i + 1; next < nodes.size(); ++next) if (nodes[next].time >= -1000.0F) { delta = (nodes[next].time - last) / static_cast<float>(next - i); break; } }
        else nodes[i].time = last + delta;
        nodes[i].time /= 100.0F;
    }
    return nodes;
}
inline void readTracksWp(UnifiedBinaryStream& s, Emitter& e) {
    using Utils::readTrackNodes;
    e.systemDuration=readTrackNodes(s);e.crossFadeDuration=readTrackNodes(s);e.spawnRate=readTrackNodes(s);e.spawnMinActive=readTrackNodes(s);e.spawnMaxActive=readTrackNodes(s);e.spawnMaxLaunched=readTrackNodes(s);e.emitterRadius=readTrackNodes(s);e.emitterOffsetX=readTrackNodes(s);e.emitterOffsetY=readTrackNodes(s);e.emitterBoxX=readTrackNodes(s);e.emitterBoxY=readTrackNodes(s);e.emitterSkewX=readTrackNodes(s);e.emitterSkewY=readTrackNodes(s);e.emitterPath=readTrackNodes(s);e.particleDuration=readTrackNodes(s);e.launchSpeed=readTrackNodes(s);e.launchAngle=readTrackNodes(s);e.systemRed=readTrackNodes(s);e.systemGreen=readTrackNodes(s);e.systemBlue=readTrackNodes(s);e.systemAlpha=readTrackNodes(s);e.systemBrightness=readTrackNodes(s);
}
inline void readWpFields(UnifiedBinaryStream& s, std::vector<Field>& fields) {
    const auto count=s.readInt32(); if (count<=0) return; fields.resize(static_cast<size_t>(count));
    for(auto& f:fields){f.x=Utils::readTrackNodes(s);f.y=Utils::readTrackNodes(s);if(const auto type=s.readInt32();type!=0)f.type=type;}
}
inline Data decodeBinary(std::span<const uint8_t> input, Format format, bool useZlib) {
    const auto big=format==Format::GameConsole;
    const auto phone64=format==Format::Phone64;
    const auto bytes=Definition::decompress(input,phone64,big?UnifiedBinaryStream::Endian::Big:UnifiedBinaryStream::Endian::Little,useZlib);
    UnifiedBinaryStream s(bytes,big?UnifiedBinaryStream::Endian::Big:UnifiedBinaryStream::Endian::Little);
    Data data;
    if(format==Format::WP){
        static constexpr std::array<uint8_t,6> magic{0x58,0x4e,0x42,0x6d,0x05,0};
        static constexpr std::array<uint8_t,44> info{0x01,0x24,0x53,0x65,0x78,0x79,0x2e,0x54,0x6f,0x64,0x4c,0x69,0x62,0x2e,0x53,0x65,0x78,0x79,0x50,0x61,0x72,0x74,0x69,0x63,0x6c,0x65,0x52,0x65,0x61,0x64,0x65,0x72,0x2c,0x20,0x4c,0x41,0x57,0x4e,0,0,0,0,0,0x01};
        s.verifyBytes(magic.data(), magic.size());s.readInt32();s.verifyBytes(info.data(), info.size());const auto count=s.readInt32();if(count<=0)return data;data.emitters.resize(static_cast<size_t>(count));
        for(auto& e:data.emitters){e.image=s.readStringByVarInt32Head();if(const auto v=s.readInt32();v!=0)e.imageCol=v;if(const auto v=s.readInt32();v!=0)e.imageRow=v;if(const auto v=s.readInt32();v!=1)e.imageFrames=v;if(const auto v=s.readInt32();v!=0)e.animated=v;e.particleFlags=s.readInt32();if(const auto v=s.readInt32();v!=1)e.type=v;e.name=s.readStringByVarInt32Head();e.onDuration=s.readStringByVarInt32Head();readTracksWp(s,e);readWpFields(s,e.field);readWpFields(s,e.systemField);Definition::readTracks2(s,e);}
        return data;
    }
    if(phone64){s.setPosition(12);const auto count=s.readInt32();s.setPosition(s.getPosition()+8);if(count<=0)return data;data.emitters.resize(static_cast<size_t>(count));}
    else{s.setPosition(8);const auto count=s.readInt32();s.readInt32();if(count<=0)return data;data.emitters.resize(static_cast<size_t>(count));}
    for(auto& e:data.emitters) Definition::readHeader(s,e,phone64?9:4);
    for(auto& e:data.emitters){
        if(format==Format::Phone32||phone64)e.image=Utils::imageName(s.readInt32());
        else e.image=s.readStringByInt32Head();
        if(format==Format::TV)e.imagePath=s.readStringByInt32Head();
        e.name=s.readStringByInt32Head();Definition::readTracks1(s,e);Utils::readFields(s,e.field,phone64?36:16);Utils::readFields(s,e.systemField,phone64?36:16);Definition::readTracks2(s,e);
    }
    return data;
}

inline std::vector<TrackNode> jsonTracks(json::Value value) {
    std::vector<TrackNode> out;if(!value.is_arr())return out;
    for(auto item:value.array()){TrackNode n;if(auto v=item.obj_get("Time");v&&v.is_num())n.time=static_cast<float>(v.get_num());if(auto v=item.obj_get("LowValue");v&&v.is_num())n.lowValue=static_cast<float>(v.get_num());if(auto v=item.obj_get("HighValue");v&&v.is_num())n.highValue=static_cast<float>(v.get_num());for(auto [key,target]:{std::pair{"CurveType",&n.curveType},std::pair{"Distribution",&n.distribution}}){auto v=item.obj_get(key);if(v.is_int())*target=static_cast<int32_t>(v.get_sint());else if(v.is_str())*target=Utils::indexOf(v.get_str_view(),Utils::curveTypes);}out.push_back(std::move(n));}return out;
}
inline std::vector<Field> jsonFields(json::Value value){std::vector<Field> out;if(!value.is_arr())return out;for(auto item:value.array()){Field f;auto type=item.obj_get("FieldType");if(type.is_int())f.type=static_cast<int32_t>(type.get_sint());else if(type.is_str())f.type=Utils::indexOf(type.get_str_view(),Utils::fieldTypes);f.x=jsonTracks(item.obj_get("X"));f.y=jsonTracks(item.obj_get("Y"));out.push_back(std::move(f));}return out;}
inline void jsonEmitterTracks(json::Value o, Emitter& e) {
#define P_TRACK(jsonName, member) e.member=jsonTracks(o.obj_get(jsonName))
P_TRACK("SystemDuration",systemDuration);P_TRACK("CrossFadeDuration",crossFadeDuration);P_TRACK("SpawnRate",spawnRate);P_TRACK("SpawnMinActive",spawnMinActive);P_TRACK("SpawnMaxActive",spawnMaxActive);P_TRACK("SpawnMaxLaunched",spawnMaxLaunched);P_TRACK("EmitterRadius",emitterRadius);P_TRACK("EmitterOffsetX",emitterOffsetX);P_TRACK("EmitterOffsetY",emitterOffsetY);P_TRACK("EmitterBoxX",emitterBoxX);P_TRACK("EmitterBoxY",emitterBoxY);P_TRACK("EmitterPath",emitterPath);P_TRACK("EmitterSkewX",emitterSkewX);P_TRACK("EmitterSkewY",emitterSkewY);P_TRACK("ParticleDuration",particleDuration);P_TRACK("SystemRed",systemRed);P_TRACK("SystemGreen",systemGreen);P_TRACK("SystemBlue",systemBlue);P_TRACK("SystemAlpha",systemAlpha);P_TRACK("SystemBrightness",systemBrightness);P_TRACK("LaunchSpeed",launchSpeed);P_TRACK("LaunchAngle",launchAngle);P_TRACK("ParticleRed",particleRed);P_TRACK("ParticleGreen",particleGreen);P_TRACK("ParticleBlue",particleBlue);P_TRACK("ParticleAlpha",particleAlpha);P_TRACK("ParticleBrightness",particleBrightness);P_TRACK("ParticleSpinAngle",particleSpinAngle);P_TRACK("ParticleSpinSpeed",particleSpinSpeed);P_TRACK("ParticleScale",particleScale);P_TRACK("ParticleStretch",particleStretch);P_TRACK("CollisionReflect",collisionReflect);P_TRACK("CollisionSpin",collisionSpin);P_TRACK("ClipTop",clipTop);P_TRACK("ClipBottom",clipBottom);P_TRACK("ClipLeft",clipLeft);P_TRACK("ClipRight",clipRight);P_TRACK("AnimationRate",animationRate);
#undef P_TRACK
}
inline Data decodeJson(std::string_view text) {
    auto doc=json::Document::parse(text);Data data;if(!doc||!doc.root().is_obj())return data;auto emitters=doc.root().obj_get("Emitters");if(!emitters.is_arr())return data;
    constexpr std::array flags{"RandomLaunchSpin","AlignLaunchSpin","AlignToPixel","SystemLoops","ParticleLoops","ParticlesDontFollow","RandomStartTime","DieIfOverloaded","Additive","FullScreen","SoftwareOnly","HardwareOnly"};
    for(auto o:emitters.array()){Emitter e;for(auto [key,target]:{std::pair{"Name",&e.name},std::pair{"Image",&e.image},std::pair{"ImageResource",&e.imagePath},std::pair{"OnDuration",&e.onDuration}}){auto v=o.obj_get(key);if(v&&v.is_str())*target=v.get_str_view();}for(auto [key,target]:{std::pair{"ImageCol",&e.imageCol},std::pair{"ImageRow",&e.imageRow},std::pair{"ImageFrames",&e.imageFrames},std::pair{"Animated",&e.animated}}){auto v=o.obj_get(key);if(v&&v.is_int())*target=static_cast<int32_t>(v.get_sint());}for(size_t i=0;i<flags.size();++i){auto v=o.obj_get(flags[i]);if(v&&v.is_bool()&&v.get_bool())e.particleFlags|=1<<i;}auto type=o.obj_get("EmitterType");if(type.is_int())e.type=static_cast<int32_t>(type.get_sint());else if(type.is_str())e.type=Utils::indexOf(type.get_str_view(),Utils::emitterTypes);e.field=jsonFields(o.obj_get("Field"));e.systemField=jsonFields(o.obj_get("SystemField"));jsonEmitterTracks(o,e);data.emitters.push_back(std::move(e));}return data;
}
inline Data decodeXml(std::string_view text) {
    std::string document = "<?xml version=\"1.0\" encoding=\"utf-8\"?><root>"; document += text; document += "</root>";
    auto parsed = xml::Document::parse(document); if (!parsed) return {};
    Data data;
    constexpr std::array flags{"RandomLaunchSpin","AlignLaunchSpin","AlignToPixel","SystemLoops","ParticleLoops","ParticlesDontFollow","RandomStartTime","DieIfOverloaded","Additive","FullScreen","SoftwareOnly","HardwareOnly"};
    for (auto emitterNode : parsed.value().child("root").children("Emitter")) {
        Emitter emitter;
        for (auto child : emitterNode.children()) {
            const std::string_view name = child.name(), value = child.text();
            if (name == "Name") emitter.name = value; else if (name == "Image") emitter.image = value; else if (name == "ImageResource") emitter.imagePath = value; else if (name == "OnDuration") emitter.onDuration = value;
            else if (name == "ImageCol") emitter.imageCol = std::atoi(std::string(value).c_str()); else if (name == "ImageRow") emitter.imageRow = std::atoi(std::string(value).c_str()); else if (name == "ImageFrames") emitter.imageFrames = std::atoi(std::string(value).c_str()); else if (name == "Animated") emitter.animated = std::atoi(std::string(value).c_str());
            else if (name == "EmitterType") emitter.type = namedValue(value, Utils::emitterTypes, "Emitter(");
            else { bool isFlag = false; for (size_t i = 0; i < flags.size(); ++i) if (name == flags[i]) { if (value == "1") emitter.particleFlags |= 1 << i; isFlag = true; break; } if (isFlag) continue;
#define XML_TRACK(xmlName, member) if (name == xmlName) { emitter.member = xmlTracks(value); continue; }
XML_TRACK("SystemDuration",systemDuration) XML_TRACK("CrossFadeDuration",crossFadeDuration) XML_TRACK("SpawnRate",spawnRate) XML_TRACK("SpawnMinActive",spawnMinActive) XML_TRACK("SpawnMaxActive",spawnMaxActive) XML_TRACK("SpawnMaxLaunched",spawnMaxLaunched) XML_TRACK("EmitterRadius",emitterRadius) XML_TRACK("EmitterOffsetX",emitterOffsetX) XML_TRACK("EmitterOffsetY",emitterOffsetY) XML_TRACK("EmitterBoxX",emitterBoxX) XML_TRACK("EmitterBoxY",emitterBoxY) XML_TRACK("EmitterPath",emitterPath) XML_TRACK("EmitterSkewX",emitterSkewX) XML_TRACK("EmitterSkewY",emitterSkewY) XML_TRACK("ParticleDuration",particleDuration) XML_TRACK("SystemRed",systemRed) XML_TRACK("SystemGreen",systemGreen) XML_TRACK("SystemBlue",systemBlue) XML_TRACK("SystemAlpha",systemAlpha) XML_TRACK("SystemBrightness",systemBrightness) XML_TRACK("LaunchSpeed",launchSpeed) XML_TRACK("LaunchAngle",launchAngle) XML_TRACK("ParticleRed",particleRed) XML_TRACK("ParticleGreen",particleGreen) XML_TRACK("ParticleBlue",particleBlue) XML_TRACK("ParticleAlpha",particleAlpha) XML_TRACK("ParticleBrightness",particleBrightness) XML_TRACK("ParticleSpinAngle",particleSpinAngle) XML_TRACK("ParticleSpinSpeed",particleSpinSpeed) XML_TRACK("ParticleScale",particleScale) XML_TRACK("ParticleStretch",particleStretch) XML_TRACK("CollisionReflect",collisionReflect) XML_TRACK("CollisionSpin",collisionSpin) XML_TRACK("ClipTop",clipTop) XML_TRACK("ClipBottom",clipBottom) XML_TRACK("ClipLeft",clipLeft) XML_TRACK("ClipRight",clipRight) XML_TRACK("AnimationRate",animationRate)
#undef XML_TRACK
                if (name == "Field" || name == "SystemField") { Field field; for (auto fieldNode : child.children()) { if (fieldNode.name() == std::string_view{"FieldType"}) field.type = namedValue(fieldNode.text(), Utils::fieldTypes, "Field("); else if (fieldNode.name() == std::string_view{"X"}) field.x = xmlTracks(fieldNode.text()); else if (fieldNode.name() == std::string_view{"Y"}) field.y = xmlTracks(fieldNode.text()); } (name == "Field" ? emitter.field : emitter.systemField).push_back(std::move(field)); }
            }
        }
        data.emitters.push_back(std::move(emitter));
    }
    return data;
}
}

class Decoder {
public:
    [[nodiscard]] static Data data(std::span<const uint8_t> bytes, Format format, bool useZlib = true) {
        if(format==Format::JSON)return Detail::decodeJson({reinterpret_cast<const char*>(bytes.data()),bytes.size()});
        if(format==Format::XML)return Detail::decodeXml({reinterpret_cast<const char*>(bytes.data()),bytes.size()});
        return Detail::decodeBinary(bytes,format,useZlib);
    }
};

class ParticlesDecoder {
public:
    [[nodiscard]] static std::string decode(std::span<const uint8_t> bytes, Format format, bool useXml = false, bool useZlib = true) {
        const auto parsed = Decoder::data(bytes, format, useZlib);
        return useXml ? Encoder::xml(parsed) : Encoder::json(parsed);
    }
};

class ParticlesEncoder {
public:
    [[nodiscard]] static std::vector<uint8_t> encode(std::string_view text, Format format, bool isXml = false, bool useZlib = true) {
        const auto parsed = isXml ? Detail::decodeXml(text) : Detail::decodeJson(text);
        if (format == Format::JSON) {
            const auto output = Encoder::json(parsed);
            return {output.begin(), output.end()};
        }
        if (format == Format::XML) {
            const auto output = Encoder::xml(parsed);
            return {output.begin(), output.end()};
        }
        return Encoder::binary(parsed, format, useZlib);
    }
};

}
