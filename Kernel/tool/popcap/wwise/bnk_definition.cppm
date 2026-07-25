module;
#include <cstdint>
#include <stdexcept>
#include <string>
#include <string_view>
#include <utility>
#include <vector>
export module tool.popcap.wwise.bnk.definition;
import utility.json;
export import tool.popcap.wwise.bnk.core;

export namespace WwiseSoundBank::Definition {

[[nodiscard]] inline uint32_t jsonToU32(json::Value v, std::string_view key = {}) {
    if (!v) throw std::runtime_error("Missing JSON integer");
    if (v.is_uint()) return static_cast<uint32_t>(v.get_uint());
    if (v.is_int()) return static_cast<uint32_t>(v.get_sint());
    throw std::runtime_error(std::string("Invalid JSON integer: ") + std::string(key));
}

[[nodiscard]] inline uint16_t jsonToU16(json::Value v, std::string_view key = {}) {
    return static_cast<uint16_t>(jsonToU32(v, key));
}

[[nodiscard]] inline uint8_t jsonToU8(json::Value v, std::string_view key = {}) {
    return static_cast<uint8_t>(jsonToU32(v, key));
}

[[nodiscard]] inline std::string jsonToString(json::Value v, std::string_view key = {}) {
    if (!v) throw std::runtime_error("Missing JSON string");
    if (!v.is_str()) throw std::runtime_error(std::string("Invalid JSON string: ") + std::string(key));
    return std::string(v.get_str_view());
}

[[nodiscard]] inline json::Value require(json::Value obj, std::string_view key) {
    auto v = obj.obj_get(key);
    if (!v) throw std::runtime_error(std::string("Missing JSON key: ") + std::string(key));
    return v;
}

[[nodiscard]] inline std::vector<std::string> parseStringArray(json::Value arr, std::string_view key = {}) {
    if (!arr) return {};
    if (!arr.is_arr()) throw std::runtime_error(std::string("Invalid JSON array: ") + std::string(key));
    std::vector<std::string> out;
    out.reserve(arr.arr_size());
    for (auto v : arr.array()) out.emplace_back(jsonToString(v, key));
    return out;
}

[[nodiscard]] inline std::vector<uint32_t> parseU32Array(json::Value arr, std::string_view key = {}) {
    if (!arr) return {};
    if (!arr.is_arr()) throw std::runtime_error(std::string("Invalid JSON array: ") + std::string(key));
    std::vector<uint32_t> out;
    out.reserve(arr.arr_size());
    for (auto v : arr.array()) out.push_back(jsonToU32(v, key));
    return out;
}

[[nodiscard]] inline std::string envFilterValueKey(json::Value obj) {
    if (obj.obj_get("low_pass_filter_vaule")) return "low_pass_filter_vaule";
    if (obj.obj_get("high_pass_filter_vaule")) return "high_pass_filter_vaule";
    if (obj.obj_get("value")) return "value";
    throw std::runtime_error("Missing environment filter value");
}

[[nodiscard]] inline std::string envFilterPointKey(json::Value obj) {
    if (obj.obj_get("low_pass_filter_point")) return "low_pass_filter_point";
    if (obj.obj_get("high_pass_filter_point")) return "high_pass_filter_point";
    if (obj.obj_get("point")) return "point";
    throw std::runtime_error("Missing environment filter point");
}

[[nodiscard]] inline EnvironmentFilter parseEnvironmentFilter(json::Value obj) {
    const auto valueKey = envFilterValueKey(obj);
    const auto pointKey = envFilterPointKey(obj);
    return {
        .value = jsonToString(require(obj, valueKey), valueKey),
        .point = parseStringArray(require(obj, pointKey), pointKey)
    };
}

[[nodiscard]] inline EnvironmentVolume parseEnvironmentVolume(json::Value obj) {
    return {
        .volumeValue = jsonToString(require(obj, "volume_value"), "volume_value"),
        .volumePoint = parseStringArray(require(obj, "volume_point"), "volume_point")
    };
}

[[nodiscard]] inline EnvironmentItem parseEnvironmentItem(json::Value obj) {
    EnvironmentItem item;
    item.volume = parseEnvironmentVolume(require(obj, "volume"));
    item.lowPassFilter = parseEnvironmentFilter(require(obj, "low_pass_filter"));
    if (auto hp = obj.obj_get("high_pass_filter")) item.highPassFilter = parseEnvironmentFilter(hp);
    return item;
}

inline void addU32(json::MutDocument& doc, json::MutValue obj, std::string_view key, uint32_t v) {
    obj.obj_add(doc.mut_str(key), doc.mut_uint(v));
}

inline void addString(json::MutDocument& doc, json::MutValue obj, std::string_view key, std::string_view v) {
    doc.obj_add_str(obj, key, v);
}

[[nodiscard]] inline json::MutValue makeStringArray(json::MutDocument& doc, const std::vector<std::string>& values) {
    auto arr = doc.mut_arr();
    for (const auto& v : values) arr.arr_append(doc.mut_strdup(v));
    return arr;
}

[[nodiscard]] inline json::MutValue makeU32Array(json::MutDocument& doc, const std::vector<uint32_t>& values) {
    auto arr = doc.mut_arr();
    for (const auto v : values) arr.arr_append(doc.mut_uint(v));
    return arr;
}

[[nodiscard]] inline json::MutValue serializeEnvironmentFilter(json::MutDocument& doc, const EnvironmentFilter& f) {
    auto obj = doc.mut_obj();
    addString(doc, obj, "low_pass_filter_vaule", f.value);
    obj.obj_add(doc.mut_str("low_pass_filter_point"), makeStringArray(doc, f.point));
    return obj;
}

[[nodiscard]] inline std::string toJsonString(const Bank& bank) {
    json::MutDocument doc;
    auto root = doc.mut_obj();
    doc.set_root(root);

    auto header = doc.mut_obj();
    addU32(doc, header, "version", bank.header.version);
    addU32(doc, header, "id", bank.header.id);
    addU32(doc, header, "language", bank.header.language);
    addString(doc, header, "head_expand", bank.header.headExpand);
    root.obj_add(doc.mut_str("bank_header"), header);

    if (!bank.embeddedMedia.empty()) root.obj_add(doc.mut_str("embedded_media"), makeU32Array(doc, bank.embeddedMedia));

    if (bank.initialization) {
        auto arr = doc.mut_arr();
        for (const auto& e : *bank.initialization) {
            auto obj = doc.mut_obj();
            addU32(doc, obj, "id", e.id);
            addString(doc, obj, "name", e.name);
            arr.arr_append(obj);
        }
        root.obj_add(doc.mut_str("initialization"), arr);
    }

    if (bank.gameSync) {
        const auto& stmg = *bank.gameSync;
        auto obj = doc.mut_obj();
        addString(doc, obj, "volume_threshold", stmg.volumeThreshold);
        addString(doc, obj, "max_voice_instances", stmg.maxVoiceInstances);
        addU32(doc, obj, "unknown_type_1", stmg.unknownType1);

        auto stageArr = doc.mut_arr();
        for (const auto& g : stmg.stageGroup) {
            auto sg = doc.mut_obj();
            auto data = doc.mut_obj();
            addU32(doc, sg, "id", g.id);
            addString(doc, data, "default_transition_time", g.data.defaultTransitionTime);
            data.obj_add(doc.mut_str("custom_transition"), makeStringArray(doc, g.data.customTransition));
            sg.obj_add(doc.mut_str("data"), data);
            stageArr.arr_append(sg);
        }
        obj.obj_add(doc.mut_str("stage_group"), stageArr);

        auto switchArr = doc.mut_arr();
        for (const auto& g : stmg.switchGroup) {
            auto sg = doc.mut_obj();
            auto data = doc.mut_obj();
            addU32(doc, sg, "id", g.id);
            addU32(doc, data, "parameter", g.data.parameter);
            addU32(doc, data, "parameter_category", g.data.parameterCategory);
            data.obj_add(doc.mut_str("point"), makeStringArray(doc, g.data.point));
            sg.obj_add(doc.mut_str("data"), data);
            switchArr.arr_append(sg);
        }
        obj.obj_add(doc.mut_str("switch_group"), switchArr);

        auto paramArr = doc.mut_arr();
        for (const auto& p : stmg.gameParameter) {
            auto gp = doc.mut_obj();
            addU32(doc, gp, "id", p.id);
            addString(doc, gp, "data", p.data);
            paramArr.arr_append(gp);
        }
        obj.obj_add(doc.mut_str("game_parameter"), paramArr);
        addU32(doc, obj, "unknown_type_2", stmg.unknownType2);
        root.obj_add(doc.mut_str("game_synchronization"), obj);
    }

    if (bank.environments) {
        auto envsObj = doc.mut_obj();
        auto serializeItem = [&](const EnvironmentItem& item) {
            auto obj = doc.mut_obj();
            auto volume = doc.mut_obj();
            addString(doc, volume, "volume_value", item.volume.volumeValue);
            volume.obj_add(doc.mut_str("volume_point"), makeStringArray(doc, item.volume.volumePoint));
            obj.obj_add(doc.mut_str("volume"), volume);
            obj.obj_add(doc.mut_str("low_pass_filter"), serializeEnvironmentFilter(doc, item.lowPassFilter));
            if (item.highPassFilter) obj.obj_add(doc.mut_str("high_pass_filter"), serializeEnvironmentFilter(doc, *item.highPassFilter));
            return obj;
        };
        envsObj.obj_add(doc.mut_str("obstruction"), serializeItem(bank.environments->obstruction));
        envsObj.obj_add(doc.mut_str("occlusion"), serializeItem(bank.environments->occlusion));
        root.obj_add(doc.mut_str("environments"), envsObj);
    }

    if (!bank.hierarchy.empty()) {
        auto arr = doc.mut_arr();
        for (const auto& h : bank.hierarchy) {
            auto obj = doc.mut_obj();
            addU32(doc, obj, "type", h.objType);
            addU32(doc, obj, "id", h.id);
            addString(doc, obj, "data", h.data);
            arr.arr_append(obj);
        }
        root.obj_add(doc.mut_str("hierarchy"), arr);
    }

    if (bank.reference) {
        auto obj = doc.mut_obj();
        auto arr = doc.mut_arr();
        for (const auto& e : bank.reference->entries) {
            auto item = doc.mut_obj();
            addU32(doc, item, "id", e.id);
            addString(doc, item, "name", e.name);
            arr.arr_append(item);
        }
        obj.obj_add(doc.mut_str("data"), arr);
        addU32(doc, obj, "unknown_type", bank.reference->unknownType);
        root.obj_add(doc.mut_str("reference"), obj);
    }

    if (bank.platform) {
        auto obj = doc.mut_obj();
        addString(doc, obj, "platform", bank.platform->platform);
        root.obj_add(doc.mut_str("platform_setting"), obj);
    }

    return doc.write(json::WriteFlag::Pretty);
}

[[nodiscard]] inline Bank fromJsonString(std::string_view text) {
    auto doc = json::Document::parse(text, json::ReadFlag::AllowComments | json::ReadFlag::AllowTrailingCommas);
    if (!doc) throw std::runtime_error("Failed to parse definition.json");
    auto root = doc.root();
    if (!root.is_obj()) throw std::runtime_error("Invalid definition root");

    Bank bank;
    auto header = require(root, "bank_header");
    if (!header.is_obj()) throw std::runtime_error("Invalid bank_header");
    bank.header.version = jsonToU32(require(header, "version"), "version");
    bank.header.id = jsonToU32(require(header, "id"), "id");
    bank.header.language = jsonToU32(require(header, "language"), "language");
    bank.header.headExpand = jsonToString(require(header, "head_expand"), "head_expand");

    if (auto arr = root.obj_get("embedded_media")) bank.embeddedMedia = parseU32Array(arr, "embedded_media");

    if (auto arr = root.obj_get("initialization")) {
        if (!arr.is_arr()) throw std::runtime_error("Invalid initialization");
        std::vector<InitEntry> init;
        init.reserve(arr.arr_size());
        for (auto v : arr.array()) {
            if (!v.is_obj()) throw std::runtime_error("Invalid initialization item");
            init.push_back({
                .id = jsonToU32(require(v, "id"), "initialization.id"),
                .name = jsonToString(require(v, "name"), "initialization.name")
            });
        }
        bank.initialization = std::move(init);
    }

    if (auto obj = root.obj_get("game_synchronization")) {
        if (!obj.is_obj()) throw std::runtime_error("Invalid game_synchronization");
        GameSync stmg;
        stmg.volumeThreshold = jsonToString(require(obj, "volume_threshold"), "volume_threshold");
        stmg.maxVoiceInstances = jsonToString(require(obj, "max_voice_instances"), "max_voice_instances");
        stmg.unknownType1 = jsonToU16(require(obj, "unknown_type_1"), "unknown_type_1");

        auto stageArr = require(obj, "stage_group");
        if (!stageArr.is_arr()) throw std::runtime_error("Invalid stage_group");
        stmg.stageGroup.reserve(stageArr.arr_size());
        for (auto v : stageArr.array()) {
            auto data = require(v, "data");
            stmg.stageGroup.push_back({
                .id = jsonToU32(require(v, "id"), "stage_group.id"),
                .data = {
                    .defaultTransitionTime = jsonToString(require(data, "default_transition_time"), "default_transition_time"),
                    .customTransition = parseStringArray(require(data, "custom_transition"), "custom_transition")
                }
            });
        }

        auto switchArr = require(obj, "switch_group");
        if (!switchArr.is_arr()) throw std::runtime_error("Invalid switch_group");
        stmg.switchGroup.reserve(switchArr.arr_size());
        for (auto v : switchArr.array()) {
            auto data = require(v, "data");
            stmg.switchGroup.push_back({
                .id = jsonToU32(require(v, "id"), "switch_group.id"),
                .data = {
                    .parameter = jsonToU32(require(data, "parameter"), "parameter"),
                    .parameterCategory = jsonToU8(require(data, "parameter_category"), "parameter_category"),
                    .point = parseStringArray(require(data, "point"), "point")
                }
            });
        }

        auto paramArr = require(obj, "game_parameter");
        if (!paramArr.is_arr()) throw std::runtime_error("Invalid game_parameter");
        stmg.gameParameter.reserve(paramArr.arr_size());
        for (auto v : paramArr.array()) {
            stmg.gameParameter.push_back({
                .id = jsonToU32(require(v, "id"), "game_parameter.id"),
                .data = jsonToString(require(v, "data"), "game_parameter.data")
            });
        }

        stmg.unknownType2 = jsonToU32(require(obj, "unknown_type_2"), "unknown_type_2");
        bank.gameSync = std::move(stmg);
    }

    if (auto obj = root.obj_get("environments")) {
        if (!obj.is_obj()) throw std::runtime_error("Invalid environments");
        bank.environments = Environments{
            .obstruction = parseEnvironmentItem(require(obj, "obstruction")),
            .occlusion = parseEnvironmentItem(require(obj, "occlusion"))
        };
    }

    if (auto arr = root.obj_get("hierarchy")) {
        if (!arr.is_arr()) throw std::runtime_error("Invalid hierarchy");
        bank.hierarchy.reserve(arr.arr_size());
        for (auto v : arr.array()) {
            if (!v.is_obj()) throw std::runtime_error("Invalid hierarchy item");
            bank.hierarchy.push_back({
                .objType = jsonToU8(require(v, "type"), "hierarchy.type"),
                .id = jsonToU32(require(v, "id"), "hierarchy.id"),
                .data = jsonToString(require(v, "data"), "hierarchy.data")
            });
        }
    }

    if (auto obj = root.obj_get("reference")) {
        if (!obj.is_obj()) throw std::runtime_error("Invalid reference");
        Reference ref;
        auto entries = obj.obj_get("data");
        if (!entries) entries = obj.obj_get("entries");
        if (entries) {
            if (!entries.is_arr()) throw std::runtime_error("Invalid reference.data");
            ref.entries.reserve(entries.arr_size());
            for (auto v : entries.array()) {
                if (!v.is_obj()) throw std::runtime_error("Invalid reference item");
                ref.entries.push_back({
                    .id = jsonToU32(require(v, "id"), "reference.id"),
                    .name = jsonToString(require(v, "name"), "reference.name")
                });
            }
        }
        ref.unknownType = jsonToU32(require(obj, "unknown_type"), "reference.unknown_type");
        bank.reference = std::move(ref);
    }

    if (auto obj = root.obj_get("platform_setting")) {
        if (!obj.is_obj()) throw std::runtime_error("Invalid platform_setting");
        bank.platform = PlatformSetting{jsonToString(require(obj, "platform"), "platform")};
    }

    return bank;
}

} 