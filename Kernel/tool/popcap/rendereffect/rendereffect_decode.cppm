module;
#include <array>
#include <cstdint>
#include <span>
#include <stdexcept>
#include <string>
#include <vector>
export module tool.popcap.rendereffect.decode;
import tool.popcap.rendereffect.utils;

export namespace PopCap::RenderEffect::Decoder {

[[nodiscard]] inline Effect decode_object(std::span<const uint8_t> data, Version version = {}) {
    validate_version(version);
    if (data.size() < header_size(version)) throw std::runtime_error("Invalid popfx size");
    if (read_u32(data, 0) != magic) throw std::runtime_error("Invalid popfx magic");
    if (read_u32(data, 4) != version.number) throw std::runtime_error("Invalid popfx version");
    std::array<SectionInfo, 8> sections{};
    size_t position = 8;
    for (uint32_t i = 0; i < section_count(version); ++i, position += 12)
        sections[i] = {read_u32(data, position), read_u32(data, position + 4), read_u32(data, position + 8)};
    const auto string_offset = read_u32(data, position);
    if (string_offset > data.size()) throw std::runtime_error("Invalid popfx string chunk offset");
    const auto strings = data.subspan(string_offset);
    const auto validate = [&](const SectionInfo& section, uint32_t size) {
        if (section.size != size || static_cast<uint64_t>(section.offset) + static_cast<uint64_t>(section.count) * size > data.size())
            throw std::runtime_error("Invalid popfx block range");
    };
    const auto base_of = [](const SectionInfo& section, uint32_t index, uint32_t size) { return static_cast<size_t>(section.offset) + static_cast<size_t>(index) * size; };
    Effect effect;
    {
        const auto& s = sections[0]; validate(s, block1_size); effect.block_1.resize(s.count);
        for (uint32_t i=0;i<s.count;++i) { auto& v=effect.block_1[i]; const auto b=base_of(s,i,block1_size); v={read_u32(data,b),read_u32(data,b+4),read_u32(data,b+8),read_u32(data,b+12),read_u32(data,b+16),read_u32(data,b+20)}; }
    }
    {
        const auto& s = sections[1]; const auto size=block2_size(version); validate(s,size); effect.block_2.resize(s.count);
        for (uint32_t i=0;i<s.count;++i) { auto& v=effect.block_2[i]; const auto b=base_of(s,i,size); v.unknown_1=read_u32(data,b); v.unknown_2=read_u32(data,b+4); if(version.variant==1) v.unknown_3=read_u32(data,b+8); }
    }
    {
        const auto& s = sections[2]; validate(s,block3_size); effect.block_3.resize(s.count);
        for (uint32_t i=0;i<s.count;++i) { auto& v=effect.block_3[i]; const auto b=base_of(s,i,block3_size); const auto length=read_u32(data,b); v.unknown_2=read_u32(data,b+4); v.string=read_c_string(strings,read_u32(data,b+8)); if(v.string.size()!=length) throw std::runtime_error("Invalid popfx string size"); }
    }
    {
        const auto& s = sections[3]; validate(s,block4_size); effect.block_4.resize(s.count);
        for (uint32_t i=0;i<s.count;++i) { auto& v=effect.block_4[i]; const auto b=base_of(s,i,block4_size); v={read_u32(data,b),read_u32(data,b+4),read_u32(data,b+8),read_u32(data,b+12),read_u32(data,b+16)}; }
    }
    {
        const auto& s = sections[4]; validate(s,block5_size); effect.block_5.resize(s.count);
        for (uint32_t i=0;i<s.count;++i) { auto& v=effect.block_5[i]; const auto b=base_of(s,i,block5_size); v={read_u32(data,b),read_u32(data,b+4),read_u32(data,b+8),read_u32(data,b+12),read_u32(data,b+16),read_u32(data,b+20),read_u32(data,b+24)}; }
    }
    {
        const auto& s = sections[5]; validate(s,block6_size); effect.block_6.resize(s.count);
        for (uint32_t i=0;i<s.count;++i) { auto& v=effect.block_6[i]; const auto b=base_of(s,i,block6_size); v={read_u32(data,b),read_u32(data,b+4),read_u32(data,b+8),read_u32(data,b+12),read_u32(data,b+16)}; }
    }
    if (version.variant == 3) {
        const auto& s = sections[6]; validate(s,block7_size); effect.block_7.resize(s.count);
        for (uint32_t i=0;i<s.count;++i) { auto& v=effect.block_7[i]; const auto b=base_of(s,i,block7_size); v={read_u32(data,b),read_u32(data,b+4)}; }
    }
    {
        const auto& s = sections[block8_index(version)]; const auto size=block8_size(version); validate(s,size); effect.block_8.resize(s.count);
        for (uint32_t i=0;i<s.count;++i) { auto& v=effect.block_8[i]; const auto b=base_of(s,i,size); v.unknown_1=read_u32(data,b); v.unknown_2=read_u32(data,b+4); v.unknown_3=read_u32(data,b+8); if(version.variant==3) { v.unknown_4=read_u32(data,b+8); v.unknown_5=read_u32(data,b+12); v.unknown_3=read_u32(data,b+16); } }
    }
    return effect;
}

[[nodiscard]] inline std::string decode_to_json(std::span<const uint8_t> data, Version version = {}) { return to_json_text(decode_object(data, version), version); }

}
