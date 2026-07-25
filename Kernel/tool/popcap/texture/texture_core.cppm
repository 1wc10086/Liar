module;
#include <cstddef>
#include <cstdint>
#include <cmath>
#include <vector>
#include <algorithm>
export module tool.popcap.texture.texture_core;
import utility.binary.unified_binary_stream;
import utility.png.png;
export import utility.etc.etc;
export import utility.astc.astc;
export import utility.pvrtc.pvrtc;
export {
#define GEN_BASIC_FMT(Name, ReadLogic, PitchCalc, ...) struct Name { \
    static ImageBitmap* read(UnifiedBinaryStream& bs, int w, int h) { \
        auto img = ImageBitmap::create(w, h); auto px = img->getPixels(); \
        for(int i = 0; i < w * h; i++) { ReadLogic; } return img; \
    } \
    static uint32_t write(UnifiedBinaryStream& bs, const ImageBitmap* img) { \
        auto px = img->getPixels(); for(int i = 0; i < img->getSize(); i++) { __VA_ARGS__; } \
        return PitchCalc; \
    } \
};

GEN_BASIC_FMT(ABGR8888, uint32_t t = bs.readUInt32(); px[i] = ImageColor(uint8_t(t&0xFF), uint8_t((t>>8)&0xFF), uint8_t((t>>16)&0xFF), uint8_t(t>>24)), img->getWidth()<<2, bs.writeUInt32(px[i].r | (px[i].g<<8) | (px[i].b<<16) | (px[i].a<<24)))
GEN_BASIC_FMT(ARGB8888, uint32_t t = bs.readUInt32(); px[i] = ImageColor(uint8_t((t>>16)&0xFF), uint8_t((t>>8)&0xFF), uint8_t(t&0xFF), uint8_t(t>>24)), img->getWidth()<<2, bs.writeUInt32((px[i].a<<24) | (px[i].r<<16) | (px[i].g<<8) | px[i].b))
GEN_BASIC_FMT(XRGB8888, uint32_t t = bs.readUInt32(); px[i] = ImageColor(uint8_t((t>>16)&0xFF), uint8_t((t>>8)&0xFF), uint8_t(t&0xFF), 255), img->getWidth()<<2, bs.writeUInt32(0xFF000000 | (px[i].r<<16) | (px[i].g<<8) | px[i].b))
GEN_BASIC_FMT(ARGB1555, uint16_t t = bs.readUInt16(); int r=(t>>10)&31; int g=(t>>5)&31; int b=t&31; px[i] = ImageColor(uint8_t((r<<3)|(r>>2)), uint8_t((g<<3)|(g>>2)), uint8_t((b<<3)|(b>>2)), uint8_t((t>>15)?255:0)), img->getWidth()<<1, bs.writeUInt16(((px[i].a&0x80)<<8) | ((px[i].r&0xF8)<<7) | ((px[i].g&0xF8)<<2) | ((px[i].b&0xF8)>>3)))
GEN_BASIC_FMT(ARGB4444, uint16_t t = bs.readUInt16(); int a=t>>12; int r=(t>>8)&15; int g=(t>>4)&15; int b=t&15; px[i] = ImageColor(uint8_t((r<<4)|r), uint8_t((g<<4)|g), uint8_t((b<<4)|b), uint8_t((a<<4)|a)), img->getWidth()<<1, bs.writeUInt16(((px[i].a&0xF0)<<8) | ((px[i].r&0xF0)<<4) | (px[i].g&0xF0) | ((px[i].b&0xF0)>>4)))
GEN_BASIC_FMT(RGBA4444, uint16_t t = bs.readUInt16(); int r=(t>>12)&15; int g=(t>>8)&15; int b=(t>>4)&15; int a=t&15; px[i] = ImageColor(uint8_t((r<<4)|r), uint8_t((g<<4)|g), uint8_t((b<<4)|b), uint8_t((a<<4)|a)), img->getWidth()<<1, bs.writeUInt16(((px[i].r&0xF0)<<8) | ((px[i].g&0xF0)<<4) | (px[i].b&0xF0) | ((px[i].a&0xF0)>>4)))
GEN_BASIC_FMT(RGBA5551, uint16_t t = bs.readUInt16(); int r=(t>>11)&31; int g=(t>>6)&31; int b=(t>>1)&31; px[i] = ImageColor(uint8_t((r<<3)|(r>>2)), uint8_t((g<<3)|(g>>2)), uint8_t((b<<3)|(b>>2)), uint8_t((t&1)?255:0)), img->getWidth()<<1, bs.writeUInt16(((px[i].r&0xF8)<<8) | ((px[i].g&0xF8)<<3) | ((px[i].b&0xF8)>>2) | ((px[i].a&0x80)>>7)))
GEN_BASIC_FMT(RGB565, uint16_t t = bs.readUInt16(); int r=(t>>11)&31; int g=(t>>5)&63; int b=t&31; px[i] = ImageColor(uint8_t((r<<3)|(r>>2)), uint8_t((g<<2)|(g>>4)), uint8_t((b<<3)|(b>>2)), 255), img->getWidth()<<1, bs.writeUInt16(((px[i].r&0xF8)<<8) | ((px[i].g&0xFC)<<3) | ((px[i].b&0xF8)>>3)))
GEN_BASIC_FMT(L8, uint8_t l = bs.readUInt8(); px[i] = ImageColor(l, l, l, 255), img->getWidth(), bs.writeUInt8((uint8_t)std::clamp<int>(px[i].r*0.299 + px[i].g*0.587 + px[i].b*0.114, 0, 255)))
GEN_BASIC_FMT(LA88, uint16_t t = bs.readUInt16(); uint8_t l = (t>>8)&0xFF; px[i] = ImageColor(l, l, l, uint8_t(t&0xFF)), img->getWidth()<<1, bs.writeUInt16(((uint16_t)std::clamp<int>(px[i].r*0.299 + px[i].g*0.587 + px[i].b*0.114, 0, 255)<<8) | px[i].a))

