module;
#include <cstdint>
#include <algorithm>
#include <optional>
#include <string>
#include <string_view>
#include <stdexcept>
#include <vector>
#include <utility>
export module tool.popcap.pak.core;
import utility.binary.unified_binary_stream;

export namespace Pak {

inline constexpr int32_t kMagicNormal = static_cast<int32_t>(0xBAC04AC0);
inline constexpr int32_t kMagicPc = static_cast<int32_t>(0x4D37BD37);
inline constexpr int32_t kMagicXmem = static_cast<int32_t>(0xED12F50F);
inline constexpr int32_t kMagicTvZip = 67324752;
inline constexpr int32_t kVersion = 0;
inline constexpr uint8_t kInfoEnd = 0x80;
inline constexpr int64_t kDefaultFileTime = 129146222018596744LL;

enum class Compression : uint8_t { Store, Zlib };

struct Definition {
    bool pc = true;
    bool win = true;
    bool x360 = false;
    bool xmem = false;
    bool zlib = false;
    Compression defaultCompression = Compression::Store;
    std::vector<std::pair<std::string, Compression>> compressionByExtension{
        {".ptx", Compression::Zlib},
        {".compiled", Compression::Zlib},
        {".txt", Compression::Zlib},
        {".xml", Compression::Zlib},
        {".reanim", Compression::Zlib},
    };
};

struct FileInfo {
    std::string name;
    int32_t compressedSize = 0;
    int32_t size = 0;
    int64_t timestamp = kDefaultFileTime;

    void write(UnifiedBinaryStream& stream, bool hasCompressionSize) const {
        stream.writeStringByUInt8Head(name);
        stream.writeInt32(compressedSize);
        if (hasCompressionSize) stream.writeInt32(size);
        stream.writeInt64(timestamp);
    }

    void read(UnifiedBinaryStream& stream, bool hasCompressionSize) {
        name = stream.readStringByUInt8Head();
        compressedSize = stream.readInt32();
        size = hasCompressionSize ? stream.readInt32() : 0;
        timestamp = stream.readInt64();
    }
};

class ArchiveInfo {
public:
    std::vector<FileInfo> files;
    std::optional<bool> compressed;
    Definition definition;

    void write(UnifiedBinaryStream& stream) const {
        stream.writeInt32(kMagicNormal);
        stream.writeInt32(kVersion);
        for (const auto& file : files) {
            stream.writeUInt8(0);
            file.write(stream, compressed.value_or(false));
        }
        stream.writeUInt8(kInfoEnd);
    }

    void read(UnifiedBinaryStream& stream) {
        (void)stream.readInt32();
        (void)stream.readInt32();
        const auto firstEntry = stream.getPosition();

        auto load = [&](bool hasCompressionSize) {
            files.clear();
            stream.setPosition(firstEntry);
            stream.clearError();
            while (!stream.hasErrorOccurred()) {
                if (stream.readUInt8() == kInfoEnd) return true;
                FileInfo file;
                file.read(stream, hasCompressionSize);
                if (stream.hasErrorOccurred() || file.compressedSize < 0 || file.size < 0) return false;
                files.push_back(std::move(file));
            }
            return false;
        };

        const auto probe = firstEntry + 1;
        stream.setPosition(probe);
        (void)stream.readStringByUInt8Head();
        stream.setPosition(stream.getPosition() + sizeof(int32_t) + sizeof(int32_t) + sizeof(int64_t));
        const auto nextEntry = stream.readUInt8();
        const bool compressedCandidate = !stream.hasErrorOccurred() && (nextEntry == 0 || nextEntry == kInfoEnd);
        stream.clearError();

        if (compressedCandidate && load(true)) {
            compressed = true;
        } else if (load(false)) {
            compressed = false;
        } else {
            stream.clearError();
            stream.setPosition(firstEntry);
            compressed = false;
            files.clear();
            FileInfo file;
            file.read(stream, false);
            if (stream.hasErrorOccurred() || file.compressedSize < 0) throw std::runtime_error("Invalid PAK file table");
            files.push_back(std::move(file));
        }
        definition.zlib = compressed.value_or(false);
        definition.win = true;
        for (const auto& file : files) {
            if (file.name.contains('/')) {
                definition.win = false;
                break;
            }
        }
    }

    [[nodiscard]] static bool skipPadding(UnifiedBinaryStream& stream) {
        const auto bytes = stream.readUInt16();
        stream.setPosition(stream.getPosition() + bytes);
        return bytes > 8;
    }

    static void writePadding(UnifiedBinaryStream& stream, bool x360) {
        const size_t alignment = x360 ? 0x1000 : 8;
        const size_t offset = stream.getPosition() & (alignment - 1);
        const auto padding = static_cast<uint16_t>((alignment - 2 - offset) & (alignment - 1));
        stream.writeUInt16(padding);
        for (uint16_t i = 0; i < padding; ++i) stream.writeUInt8(0);
    }
};

}
