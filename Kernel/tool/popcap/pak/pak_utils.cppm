module;
#include <algorithm>
#include <cctype>
#include <cstdint>
#include <cstdlib>
#include <optional>
#include <span>
#include <stdexcept>
#include <string>
#include <string_view>
#include <vector>
#include "lib/XMemCompression/XMemCompression/XMemCompression.h"
export module tool.popcap.pak.utils;
import utility.io;
import utility.json;
import utility.zlib.zlib_compress;
import utility.zlib.zlib_uncompress;
import tool.popcap.pak.core;

export namespace Pak {

[[nodiscard]] inline bool equalExtension(std::string_view lhs, std::string_view rhs) noexcept {
    return lhs.size() == rhs.size() && std::equal(lhs.begin(), lhs.end(), rhs.begin(), [](unsigned char a, unsigned char b) {
        return std::tolower(a) == std::tolower(b);
    });
}

[[nodiscard]] inline Compression compressionFor(std::string_view extension, const Definition& definition) noexcept {
    if (!definition.zlib) return Compression::Store;
    for (const auto& [candidate, compression] : definition.compressionByExtension) {
        if (equalExtension(extension, candidate)) return compression;
    }
    return definition.defaultCompression;
}

[[nodiscard]] inline std::optional<Compression> compressionFromString(std::string_view value) noexcept {
    const auto equal = [value](std::string_view candidate) {
        return value.size() == candidate.size() && std::equal(value.begin(), value.end(), candidate.begin(), [](unsigned char a, unsigned char b) {
            return std::tolower(a) == std::tolower(b);
        });
    };
    if (equal("store")) return Compression::Store;
    if (equal("zlib")) return Compression::Zlib;
    return std::nullopt;
}

[[nodiscard]] inline std::string_view compressionToString(Compression value) noexcept {
    return value == Compression::Zlib ? "zlib" : "store";
}

[[nodiscard]] inline std::vector<uint8_t> zlibCompress(std::span<const uint8_t> input) {
    const auto output = zlib_ns::Compressor::compress(input);
    return output.value_or(std::vector<uint8_t>{});
}

[[nodiscard]] inline std::vector<uint8_t> zlibDecompress(std::span<const uint8_t> input, size_t expectedSize) {
    const auto output = zlib_ns::Decompressor::decompress(input, expectedSize);
    return output.value_or(std::vector<uint8_t>{});
}

[[nodiscard]] inline std::vector<uint8_t> xmemCompress(std::span<const uint8_t> input) {
    if (input.empty()) return {};
    uint8_t* raw = nullptr;
    size_t size = 0;
    const auto result = XMemCompressLzxTdBuffer(input.data(), input.size(), &raw, &size);
    if (result != 0 || !raw) return {};
    std::vector<uint8_t> output(raw, raw + size);
    std::free(raw);
    return output;
}

[[nodiscard]] inline std::vector<uint8_t> xmemDecompress(std::span<const uint8_t> input) {
    if (input.empty()) return {};
    uint8_t* raw = nullptr;
    size_t size = 0;
    const auto result = XMemDecompressLzxTdBuffer(input.data(), input.size(), &raw, &size);
    if (result != 0 || !raw) return {};
    std::vector<uint8_t> output(raw, raw + size);
    std::free(raw);
    return output;
}

[[nodiscard]] inline Definition readDefinition(std::string_view path) {
    const auto bytes = FileUtils::readFileBytes(std::string(path));
    if (bytes.empty()) return {};
    const auto doc = json::Document::parse({reinterpret_cast<const char*>(bytes.data()), bytes.size()});
    if (!doc || !doc.root().is_obj()) throw std::runtime_error("Invalid definition.json");
    const auto root = doc.root();
    const auto boolean = [root](std::string_view key, bool fallback) {
        const auto value = root.obj_get(key);
        return value && value.is_bool() ? value.get_bool() : fallback;
    };
    Definition definition{
        .pc = boolean("pc", true),
        .win = boolean("windowsPathSeparate", true),
        .x360 = boolean("xbox360PtxAlign", false),
        .xmem = boolean("xmemCompress", false),
        .zlib = boolean("zlibCompress", false),
    };
    if (const auto value = root.obj_get("defaultCompression"); value && value.is_str()) {
        if (const auto compression = compressionFromString(value.get_str_view())) definition.defaultCompression = *compression;
    }
    if (const auto rules = root.obj_get("compressionByExtension"); rules && rules.is_obj()) {
        definition.compressionByExtension.clear();
        for (const auto [extension, value] : rules.object()) {
            if (!extension.is_str() || !value.is_str()) continue;
            if (const auto compression = compressionFromString(value.get_str_view()))
                definition.compressionByExtension.emplace_back(extension.get_str_view(), *compression);
        }
    }
    return definition;
}

inline void writeDefinition(std::string_view path, const Definition& definition) {
    json::MutDocument doc;
    auto root = doc.mut_obj();
    doc.set_root(root);
    doc.obj_add_bool(root, "pc", definition.pc);
    doc.obj_add_bool(root, "windowsPathSeparate", definition.win);
    doc.obj_add_bool(root, "xbox360PtxAlign", definition.x360);
    doc.obj_add_bool(root, "xmemCompress", definition.xmem);
    doc.obj_add_bool(root, "zlibCompress", definition.zlib);
    doc.obj_add_str(root, "defaultCompression", compressionToString(definition.defaultCompression));
    const auto rules = doc.mut_obj();
    for (const auto& [extension, compression] : definition.compressionByExtension)
        doc.obj_add_str(rules, extension, compressionToString(compression));
    root.obj_add(doc.mut_str("compressionByExtension"), rules);
    if (!FileUtils::writeTextFile(std::string(path), doc.write(json::WriteFlag::Pretty)))
        throw std::runtime_error("Cannot write definition.json");
}

}
