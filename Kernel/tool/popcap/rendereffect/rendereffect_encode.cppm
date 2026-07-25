module;
#include <array>
#include <bit>
#include <cstdint>
#include <limits>
#include <stdexcept>
#include <string>
#include <string_view>
#include <vector>
export module tool.popcap.rendereffect.encode;
import tool.popcap.rendereffect.utils;

export namespace PopCap::RenderEffect::Encoder {

[[nodiscard]] inline std::vector<uint8_t> encode_object(const Effect& effect, Version version = {}) {
    validate_version(version);
    if (version.variant != 3 && !effect.block_7.empty()) throw std::runtime_error("block_7 is not available in this popfx variant");
    const auto header = header_size(version);
    const auto section_bytes = section_data_size(effect, version);
    const auto string_bytes = string_data_size(effect);
    if (header > std::numeric_limits<size_t>::max() - section_bytes || header + section_bytes > std::numeric_limits<size_t>::max() - string_bytes)
        throw std::runtime_error("popfx size overflow");
    std::vector<uint8_t> out;
    out.reserve(header + section_bytes + string_bytes);
    out.resize(header);
    std::array<SectionInfo, 8> sections{};
    const auto start = [&](uint32_t index, size_t count, uint32_t size) { sections[index] = {narrow_u32(count), narrow_u32(out.size()), size}; };
    const auto write = [&](uint32_t value) { write_u32(out, value); };
    start(0,effect.block_1.size(),block1_size); for(const auto& v:effect.block_1) { write(v.unknown_1);write(v.unknown_2);write(v.unknown_3);write(v.unknown_4);write(v.unknown_5);write(v.unknown_6); }
    start(4,effect.block_5.size(),block5_size); for(const auto& v:effect.block_5) { write(v.unknown_1);write(v.unknown_2);write(v.unknown_3);write(v.unknown_4);write(v.unknown_5);write(v.unknown_6);write(v.unknown_7); }
    start(5,effect.block_6.size(),block6_size); for(const auto& v:effect.block_6) { write(v.unknown_1);write(v.unknown_2);write(v.unknown_3);write(v.unknown_4);write(v.unknown_5); }
    start(1,effect.block_2.size(),block2_size(version)); for(const auto& v:effect.block_2) { write(v.unknown_1);write(v.unknown_2);if(version.variant==1)write(v.unknown_3); }
    std::vector<uint8_t> strings; strings.reserve(string_bytes);
    start(2,effect.block_3.size(),block3_size); for(const auto& v:effect.block_3) { write(narrow_u32(v.string.size()));write(v.unknown_2);write(narrow_u32(strings.size()));strings.insert(strings.end(),v.string.begin(),v.string.end());strings.push_back(0); }
    start(3,effect.block_4.size(),block4_size); for(const auto& v:effect.block_4) { write(v.unknown_1);write(v.unknown_2);write(v.unknown_3);write(v.unknown_4);write(v.unknown_5); }
    if(version.variant==3) { start(6,effect.block_7.size(),block7_size); for(const auto& v:effect.block_7) { write(v.unknown_1);write(v.unknown_2); } }
    start(block8_index(version),effect.block_8.size(),block8_size(version)); for(const auto& v:effect.block_8) { write(v.unknown_1);write(v.unknown_2);if(version.variant==3){write(v.unknown_4);write(v.unknown_5);write(v.unknown_3);}else write(v.unknown_3); }
    const auto strings_offset = narrow_u32(out.size()); out.insert(out.end(),strings.begin(),strings.end());
    const auto patch = [&](size_t offset, uint32_t value) { out[offset]=static_cast<uint8_t>(value); out[offset+1]=static_cast<uint8_t>(value>>8); out[offset+2]=static_cast<uint8_t>(value>>16); out[offset+3]=static_cast<uint8_t>(value>>24); };
    patch(0,magic); patch(4,version.number); size_t position=8;
    for(uint32_t i=0;i<section_count(version);++i,position+=12) { patch(position,sections[i].count);patch(position+4,sections[i].offset);patch(position+8,sections[i].size); }
    patch(position,strings_offset);
    return out;
}

[[nodiscard]] inline std::vector<uint8_t> encode_from_json(std::string_view text, Version version = {}) { return encode_object(from_json_text(text, version), version); }

}
