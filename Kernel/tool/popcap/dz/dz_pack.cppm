module;
#include <algorithm>
#include <cstdint>
#include <filesystem>
#include <optional>
#include <stdexcept>
#include <string>
#include <unordered_map>
#include <vector>

export module tool.popcap.dz.pack;

import utility.io;
import utility.binary.unified_binary_stream;
import utility.gzip.gzip_compress;
import utility.bzip2.bzip2_compress;
import utility.lzma.lzma_compress;
import tool.popcap.dz.core;
import tool.popcap.dz.utils;
import tool.popcap.dz.definition;

export namespace Dz {

class DzPacker {
public:
    static void pack(const std::string& projectFolder, const std::string& outFile) {
        const auto projectPath = std::filesystem::path(FileUtils::normalizeFsPath(projectFolder));
        auto def = readDefinition(projectPath / "definition.json");
        packResource(projectPath / def.resourceFolder, FileUtils::normalizeFsPath(outFile), def);
    }

    static void packResource(const std::filesystem::path& resourceFolder, const std::string& outFile, const DzDefinition& def = defaultDefinition()) {
        const auto inPath = std::filesystem::path(FileUtils::normalizeFsPath(resourceFolder.string()));
        const auto outPath = FileUtils::normalizeFsPath(outFile);
        if (!FileUtils::isDirectory(inPath.string())) throw std::runtime_error("Folder not found: " + inPath.string());

        auto files = FileUtils::collectFiles(inPath.string());
        if (files.empty()) return;

        UnifiedBinaryStream bs(UnifiedBinaryStream::Mode::Write, UnifiedBinaryStream::Endian::Little);
        std::vector<std::string> fileNames(files.size());
        std::vector<std::string> relativeFolders(files.size());
        std::vector<std::string> folderPool{std::string{}};
        std::unordered_map<std::string, uint16_t> folderMap{{std::string{}, 0}};

        const auto root = inPath.lexically_normal();
        for (size_t i = 0; i < files.size(); ++i) {
            const auto file = std::filesystem::path(files[i]).lexically_normal();
            fileNames[i] = file.filename().string();
            auto relFolder = file.parent_path().lexically_relative(root).string();
            if (relFolder == ".") relFolder.clear();
            relativeFolders[i] = relFolder.empty() ? std::string{} : slashToBackslash(FileUtils::normalizeFsPath(relFolder));
            if (!folderMap.contains(relativeFolders[i])) {
                folderMap.emplace(relativeFolders[i], static_cast<uint16_t>(folderPool.size()));
                folderPool.push_back(relativeFolders[i]);
            }
        }

        DtrzInfo dz;
        dz.FileNameLibrary = std::move(fileNames);
        dz.FolderNameLibrary = std::move(folderPool);
        dz.Chunks.resize(files.size());
        for (size_t i = 0; i < files.size(); ++i) {
            dz.Chunks[i] = ChunkInfo(folderMap[relativeFolders[i]], static_cast<uint16_t>(i), static_cast<uint16_t>(i));
        }

        dz.writePart1(bs);
        const auto backupOffset = bs.getPosition();
        bs.setPosition(backupOffset + (files.size() << 4));

        for (size_t i = 0; i < files.size(); ++i) {
            auto& chunk = dz.Chunks[i];
            auto relativeFile = std::filesystem::path(files[i]).lexically_normal().lexically_relative(root).string();
            FileUtils::normalizePath(relativeFile);
            auto flags = resolveFlags(def, relativeFile);
            chunk.Flags = flags;
            chunk.Offset = static_cast<int32_t>(bs.getPosition());

            const auto fileData = FileUtils::readFileBytes(files[i]);
            chunk.Size = static_cast<int32_t>(fileData.size());
            chunk.ZSize_For_Dz = static_cast<int32_t>(fileData.size());

            if (hasFlag(flags, CompressFlags::DZ)) {
                chunk.Flags = (flags & ~CompressFlags::DZ) | CompressFlags::STORE;
                bs.writeBytes(fileData);
            } else if (hasFlag(flags, CompressFlags::ZLIB)) {
                writeCompressed(bs, chunk, flags, CompressFlags::ZLIB, gzip_ns::Compressor::compress(fileData), fileData);
            } else if (hasFlag(flags, CompressFlags::BZIP)) {
                writeCompressed(bs, chunk, flags, CompressFlags::BZIP, bzip2_ns::Compressor::compress(fileData), fileData);
            } else if (hasFlag(flags, CompressFlags::ZERO)) {
                chunk.ZSize_For_Dz = 0;
            } else if (hasFlag(flags, CompressFlags::STORE)) {
                bs.writeBytes(fileData);
            } else if (hasFlag(flags, CompressFlags::LZMA)) {
                writeCompressed(bs, chunk, flags, CompressFlags::LZMA, lzma_ns::Compressor::compress(fileData), fileData);
            } else {
                chunk.Flags = flags | CompressFlags::STORE;
                bs.writeBytes(fileData);
            }
        }

        const auto currentPos = bs.getPosition();
        bs.setPosition(backupOffset);
        dz.writePart2(bs);
        bs.setPosition(currentPos);
        bs.saveToFile(outPath);
    }

private:
    static void writeCompressed(UnifiedBinaryStream& bs, ChunkInfo& chunk, CompressFlags flags, CompressFlags method, const std::optional<std::vector<uint8_t>>& compressed, const std::vector<uint8_t>& original) {
        if (compressed) {
            bs.writeBytes(*compressed);
            return;
        }
        chunk.Flags = (flags & ~method) | CompressFlags::STORE;
        bs.writeBytes(original);
    }
};

}
