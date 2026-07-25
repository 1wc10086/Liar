module;
#include <string>
#include <cstdint>
#include <cstring>
#include <memory>
#include <stdexcept>
export module tool.popcap.texture.ptxxbox360.ptxxbox360_core;
import utility.io;
import utility.binary.unified_binary_stream;
import utility.png.png;
import tool.popcap.texture.texture_core;
export {
namespace ImagePtxXbox360Codec {
    inline void decode(const std::string& in, const std::string& out) {
        auto data = FileUtils::readFileBytes(in); UnifiedBinaryStream bs(data, UnifiedBinaryStream::Endian::Big); bs.setPosition(data.size()-16);
        int w=bs.readInt32(), h=bs.readInt32(), blk=bs.readInt32(); if(bs.readInt32()!=1409294362) throw std::runtime_error("");
        bs.setPosition(0); std::unique_ptr<ImageBitmap> bmp(DXT5_RGBA_Padding::read(bs, w, h, blk)); bmp->save(out);
    }
    inline void encode(const std::string& in, const std::string& out) {
        std::unique_ptr<ImageBitmap> bmp(ImageBitmap::create(in)); int w=bmp->getWidth(); if(w%128!=0) w=(w/128)*128+128;
        UnifiedBinaryStream bs(UnifiedBinaryStream::Mode::Write, UnifiedBinaryStream::Endian::Big); DXT5_RGBA_Padding::write(bs, bmp.get(), w<<2);
        bs.writeInt32(bmp->getWidth()); bs.writeInt32(bmp->getHeight()); bs.writeInt32(w<<2); bs.writeInt32(1409294362); FileUtils::writeFileBytes(out, bs.getData());
    }
}

}
