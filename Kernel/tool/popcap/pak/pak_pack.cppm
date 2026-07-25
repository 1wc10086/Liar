module;
#include <algorithm>
#include <cctype>
#include <cstdint>
#include <limits>
#include <span>
#include <stdexcept>
#include <string>
#include <string_view>
#include <utility>
#include <vector>
export module tool.popcap.pak.pack;
import utility.binary.unified_binary_stream;
import utility.io;
import tool.popcap.pak.core;
import tool.popcap.pak.utils;

export namespace Pak {

class Pack {
public:
    static void pack(std::string_view projectPath, std::string_view outputPath) {
        const std::string project(projectPath);
        const auto resourcePath = FileUtils::joinPath(project, "resource");
        if (!FileUtils::isDirectory(resourcePath)) throw std::runtime_error("Resource folder not found: " + resourcePath);

        auto definition = readDefinition(FileUtils::joinPath(project, "definition.json"));
        if (definition.xmem && definition.pc) throw std::runtime_error("XMem compression requires a non-PC PAK");
        FileUtils::createDirectory(FileUtils::getParentDirectory(std::string(outputPath)));

        ArchiveInfo archive;
        archive.definition = definition;
        archive.compressed = definition.zlib;
        const auto sourceFiles = FileUtils::collectFiles(resourcePath);
        archive.files.reserve(sourceFiles.size());
        for (const auto& source : sourceFiles) {
            auto name = source.substr(resourcePath.size());
            if (!name.empty() && (name.front() == '/' || name.front() == '\\')) name.erase(0, 1);
            std::replace(name.begin(), name.end(), definition.win ? '/' : '\\', definition.win ? '\\' : '/');
            archive.files.push_back({.name = std::move(name)});
        }

        UnifiedBinaryStream stream(UnifiedBinaryStream::Mode::Write, UnifiedBinaryStream::Endian::Little);
        archive.write(stream);
        for (size_t index = 0; index < sourceFiles.size(); ++index) {
            const auto& source = sourceFiles[index];
            auto& info = archive.files[index];
            const auto extension = FileUtils::getFileExtension(source);
            const bool isPtx = equalExtension(extension, ".ptx");
            if (!definition.pc) ArchiveInfo::writePadding(stream, definition.x360 && isPtx);
            const auto bytes = FileUtils::readFileBytes(source);
            if (bytes.size() > static_cast<size_t>(std::numeric_limits<int32_t>::max())) throw std::runtime_error("File exceeds PAK size limit: " + source);
            if (compressionFor(extension, definition) == Compression::Zlib) {
                auto compressed = zlibCompress(bytes);
                if (!compressed.empty() && compressed.size() <= static_cast<size_t>(std::numeric_limits<int32_t>::max())) {
                    stream.writeBytes(compressed);
                    info.size = static_cast<int32_t>(bytes.size());
                    info.compressedSize = static_cast<int32_t>(compressed.size());
                    continue;
                }
            }
            stream.writeBytes(bytes);
            info.compressedSize = static_cast<int32_t>(bytes.size());
        }

        stream.setPosition(0);
        archive.write(stream);
        auto payload = stream.toByteArray();
        if (definition.pc) {
            for (auto& byte : payload) byte ^= 0xF7;
        } else if (definition.xmem) {
            auto compressed = xmemCompress(payload);
            if (compressed.empty()) throw std::runtime_error("XMem compression failed");
            payload = std::move(compressed);
        }
        if (!FileUtils::writeFileBytes(std::string(outputPath), payload)) throw std::runtime_error("Cannot write PAK file");
    }
};

}
