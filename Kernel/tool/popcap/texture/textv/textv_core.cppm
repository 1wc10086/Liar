module;
#include <string>
#include <cstdint>
#include <cstring>
#include <memory>
#include <stdexcept>
#include <vector>
export module tool.popcap.texture.textv.textv_core;
import utility.io;
import utility.binary.unified_binary_stream;
import utility.png.png;
import tool.popcap.texture.texture_core;
import utility.zlib.zlib_uncompress;
import utility.zlib.zlib_compress;
export {
namespace ImageTexTVCodec {
    inline int parseFormat(const std::string& fmt, bool& z) {
        z = true; std::string base = fmt;
        if (fmt.size() > 4 && fmt.substr(fmt.size() - 4) == "_RAW") { z = false; base = fmt.substr(0, fmt.size() - 4); }
        int id = 1;
        if (base == "L8") id = 1; else if (base == "ARGB8888") id = 2; else if (base == "ARGB4444") id = 3; else if (base == "ARGB1555") id = 4;
        else if (base == "RGB565") id = 5; else if (base == "ABGR8888") id = 6; else if (base == "RGBA4444") id = 7; else if (base == "RGBA5551") id = 8;
        else if (base == "XRGB8888") id = 9; else if (base == "LA88") id = 10;
        else { try { int v = std::stoi(base); if (v >= 10) { v -= 9; z = false; } id = v + 1; } catch(...) {} }
        return id;
    }

    inline void decode(const std::string& in, const std::string& out) {
        auto data = FileUtils::readFileBytes(in); UnifiedBinaryStream bs(data); 
        bs.verifyBytes((const uint8_t*)"SEXYTEX\0", 8); if (bs.readInt32() != 0) throw std::runtime_error("");
        int w = bs.readInt32(), h = bs.readInt32(), fmt = bs.readInt32(), flags = bs.readUInt32(); 
        (void)bs.readInt32(); (void)bs.readInt32(); (void)bs.readInt32(); (void)bs.readInt32(); (void)bs.readInt32();
        
        std::vector<uint8_t> d; 
        if (flags & 1) { 
            auto compressed = bs.readBytes(bs.getLength() - bs.getPosition());
            auto opt = zlib_ns::Decompressor::decompress(compressed, w * h * 4); 
            if (!opt) throw std::runtime_error(""); 
            d = std::move(*opt); 
        } else {
            d = bs.readBytes(bs.getLength() - bs.getPosition());
        }
        
        UnifiedBinaryStream tBs(d); std::unique_ptr<ImageBitmap> bmp;
        switch (fmt) { 
            case 1: bmp.reset(L8::read(tBs, w, h)); break; 
            case 2: bmp.reset(ARGB8888::read(tBs, w, h)); break; 
            case 3: bmp.reset(ARGB4444::read(tBs, w, h)); break; 
            case 4: bmp.reset(ARGB1555::read(tBs, w, h)); break; 
            case 5: bmp.reset(RGB565::read(tBs, w, h)); break; 
            case 6: bmp.reset(ABGR8888::read(tBs, w, h)); break; 
            case 7: bmp.reset(RGBA4444::read(tBs, w, h)); break; 
            case 8: bmp.reset(RGBA5551::read(tBs, w, h)); break; 
            case 9: bmp.reset(XRGB8888::read(tBs, w, h)); break; 
            case 10: bmp.reset(LA88::read(tBs, w, h)); break; 
            default: throw std::runtime_error("");
        } 
        bmp->save(out);
    }

    inline void encode(const std::string& in, const std::string& out, const std::string& fmtStr) {
        bool z = true; int fmt = parseFormat(fmtStr, z);
        std::unique_ptr<ImageBitmap> bmp(ImageBitmap::create(in)); UnifiedBinaryStream tBs(UnifiedBinaryStream::Mode::Write);
        
        switch (fmt) { 
            case 1: L8::write(tBs, bmp.get()); break; 
            case 2: ARGB8888::write(tBs, bmp.get()); break; 
            case 3: ARGB4444::write(tBs, bmp.get()); break; 
            case 4: ARGB1555::write(tBs, bmp.get()); break; 
            case 5: RGB565::write(tBs, bmp.get()); break; 
            case 6: ABGR8888::write(tBs, bmp.get()); break; 
            case 7: RGBA4444::write(tBs, bmp.get()); break; 
            case 8: RGBA5551::write(tBs, bmp.get()); break; 
            case 9: XRGB8888::write(tBs, bmp.get()); break; 
            case 10: LA88::write(tBs, bmp.get()); break; 
            default: throw std::runtime_error("");
        }
        
        UnifiedBinaryStream bs(UnifiedBinaryStream::Mode::Write); 
        bs.writeBytes((const uint8_t*)"SEXYTEX\0", 8); bs.writeInt32(0); bs.writeInt32(bmp->getWidth()); bs.writeInt32(bmp->getHeight()); bs.writeInt32(fmt);
        
        if (z) { 
            auto opt = zlib_ns::Compressor::compress(tBs.getData(), 9); 
            bs.writeUInt32(1); bs.writeInt32(1); bs.writeInt32(opt->size()); bs.writeInt32(0); bs.writeInt32(0); bs.writeInt32(0); bs.writeBytes(opt->data(), opt->size()); 
        } else { 
            bs.writeUInt32(0); bs.writeInt32(1); bs.writeInt32(0); bs.writeInt32(0); bs.writeInt32(0); bs.writeInt32(0); bs.writeBytes(tBs.getData()); 
        } 
        FileUtils::writeFileBytes(out, bs.getData());
    }
}

}
