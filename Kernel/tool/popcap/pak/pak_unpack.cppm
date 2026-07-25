module;
#include <cstdint>
#include <span>
#include <stdexcept>
#include <string>
#include <string_view>
#include <vector>
export module tool.popcap.pak.unpack;
import utility.binary.unified_binary_stream;
import utility.io;
import tool.popcap.pak.core;
import tool.popcap.pak.utils;

export namespace Pak {

class Unpack {
public:
    static void unpack(std::string_view inputPath, std::string_view projectPath) {
        auto input = FileUtils::readFileBytes(std::string(inputPath));
        if (input.size() < sizeof(int32_t)) throw std::runtime_error("PAK file not found, empty, or truncated");

        UnifiedBinaryStream probe(input, UnifiedBinaryStream::Endian::Little);
        const auto magic = probe.peekInt32();
        Definition definition;
        std::vector<uint8_t> payload;
        if (magic == kMagicPc) {
            definition.pc = true;
            payload = std::move(input);
            for (auto& byte : payload) byte ^= 0xF7;
        } else if (magic == kMagicNormal) {
            definition.pc = false;
            payload = std::move(input);
        } else if (magic == kMagicXmem) {
            definition.pc = false;
            definition.xmem = true;
            payload = xmemDecompress(input);
            if (payload.empty()) throw std::runtime_error("XMem decompression failed");
        } else if (magic == kMagicTvZip) {
            throw std::runtime_error("TV ZIP format is not supported");
        } else {
            throw std::runtime_error("Unknown PAK magic");
        }

        UnifiedBinaryStream stream(payload, UnifiedBinaryStream::Endian::Little);
        ArchiveInfo archive;
        archive.definition = definition;
        archive.read(stream);
        archive.definition.pc = definition.pc;
        archive.definition.xmem = definition.xmem;

        const std::string project(projectPath);
        const auto resourcePath = FileUtils::joinPath(project, "resource");
        FileUtils::createDirectory(resourcePath);
        for (const auto& file : archive.files) {
            if (!archive.definition.pc) {
                const bool x360Padding = ArchiveInfo::skipPadding(stream);
                archive.definition.x360 = archive.definition.x360 || x360Padding;
            }
            std::string output = FileUtils::joinPath(resourcePath, file.name);
            FileUtils::normalizePath(output);
            if (file.compressedSize < 0 || file.size < 0) throw std::runtime_error("Invalid PAK entry size");
            const auto bytes = stream.readBytes(static_cast<size_t>(file.compressedSize));
            if (stream.hasErrorOccurred()) throw std::runtime_error("Truncated PAK entry: " + file.name);
            if (file.size != 0) {
                auto decompressed = zlibDecompress(bytes, static_cast<size_t>(file.size));
                if (decompressed.empty()) throw std::runtime_error("Decompression failed for " + file.name);
                if (!FileUtils::writeFileBytes(output, decompressed)) throw std::runtime_error("Cannot write " + output);
            } else if (!FileUtils::writeFileBytes(output, bytes)) {
                throw std::runtime_error("Cannot write " + output);
            }
        }
        writeDefinition(FileUtils::joinPath(project, "definition.json"), archive.definition);
    }
};

}