struct XRGB8888_A8 {
    static ImageBitmap* read(UnifiedBinaryStream& bs, int w, int h) {
        auto img = ImageBitmap::create(w, h); auto px = img->getPixels();
        for(int i = 0; i < w * h; i++) { uint32_t t = bs.readUInt32(); px[i] = ImageColor(uint8_t((t>>16)&0xFF), uint8_t((t>>8)&0xFF), uint8_t(t&0xFF), 255); }
        for(int i = 0; i < w * h; i++) px[i].a = bs.readUInt8(); return img;
    }
    static uint32_t write(UnifiedBinaryStream& bs, const ImageBitmap* img) {
        auto px = img->getPixels();
        for(int i = 0; i < img->getSize(); i++) bs.writeUInt32(0xFF000000 | (px[i].r<<16) | (px[i].g<<8) | px[i].b);
        for(int i = 0; i < img->getSize(); i++) bs.writeUInt8(px[i].a);
        return img->getWidth() << 3;
    }
};

struct XBGR8888_A8 {
    static ImageBitmap* read(UnifiedBinaryStream& bs, int w, int h) {
        auto img = ImageBitmap::create(w, h); auto px = img->getPixels();
        for(int i = 0; i < w * h; i++) { uint32_t t = bs.readUInt32(); px[i] = ImageColor(uint8_t(t&0xFF), uint8_t((t>>8)&0xFF), uint8_t((t>>16)&0xFF), 255); }
        for(int i = 0; i < w * h; i++) px[i].a = bs.readUInt8(); return img;
    }
    static uint32_t write(UnifiedBinaryStream& bs, const ImageBitmap* img) {
        auto px = img->getPixels();
        for(int i = 0; i < img->getSize(); i++) bs.writeUInt32(0xFF000000 | (px[i].b<<16) | (px[i].g<<8) | px[i].r);
        for(int i = 0; i < img->getSize(); i++) bs.writeUInt8(px[i].a);
        return img->getWidth() << 3;
    }
};

struct ARGB8888_Padding {
    static ImageBitmap* read(UnifiedBinaryStream& bs, int w, int h, int blockSize) {
        auto img = ImageBitmap::create(w, h); auto px = img->getPixels(); size_t off = bs.getPosition(); int times = 0;
        for(int i = 0; i < h; i++) {
            for(int j = 0; j < w; j++) { uint32_t t = bs.readUInt32(); *px++ = ImageColor(uint8_t((t>>16)&0xFF), uint8_t((t>>8)&0xFF), uint8_t(t&0xFF), uint8_t(t>>24)); }
            bs.setPosition(off + (++times) * blockSize);
        } return img;
    }
    static uint32_t write(UnifiedBinaryStream& bs, const ImageBitmap* img, int blockSize) {
        auto px = img->getPixels(); int h = img->getHeight(), w = img->getWidth(), pPad = blockSize - (w << 2);
        for(int i = 0; i < h; i++) {
            for(int j = 0; j < w; j++) { bs.writeUInt32((px->a<<24) | (px->r<<16) | (px->g<<8) | px->b); px++; }
            for(int j = 0; j < pPad; j++) bs.writeUInt8(0);
        } return blockSize;
    }
};

