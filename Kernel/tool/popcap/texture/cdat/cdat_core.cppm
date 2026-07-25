module;
#include <string>
#include <cstdint>
#include <cstring>
export module tool.popcap.texture.cdat.cdat_core;
import utility.io;
import utility.binary.unified_binary_stream;
import tool.popcap.texture.texture_core;
export {
namespace CdatUtils {
    inline constexpr uint8_t CDAT_MAGIC[11] = {0x43, 0x52, 0x59, 0x50, 0x54, 0x5F, 0x52, 0x45, 0x53, 0x0A, 0x00};
    inline constexpr const char* CDAT_KEY = "AS23DSREPLKL335KO4439032N8345NF";
}
namespace ImageCdatCodec {
    inline void decode(const std::string& inFile, const std::string& outFile) {
        auto data = FileUtils::readFileBytes(inFile); UnifiedBinaryStream bs(data); bs.verifyBytes(CdatUtils::CDAT_MAGIC, 11); (void)bs.readInt64();
        UnifiedBinaryStream outBs(UnifiedBinaryStream::Mode::Write); size_t kLen = std::strlen(CdatUtils::CDAT_KEY);
        if (data.size() >= 0x112) { for (int i=0, idx=0; i<0x100; i++) { outBs.writeUInt8(bs.readUInt8() ^ CdatUtils::CDAT_KEY[idx]); idx = (idx+1)%kLen; } }
        outBs.writeBytes(bs.readBytes(bs.getLength() - bs.getPosition())); FileUtils::writeFileBytes(outFile, outBs.getData());
    }
    inline void encode(const std::string& inFile, const std::string& outFile) {
        auto data = FileUtils::readFileBytes(inFile); UnifiedBinaryStream bs(UnifiedBinaryStream::Mode::Write);
        bs.writeBytes(CdatUtils::CDAT_MAGIC, 11); bs.writeInt64(static_cast<int64_t>(data.size())); size_t kLen = std::strlen(CdatUtils::CDAT_KEY);
        if (data.size() >= 0x100) { for (int i=0, idx=0; i<0x100; i++) { bs.writeUInt8(data[i] ^ CdatUtils::CDAT_KEY[idx]); idx = (idx+1)%kLen; } for (size_t i=0x100; i<data.size(); i++) bs.writeUInt8(data[i]); } 
        else { for (auto b : data) bs.writeUInt8(b); } FileUtils::writeFileBytes(outFile, bs.getData());
    }
}

}
