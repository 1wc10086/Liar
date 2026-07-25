module;
#include <algorithm>
#include <cctype>
#include <cstdint>
#include <charconv>
#include <filesystem>
#include <string>
#include <string_view>
#include <system_error>
#include <unordered_map>
#include <utility>
#include <vector>

export module tool.popcap.dz.definition;

import utility.io;
import utility.json;
import tool.popcap.dz.core;
import tool.popcap.dz.utils;

export namespace Dz {

struct DzDefinition {
    std::string resourceFolder = "resource";
    CompressFlags defaultMethod = CompressFlags::LZMA;
    std::unordered_map<std::string, CompressFlags> extensionFlags;
    std::unordered_map<std::string, CompressFlags> fileFlags;
};

[[nodiscard]] inline std::string_view flagName(CompressFlags flag) noexcept {
    switch (flag) {
        case CompressFlags::COMBUF: return "COMBUF";
        case CompressFlags::DZ: return "DZ";
        case CompressFlags::ZLIB: return "ZLIB";
        case CompressFlags::BZIP: return "BZIP";
        case CompressFlags::MP3: return "MP3";
        case CompressFlags::JPEG: return "JPEG";
        case CompressFlags::ZERO: return "ZERO";
        case CompressFlags::STORE: return "STORE";
        case CompressFlags::LZMA: return "LZMA";
        case CompressFlags::RANDOMACCESS: return "RANDOMACCESS";
    }
    return "STORE";
}

[[nodiscard]] inline std::string flagsToString(CompressFlags flags) {
    std::string out;
    for (const auto flag : {CompressFlags::COMBUF, CompressFlags::DZ, CompressFlags::ZLIB, CompressFlags::BZIP, CompressFlags::MP3, CompressFlags::JPEG, CompressFlags::ZERO, CompressFlags::STORE, CompressFlags::LZMA, CompressFlags::RANDOMACCESS}) {
        if (!hasFlag(flags, flag)) continue;
        if (!out.empty()) out += '|';
        out += flagName(flag);
    }
    return out.empty() ? "STORE" : out;
}

[[nodiscard]] inline CompressFlags namedFlag(std::string_view s) noexcept {
    if (s == "COMBUF") return CompressFlags::COMBUF;
    if (s == "DZ") return CompressFlags::DZ;
    if (s == "ZLIB") return CompressFlags::ZLIB;
    if (s == "BZIP") return CompressFlags::BZIP;
    if (s == "MP3") return CompressFlags::MP3;
    if (s == "JPEG") return CompressFlags::JPEG;
    if (s == "ZERO") return CompressFlags::ZERO;
    if (s == "STORE") return CompressFlags::STORE;
    if (s == "LZMA") return CompressFlags::LZMA;
    if (s == "RANDOMACCESS") return CompressFlags::RANDOMACCESS;
    return CompressFlags::STORE;
}

[[nodiscard]] inline std::string upperToken(std::string_view s) {
    std::string out(s);
    std::ranges::transform(out, out.begin(), [](unsigned char c) { return static_cast<char>(std::toupper(c)); });
    return out;
}

[[nodiscard]] inline CompressFlags parseFlags(std::string_view s) {
    uint16_t numeric = 0;
    auto parsed = std::from_chars(s.data(), s.data() + s.size(), numeric);
    if (parsed.ec == std::errc{} && parsed.ptr == s.data() + s.size()) return static_cast<CompressFlags>(numeric);

    uint16_t value = 0;
    while (!s.empty()) {
        const auto pos = s.find('|');
        const auto token = upperToken(s.substr(0, pos));
        value |= static_cast<uint16_t>(namedFlag(token));
        if (pos == std::string_view::npos) break;
        s.remove_prefix(pos + 1);
    }
    return value ? static_cast<CompressFlags>(value) : CompressFlags::STORE;
}

[[nodiscard]] inline CompressFlags valueToFlags(json::Value v, CompressFlags fallback = CompressFlags::STORE) {
    if (!v) return fallback;
    if (v.is_str()) return parseFlags(v.get_str_view());
    if (v.is_int()) return static_cast<CompressFlags>(static_cast<uint16_t>(v.is_uint() ? v.get_uint() : v.get_sint()));
    return fallback;
}

[[nodiscard]] inline std::string normalizeExtensionKey(std::string_view key) {
    std::string ext(key);
    if (!ext.empty() && ext.front() != '.') ext.insert(ext.begin(), '.');
    std::ranges::transform(ext, ext.begin(), [](unsigned char c) { return static_cast<char>(std::tolower(c)); });
    return ext;
}

[[nodiscard]] inline DzDefinition defaultDefinition() {
    DzDefinition def;
    def.extensionFlags.emplace(".png", CompressFlags::STORE);
    def.extensionFlags.emplace(".jpg", CompressFlags::STORE);
    def.extensionFlags.emplace(".jpeg", CompressFlags::STORE);
    def.extensionFlags.emplace(".compiled", CompressFlags::STORE);
    def.extensionFlags.emplace(".txt", CompressFlags::ZLIB);
    return def;
}

[[nodiscard]] inline DzDefinition readDefinition(const std::filesystem::path& path) {
    auto def = defaultDefinition();
    auto data = FileUtils::readFileBytes(path.string());
    if (data.empty()) return def;

    auto doc = json::Document::parse({reinterpret_cast<const char*>(data.data()), data.size()});
    if (!doc) return def;
    auto root = doc.root();
    if (!root || !root.is_obj()) return def;

    if (auto v = root.obj_get("resource"); v && v.is_str()) def.resourceFolder = v.get_str();
    def.defaultMethod = valueToFlags(root.obj_get("defaultMethod"), def.defaultMethod);

    if (auto compress = root.obj_get("compress"); compress && compress.is_obj()) {
        def.extensionFlags.clear();
        for (auto [key, val] : compress.object()) def.extensionFlags[normalizeExtensionKey(key.get_str_view())] = valueToFlags(val);
    }

    if (auto files = root.obj_get("files"); files && files.is_arr()) {
        for (auto item : files.array()) {
            if (!item || !item.is_obj()) continue;
            auto pathVal = item.obj_get("path");
            if (!pathVal || !pathVal.is_str()) continue;
            def.fileFlags[backslashToSlash(std::string(pathVal.get_str_view()))] = valueToFlags(item.obj_get("flags"), def.defaultMethod);
        }
    }

    return def;
}

inline void writeDefinition(const std::filesystem::path& path, const DzDefinition& def, const std::vector<std::pair<std::string, CompressFlags>>& files = {}) {
    json::MutDocument doc;
    auto root = doc.mut_obj();
    doc.set_root(root);
    doc.obj_add_int(root, "version", 1);
    doc.obj_add_str(root, "resource", def.resourceFolder);
    doc.obj_add_str(root, "defaultMethod", flagsToString(def.defaultMethod));

    auto compress = doc.mut_obj();
    for (const auto& [ext, flags] : def.extensionFlags) doc.obj_add_str(compress, ext, flagsToString(flags));
    root.obj_add(doc.mut_str("compress"), compress);

    auto arr = doc.mut_arr();
    for (const auto& [file, flags] : files) {
        auto item = doc.mut_obj();
        doc.obj_add_str(item, "path", file);
        doc.obj_add_str(item, "flags", flagsToString(flags));
        arr.arr_append(item);
    }
    root.obj_add(doc.mut_str("files"), arr);

    FileUtils::writeTextFile(path.string(), doc.write(json::WriteFlag::Pretty));
}

[[nodiscard]] inline CompressFlags resolveFlags(const DzDefinition& def, std::string_view relativePath) {
    auto normalized = backslashToSlash(std::string(relativePath));
    if (auto it = def.fileFlags.find(normalized); it != def.fileFlags.end()) return it->second;
    auto ext = lowerExtension(normalized);
    if (auto it = def.extensionFlags.find(ext); it != def.extensionFlags.end()) return it->second;
    return def.defaultMethod;
}

}