#define GEN_BLOCK_FMT(Name, ReadPx, WritePx) struct Name { \
    static ImageBitmap* read(UnifiedBinaryStream& bs, int w, int h) { \
        auto img = ImageBitmap::create(w, h); auto px = img->getPixels(); \
        for(int i = 0; i < h; i += 32) for(int w_ = 0; w_ < w; w_ += 32) for(int j = 0; j < 32; j++) for(int k = 0; k < 32; k++) { \
            uint16_t temp = bs.readUInt16(); if ((i + j) < h && (w_ + k) < w) { px[(i + j) * w + w_ + k] = ReadPx; } \
        } return img; \
    } \
    static uint32_t write(UnifiedBinaryStream& bs, const ImageBitmap* img) { \
        auto px = img->getPixels(); int w = img->getWidth(), h = img->getHeight(); \
        int newwidth = w; if ((newwidth & 31) != 0) { newwidth |= 31; newwidth++; } \
        for(int i = 0; i < h; i += 32) for(int w_ = 0; w_ < w; w_ += 32) for(int j = 0; j < 32; j++) for(int k = 0; k < 32; k++) { \
            if ((i + j) < h && (w_ + k) < w) { bs.writeUInt16(WritePx(px[(i + j) * w + w_ + k])); } else bs.writeUInt16(0); \
        } return newwidth << 1; \
    } \
};

GEN_BLOCK_FMT(RGB565_Block, ImageColor((temp&0xF800)>>8, (temp&0x7E0)>>3, (temp&0x1F)<<3, 255), [](const ImageColor& p)->uint16_t { return ((p.b&0xF8)>>3) | ((p.g&0xFC)<<3) | ((p.r&0xF8)<<8); })
GEN_BLOCK_FMT(RGBA4444_Block, ImageColor(((temp>>12)<<4)|(temp>>12), (((temp&0xF00)>>8)<<4)|((temp&0xF00)>>8), (((temp&0xF0)>>4)<<4)|((temp&0xF0)>>4), ((temp&0xF)<<4)|(temp&0xF)), [](const ImageColor& p)->uint16_t { return (p.a>>4) | (p.b&0xF0) | ((p.g&0xF0)<<4) | ((p.r&0xF0)<<8); })
GEN_BLOCK_FMT(RGBA5551_Block, ImageColor(((temp>>11)<<3)|((temp>>11)>>2), (((temp&0x7C0)>>6)<<3)|(((temp&0x7C0)>>6)>>2), (((temp&0x3E)>>1)<<3)|(((temp&0x3E)>>1)>>2), (temp&1)?255:0), [](const ImageColor& p)->uint16_t { return ((p.a&0x80)>>7) | ((p.b&0xF8)>>2) | ((p.g&0xF8)<<3) | ((p.r&0xF8)<<8); })

struct DXTEncode {
    static constexpr uint16_t colorTo565(const ImageColor& c) { return ((c.r >> 3) << 11) | ((c.g >> 2) << 5) | (c.b >> 3); }
    static constexpr uint16_t colorTo555(const ImageColor& c) { return ((c.r >> 3) << 10) | ((c.g >> 3) << 5) | (c.b >> 3); }

    static void getMinMaxColors(const ImageColor* cb, ImageColor* minC, ImageColor* maxC) {
        *minC = ImageColor(); *maxC = ImageColor(); int maxD = -1;
        for (int i = 0; i < 15; i++) {
            for (int j = i + 1; j < 16; j++) {
                int d = (cb[i].r - cb[j].r) * (cb[i].r - cb[j].r) + (cb[i].g - cb[j].g) * (cb[i].g - cb[j].g) + (cb[i].b - cb[j].b) * (cb[i].b - cb[j].b);
                if (d > maxD) { maxD = d; *minC = cb[i]; *maxC = cb[j]; }
            }
        }
        if (colorTo565(*maxC) < colorTo565(*minC)) std::swap(*minC, *maxC);
    }

