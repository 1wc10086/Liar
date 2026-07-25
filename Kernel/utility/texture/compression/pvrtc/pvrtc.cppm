module;
#include <algorithm>
#include <cstdint>
#include <cstring>
#include <stdexcept>
#include <vector>
#include "lib/PVRTCCompressor/Bitmap.h"
#include "lib/PVRTCCompressor/PvrTcDecoder.h"
#include "lib/PVRTCCompressor/PvrTcEncoder.h"
#include "lib/PVRTCCompressor/RgbaBitmap.h"
#include "lib/PVRTCCompressor/RgbBitmap.h"
export module utility.pvrtc.pvrtc;
import utility.binary.unified_binary_stream;
import utility.png.png;

export {
struct PVRTC_4BPP_RGBA {
    static int GetNextPOT(int value) { int result = 1; while (result < value) result <<= 1; return std::max(8, result); }
    static ImageBitmap* read(UnifiedBinaryStream& bs, int w, int h) {
        int nw = std::max(8, w), nh = std::max(8, h);
        if ((nw & (nw - 1)) != 0) nw = GetNextPOT(nw);
        if ((nh & (nh - 1)) != 0) nh = GetNextPOT(nh);
        const auto data = bs.readBytes((nw * nh) >> 1);
        std::vector<Javelin::ColorRgba<unsigned char>> decoded(static_cast<size_t>(nw) * nh);
        Javelin::PvrTcDecoder::DecodeRgba4Bpp(decoded.data(), Javelin::Point2<int>{nw, nh}, data.data());
        auto img = ImageBitmap::create(w, h);
        auto px = img->getPixels();
        for (int y = 0; y < h; ++y) for (int x = 0; x < w; ++x) { const auto& p = decoded[y * nw + x]; px[y * w + x] = ImageColor(p.r, p.g, p.b, p.a); }
        return img;
    }
    static uint32_t write(UnifiedBinaryStream& bs, ImageBitmap* img) {
        const int w = img->getWidth(), h = img->getHeight();
        int nw = std::max(8, w), nh = std::max(8, h);
        if ((nw & (nw - 1)) != 0) nw = GetNextPOT(nw);
        if ((nh & (nh - 1)) != 0) nh = GetNextPOT(nh);
        std::vector<Javelin::ColorRgba<unsigned char>> padded(static_cast<size_t>(nw) * nh, {0, 0, 0, 0});
        const auto* px = img->getPixels();
        for (int y = 0; y < h; ++y) for (int x = 0; x < w; ++x) { const auto& p = px[y * w + x]; padded[y * nw + x] = {p.r, p.g, p.b, p.a}; }
        Javelin::RgbaBitmap bitmap(nw, nh);
        std::memcpy(bitmap.GetData(), padded.data(), padded.size() * sizeof(padded.front()));
        std::vector<uint8_t> compressed((nw * nh) >> 1);
        Javelin::PvrTcEncoder::EncodeRgba4Bpp(compressed.data(), bitmap);
        bs.writeBytes(compressed.data(), compressed.size());
        return nw >> 1;
    }
};

struct PVRTC_4BPP_RGBA_A8 {
    static int GetNextPOT(int value) { return PVRTC_4BPP_RGBA::GetNextPOT(value); }
    static ImageBitmap* read(UnifiedBinaryStream& bs, int w, int h) {
        int nw = std::max(8, w), nh = std::max(8, h);
        if ((nw & (nw - 1)) != 0) nw = GetNextPOT(nw);
        if ((nh & (nh - 1)) != 0) nh = GetNextPOT(nh);
        const auto data = bs.readBytes((nw * nh) >> 1);
        std::vector<Javelin::ColorRgb<unsigned char>> decoded(static_cast<size_t>(nw) * nh);
        Javelin::PvrTcDecoder::DecodeRgb4Bpp(decoded.data(), Javelin::Point2<int>{nw, nh}, data.data());
        auto img = ImageBitmap::create(w, h);
        auto px = img->getPixels();
        for (int y = 0; y < h; ++y) for (int x = 0; x < w; ++x) { const auto& p = decoded[y * nw + x]; px[y * w + x] = ImageColor(p.r, p.g, p.b, 255); }
        for (int i = 0; i < w * h; ++i) px[i].a = bs.readUInt8();
        return img;
    }
    static uint32_t write(UnifiedBinaryStream& bs, ImageBitmap* img) {
        const int w = img->getWidth(), h = img->getHeight();
        int nw = std::max(8, w), nh = std::max(8, h);
        if ((nw & (nw - 1)) != 0) nw = GetNextPOT(nw);
        if ((nh & (nh - 1)) != 0) nh = GetNextPOT(nh);
        std::vector<Javelin::ColorRgb<unsigned char>> padded(static_cast<size_t>(nw) * nh, {0, 0, 0});
        const auto* px = img->getPixels();
        for (int y = 0; y < h; ++y) for (int x = 0; x < w; ++x) { const auto& p = px[y * w + x]; padded[y * nw + x] = {p.r, p.g, p.b}; }
        Javelin::RgbBitmap bitmap(nw, nh);
        std::memcpy(bitmap.GetData(), padded.data(), padded.size() * sizeof(padded.front()));
        std::vector<uint8_t> compressed((nw * nh) >> 1);
        Javelin::PvrTcEncoder::EncodeRgb4Bpp(compressed.data(), bitmap);
        bs.writeBytes(compressed.data(), compressed.size());
        for (int i = 0; i < w * h; ++i) bs.writeUInt8(px[i].a);
        return nw << 2;
    }
};

struct PVRTC_2BPP_RGBA {
    static int GetNextPOT(int value) { return PVRTC_4BPP_RGBA::GetNextPOT(value); }
    static ImageBitmap* read(UnifiedBinaryStream&, int, int) { throw std::runtime_error(""); }
    static uint32_t write(UnifiedBinaryStream&, ImageBitmap*) { throw std::runtime_error(""); }
};
}
