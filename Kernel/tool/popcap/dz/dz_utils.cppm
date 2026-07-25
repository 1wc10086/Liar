module;
#include <algorithm>
#include <array>
#include <cctype>
#include <cstdint>
#include <stdexcept>
#include <string>
#include <string_view>
#include <vector>

export module tool.popcap.dz.utils;

import utility.binary.unified_binary_stream;
import tool.popcap.dz.core;

export namespace Dz {

struct ChunkInfo {
    uint16_t FolderNameIndex = 0;
    uint16_t FileNameIndex = 0;
    uint16_t ChunkIndex = 0;
    int32_t Offset = 0;
    int32_t ZSize_For_Dz = 0;
    int32_t Size = 0;
    CompressFlags Flags = CompressFlags::STORE;
    int32_t ZSize_For_Compress = 0;
    int32_t MultiIndex = 0;
    uint16_t ArchiveIndex = 0;

    ChunkInfo() = default;
    constexpr ChunkInfo(uint16_t folderNameIndex, uint16_t fileNameIndex, uint16_t chunkIndex, int32_t multiIndex = 0) noexcept
        : FolderNameIndex(folderNameIndex), FileNameIndex(fileNameIndex), ChunkIndex(chunkIndex), MultiIndex(multiIndex) {}

    void readInfo(UnifiedBinaryStream& bs) {
        Offset = bs.readInt32();
        ZSize_For_Dz = bs.readInt32();
        Size = bs.readInt32();
        Flags = static_cast<CompressFlags>(bs.readUInt16());
        ArchiveIndex = bs.readUInt16();
    }

    void writeInfo(UnifiedBinaryStream& bs) const {
        bs.writeInt32(Offset);
        bs.writeInt32(ZSize_For_Dz);
        bs.writeInt32(Size);
        bs.writeUInt16(static_cast<uint16_t>(Flags));
        bs.writeUInt16(ArchiveIndex);
    }
};

struct DtrzInfo {
    static constexpr std::array<uint8_t, 4> Magic{0x44, 0x54, 0x52, 0x5A};
    static constexpr uint8_t Version = 0;

    uint16_t FileNameNumber = 0;
    uint16_t FolderNameNumber = 0;
    std::vector<std::string> FileNameLibrary;
    std::vector<std::string> FolderNameLibrary;
    std::vector<ChunkInfo> Chunks;
    uint16_t ArchivesCount = 0;
    uint16_t ChunksCount = 0;
    std::vector<std::string> ArchiveNameLibrary;

    static std::string readCString(UnifiedBinaryStream& bs) {
        std::string out;
        while (!bs.hasErrorOccurred()) {
            const auto c = static_cast<char>(bs.readUInt8());
            if (c == '\0') break;
            out += c;
        }
        return out;
    }

    static void writeCString(UnifiedBinaryStream& bs, std::string_view s) {
        bs.writeBytes(reinterpret_cast<const uint8_t*>(s.data()), s.size());
        bs.writeUInt8(0);
    }

    void writePart1(UnifiedBinaryStream& bs) {
        FileNameNumber = static_cast<uint16_t>(FileNameLibrary.size());
        FolderNameNumber = static_cast<uint16_t>(FolderNameLibrary.size());
        bs.writeBytes(Magic.data(), Magic.size());
        bs.writeUInt16(FileNameNumber);
        bs.writeUInt16(FolderNameNumber);
        bs.writeUInt8(Version);
        for (uint16_t i = 0; i < FileNameNumber; ++i) writeCString(bs, FileNameLibrary[i]);
        for (uint16_t i = 1; i < FolderNameNumber; ++i) writeCString(bs, FolderNameLibrary[i]);

        std::ranges::sort(Chunks, {}, &ChunkInfo::FileNameIndex);
        for (const auto& chunk : Chunks) {
            bs.writeUInt16(chunk.FolderNameIndex);
            bs.writeUInt16(chunk.ChunkIndex);
            bs.writeUInt16(0xFFFF);
        }
        bs.writeUInt16(1);
        bs.writeUInt16(static_cast<uint16_t>(Chunks.size()));
    }

    void writePart2(UnifiedBinaryStream& bs) const {
        for (const auto& chunk : Chunks) chunk.writeInfo(bs);
    }

    void read(UnifiedBinaryStream& bs) {
        bs.verifyBytes(Magic.data(), Magic.size());
        FileNameNumber = bs.readUInt16();
        FolderNameNumber = bs.readUInt16();
        if (bs.readUInt8() != Version) throw std::runtime_error("DTRZ version mismatch");

        FileNameLibrary.resize(FileNameNumber);
        for (auto& name : FileNameLibrary) name = readCString(bs);

        FolderNameLibrary.assign(FolderNameNumber, {});
        for (uint16_t i = 1; i < FolderNameNumber; ++i) FolderNameLibrary[i] = readCString(bs);

        std::vector<ChunkInfo> tempChunks;
        int maxChunk = -1;
        for (uint16_t i = 0; i < FileNameNumber; ++i) {
            const auto folderIndex = bs.readUInt16();
            uint16_t next = 0;
            int multiIndex = 0;
            while ((next = bs.readUInt16()) != 0xFFFF) {
                if (bs.hasErrorOccurred()) break;
                tempChunks.emplace_back(folderIndex, i, next, multiIndex++);
                maxChunk = std::max(maxChunk, static_cast<int>(next));
            }
        }

        Chunks.assign(maxChunk < 0 ? 0 : static_cast<size_t>(maxChunk + 1), {});
        for (const auto& item : tempChunks) {
            if (item.ChunkIndex < Chunks.size()) Chunks[item.ChunkIndex] = item;
        }

        ArchivesCount = bs.readUInt16();
        if (ArchivesCount == 0) throw std::runtime_error("Invalid ArchivesCount");

        ArchiveNameLibrary.assign(ArchivesCount, {});
        std::vector<std::vector<ChunkInfo*>> chunksByArchive(ArchivesCount);
        ChunksCount = bs.readUInt16();

        for (auto& chunk : Chunks) {
            chunk.readInfo(bs);
            if (chunk.ArchiveIndex < ArchivesCount) chunksByArchive[chunk.ArchiveIndex].push_back(&chunk);
        }

        for (uint16_t i = 1; i < ArchivesCount; ++i) ArchiveNameLibrary[i] = readCString(bs);

        for (auto& chunks : chunksByArchive) {
            if (chunks.empty()) continue;
            std::ranges::sort(chunks, [](const ChunkInfo* a, const ChunkInfo* b) { return a->Offset < b->Offset; });
            for (size_t i = 1; i < chunks.size(); ++i) {
                chunks[i - 1]->ZSize_For_Compress = chunks[i]->Offset - chunks[i - 1]->Offset;
            }
            auto* last = chunks.back();
            last->ZSize_For_Compress = hasFlag(last->Flags, CompressFlags::STORE) ? last->Size : -1;
        }
    }
};

[[nodiscard]] inline std::string lowerExtension(std::string path) {
    const auto pos = path.find_last_of('.');
    if (pos == std::string::npos) return {};
    path.erase(0, pos);
    std::ranges::transform(path, path.begin(), [](unsigned char c) { return static_cast<char>(std::tolower(c)); });
    return path;
}

[[nodiscard]] inline std::string slashToBackslash(std::string s) {
    std::ranges::replace(s, '/', '\\');
    return s;
}

[[nodiscard]] inline std::string backslashToSlash(std::string s) {
    std::ranges::replace(s, '\\', '/');
    return s;
}

}