    static int emitColorIndices(const ImageColor* cb, const ImageColor& minC, const ImageColor& maxC) {
        int c[16]; int r = 0;
        c[0] = (maxC.r & 0xF8) | (maxC.r >> 5); c[1] = (maxC.g & 0xFC) | (maxC.g >> 6); c[2] = (maxC.b & 0xF8) | (maxC.b >> 5);
        c[4] = (minC.r & 0xF8) | (minC.r >> 5); c[5] = (minC.g & 0xFC) | (minC.g >> 6); c[6] = (minC.b & 0xF8) | (minC.b >> 5);
        c[8] = ((c[0] << 1) + c[4]) / 3; c[9] = ((c[1] << 1) + c[5]) / 3; c[10] = ((c[2] << 1) + c[6]) / 3;
        c[12] = (c[0] + (c[4] << 1)) / 3; c[13] = (c[1] + (c[5] << 1)) / 3; c[14] = (c[2] + (c[6] << 1)) / 3;
        
        for (int i = 15; i >= 0; i--) {
            int d0 = std::abs(c[0] - cb[i].r) + std::abs(c[1] - cb[i].g) + std::abs(c[2] - cb[i].b);
            int d1 = std::abs(c[4] - cb[i].r) + std::abs(c[5] - cb[i].g) + std::abs(c[6] - cb[i].b);
            int d2 = std::abs(c[8] - cb[i].r) + std::abs(c[9] - cb[i].g) + std::abs(c[10] - cb[i].b);
            int d3 = std::abs(c[12] - cb[i].r) + std::abs(c[13] - cb[i].g) + std::abs(c[14] - cb[i].b);
            int b0 = d0 > d3, b1 = d1 > d2, b2 = d0 > d2, b3 = d1 > d3, b4 = d2 > d3;
            r |= ((b0 & b4) | (((b1 & b2) | (b0 & b3)) << 1)) << (i << 1);
        }
        return r;
    }

    static uint64_t emitDxt5Alpha(const ImageColor* cb, uint8_t& minA, uint8_t& maxA) {
        minA = 255; maxA = 0;
        for (int i = 0; i < 16; i++) { minA = std::min(minA, cb[i].a); maxA = std::max(maxA, cb[i].a); }
        if (minA == maxA) return 0;
        int temp = (maxA - minA) >> 4;
        maxA = std::clamp<int>(maxA - temp, 0, 255); minA = std::clamp<int>(minA + temp, 0, 255);
        uint8_t ta[8] = {
            maxA, minA, uint8_t((6 * maxA + minA) / 7), uint8_t((5 * maxA + (minA << 1)) / 7), 
            uint8_t(((maxA << 2) + 3 * minA) / 7), uint8_t((3 * maxA + (minA << 2)) / 7), 
            uint8_t(((maxA << 1) + 5 * minA) / 7), uint8_t((maxA + 6 * minA) / 7)
        };
        uint64_t a48 = 0;
        for (int i = 0; i < 16; i++) {
            int minD = 999, idx = 0;
            for (int j = 0; j < 8; j++) { int d = std::abs(cb[i].a - ta[j]); if (d < minD) { minD = d; idx = j; } }
            a48 |= ((uint64_t)idx) << (i * 3);
        }
        return a48;
    }
    
    static inline void decodeDxtColor(uint16_t c0, uint16_t c1, uint32_t cb, ImageColor* color, const uint8_t* alpha, bool useG3 = false, bool isDxt1 = false) {
        ImageColor tc[4];
        auto decC = [&](uint16_t v) { int b = v & 0x1F, g = (v & 0x7E0) >> 5, r = (v & 0xF800) >> 11; return ImageColor((r << 3) | (r >> 2), useG3 ? (g << 2) | (g >> 3) : (g << 2) | (g >> 4), (b << 3) | (b >> 2)); };
        tc[0] = decC(c0); tc[1] = decC(c1);
        if (!isDxt1 || c0 > c1) {
            tc[2] = { (uint8_t)(((tc[0].r << 1) + tc[1].r + 1) / 3), (uint8_t)(((tc[0].g << 1) + tc[1].g + 1) / 3), (uint8_t)(((tc[0].b << 1) + tc[1].b + 1) / 3), 255 };
            tc[3] = { (uint8_t)((tc[0].r + (tc[1].r << 1) + 1) / 3), (uint8_t)((tc[0].g + (tc[1].g << 1) + 1) / 3), (uint8_t)((tc[0].b + (tc[1].b << 1) + 1) / 3), 255 };
        } else {
            tc[2] = { (uint8_t)((tc[0].r + tc[1].r) >> 1), (uint8_t)((tc[0].g + tc[1].g) >> 1), (uint8_t)((tc[0].b + tc[1].b) >> 1), 255 };
            tc[3] = { 0, 0, 0, 0 }; 
        }
        for (int i = 0; i < 16; i++) { int idx = (cb >> (i * 2)) & 3; color[i] = { tc[idx].r, tc[idx].g, tc[idx].b, alpha ? alpha[i] : tc[idx].a }; }
    }

    static inline void decodeDxt3Alpha(uint64_t a64, uint8_t* alpha) {
        for (int i = 0; i < 16; i++) { int t = (a64 >> (i * 4)) & 0xF; alpha[i] = (t << 4) | t; }
    }

