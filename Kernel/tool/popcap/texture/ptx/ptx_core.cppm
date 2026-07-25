module;
#include <cstddef>
#include <cstdint>
#include <array>
#include <charconv>
#include <span>
#include <vector>
#include <algorithm>
#include <stdexcept>
#include <memory>
#include <string>
#include <string_view>
export module tool.popcap.texture.ptx.ptx_core;
import utility.io;
import utility.binary.unified_binary_stream;
import utility.png.png;
import tool.popcap.texture.texture_core;
import tool.popcap.texture.texture_bmp;
import tool.shell.config_manager;
export import tool.popcap.texture.ptx.ptx_types;
export import tool.popcap.texture.ptx.ptx_alpha;
export {

namespace ImagePtxCodec {

namespace Detail {

[[nodiscard]] inline uint32_t nextPow2Min8(uint32_t v) noexcept {
    uint32_t n = 8;
    while (n < v) n <<= 1;
    return n;
}

[[nodiscard]] inline size_t calcAuxColorPayloadSize(uint32_t fileId, uint32_t w, uint32_t h) noexcept {
    switch (fileId) {
        case 147:
        case 150: {
            const uint32_t aw = (w + 3u) & ~3u;
            const uint32_t ah = (h + 3u) & ~3u;
            return (static_cast<size_t>(aw) * ah) >> 1;
        }
        case 148: {
            const uint32_t aw = nextPow2Min8(w);
            const uint32_t ah = nextPow2Min8(h);
            return (static_cast<size_t>(aw) * ah) >> 1;
        }
        case 149:
            return static_cast<size_t>(w) * h * 4u;
        default:
            return 0;
    }
}

inline void writePtxHeader(UnifiedBinaryStream& bs, const PtxUtils::PtxHeader& h, bool bigEndianHeader) {
    bs.setEndian(bigEndianHeader ? UnifiedBinaryStream::Endian::Big
                                 : UnifiedBinaryStream::Endian::Little);
    bs.writeUInt32(h.magic);
    bs.writeUInt32(h.version);
    bs.writeUInt32(h.width);
    bs.writeUInt32(h.height);
    bs.writeUInt32(h.pitch);
    bs.writeUInt32(h.format);
    bs.writeUInt32(h.alphaSize);
    bs.writeUInt32(h.alphaFormat);
}

[[nodiscard]] inline PtxUtils::PtxHeader readPtxHeader(UnifiedBinaryStream& bs, bool& bigEndianHeader) {
    PtxUtils::PtxHeader h{};
    h.magic = bs.readUInt32();
    bigEndianHeader = false;

    if (h.magic == 0x31787470u) {
        bs.setEndian(UnifiedBinaryStream::Endian::Big);
        h.magic = PtxUtils::PTX_MAGIC;
        bigEndianHeader = true;
    } else if (h.magic != PtxUtils::PTX_MAGIC) {
        throw std::runtime_error("Invalid PTX magic");
    }

    h.version     = bs.readUInt32();
    h.width       = bs.readUInt32();
    h.height      = bs.readUInt32();
    h.pitch       = bs.readUInt32();
    h.format      = bs.readUInt32();
    h.alphaSize   = bs.readUInt32();
    h.alphaFormat = bs.readUInt32();
    return h;
}

[[nodiscard]] inline FormatSpec specFromHeader(
    const PtxUtils::PtxHeader& h,
    bool bigEndianHeader,
    std::span<const uint8_t> payload,
    std::string_view hintedFmt,
    std::string_view fmt0Mode
) {
    const auto hinted = hintedFmt.empty() ? invalidSpec() : parseFormatStr(hintedFmt);

    switch (h.format) {
        case 0:
            if (bigEndianHeader) {
                if (hinted.valid && hinted.tag == Tag::ARGB8888_Padding_BE) {
                    return makeSimple(Tag::ARGB8888_Padding_BE, 0, true);
                }
                return makeSimple(Tag::ARGB8888_BE, 0, true);
            }
            if (fmt0Mode == "ABGR" || (hinted.valid && hinted.tag == Tag::ABGR8888)) {
                return makeSimple(Tag::ABGR8888, 0, false);
            }
            return makeSimple(Tag::ARGB8888, 0, false);

        case 1:  return makeSimple(Tag::RGBA4444, 1);
        case 2:  return makeSimple(Tag::RGB565, 2);
        case 3:  return makeSimple(Tag::RGBA5551, 3);
        case 5:  return bigEndianHeader ? makeSimple(Tag::DXT5_RGBA_BE, 5, true)
                                        : makeSimple(Tag::DXT5_RGBA_MortonBlock, 5);
        case 21: return makeSimple(Tag::RGBA4444_Block, 21);
        case 22: return makeSimple(Tag::RGB565_Block, 22);
        case 23: return makeSimple(Tag::RGBA5551_Block, 23);
        case 30: return makeSimple(Tag::PVRTC_4BPP_RGBA, 30);
        case 31: return makeSimple(Tag::PVRTC_2BPP_RGBA, 31);
        case 32: return makeSimple(Tag::ETC1_RGB, 32);
        case 35: return makeSimple(Tag::DXT1_RGB, 35);
        case 36: return makeSimple(Tag::DXT3_RGBA, 36);
        case 37: return makeSimple(Tag::DXT5_RGBA, 37);
        case 160:return makeSimple(Tag::ASTC_4x4, 160, false, true);
        case 161:return makeSimple(Tag::ASTC_5x5, 161, false, true);
        case 162:return makeSimple(Tag::ASTC_6x6, 162, false, true);
        case 163:return makeSimple(Tag::ASTC_8x8, 163, false, true);

        case 147:
        case 148:
        case 149:
        case 150: {
            FormatSpec spec{};
            spec.fileId = h.format;
            spec.valid  = true;

            if (h.format == 147 || h.format == 150) spec.tag = Tag::ETC1_RGB;
            else if (h.format == 148)               spec.tag = Tag::PVRTC_4BPP_RGBA;
            else if (fmt0Mode == "ABGR" || (hinted.valid && hinted.tag == Tag::XBGR8888_AuxAlpha))
                spec.tag = Tag::XBGR8888_AuxAlpha;
            else
                spec.tag = Tag::XRGB8888_AuxAlpha;

            if (hinted.valid && hinted.fileId == h.format && hinted.alpha != AlphaScheme::None) {
                spec.alpha = hinted.alpha;
                spec.markAlpha64 = hinted.markAlpha64;
                return spec;
            }

            const size_t pixels = static_cast<size_t>(h.width) * h.height;
            const size_t tex0Size = calcAuxColorPayloadSize(h.format, h.width, h.height);
            if (tex0Size > payload.size()) throw std::runtime_error("PTX payload truncated");

            const auto alphaPayload = payload.subspan(tex0Size);
            const auto detected = Alpha::autoDetect(
                h.format, alphaPayload, pixels, h.alphaSize, h.alphaFormat
            );

            spec.alpha = detected.valid
                ? static_cast<AlphaScheme>(detected.scheme)
                : AlphaScheme::None;
            spec.markAlpha64 = (h.alphaFormat == 0x64u);
            return spec;
        }

        default:
            throw std::runtime_error("Unsupported PTX format id");
    }
}

inline void writeXBGRColorOnly(UnifiedBinaryStream& bs, const ImageBitmap* bmp) {
    const auto* px = bmp->getPixels();
    for (int i = 0; i < bmp->getSize(); ++i) {
        bs.writeUInt32(0xFF000000u
            | (static_cast<uint32_t>(px[i].b) << 16)
            | (static_cast<uint32_t>(px[i].g) << 8)
            |  static_cast<uint32_t>(px[i].r));
    }
}

[[nodiscard]] inline ImageBitmap* readXBGRColorOnly(UnifiedBinaryStream& bs, int w, int h) {
    auto* img = ImageBitmap::create(w, h);
    auto* px = img->getPixels();
    for (int i = 0; i < w * h; ++i) {
        const uint32_t t = bs.readUInt32();
        px[i] = ImageColor(
            static_cast<uint8_t>( t        & 0xFF),
            static_cast<uint8_t>((t >> 8 ) & 0xFF),
            static_cast<uint8_t>((t >> 16) & 0xFF),
            255
        );
    }
    return img;
}

[[nodiscard]] inline std::unique_ptr<ImageBitmap> decodeSimple(
    Tag tag,
    std::span<const uint8_t> payload,
    int w,
    int h,
    std::string_view fmt0Mode
) {
    UnifiedBinaryStream bs(payload);

    switch (tag) {
        case Tag::ARGB8888:
            if (fmt0Mode == "ABGR") return std::unique_ptr<ImageBitmap>(ABGR8888::read(bs, w, h));
            if (fmt0Mode == "ARGB_Padding") {
                int padW = w;
                if ((padW % 64) != 0) padW = (padW / 64) * 64 + 64;
                return std::unique_ptr<ImageBitmap>(ARGB8888_Padding::read(bs, w, h, padW << 2));
            }
            return std::unique_ptr<ImageBitmap>(ARGB8888::read(bs, w, h));

        case Tag::ABGR8888:          return std::unique_ptr<ImageBitmap>(ABGR8888::read(bs, w, h));
        case Tag::RGBA4444:          return std::unique_ptr<ImageBitmap>(RGBA4444::read(bs, w, h));
        case Tag::RGB565:            return std::unique_ptr<ImageBitmap>(RGB565::read(bs, w, h));
        case Tag::RGBA5551:          return std::unique_ptr<ImageBitmap>(RGBA5551::read(bs, w, h));
        case Tag::RGBA4444_Block:    return std::unique_ptr<ImageBitmap>(RGBA4444_Block::read(bs, w, h));
        case Tag::RGB565_Block:      return std::unique_ptr<ImageBitmap>(RGB565_Block::read(bs, w, h));
        case Tag::RGBA5551_Block:    return std::unique_ptr<ImageBitmap>(RGBA5551_Block::read(bs, w, h));

        case Tag::ARGB8888_BE:
            bs.setEndian(UnifiedBinaryStream::Endian::Big);
            return std::unique_ptr<ImageBitmap>(ARGB8888::read(bs, w, h));

        case Tag::ARGB8888_Padding_BE: {
            bs.setEndian(UnifiedBinaryStream::Endian::Big);
            int padW = w;
            if ((padW % 64) != 0) padW = (padW / 64) * 64 + 64;
            return std::unique_ptr<ImageBitmap>(ARGB8888_Padding::read(bs, w, h, padW << 2));
        }

        case Tag::DXT1_RGB:              return std::unique_ptr<ImageBitmap>(DXT1_RGB::read(bs, w, h));
        case Tag::DXT3_RGBA:             return std::unique_ptr<ImageBitmap>(DXT3_RGBA::read(bs, w, h));
        case Tag::DXT5_RGBA:             return std::unique_ptr<ImageBitmap>(DXT5_RGBA::read(bs, w, h));
        case Tag::DXT5_RGBA_MortonBlock: return std::unique_ptr<ImageBitmap>(DXT5_RGBA_MortonBlock::read(bs, w, h));

        case Tag::DXT5_RGBA_BE:
            bs.setEndian(UnifiedBinaryStream::Endian::Little);
            return std::unique_ptr<ImageBitmap>(DXT5_RGBA::read(bs, w, h));

        case Tag::ETC1_RGB:         return std::unique_ptr<ImageBitmap>(ETC1_RGB::read(bs, w, h));
        case Tag::PVRTC_4BPP_RGBA:  return std::unique_ptr<ImageBitmap>(PVRTC_4BPP_RGBA::read(bs, w, h));
        case Tag::PVRTC_2BPP_RGBA:  return std::unique_ptr<ImageBitmap>(PVRTC_2BPP_RGBA::read(bs, w, h));

        case Tag::ASTC_4x4: return std::unique_ptr<ImageBitmap>(ASTC_RGBA<4, 4>::read(bs, w, h));
        case Tag::ASTC_5x5: return std::unique_ptr<ImageBitmap>(ASTC_RGBA<5, 5>::read(bs, w, h));
        case Tag::ASTC_6x6: return std::unique_ptr<ImageBitmap>(ASTC_RGBA<6, 6>::read(bs, w, h));
        case Tag::ASTC_8x8: return std::unique_ptr<ImageBitmap>(ASTC_RGBA<8, 8>::read(bs, w, h));

        case Tag::XRGB8888_AuxAlpha:
            return std::unique_ptr<ImageBitmap>(XRGB8888::read(bs, w, h));

        case Tag::XBGR8888_AuxAlpha:
            return std::unique_ptr<ImageBitmap>(readXBGRColorOnly(bs, w, h));
    }

    throw std::runtime_error("Unsupported PTX decode tag");
}

[[nodiscard]] inline uint32_t encodeSimple(
    Tag tag,
    UnifiedBinaryStream& bs,
    const ImageBitmap* bmp,
    std::string_view fmt0Mode,
    bool& bigEndianHeader
) {
    bigEndianHeader = false;

    switch (tag) {
        case Tag::ARGB8888:
            if (fmt0Mode == "ABGR") return ABGR8888::write(bs, bmp);
            if (fmt0Mode == "ARGB_Padding") {
                int padW = bmp->getWidth();
                if ((padW % 64) != 0) padW = (padW / 64) * 64 + 64;
                return ARGB8888_Padding::write(bs, bmp, padW << 2);
            }
            return ARGB8888::write(bs, bmp);

        case Tag::ABGR8888:       return ABGR8888::write(bs, bmp);
        case Tag::RGBA4444:       return RGBA4444::write(bs, bmp);
        case Tag::RGB565:         return RGB565::write(bs, bmp);
        case Tag::RGBA5551:       return RGBA5551::write(bs, bmp);

        case Tag::RGBA4444_Block: return RGBA4444_Block::write(bs, bmp);
        case Tag::RGB565_Block:   return RGB565_Block::write(bs, bmp);
        case Tag::RGBA5551_Block: return RGBA5551_Block::write(bs, bmp);

        case Tag::XRGB8888_AuxAlpha:
            return XRGB8888::write(bs, bmp);

        case Tag::XBGR8888_AuxAlpha:
            writeXBGRColorOnly(bs, bmp);
            return static_cast<uint32_t>(bmp->getWidth() << 2);

        case Tag::ARGB8888_BE:
            bs.setEndian(UnifiedBinaryStream::Endian::Big);
            bigEndianHeader = true;
            return ARGB8888::write(bs, bmp);

        case Tag::ARGB8888_Padding_BE: {
            bs.setEndian(UnifiedBinaryStream::Endian::Big);
            bigEndianHeader = true;
            int padW = bmp->getWidth();
            if ((padW % 64) != 0) padW = (padW / 64) * 64 + 64;
            return ARGB8888_Padding::write(bs, bmp, padW << 2);
        }

        case Tag::DXT1_RGB:              return DXT1_RGB::write(bs, bmp);
        case Tag::DXT3_RGBA:             return DXT3_RGBA::write(bs, bmp);
        case Tag::DXT5_RGBA:             return DXT5_RGBA::write(bs, bmp);
        case Tag::DXT5_RGBA_MortonBlock: return DXT5_RGBA_MortonBlock::write(bs, bmp);

        case Tag::DXT5_RGBA_BE:
            bigEndianHeader = true;
            return DXT5_RGBA::write(bs, bmp);

        case Tag::ETC1_RGB:
            return ETC1_RGB::write(bs, bmp);

        case Tag::PVRTC_4BPP_RGBA:
            return PVRTC_4BPP_RGBA::write(bs, const_cast<ImageBitmap*>(bmp));

        case Tag::PVRTC_2BPP_RGBA:
            return PVRTC_2BPP_RGBA::write(bs, const_cast<ImageBitmap*>(bmp));

        case Tag::ASTC_4x4: return ASTC_RGBA<4, 4>::write(bs, bmp);
        case Tag::ASTC_5x5: return ASTC_RGBA<5, 5>::write(bs, bmp);
        case Tag::ASTC_6x6: return ASTC_RGBA<6, 6>::write(bs, bmp);
        case Tag::ASTC_8x8: return ASTC_RGBA<8, 8>::write(bs, bmp);
    }

    throw std::runtime_error("Unsupported PTX encode tag");
}

[[nodiscard]] inline Tag colorOnlyTagForAux(const FormatSpec& spec) noexcept {
    switch (spec.fileId) {
        case 147:
        case 150:
            return Tag::ETC1_RGB;
        case 148:
            return Tag::PVRTC_4BPP_RGBA;
        case 149:
            return spec.tag;
        default:
            return spec.tag;
    }
}

[[nodiscard]] inline uint32_t auxHeaderPitch(const FormatSpec& spec, const ImageBitmap* bmp) noexcept {
    switch (spec.fileId) {
        case 147:
        case 148:
        case 149:
        case 150:
            return static_cast<uint32_t>(bmp->getWidth() << 2);
        default:
            return 0;
    }
}

[[nodiscard]] inline bool shouldWriteAlpha64(const FormatSpec& spec) noexcept {
    if (spec.markAlpha64) return true;
    if (isAstcFileId(spec.fileId)) return true;
    return false;
}

[[nodiscard]] inline bool isVariableAlphaSize(AlphaScheme scheme) noexcept {
    return scheme == AlphaScheme::A8_A1
        || scheme == AlphaScheme::Palette4_A1
        || scheme == AlphaScheme::Palette5_A1;
}

}

inline void decode(
    const std::string& inFile,
    const std::string& outFile,
    bool useHeader,
    const std::string& userFmt,
    int userW,
    int userH,
    const std::string& fmt0Mode = "ARGB"
) {
    auto data = FileUtils::readFileBytes(inFile);

    PtxUtils::PtxHeader h{};
    FormatSpec spec{};
    std::span<const uint8_t> payload;

    if (useHeader) {
        UnifiedBinaryStream bs(data);
        bool bigEndianHeader = false;
        h = Detail::readPtxHeader(bs, bigEndianHeader);
        payload = std::span<const uint8_t>(data.data() + bs.getPosition(), data.size() - bs.getPosition());
        spec = Detail::specFromHeader(h, bigEndianHeader, payload, userFmt, fmt0Mode);
    } else {
        spec = parseFormatStr(userFmt);
        if (!spec.valid) throw std::runtime_error("Unknown PTX format");
        h.width  = static_cast<uint32_t>(userW);
        h.height = static_cast<uint32_t>(userH);
        h.format = spec.fileId;
        payload = data;
    }

    if (ImagePtxCodec::isAuxFileId(spec.fileId)) {
        const size_t tex0Size = Detail::calcAuxColorPayloadSize(spec.fileId, h.width, h.height);
        if (tex0Size > payload.size()) throw std::runtime_error("PTX payload truncated");

        auto colorSpan = payload.first(tex0Size);
        auto alphaSpan = payload.subspan(tex0Size);

        auto bmp = Detail::decodeSimple(
            Detail::colorOnlyTagForAux(spec),
            colorSpan,
            static_cast<int>(h.width),
            static_cast<int>(h.height),
            fmt0Mode
        );

        Alpha::decodeByScheme(
            static_cast<Alpha::Scheme>(spec.alpha),
            alphaSpan,
            *bmp
        );

        if (TextureBmp::enabled()) TextureBmp::write(*bmp, outFile);
        else bmp->save(outFile, ConfigManager::get().getSettingInt("png_compression_level", -1));
        return;
    }

    auto bmp = Detail::decodeSimple(
        spec.tag,
        payload,
        static_cast<int>(h.width),
        static_cast<int>(h.height),
        fmt0Mode
    );
    if (TextureBmp::enabled()) TextureBmp::write(*bmp, outFile);
    else bmp->save(outFile, ConfigManager::get().getSettingInt("png_compression_level", -1));
}

inline void encode(
    const std::string& inFile,
    const std::string& outFile,
    const std::string& userFmt,
    bool writeHdr,
    const std::string& fmt0Mode = "ARGB"
) {
    const auto spec = parseFormatStr(userFmt);
    if (!spec.valid) throw std::runtime_error("Unknown PTX format");

    std::unique_ptr<ImageBitmap> bmp(TextureBmp::enabled()
        ? TextureBmp::read(inFile)
        : ImageBitmap::create(inFile));
    UnifiedBinaryStream bs(UnifiedBinaryStream::Mode::Write);

    PtxUtils::PtxHeader h;
    h.magic   = PtxUtils::PTX_MAGIC;
    h.version = PtxUtils::VERSION;
    h.width   = static_cast<uint32_t>(bmp->getWidth());
    h.height  = static_cast<uint32_t>(bmp->getHeight());
    h.format  = spec.fileId;

    size_t headerPos = 0;
    if (writeHdr) {
        headerPos = bs.getPosition();
        for (int i = 0; i < 8; ++i) bs.writeUInt32(0);
    }

    bool bigEndianHeader = spec.headerBigEndian;

    if (ImagePtxCodec::isAuxFileId(spec.fileId)) {
        bool dummyBE = false;
        h.pitch = Detail::encodeSimple(
            Detail::colorOnlyTagForAux(spec),
            bs,
            bmp.get(),
            fmt0Mode,
            dummyBE
        );

        h.pitch = Detail::auxHeaderPitch(spec, bmp.get());

        const auto alphaBytes = Alpha::encodeByScheme(
            static_cast<Alpha::Scheme>(spec.alpha),
            *bmp
        );
        if (!alphaBytes.empty()) bs.writeBytes(alphaBytes);

        h.alphaSize   = Detail::isVariableAlphaSize(spec.alpha)
            ? static_cast<uint32_t>(alphaBytes.size())
            : 0u;
        h.alphaFormat = Detail::shouldWriteAlpha64(spec) ? 0x64u : 0u;
    } else {
        h.pitch = Detail::encodeSimple(spec.tag, bs, bmp.get(), fmt0Mode, bigEndianHeader);
        h.alphaSize   = 0;
        h.alphaFormat = Detail::shouldWriteAlpha64(spec) ? 0x64u : 0u;
    }

    if (writeHdr) {
        const size_t endPos = bs.getPosition();
        bs.setPosition(headerPos);
        Detail::writePtxHeader(bs, h, bigEndianHeader);
        bs.setPosition(endPos);
    }

    FileUtils::writeFileBytes(outFile, bs.getData());
}

}

}
