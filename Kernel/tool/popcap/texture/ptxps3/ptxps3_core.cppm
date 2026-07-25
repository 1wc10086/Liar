module;
#include <string>
#include <cstdint>
#include <cstring>
#include <memory>
#include <stdexcept>
export module tool.popcap.texture.ptxps3.ptxps3_core;
import utility.io;
import utility.binary.unified_binary_stream;
import utility.png.png;
import tool.popcap.texture.texture_core;
export {
namespace ImagePtxPS3Codec {
    inline void decode(const std::string& in, const std::string& out) {
        auto data = FileUtils::readFileBytes(in); UnifiedBinaryStream bs(data); 
        bs.verifyBytes((const uint8_t*)"DDS ", 4);
        if (bs.readInt32()!=0x7C || bs.readInt32()!=528391) throw std::runtime_error("");
        int h=bs.readInt32(), w=bs.readInt32(); (void)bs.readInt32(); for(int i=0;i<11;i++) (void)bs.readInt32();
        bs.verifyBytes((const uint8_t*)"NVTT", 4); (void)bs.readInt32(); (void)bs.readInt32(); (void)bs.readInt32();
        bs.verifyBytes((const uint8_t*)"DXT5", 4); for(int i=0;i<5;i++) (void)bs.readInt32(); (void)bs.readInt32(); for(int i=0;i<4;i++) (void)bs.readInt32();
        std::unique_ptr<ImageBitmap> bmp(DXT5_RGBA::read(bs, w, h)); bmp->save(out);
    }
    inline void encode(const std::string& in, const std::string& out) {
        std::unique_ptr<ImageBitmap> bmp(ImageBitmap::create(in)); UnifiedBinaryStream bs(UnifiedBinaryStream::Mode::Write);
        bs.writeBytes((const uint8_t*)"DDS ", 4); bs.writeInt32(0x7C); bs.writeInt32(528391); bs.writeInt32(bmp->getHeight()); bs.writeInt32(bmp->getWidth()); bs.writeInt32(0); for(int i=0;i<11;i++) bs.writeInt32(0);
        bs.writeBytes((const uint8_t*)"NVTT", 4); bs.writeInt32(131080); bs.writeInt32(32); bs.writeInt32(4); bs.writeBytes((const uint8_t*)"DXT5", 4); for(int i=0;i<5;i++) bs.writeInt32(0); bs.writeInt32(4096); for(int i=0;i<4;i++) bs.writeInt32(0);
        DXT5_RGBA::write(bs, bmp.get()); bs.setPosition(0x14); bs.writeInt32(bmp->getWidth()*bmp->getHeight()); FileUtils::writeFileBytes(out, bs.getData());
    }
}

}