    static inline void decodeDxt5Alpha(uint8_t a0, uint8_t a1, uint64_t a48, uint8_t* alpha) {
        uint8_t ta[8];
        if (a0 > a1) { ta[0] = a0; ta[1] = a1; ta[2] = (6 * a0 + a1) / 7; ta[3] = (5 * a0 + (a1 << 1)) / 7; ta[4] = ((a0 << 2) + 3 * a1) / 7; ta[5] = (3 * a0 + (a1 << 2)) / 7; ta[6] = ((a0 << 1) + 5 * a1) / 7; ta[7] = (a0 + 6 * a1) / 7; }
        else { ta[0] = a0; ta[1] = a1; ta[2] = ((a0 << 2) + a1) / 5; ta[3] = (3 * a0 + (a1 << 1)) / 5; ta[4] = ((a0 << 1) + 3 * a1) / 5; ta[5] = (a0 + (a1 << 2)) / 5; ta[6] = 0; ta[7] = 255; }
        for (int i = 0; i < 16; i++) { alpha[i] = ta[a48 & 7]; a48 >>= 3; }
    }

    static inline int getMortonIndex(int i, int minwh, int mink, bool bigw, int w) {
        int mx = 0, my = 0;
        for (int j = 0; j < 16; j++) { mx |= (i & (1 << (j << 1))) >> j; my |= (i & ((1 << (j << 1)) << 1)) >> j; }
        my >>= 1; int j = (i >> (2 * mink)) << (2 * mink);
        int x = bigw ? (j | ((my & (minwh - 1)) << mink) | (mx & (minwh - 1))) / minwh : (j | ((mx & (minwh - 1)) << mink) | (my & (minwh - 1))) % minwh;
        int y = bigw ? (j | ((my & (minwh - 1)) << mink) | (mx & (minwh - 1))) % minwh : (j | ((mx & (minwh - 1)) << mink) | (my & (minwh - 1))) / minwh;
        return y * w + x;
    }
};

#define DXT_READ_LOOP(isDxt1, Dxt3Alpha, Dxt5Alpha, useG3) \
    ImageColor color[16]; uint8_t alpha[16]; \
    for (int y = 0; y < height; y += 4) for (int x = 0; x < width; x += 4) { \
        if constexpr (Dxt3Alpha) { uint64_t a0=bs.readUInt16(), a1=bs.readUInt16(), a2=bs.readUInt16(), a3=bs.readUInt16(); uint64_t a64 = a0|(a1<<16)|(a2<<32)|(a3<<48); DXTEncode::decodeDxt3Alpha(a64, alpha); } \
        else if constexpr (Dxt5Alpha) { uint16_t t=bs.readUInt16(); uint64_t a0=bs.readUInt16(), a1=bs.readUInt16(), a2=bs.readUInt16(); uint64_t a48 = a0|(a1<<16)|(a2<<32); DXTEncode::decodeDxt5Alpha(t&0xFF, t>>8, a48, alpha); } \
        uint16_t c0=bs.readUInt16(), c1=bs.readUInt16(); uint32_t cb0=bs.readUInt16(), cb1=bs.readUInt16(); uint32_t cb = cb0 | (cb1<<16); \
        DXTEncode::decodeDxtColor(c0, c1, cb, color, (Dxt3Alpha||Dxt5Alpha)?alpha:nullptr, useG3, isDxt1); \
        for(int i = 0; i < 4; i++) for(int j = 0; j < 4; j++) if((x+j)<width && (y+i)<height) px[(i+y)*width+x+j] = color[(i<<2)|j]; \
    }

#define DXT_WRITE_LOOP(Dxt3Alpha, Dxt5Alpha) \
    ImageColor color[16], minC, maxC; \
    for (int y = 0; y < img->getHeight(); y += 4) for (int x = 0; x < img->getWidth(); x += 4) { \
        for(int j = 0; j < 4; j++) for(int k = 0; k < 4; k++) color[(j<<2)|k] = ((y+j)<img->getHeight() && (x+k)<img->getWidth()) ? px[(y+j)*img->getWidth()+x+k] : ImageColor(0,0,0,0); \
        if constexpr (Dxt3Alpha) { uint16_t t[4]={0}; for(int j=0;j<4;j++) for(int k=0;k<4;k++) t[j] |= (color[(j<<2)|k].a>>4)<<(k<<2); for(int j=0;j<4;j++) bs.writeUInt16(t[j]); } \
        else if constexpr (Dxt5Alpha) { uint8_t minA, maxA; uint64_t a48 = DXTEncode::emitDxt5Alpha(color, minA, maxA); bs.writeUInt16((minA<<8)|maxA); bs.writeUInt16(a48&0xFFFF); bs.writeUInt16((a48>>16)&0xFFFF); bs.writeUInt16(a48>>32); } \
        DXTEncode::getMinMaxColors(color, &minC, &maxC); int r = DXTEncode::emitColorIndices(color, minC, maxC); \
        bs.writeUInt16(DXTEncode::colorTo565(maxC)); bs.writeUInt16(DXTEncode::colorTo565(minC)); bs.writeUInt16(r&0xFFFF); bs.writeUInt16(r>>16); \
    }

