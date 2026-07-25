module;
#include <algorithm>
#include <cstdint>
#include <filesystem>
#include <memory>
#include <stdexcept>
#include <string>
#include <vector>

export module tool.popcap.dz.unpack;

import utility.io;
import utility.binary.unified_binary_stream;
import utility.gzip.gzip_uncompress;
import utility.bzip2.bzip2_uncompress;
import utility.lzma.lzma_uncompress;
import tool.popcap.dz.core;
import tool.popcap.dz.utils;
import tool.popcap.dz.definition;

export namespace Dz {

class DzUnpacker {
public:
    static void unpack(const std::string& inFile, const std::string& outFolder) {
        const auto inPath = FileUtils::normalizeFsPath(inFile);
        const auto outPath = std::filesystem::path(FileUtils::normalizeFsPath(outFolder));
        if (!FileUtils::fileExists(inPath)) throw std::runtime_error("File not found: " + inPath);

        auto mainStream = std::make_shared<UnifiedBinaryStream>(inPath, UnifiedBinaryStream::Endian::Little);
        if (mainStream->hasErrorOccurred()) throw std::runtime_error("Cannot open file: " + inPath);

        DtrzInfo dz;
        dz.read(*mainStream);

        std::vector<std::shared_ptr<UnifiedBinaryStream>> streams(dz.ArchivesCount);
        const auto archiveRoot = std::filesystem::path(inPath).parent_path();
        for (uint16_t i = 0; i < dz.ArchivesCount; ++i) {
            streams[i] = dz.ArchiveNameLibrary[i].empty()
                ? mainStream
                : std::make_shared<UnifiedBinaryStream>((archiveRoot / dz.ArchiveNameLibrary[i]).string(), UnifiedBinaryStream::Endian::Little);
        }

        DzDefinition definition = defaultDefinition();
        std::vector<std::pair<std::string, CompressFlags>> fileRules;
        const auto resourceRoot = outPath / definition.resourceFolder;
        FileUtils::createDirectory(resourceRoot.string());

        for (const auto& chunk : dz.Chunks) {
            if (chunk.ArchiveIndex >= streams.size() || !streams[chunk.ArchiveIndex]) continue;
            auto& stream = *streams[chunk.ArchiveIndex];
            const auto folderName = chunk.FolderNameIndex < dz.FolderNameLibrary.size() ? backslashToSlash(dz.FolderNameLibrary[chunk.FolderNameIndex]) : std::string{};
            auto fileName = chunk.FileNameIndex < dz.FileNameLibrary.size() ? backslashToSlash(dz.FileNameLibrary[chunk.FileNameIndex]) : std::string{};
            if (fileName.empty()) continue;

            auto relativePath = (std::filesystem::path(folderName) / fileName).string();
            FileUtils::normalizePath(relativePath);
            auto outputPath = resourceRoot / relativePath;
            if (chunk.MultiIndex != 0) {
                auto ext = outputPath.extension().string();
                outputPath = outputPath.parent_path() / (outputPath.stem().string() + "_multi_" + std::to_string(chunk.MultiIndex) + ext);
                relativePath = outputPath.lexically_relative(resourceRoot).string();
                FileUtils::normalizePath(relativePath);
            }

            stream.setPosition(static_cast<size_t>(std::max<int32_t>(0, chunk.Offset)));
            const auto remaining = stream.getLength() > stream.getPosition() ? stream.getLength() - stream.getPosition() : 0;
            auto zsize = chunk.ZSize_For_Compress;
            if (zsize < 0 || zsize > static_cast<int32_t>(remaining)) zsize = static_cast<int32_t>(remaining);

            auto outData = readChunk(stream, chunk, static_cast<size_t>(std::max(0, zsize)), remaining);
            if (!outData.empty() || chunk.Size == 0) FileUtils::writeFileBytes(outputPath.string(), outData);
            fileRules.emplace_back(relativePath, chunk.Flags);
        }

        writeDefinition(outPath / "definition.json", definition, fileRules);
    }

private:
    [[nodiscard]] static std::vector<uint8_t> readChunk(UnifiedBinaryStream& stream, const ChunkInfo& chunk, size_t zsize, size_t remaining) {
        const auto readSize = [](int32_t size, size_t max) { return static_cast<size_t>(std::clamp<int64_t>(size, 0, static_cast<int64_t>(max))); };
        if (hasFlag(chunk.Flags, CompressFlags::DZ)) return stream.readBytes(readSize(chunk.ZSize_For_Dz, remaining));
        if (hasFlag(chunk.Flags, CompressFlags::ZLIB)) {
            auto dec = gzip_ns::Decompressor::decompress(stream.readBytes(zsize), static_cast<size_t>(std::max(0, chunk.Size)));
            return dec ? std::move(*dec) : std::vector<uint8_t>{};
        }
        if (hasFlag(chunk.Flags, CompressFlags::BZIP)) {
            auto dec = bzip2_ns::Decompressor::decompress(stream.readBytes(zsize));
            return dec ? std::move(*dec) : std::vector<uint8_t>{};
        }
        if (hasFlag(chunk.Flags, CompressFlags::ZERO)) return std::vector<uint8_t>(static_cast<size_t>(std::max(0, chunk.Size)), 0);
        if (hasFlag(chunk.Flags, CompressFlags::STORE)) return stream.readBytes(readSize(chunk.Size, remaining));
        if (hasFlag(chunk.Flags, CompressFlags::LZMA)) {
            auto dec = lzma_ns::Decompressor::decompress(stream.readBytes(zsize));
            return dec ? std::move(*dec) : std::vector<uint8_t>{};
        }
        return stream.readBytes(readSize(chunk.Size, remaining));
    }
};

}
