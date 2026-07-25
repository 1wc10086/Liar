module;
#include <string>
#include <cstdint>
#include <cstring>
#include <memory>
#include <stdexcept>
export module tool.popcap.texture.txz.txz_core;
import utility.io;
import utility.binary.unified_binary_stream;
import utility.png.png;
import tool.popcap.texture.texture_core;
import utility.zlib.zlib_uncompress;
import utility.zlib.zlib_compress;
export {
namespace ImageTxzCodec {
    inline uint16_t parseFormat(const std::string& fmt) {
        if (fmt == "ABGR8888") return 1; if (fmt == "RGBA4444") return 2; if (fmt == "RGBA5551") return 3; if (fmt == "RGB565") return 4;
        try { int v = std::stoi(fmt); return v + 1; } catch(...) { return 1; }
    }
    inline void decode(const std::string& in, const std::string& out) {
        auto data = FileUtils::readFileBytes(in); UnifiedBinaryStream bs(data); 
        if(bs.readUInt16()!=2677) throw std::runtime_error("");
        int w=bs.readUInt16(), h=bs.readUInt16(), fmt=bs.readUInt16(); auto opt=zlib_ns::Decompressor::decompress(bs.readBytes(bs.getLength()-bs.getPosition()), w*h*4); if(!opt) throw std::runtime_error("");
        UnifiedBinaryStream tBs(*opt); std::unique_ptr<ImageBitmap> bmp; switch(fmt){ case 1:bmp.reset(ABGR8888::read(tBs,w,h));break; case 2:bmp.reset(RGBA4444::read(tBs,w,h));break; case 3:bmp.reset(RGBA5551::read(tBs,w,h));break; case 4:bmp.reset(RGB565::read(tBs,w,h));break; } bmp->save(out);
    }
    inline void encode(const std::string& in, const std::string& out, const std::string& fmtName) {
        uint16_t fmt = parseFormat(fmtName);
        std::unique_ptr<ImageBitmap> bmp(ImageBitmap::create(in)); UnifiedBinaryStream tBs(UnifiedBinaryStream::Mode::Write);
        switch(fmt){ case 1:ABGR8888::write(tBs,bmp.get());break; case 2:RGBA4444::write(tBs,bmp.get());break; case 3:RGBA5551::write(tBs,bmp.get());break; case 4:RGB565::write(tBs,bmp.get());break; }
        UnifiedBinaryStream bs(UnifiedBinaryStream::Mode::Write); bs.writeUInt16(2677); bs.writeUInt16(bmp->getWidth()); bs.writeUInt16(bmp->getHeight()); bs.writeUInt16(fmt);
        auto opt = zlib_ns::Compressor::compress(tBs.getData(), 9); bs.writeBytes(*opt); FileUtils::writeFileBytes(out, bs.getData());
    }
}

}