struct DXT1_RGB {
    static ImageBitmap* read(UnifiedBinaryStream& bs, int width, int height) { auto img=ImageBitmap::create(width,height); auto px=img->getPixels(); DXT_READ_LOOP(true, false, false, false); return img; }
    static uint32_t write(UnifiedBinaryStream& bs, const ImageBitmap* img) { auto px=img->getPixels(); DXT_WRITE_LOOP(false, false); return img->getWidth() >> 1; }
};
struct DXT3_RGBA {
    static ImageBitmap* read(UnifiedBinaryStream& bs, int width, int height) { auto img=ImageBitmap::create(width,height); auto px=img->getPixels(); DXT_READ_LOOP(false, true, false, false); return img; }
    static uint32_t write(UnifiedBinaryStream& bs, const ImageBitmap* img) { auto px=img->getPixels(); DXT_WRITE_LOOP(true, false); return img->getWidth(); }
};
struct DXT5_RGBA {
    static ImageBitmap* read(UnifiedBinaryStream& bs, int width, int height) { auto img=ImageBitmap::create(width,height); auto px=img->getPixels(); DXT_READ_LOOP(false, false, true, false); return img; }
    static uint32_t write(UnifiedBinaryStream& bs, const ImageBitmap* img) { auto px=img->getPixels(); DXT_WRITE_LOOP(false, true); return img->getWidth(); }
};

struct DXT5_RGBA_Morton {
    inline static constexpr int Order[16] = { 0, 2, 8, 10, 1, 3, 9, 11, 4, 6, 12, 14, 5, 7, 13, 15 };
    static ImageBitmap* read(UnifiedBinaryStream& bs, int width, int height) {
        int nw = width, nh = height; bool rz = false;
        if ((nw & (nw-1)) != 0) { nw = 2 << (int)std::floor(std::log2(nw)); rz = true; }
        if ((nh & (nh-1)) != 0) { nh = 2 << (int)std::floor(std::log2(nh)); rz = true; }
        auto img = ImageBitmap::create(nw, nh); auto px = img->getPixels();
        ImageColor color[16]; uint8_t alpha[16]; int pO = 0; int minwh = std::min(nw, nh), mink = std::log2(minwh); bool bigw = nw > nh;
        for (int y = 0; y < nh; y += 4) for (int x = 0; x < nw; x += 4) {
            uint16_t t=bs.readUInt16(); uint64_t a0=bs.readUInt16(), a1=bs.readUInt16(), a2=bs.readUInt16(); uint64_t a48 = a0|(a1<<16)|(a2<<32); DXTEncode::decodeDxt5Alpha(t&0xFF, t>>8, a48, alpha);
            uint16_t c0=bs.readUInt16(), c1=bs.readUInt16(); uint32_t cb0=bs.readUInt16(), cb1=bs.readUInt16(); uint32_t cb = cb0 | (cb1<<16);
            DXTEncode::decodeDxtColor(c0, c1, cb, color, alpha, true, false);
            for(int i = 0; i < 16; i++) px[DXTEncode::getMortonIndex(pO + Order[i], minwh, mink, bigw, nw)] = color[i]; pO += 16;
        }
        if (rz) {
            auto rimg = ImageBitmap::create(width, height); auto dpx = rimg->getPixels();
            for(int y = 0; y < height; y++) for(int x = 0; x < width; x++) dpx[y*width+x] = px[y*nw+x];
            delete img; return rimg;
        } 
        return img;
    }
    static uint32_t write(UnifiedBinaryStream& bs, const ImageBitmap* image) {
        int nw = image->getWidth(), nh = image->getHeight(); bool rz = false;
        if ((nw & (nw-1)) != 0) { nw = 2 << (int)std::floor(std::log2(nw)); rz = true; }
        if ((nh & (nh-1)) != 0) { nh = 2 << (int)std::floor(std::log2(nh)); rz = true; }
        std::vector<ImageColor> dpx(nw * nh); const ImageColor* px = image->getPixels();
        if (rz) { for(int y = 0; y < image->getHeight(); y++) for(int x = 0; x < image->getWidth(); x++) dpx[y*nw+x] = px[y*image->getWidth()+x]; px = dpx.data(); }
        ImageColor color[16], minC, maxC; int pO = 0, minwh = std::min(nw, nh), mink = std::log2(minwh); bool bigw = nw > nh;
        for (int y = 0; y < nh; y += 4) for (int x = 0; x < nw; x += 4) {
            for(int n = 0; n < 16; n++) color[n] = px[DXTEncode::getMortonIndex(pO + Order[n], minwh, mink, bigw, nw)]; pO += 16;
            uint8_t minA, maxA; uint64_t a48 = DXTEncode::emitDxt5Alpha(color, minA, maxA);
            bs.writeUInt16((minA<<8)|maxA); bs.writeUInt16(a48&0xFFFF); bs.writeUInt16((a48>>16)&0xFFFF); bs.writeUInt16(a48>>32);
            DXTEncode::getMinMaxColors(color, &minC, &maxC); int r = DXTEncode::emitColorIndices(color, minC, maxC);
            bs.writeUInt16(DXTEncode::colorTo565(maxC)); bs.writeUInt16(DXTEncode::colorTo565(minC)); bs.writeUInt16(r&0xFFFF); bs.writeUInt16(r>>16);
        }
        return nw;
    }
};

