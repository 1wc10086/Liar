module;
#include <string>
#include <cstdint>
#include <cstddef>
#include <cstring>
#include <memory>
#include <stdexcept>
export module tool.popcap.texture.ptxpsv.ptxpsv_core;
import utility.io;
import utility.binary.unified_binary_stream;
import utility.png.png;
import tool.popcap.texture.texture_core;
export {
namespace ImagePtxPSVCodec {
    inline void decode(const std::string& in, const std::string& out) {
        auto data = FileUtils::readFileBytes(in); UnifiedBinaryStream bs(data); 
        bs.verifyBytes((const uint8_t*)"GXT\0", 4);
        if(bs.readInt32()!=0x10000003) throw std::runtime_error(""); (void)bs.readInt32(); (void)bs.readInt32(); (void)bs.readInt32();
        for(int i=0;i<9;i++) (void)bs.readInt32(); int w=bs.readUInt16(), h=bs.readUInt16(); (void)bs.readInt32();
        std::unique_ptr<ImageBitmap> bmp(DXT5_RGBA_Morton::read(bs, w, h)); bmp->save(out);
    }
    inline void encode(const std::string& in, const std::string& out) {
        std::unique_ptr<ImageBitmap> bmp(ImageBitmap::create(in)); UnifiedBinaryStream bs(UnifiedBinaryStream::Mode::Write);
        bs.writeBytes((const uint8_t*)"GXT\0", 4); bs.writeInt32(0x10000003); bs.writeInt32(1); bs.writeInt32(0x40); size_t s1=bs.getPosition(); bs.writeInt32(0);
        bs.writeInt32(0); bs.writeInt32(0); bs.writeInt32(0); bs.writeInt32(0x40); size_t s2=bs.getPosition(); bs.writeInt32(0);
        bs.writeInt32(-1); bs.writeInt32(0); bs.writeInt32(0); bs.writeInt32(-2030043136); bs.writeUInt16(bmp->getWidth()); bs.writeUInt16(bmp->getHeight()); bs.writeInt32(1);
        DXT5_RGBA_Morton::write(bs, bmp.get()); uint32_t sz = bs.getLength()-0x40; bs.setPosition(s1); bs.writeInt32(sz); bs.setPosition(s2); bs.writeInt32(sz); FileUtils::writeFileBytes(out, bs.getData());
    }
}

}
