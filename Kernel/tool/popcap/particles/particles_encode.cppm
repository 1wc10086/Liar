module;
#include <array>
#include <cstdint>
#include <charconv>
#include <span>
#include <string>
#include <string_view>
#include <utility>
#include <vector>
export module tool.popcap.particles.encode;
import tool.popcap.particles.core;
import tool.popcap.particles.utils;
import tool.popcap.particles.definition;
import utility.binary.unified_binary_stream;
import utility.json;

export namespace Particles {

namespace Detail {
inline void writeTracksWp(UnifiedBinaryStream& stream, const Emitter& emitter) {
    using Utils::writeTrackNodes;
    writeTrackNodes(stream, emitter.systemDuration); writeTrackNodes(stream, emitter.crossFadeDuration);
    writeTrackNodes(stream, emitter.spawnRate); writeTrackNodes(stream, emitter.spawnMinActive);
    writeTrackNodes(stream, emitter.spawnMaxActive); writeTrackNodes(stream, emitter.spawnMaxLaunched);
    writeTrackNodes(stream, emitter.emitterRadius); writeTrackNodes(stream, emitter.emitterOffsetX);
    writeTrackNodes(stream, emitter.emitterOffsetY); writeTrackNodes(stream, emitter.emitterBoxX);
    writeTrackNodes(stream, emitter.emitterBoxY); writeTrackNodes(stream, emitter.emitterSkewX);
    writeTrackNodes(stream, emitter.emitterSkewY); writeTrackNodes(stream, emitter.emitterPath);
    writeTrackNodes(stream, emitter.particleDuration); writeTrackNodes(stream, emitter.launchSpeed);
    writeTrackNodes(stream, emitter.launchAngle); writeTrackNodes(stream, emitter.systemRed);
    writeTrackNodes(stream, emitter.systemGreen); writeTrackNodes(stream, emitter.systemBlue);
    writeTrackNodes(stream, emitter.systemAlpha); writeTrackNodes(stream, emitter.systemBrightness);
}

inline void writeWpFields(UnifiedBinaryStream& stream, const std::vector<Field>& fields) {
    stream.writeInt32(static_cast<int32_t>(fields.size()));
    for (const auto& field : fields) {
        Utils::writeTrackNodes(stream, field.x);
        Utils::writeTrackNodes(stream, field.y);
        stream.writeInt32(field.type.value_or(0));
    }
}

inline std::vector<uint8_t> encodeBinary(const Data& data, Format format, bool useZlib) {
    const auto isBigEndian = format == Format::GameConsole;
    const auto isPhone64 = format == Format::Phone64;
    const auto endian = isBigEndian ? UnifiedBinaryStream::Endian::Big : UnifiedBinaryStream::Endian::Little;
    UnifiedBinaryStream stream(UnifiedBinaryStream::Mode::Write, endian);

    if (format == Format::WP) {
        static constexpr std::array<uint8_t, 6> magic{0x58, 0x4e, 0x42, 0x6d, 0x05, 0};
        static constexpr std::array<uint8_t, 44> info{0x01,0x24,0x53,0x65,0x78,0x79,0x2e,0x54,0x6f,0x64,0x4c,0x69,0x62,0x2e,0x53,0x65,0x78,0x79,0x50,0x61,0x72,0x74,0x69,0x63,0x6c,0x65,0x52,0x65,0x61,0x64,0x65,0x72,0x2c,0x20,0x4c,0x41,0x57,0x4e,0,0,0,0,0,0x01};
        stream.writeBytes(magic.data(), magic.size());
        const auto sizePosition = stream.getPosition();
        stream.writeInt32(0);
        stream.writeBytes(info.data(), info.size());
        stream.writeInt32(static_cast<int32_t>(data.emitters.size()));
        for (const auto& emitter : data.emitters) {
            stream.writeStringByVarInt32Head(emitter.image);
            stream.writeInt32(emitter.imageCol.value_or(0)); stream.writeInt32(emitter.imageRow.value_or(0));
            stream.writeInt32(emitter.imageFrames.value_or(1)); stream.writeInt32(emitter.animated.value_or(0));
            stream.writeInt32(emitter.particleFlags); stream.writeInt32(emitter.type.value_or(1));
            stream.writeStringByVarInt32Head(emitter.name); stream.writeStringByVarInt32Head(emitter.onDuration);
            writeTracksWp(stream, emitter); writeWpFields(stream, emitter.field); writeWpFields(stream, emitter.systemField); Definition::writeTracks2(stream, emitter);
        }
        const auto endPosition = stream.getPosition();
        stream.setPosition(sizePosition); stream.writeInt32(static_cast<int32_t>(endPosition)); stream.setPosition(endPosition);
        return Definition::compress(stream.toByteArray(), false, endian, useZlib);
    }

    if (isPhone64) {
        stream.writeInt32(-527264279); stream.writeInt32(0); stream.writeInt32(0);
        stream.writeInt32(static_cast<int32_t>(data.emitters.size())); stream.writeInt32(0); stream.writeInt32(0x2b0);
    } else {
        stream.writeInt32(format == Format::TV ? 0 : 1092589901); stream.writeInt32(0);
        stream.writeInt32(static_cast<int32_t>(data.emitters.size())); stream.writeInt32(0x164);
    }
    for (const auto& emitter : data.emitters) Definition::writeHeader(stream, emitter, isPhone64 ? 9 : 4);
    for (const auto& emitter : data.emitters) {
        if (format == Format::Phone32 || isPhone64) stream.writeInt32(Utils::imageId(emitter.image));
        else stream.writeStringByInt32Head(emitter.image);
        if (format == Format::TV) stream.writeStringByInt32Head(emitter.imagePath);
        stream.writeStringByInt32Head(emitter.name);
        Definition::writeTracks1(stream, emitter);
        Utils::writeFields(stream, emitter.field, isPhone64 ? 0x28 : 0x14, isPhone64 ? 9 : 4);
        Utils::writeFields(stream, emitter.systemField, isPhone64 ? 0x28 : 0x14, isPhone64 ? 9 : 4);
        Definition::writeTracks2(stream, emitter);
    }
    return Definition::compress(stream.toByteArray(), isPhone64, endian, useZlib);
}
inline json::MutValue jsonTracks(json::MutDocument& d,const std::vector<TrackNode>& nodes){auto a=d.mut_arr();for(const auto& n:nodes){auto o=d.mut_obj();d.obj_add_real(o,"Time",n.time);if(n.lowValue)d.obj_add_real(o,"LowValue",*n.lowValue);if(n.highValue)d.obj_add_real(o,"HighValue",*n.highValue);for(auto [key,value]:{std::pair{"CurveType",n.curveType},std::pair{"Distribution",n.distribution}})if(value){if(*value>=0&&*value<static_cast<int32_t>(Utils::curveTypes.size()))d.obj_add_str(o,key,Utils::curveTypes[*value]);else d.obj_add_int(o,key,*value);}a.arr_append(o);}return a;}
inline json::MutValue jsonFields(json::MutDocument& d,const std::vector<Field>& fields){auto a=d.mut_arr();for(const auto& f:fields){auto o=d.mut_obj();if(f.type){if(*f.type>=0&&*f.type<static_cast<int32_t>(Utils::fieldTypes.size()))d.obj_add_str(o,"FieldType",Utils::fieldTypes[*f.type]);else d.obj_add_int(o,"FieldType",*f.type);}if(!f.x.empty())o.obj_add(d.mut_str("X"),jsonTracks(d,f.x));if(!f.y.empty())o.obj_add(d.mut_str("Y"),jsonTracks(d,f.y));a.arr_append(o);}return a;}
inline void jsonEmitterTracks(json::MutDocument& d,json::MutValue o,const Emitter& e){auto add=[&](std::string_view n,const auto& v){if(!v.empty())o.obj_add(d.mut_str(n),jsonTracks(d,v));};
#define P_TRACK(jsonName, member) add(jsonName,e.member)
P_TRACK("SystemDuration",systemDuration);P_TRACK("CrossFadeDuration",crossFadeDuration);P_TRACK("SpawnRate",spawnRate);P_TRACK("SpawnMinActive",spawnMinActive);P_TRACK("SpawnMaxActive",spawnMaxActive);P_TRACK("SpawnMaxLaunched",spawnMaxLaunched);P_TRACK("EmitterRadius",emitterRadius);P_TRACK("EmitterOffsetX",emitterOffsetX);P_TRACK("EmitterOffsetY",emitterOffsetY);P_TRACK("EmitterBoxX",emitterBoxX);P_TRACK("EmitterBoxY",emitterBoxY);P_TRACK("EmitterPath",emitterPath);P_TRACK("EmitterSkewX",emitterSkewX);P_TRACK("EmitterSkewY",emitterSkewY);P_TRACK("ParticleDuration",particleDuration);P_TRACK("SystemRed",systemRed);P_TRACK("SystemGreen",systemGreen);P_TRACK("SystemBlue",systemBlue);P_TRACK("SystemAlpha",systemAlpha);P_TRACK("SystemBrightness",systemBrightness);P_TRACK("LaunchSpeed",launchSpeed);P_TRACK("LaunchAngle",launchAngle);P_TRACK("ParticleRed",particleRed);P_TRACK("ParticleGreen",particleGreen);P_TRACK("ParticleBlue",particleBlue);P_TRACK("ParticleAlpha",particleAlpha);P_TRACK("ParticleBrightness",particleBrightness);P_TRACK("ParticleSpinAngle",particleSpinAngle);P_TRACK("ParticleSpinSpeed",particleSpinSpeed);P_TRACK("ParticleScale",particleScale);P_TRACK("ParticleStretch",particleStretch);P_TRACK("CollisionReflect",collisionReflect);P_TRACK("CollisionSpin",collisionSpin);P_TRACK("ClipTop",clipTop);P_TRACK("ClipBottom",clipBottom);P_TRACK("ClipLeft",clipLeft);P_TRACK("ClipRight",clipRight);P_TRACK("AnimationRate",animationRate);
#undef P_TRACK
}
inline std::string encodeJson(const Data& data){json::MutDocument d;auto root=d.mut_obj(),emitters=d.mut_arr();d.set_root(root);constexpr std::array flags{"RandomLaunchSpin","AlignLaunchSpin","AlignToPixel","SystemLoops","ParticleLoops","ParticlesDontFollow","RandomStartTime","DieIfOverloaded","Additive","FullScreen","SoftwareOnly","HardwareOnly"};for(const auto& e:data.emitters){auto o=d.mut_obj();for(auto [key,value]:{std::pair<std::string_view,std::string_view>{"Name",e.name},{"Image",e.image},{"ImageResource",e.imagePath},{"OnDuration",e.onDuration}})if(!value.empty())d.obj_add_str(o,key,value);for(auto [key,value]:{std::pair{"ImageCol",e.imageCol},std::pair{"ImageRow",e.imageRow},std::pair{"ImageFrames",e.imageFrames},std::pair{"Animated",e.animated}})if(value)d.obj_add_int(o,key,*value);for(size_t i=0;i<flags.size();++i)if(e.particleFlags&(1<<i))d.obj_add_bool(o,flags[i],true);if(e.type){if(*e.type>=0&&*e.type<static_cast<int32_t>(Utils::emitterTypes.size()))d.obj_add_str(o,"EmitterType",Utils::emitterTypes[*e.type]);else d.obj_add_int(o,"EmitterType",*e.type);}jsonEmitterTracks(d,o,e);if(!e.field.empty())o.obj_add(d.mut_str("Field"),jsonFields(d,e.field));if(!e.systemField.empty())o.obj_add(d.mut_str("SystemField"),jsonFields(d,e.systemField));emitters.arr_append(o);}root.obj_add(d.mut_str("Emitters"),emitters);return d.write(json::WriteFlag::Pretty);}
inline void appendFloat(std::string& out, float value) { char buffer[32]; const auto [end, ec] = std::to_chars(buffer, buffer + sizeof(buffer), value); if (ec == std::errc{}) out.append(buffer, end); }
inline void appendTracks(std::string& out, const std::vector<TrackNode>& track) {
    for (size_t i = 0; i < track.size(); ++i) {
        if (i) out += ' ';
        const auto& node = track[i];
        const float low = node.lowValue.value_or(0.0F);
        const float high = node.highValue.value_or(0.0F);
        const int32_t distribution = node.distribution.value_or(1);
        const int32_t curve = node.curveType.value_or(1);

        if (low == high && distribution == 1) {
            appendFloat(out, low);
        } else {
            out += '[';
            appendFloat(out, low);
            if (distribution != 0) {
                out += ' ';
                if (distribution >= 0 && distribution < static_cast<int32_t>(Utils::curveTypes.size())) out += Utils::curveTypes[distribution];
                else { out += "TodCurves("; out += std::to_string(distribution); out += ')'; }
                out += ' ';
                appendFloat(out, high);
            }
            out += ']';
        }
        if (node.time != 0.0F && node.time != 1.0F) { out += ','; appendFloat(out, node.time * 100.0F); }
        if (curve != 1) {
            out += ' ';
            if (curve >= 0 && curve < static_cast<int32_t>(Utils::curveTypes.size())) out += Utils::curveTypes[curve];
            else { out += "TodCurves("; out += std::to_string(curve); out += ')'; }
        }
    }
}
inline void appendElement(std::string& out, std::string_view name, std::string_view value) { if (!value.empty()) { out += "  <"; out += name; out += '>'; out += value; out += "</"; out += name; out += ">\n"; } }
inline void appendTrackElement(std::string& out, std::string_view name, const std::vector<TrackNode>& track) { if (!track.empty()) { out += "  <"; out += name; out += '>'; appendTracks(out, track); out += "</"; out += name; out += ">\n"; } }
inline std::string encodeXml(const Data& data) { std::string out; constexpr std::array flags{"RandomLaunchSpin","AlignLaunchSpin","AlignToPixel","SystemLoops","ParticleLoops","ParticlesDontFollow","RandomStartTime","DieIfOverloaded","Additive","FullScreen","SoftwareOnly","HardwareOnly"}; for (const auto& e : data.emitters) { out += "<Emitter>\n"; appendElement(out,"Name",e.name);appendElement(out,"Image",e.image);appendElement(out,"ImageResource",e.imagePath);if(e.type){out += "  <EmitterType>";if(*e.type>=0&&*e.type<static_cast<int32_t>(Utils::emitterTypes.size()))out+=Utils::emitterTypes[*e.type];else out += "Emitter("+std::to_string(*e.type)+")";out += "</EmitterType>\n";} for(size_t i=0;i<flags.size();++i)if(e.particleFlags&(1<<i))appendElement(out,flags[i],"1");appendElement(out,"OnDuration",e.onDuration);
#define P_XML(name, member) appendTrackElement(out,name,e.member)
P_XML("SystemDuration",systemDuration);P_XML("CrossFadeDuration",crossFadeDuration);P_XML("SpawnRate",spawnRate);P_XML("SpawnMinActive",spawnMinActive);P_XML("SpawnMaxActive",spawnMaxActive);P_XML("SpawnMaxLaunched",spawnMaxLaunched);P_XML("EmitterRadius",emitterRadius);P_XML("EmitterOffsetX",emitterOffsetX);P_XML("EmitterOffsetY",emitterOffsetY);P_XML("EmitterBoxX",emitterBoxX);P_XML("EmitterBoxY",emitterBoxY);P_XML("EmitterPath",emitterPath);P_XML("EmitterSkewX",emitterSkewX);P_XML("EmitterSkewY",emitterSkewY);P_XML("ParticleDuration",particleDuration);P_XML("SystemRed",systemRed);P_XML("SystemGreen",systemGreen);P_XML("SystemBlue",systemBlue);P_XML("SystemAlpha",systemAlpha);P_XML("SystemBrightness",systemBrightness);P_XML("LaunchSpeed",launchSpeed);P_XML("LaunchAngle",launchAngle);P_XML("ParticleRed",particleRed);P_XML("ParticleGreen",particleGreen);P_XML("ParticleBlue",particleBlue);P_XML("ParticleAlpha",particleAlpha);P_XML("ParticleBrightness",particleBrightness);P_XML("ParticleSpinAngle",particleSpinAngle);P_XML("ParticleSpinSpeed",particleSpinSpeed);P_XML("ParticleScale",particleScale);P_XML("ParticleStretch",particleStretch);P_XML("CollisionReflect",collisionReflect);P_XML("CollisionSpin",collisionSpin);P_XML("ClipTop",clipTop);P_XML("ClipBottom",clipBottom);P_XML("ClipLeft",clipLeft);P_XML("ClipRight",clipRight);P_XML("AnimationRate",animationRate);
#undef P_XML
for(auto [name,fields]:{std::pair<std::string_view,const std::vector<Field>&>{"Field",e.field},{"SystemField",e.systemField}})for(const auto& field:fields){out += "  <";out += name;out += ">\n";if(field.type){out += "    <FieldType>";if(*field.type>=0&&*field.type<static_cast<int32_t>(Utils::fieldTypes.size()))out+=Utils::fieldTypes[*field.type];else out += "Field("+std::to_string(*field.type)+")";out += "</FieldType>\n";}if(!field.x.empty()){out += "    <X>";appendTracks(out,field.x);out += "</X>\n";}if(!field.y.empty()){out += "    <Y>";appendTracks(out,field.y);out += "</Y>\n";}out += "  </";out += name;out += ">\n";}out += "</Emitter>\n";}return out;}
}

class Encoder {
public:
    [[nodiscard]] static std::vector<uint8_t> binary(const Data& data,Format format,bool useZlib=true){return Detail::encodeBinary(data,format,useZlib);}
    [[nodiscard]] static std::string json(const Data& data){return Detail::encodeJson(data);}
    [[nodiscard]] static std::string xml(const Data& data){return Detail::encodeXml(data);}
};

}