struct DXT5_RGBA_MortonBlock {
    inline static constexpr int mx[64] = {0,4,0,4,8,12,8,12,0,4,0,4,8,12,8,12,16,20,16,20,24,28,24,28,16,20,16,20,24,28,24,28,0,4,0,4,8,12,8,12,0,4,0,4,8,12,8,12,16,20,16,20,24,28,24,28,16,20,16,20,24,28,24,28};
    inline static constexpr int my[64] = {0,0,4,4,0,0,4,4,8,8,12,12,8,8,12,12,0,0,4,4,0,0,4,4,8,8,12,12,8,8,12,12,16,16,20,20,16,16,20,20,24,24,28,28,24,24,28,28,16,16,20,20,16,16,20,20,24,24,28,28,24,24,28,28};
    static ImageBitmap* read(UnifiedBinaryStream& bs, int width, int height) {
        int maxD = std::max(width, height), nw = width, nh = height;
        if (maxD < 32) {
            if ((nw & (nw-1)) != 0) nw = 2 << (int)std::floor(std::log2(nw));
            if ((nh & (nh-1)) != 0) nh = 2 << (int)std::floor(std::log2(nh));
            if (nw != nh) nw = nh = std::max(nw, nh);
        } else {
            if ((nw & 31) != 0) { nw |= 31; nw++; } if ((nh & 31) != 0) { nh |= 31; nh++; }
        }
        auto img = ImageBitmap::create(width, height); auto px = img->getPixels();
        ImageColor color[16]; uint8_t alpha[16];
        auto readBlk = [&](int bx, int by) {
            uint16_t t=bs.readUInt16(); uint64_t a0=bs.readUInt16(), a1=bs.readUInt16(), a2=bs.readUInt16(); uint64_t a48 = a0|(a1<<16)|(a2<<32); DXTEncode::decodeDxt5Alpha(t&0xFF, t>>8, a48, alpha);
            uint16_t c0=bs.readUInt16(), c1=bs.readUInt16(); uint32_t cb0=bs.readUInt16(), cb1=bs.readUInt16(); uint32_t cb = cb0 | (cb1<<16);
            DXTEncode::decodeDxtColor(c0, c1, cb, color, alpha, false, false);
            for(int i = 0; i < 4; i++) for(int j = 0; j < 4; j++) if((bx+j)<width && (by+i)<height) px[(by+i)*width+bx+j] = color[(i<<2)|j];
        };
        if (nw < 32) { int maxdi = (nw * nw) >> 4; for(int di = 0; di < maxdi; di++) readBlk(mx[di], my[di]); }
        else { for (int y = 0; y < nh; y += 32) for (int x = 0; x < nw; x += 32) for (int di = 0; di < 64; di++) readBlk(x+mx[di], y+my[di]); }
        return img;
    }
    static uint32_t write(UnifiedBinaryStream& bs, const ImageBitmap* image) {
        int width = image->getWidth(), height = image->getHeight(), maxD = std::max(width, height), nw = width, nh = height;
        if (maxD < 32) { if ((nw & (nw-1)) != 0) nw = 2 << (int)std::floor(std::log2(nw)); if ((nh & (nh-1)) != 0) nh = 2 << (int)std::floor(std::log2(nh)); if (nw != nh) nw = nh = std::max(nw, nh); }
        else { if ((nw & 31) != 0) { nw |= 31; nw++; } if ((nh & 31) != 0) { nh |= 31; nh++; } }
        auto px = image->getPixels(); ImageColor color[16], minC, maxC;
        auto writeBlk = [&](int bx, int by) {
            for(int j = 0; j < 4; j++) for(int k = 0; k < 4; k++) color[(j<<2)|k] = ((by+j)<height && (bx+k)<width) ? px[(by+j)*width+bx+k] : ImageColor(0,0,0,0);
            uint8_t minA, maxA; uint64_t a48 = DXTEncode::emitDxt5Alpha(color, minA, maxA);
            bs.writeUInt16((minA<<8)|maxA); bs.writeUInt16(a48&0xFFFF); bs.writeUInt16((a48>>16)&0xFFFF); bs.writeUInt16(a48>>32);
            DXTEncode::getMinMaxColors(color, &minC, &maxC); int r = DXTEncode::emitColorIndices(color, minC, maxC);
            bs.writeUInt16(DXTEncode::colorTo565(maxC)); bs.writeUInt16(DXTEncode::colorTo565(minC)); bs.writeUInt16(r&0xFFFF); bs.writeUInt16(r>>16);
        };
        if (nw < 32) { int maxdi = (nw * nw) >> 4; for(int di = 0; di < maxdi; di++) writeBlk(mx[di], my[di]); }
        else { for (int y = 0; y < nh; y += 32) for (int x = 0; x < nw; x += 32) for (int di = 0; di < 64; di++) writeBlk(x+mx[di], y+my[di]); }
        return nw;
    }
};

