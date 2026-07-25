module;
#include <algorithm>
#include <cmath>
#include <cstddef>
#include <cstdint>
#include <vector>
#include "lib/etcpak/ProcessRGB.hpp"
extern void DecodeRGBPart(uint64_t d, uint32_t* dst, uint32_t w);
export module utility.etc.etc;
import utility.binary.unified_binary_stream;
import utility.png.png;

export {
struct ETC1_RGB {
    static ImageBitmap* read(UnifiedBinaryStream& bs, int w, int h) {
        auto img = ImageBitmap::create(w, h);
        auto px = img->getPixels();
        std::vector<uint32_t> block(16);
        for (int by = 0; by < (h + 3) / 4; ++by) for (int bx = 0; bx < (w + 3) / 4; ++bx) {
            DecodeRGBPart(bs.readUInt64(), block.data(), 4);
            for (int sy = 0; sy < 4; ++sy) for (int sx = 0; sx < 4; ++sx) {
                if (const int y = by * 4 + sy, x = bx * 4 + sx; y < h && x < w) {
                    const auto c = block[sy * 4 + sx];
                    px[y * w + x] = ImageColor(c & 0xFF, (c >> 8) & 0xFF, (c >> 16) & 0xFF, 255);
                }
            }
        }
        return img;
    }

    static uint32_t write(UnifiedBinaryStream& bs, const ImageBitmap* img) {
        const int w = img->getWidth(), h = img->getHeight();
        const int aw = (w + 3) & ~3, ah = (h + 3) & ~3;
        std::vector<uint32_t> pixels(static_cast<size_t>(aw) * ah, 0xFF000000u);
        const auto* src = img->getPixels();
        for (int y = 0; y < h; ++y) for (int x = 0; x < w; ++x) {
            const auto& p = src[y * w + x];
            pixels[y * aw + x] = 0xFF000000u | (static_cast<uint32_t>(p.r) << 16) | (static_cast<uint32_t>(p.g) << 8) | p.b;
        }
        std::vector<uint64_t> blocks(static_cast<size_t>(aw / 4) * (ah / 4));
        CompressEtc1Rgb(pixels.data(), blocks.data(), static_cast<uint32_t>(blocks.size()), static_cast<size_t>(aw));
        for (const auto block : blocks) bs.writeUInt64(block);
        return w >> 1;
    }
};

struct ETC1_RGB_A8 {
    static ImageBitmap* read(UnifiedBinaryStream& bs, int w, int h) {
        auto img = ETC1_RGB::read(bs, w, h);
        auto px = img->getPixels();
        for (int i = 0; i < w * h; ++i) px[i].a = bs.readUInt8();
        return img;
    }

    static uint32_t write(UnifiedBinaryStream& bs, const ImageBitmap* img) {
        ETC1_RGB::write(bs, img);
        const auto* px = img->getPixels();
        for (int i = 0; i < img->getSize(); ++i) bs.writeUInt8(px[i].a);
        return img->getWidth() << 2;
    }
};

struct ETC1_RGB_A_Palette {
    static ImageBitmap* read(UnifiedBinaryStream& bs, int w, int h) {
        auto img = ETC1_RGB::read(bs, w, h);
        auto px = img->getPixels();
        const auto count = bs.readUInt8();
        const int bits = count == 0 ? 1 : count == 1 ? 1 : static_cast<int>(std::log2(count - 1)) + 1;
        std::vector<uint8_t> palette = count == 0 ? std::vector<uint8_t>{0, 255} : std::vector<uint8_t>(count);
        if (count != 0) for (auto& alpha : palette) { const auto value = bs.readUInt8(); alpha = (value << 4) | value; }
        uint8_t byte{};
        int remaining{};
        for (int i = 0; i < w * h; ++i) {
            int value{}, needed = bits;
            while (needed > 0) {
                if (remaining == 0) { byte = bs.readUInt8(); remaining = 8; }
                const auto take = std::min(needed, remaining);
                value = (value << take) | ((byte >> (remaining - take)) & ((1 << take) - 1));
                remaining -= take;
                needed -= take;
            }
            px[i].a = value < static_cast<int>(palette.size()) ? palette[value] : palette.back();
        }
        return img;
    }

    static uint32_t write(UnifiedBinaryStream& bs, const ImageBitmap* img, int& alphaSize) {
        const auto pitch = ETC1_RGB::write(bs, img);
        const auto* px = img->getPixels();
        const auto size = img->getSize();
        bs.writeUInt8(0x10);
        for (uint8_t i{}; i < 16; ++i) bs.writeUInt8(i);
        alphaSize = (size >> 1) + 17;
        for (int i = 0; i < (size >> 1); ++i) bs.writeUInt8((px[i << 1].a & 0xF0) | (px[(i << 1) | 1].a >> 4));
        if ((size & 1) != 0) { ++alphaSize; bs.writeUInt8(px[size - 1].a & 0xF0); }
        return pitch;
    }
};
}
