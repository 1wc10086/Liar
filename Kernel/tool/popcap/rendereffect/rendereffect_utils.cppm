module;
#include <cstdint>
#include <limits>
#include <stdexcept>
#include <string>
#include <string_view>
#include <vector>
export module tool.popcap.rendereffect.utils;
import utility.json;
export import tool.popcap.rendereffect.core;

export namespace PopCap::RenderEffect {

[[nodiscard]] inline uint32_t json_u32(json::Value object, std::string_view key, uint32_t fallback = 0) {
    const auto value = object.obj_get(key);
    if (!value) return fallback;
    if (!value.is_int()) throw std::runtime_error("Invalid integer type in popfx json");
    const auto number = value.is_uint() ? value.get_uint() : value.get_sint() < 0 ? std::numeric_limits<uint64_t>::max() : static_cast<uint64_t>(value.get_sint());
    if (number > std::numeric_limits<uint32_t>::max()) throw std::runtime_error("Invalid integer value in popfx json");
    return static_cast<uint32_t>(number);
}

[[nodiscard]] inline std::string json_string(json::Value object, std::string_view key) {
    const auto value = object.obj_get(key);
    if (!value) return {};
    if (!value.is_str()) throw std::runtime_error("Invalid string type in popfx json");
    return std::string(value.get_str_view());
}

inline void add_u32(json::MutDocument& doc, json::MutValue object, std::string_view key, uint32_t value) { object.obj_add(doc.mut_str(key), doc.mut_uint(value)); }
inline void add_string(json::MutDocument& doc, json::MutValue object, std::string_view key, std::string_view value) { object.obj_add(doc.mut_str(key), doc.mut_str(value)); }

template<class Block, class Convert>
inline void parse_blocks(json::Value root, std::string_view name, std::vector<Block>& output, Convert convert) {
    const auto array = root.obj_get(name);
    if (!array) return;
    if (!array.is_arr()) throw std::runtime_error("Invalid popfx json array");
    output.reserve(array.arr_size());
    for (const auto object : array.array()) {
        if (!object.is_obj()) throw std::runtime_error("Invalid popfx json object");
        output.push_back(convert(object));
    }
}

[[nodiscard]] inline Effect from_json_text(std::string_view text, Version version = {}) {
    validate_version(version);
    const auto doc = json::Document::parse(text);
    if (!doc || !doc.root().is_obj()) throw std::runtime_error("Invalid popfx json");
    const auto root = doc.root();
    Effect effect;
    parse_blocks(root, "block_1", effect.block_1, [](auto o) { return Block1{json_u32(o,"unknown_1"),json_u32(o,"unknown_2"),json_u32(o,"unknown_3"),json_u32(o,"unknown_4"),json_u32(o,"unknown_5"),json_u32(o,"unknown_6")}; });
    parse_blocks(root, "block_2", effect.block_2, [](auto o) { return Block2{json_u32(o,"unknown_1"),json_u32(o,"unknown_2"),json_u32(o,"unknown_3")}; });
    parse_blocks(root, "block_3", effect.block_3, [](auto o) { return Block3{json_u32(o,"unknown_2"),json_string(o,"string")}; });
    parse_blocks(root, "block_4", effect.block_4, [](auto o) { return Block4{json_u32(o,"unknown_1"),json_u32(o,"unknown_2"),json_u32(o,"unknown_3"),json_u32(o,"unknown_4"),json_u32(o,"unknown_5")}; });
    parse_blocks(root, "block_5", effect.block_5, [](auto o) { return Block5{json_u32(o,"unknown_1"),json_u32(o,"unknown_2"),json_u32(o,"unknown_3"),json_u32(o,"unknown_4"),json_u32(o,"unknown_5"),json_u32(o,"unknown_6"),json_u32(o,"unknown_7")}; });
    parse_blocks(root, "block_6", effect.block_6, [](auto o) { return Block6{json_u32(o,"unknown_1"),json_u32(o,"unknown_2"),json_u32(o,"unknown_3"),json_u32(o,"unknown_4"),json_u32(o,"unknown_5")}; });
    parse_blocks(root, "block_7", effect.block_7, [](auto o) { return Block7{json_u32(o,"unknown_1"),json_u32(o,"unknown_2")}; });
    parse_blocks(root, "block_8", effect.block_8, [](auto o) { return Block8{json_u32(o,"unknown_1"),json_u32(o,"unknown_2"),json_u32(o,"unknown_3"),json_u32(o,"unknown_4"),json_u32(o,"unknown_5")}; });
    if (version.variant != 3 && !effect.block_7.empty()) throw std::runtime_error("block_7 is not available in this popfx variant");
    return effect;
}

[[nodiscard]] inline std::string to_json_text(const Effect& effect, Version version = {}) {
    validate_version(version);
    if (version.variant != 3 && !effect.block_7.empty()) throw std::runtime_error("block_7 is not available in this popfx variant");
    json::MutDocument doc;
    auto root = doc.mut_obj();
    const auto add_blocks = [&]<class Block, class Serialize>(std::string_view name, const std::vector<Block>& blocks, Serialize serialize) {
        auto array = doc.mut_arr();
        for (const auto& block : blocks) array.arr_append(serialize(block));
        root.obj_add(doc.mut_str(name), array);
    };
    add_blocks("block_1", effect.block_1, [&](const auto& v){ auto o=doc.mut_obj(); add_u32(doc,o,"unknown_1",v.unknown_1); add_u32(doc,o,"unknown_2",v.unknown_2); add_u32(doc,o,"unknown_3",v.unknown_3); add_u32(doc,o,"unknown_4",v.unknown_4); add_u32(doc,o,"unknown_5",v.unknown_5); add_u32(doc,o,"unknown_6",v.unknown_6); return o; });
    add_blocks("block_2", effect.block_2, [&](const auto& v){ auto o=doc.mut_obj(); add_u32(doc,o,"unknown_1",v.unknown_1); add_u32(doc,o,"unknown_2",v.unknown_2); if(version.variant==1) add_u32(doc,o,"unknown_3",v.unknown_3); return o; });
    add_blocks("block_3", effect.block_3, [&](const auto& v){ auto o=doc.mut_obj(); add_u32(doc,o,"unknown_2",v.unknown_2); add_string(doc,o,"string",v.string); return o; });
    add_blocks("block_4", effect.block_4, [&](const auto& v){ auto o=doc.mut_obj(); add_u32(doc,o,"unknown_1",v.unknown_1); add_u32(doc,o,"unknown_2",v.unknown_2); add_u32(doc,o,"unknown_3",v.unknown_3); add_u32(doc,o,"unknown_4",v.unknown_4); add_u32(doc,o,"unknown_5",v.unknown_5); return o; });
    add_blocks("block_5", effect.block_5, [&](const auto& v){ auto o=doc.mut_obj(); add_u32(doc,o,"unknown_1",v.unknown_1); add_u32(doc,o,"unknown_2",v.unknown_2); add_u32(doc,o,"unknown_3",v.unknown_3); add_u32(doc,o,"unknown_4",v.unknown_4); add_u32(doc,o,"unknown_5",v.unknown_5); add_u32(doc,o,"unknown_6",v.unknown_6); add_u32(doc,o,"unknown_7",v.unknown_7); return o; });
    add_blocks("block_6", effect.block_6, [&](const auto& v){ auto o=doc.mut_obj(); add_u32(doc,o,"unknown_1",v.unknown_1); add_u32(doc,o,"unknown_2",v.unknown_2); add_u32(doc,o,"unknown_3",v.unknown_3); add_u32(doc,o,"unknown_4",v.unknown_4); add_u32(doc,o,"unknown_5",v.unknown_5); return o; });
    if (version.variant == 3) add_blocks("block_7", effect.block_7, [&](const auto& v){ auto o=doc.mut_obj(); add_u32(doc,o,"unknown_1",v.unknown_1); add_u32(doc,o,"unknown_2",v.unknown_2); return o; });
    add_blocks("block_8", effect.block_8, [&](const auto& v){ auto o=doc.mut_obj(); add_u32(doc,o,"unknown_1",v.unknown_1); add_u32(doc,o,"unknown_2",v.unknown_2); add_u32(doc,o,"unknown_3",v.unknown_3); if(version.variant==3) { add_u32(doc,o,"unknown_4",v.unknown_4); add_u32(doc,o,"unknown_5",v.unknown_5); } return o; });
    doc.set_root(root);
    auto result = doc.write(json::WriteFlag::Pretty);
    if (result.empty()) throw std::runtime_error("popfx json write failed");
    return result;
}

}