struct DXT5_RGBA_Padding {
    static ImageBitmap* read(UnifiedBinaryStream& bs, int width, int height, int blockSize) {
        auto img = ImageBitmap::create(width,height); auto px = img->getPixels();
        ImageColor color[16]; uint8_t alpha[16]; size_t off = bs.getPosition(); int times = 0;
        for (int y = 0; y < height; y += 4) {
            for (int x = 0; x < width; x += 4) {
                uint16_t t=bs.readUInt16(); uint64_t a0=bs.readUInt16(), a1=bs.readUInt16(), a2=bs.readUInt16(); uint64_t a48 = a0|(a1<<16)|(a2<<32); DXTEncode::decodeDxt5Alpha(t&0xFF, t>>8, a48, alpha);
                uint16_t c0=bs.readUInt16(), c1=bs.readUInt16(); uint32_t cb0=bs.readUInt16(), cb1=bs.readUInt16(); uint32_t cb = cb0 | (cb1<<16);
                DXTEncode::decodeDxtColor(c0, c1, cb, color, alpha, true, false);
                for(int i = 0; i < 4; i++) for(int j = 0; j < 4; j++) if((x+j)<width && (y+i)<height) px[(i+y)*width+x+j] = color[(i<<2)|j];
            }
            bs.setPosition(off + (++times) * blockSize);
        } bs.setPosition(off + times * blockSize); return img;
    }
    static uint32_t write(UnifiedBinaryStream& bs, const ImageBitmap* image, int blockSize) {
        auto px = image->getPixels(); int width = image->getWidth(), height = image->getHeight(), newwidth = width;
        if ((newwidth & 3) != 0) { newwidth |= 3; newwidth++; } int cdSize = blockSize - (newwidth << 2);
        ImageColor color[16], minC, maxC;
        for (int y = 0; y < height; y += 4) {
            for (int x = 0; x < width; x += 4) {
                for(int j = 0; j < 4; j++) for(int k = 0; k < 4; k++) color[(j<<2)|k] = ((y+j)<height && (x+k)<width) ? px[(y+j)*width+x+k] : ImageColor(0,0,0,0);
                uint8_t minA, maxA; uint64_t a48 = DXTEncode::emitDxt5Alpha(color, minA, maxA);
                bs.writeUInt16((minA<<8)|maxA); bs.writeUInt16(a48&0xFFFF); bs.writeUInt16((a48>>16)&0xFFFF); bs.writeUInt16(a48>>32);
                DXTEncode::getMinMaxColors(color, &minC, &maxC); int r = DXTEncode::emitColorIndices(color, minC, maxC);
                bs.writeUInt16(DXTEncode::colorTo565(maxC)); bs.writeUInt16(DXTEncode::colorTo565(minC)); bs.writeUInt16(r&0xFFFF); bs.writeUInt16(r>>16);
            }
            for (int j = 0; j < cdSize; j++) bs.writeUInt8(0xCD);
        } for (int j = 0; j < blockSize; j++) bs.writeUInt8(0xCD);
        return blockSize >> 2;
    }
};

}
