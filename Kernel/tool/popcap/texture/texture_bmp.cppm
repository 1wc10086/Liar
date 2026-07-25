module;
#include <climits>
#include <cstdint>
#include <cstdio>
#include <limits>
#include <stdexcept>
#include <string>
#include <vector>
export module tool.popcap.texture.texture_bmp;
import utility.png.png;
import tool.shell.config_manager;

export namespace TextureBmp {
[[nodiscard]] inline bool enabled() { return ConfigManager::get().getSetting("ptx_bmp", "false") == "true"; }

inline uint16_t read16(FILE* file) {
    uint8_t bytes[2];
    if (fread(bytes, 1, sizeof(bytes), file) != sizeof(bytes)) throw std::runtime_error("Invalid BMP");
    return uint16_t(bytes[0]) | uint16_t(bytes[1]) << 8;
}

inline uint32_t read32(FILE* file) {
    uint8_t bytes[4];
    if (fread(bytes, 1, sizeof(bytes), file) != sizeof(bytes)) throw std::runtime_error("Invalid BMP");
    return uint32_t(bytes[0]) | uint32_t(bytes[1]) << 8 | uint32_t(bytes[2]) << 16 | uint32_t(bytes[3]) << 24;
}

inline void write16(FILE* file, uint16_t value) {
    uint8_t bytes[]{uint8_t(value), uint8_t(value >> 8)};
    if (fwrite(bytes, 1, sizeof(bytes), file) != sizeof(bytes)) throw std::runtime_error("BMP write failed");
}

inline void write32(FILE* file, uint32_t value) {
    uint8_t bytes[]{uint8_t(value), uint8_t(value >> 8), uint8_t(value >> 16), uint8_t(value >> 24)};
    if (fwrite(bytes, 1, sizeof(bytes), file) != sizeof(bytes)) throw std::runtime_error("BMP write failed");
}

[[nodiscard]] inline ImageBitmap* read(const std::string& path) {
    FILE* file = fopen(path.c_str(), "rb");
    if (!file) throw std::runtime_error("Cannot open BMP");
    try {
        if (read16(file) != 0x4D42u) throw std::runtime_error("Invalid BMP signature");
        const uint32_t fileSize = read32(file);
        read16(file);
        read16(file);
        const uint32_t offset = read32(file);
        const uint32_t dibSize = read32(file);
        if (dibSize < 40) throw std::runtime_error("Unsupported BMP DIB");

        const int32_t width = static_cast<int32_t>(read32(file));
        const int32_t signedHeight = static_cast<int32_t>(read32(file));
        const uint16_t planes = read16(file);
        const uint16_t bpp = read16(file);
        const uint32_t compression = read32(file);
        const uint32_t imageSize = read32(file);
        uint32_t masks[4]{};
        for (uint32_t index = 40; index < dibSize; ++index) {
            const int byte = fgetc(file);
            if (byte == EOF) throw std::runtime_error("Invalid BMP");
            if (index >= 40 && index < 56) masks[(index - 40) / 4] |= uint32_t(uint8_t(byte)) << (8 * ((index - 40) % 4));
        }

        if (compression == 3 && dibSize == 40) {
            masks[0] = read32(file);
            masks[1] = read32(file);
            masks[2] = read32(file);
        }
        const bool rgb = compression == 0 && (bpp == 24 || bpp == 32);
        const bool bitfields = compression == 3 && bpp == 32 &&
            masks[0] == 0x00FF0000 && masks[1] == 0x0000FF00 && masks[2] == 0x000000FF &&
            (masks[3] == 0 || masks[3] == 0xFF000000);
        if (width <= 0 || signedHeight == 0 || signedHeight == std::numeric_limits<int32_t>::min() || planes != 1 ||
            (!rgb && !bitfields) || uint64_t(offset) < 14ull + dibSize + (compression == 3 && dibSize == 40 ? 12 : 0)) throw std::runtime_error("Unsupported BMP");

        const uint32_t height = signedHeight < 0 ? uint32_t(-signedHeight) : uint32_t(signedHeight);
        const uint64_t stride = (uint64_t(width) * (bpp / 8) + 3) & ~uint64_t(3);
        const uint64_t total = stride * height;
        if (width > std::numeric_limits<int>::max() || height > uint32_t(std::numeric_limits<int>::max()) ||
            total > std::numeric_limits<size_t>::max() || offset > fileSize || total > uint64_t(fileSize - offset) ||
            (imageSize && imageSize < total) || uint64_t(offset) > uint64_t(LONG_MAX)) throw std::runtime_error("Invalid BMP dimensions");
        if (fseek(file, static_cast<long>(offset), SEEK_SET) != 0) throw std::runtime_error("Invalid BMP offset");

        auto* bitmap = ImageBitmap::create(width, static_cast<int>(height));
        std::vector<uint8_t> row(static_cast<size_t>(stride));
        for (uint32_t y = 0; y < height; ++y) {
            if (fread(row.data(), 1, row.size(), file) != row.size()) throw std::runtime_error("Truncated BMP");
            auto* output = bitmap->getPixels() + static_cast<size_t>(signedHeight < 0 ? y : height - 1 - y) * width;
            for (int x = 0; x < width; ++x) {
                const auto* pixel = row.data() + x * (bpp / 8);
                output[x] = ImageColor(pixel[2], pixel[1], pixel[0], bpp == 32 && (!bitfields || masks[3]) ? pixel[3] : 255);
            }
        }
        fclose(file);
        return bitmap;
    } catch (...) {
        fclose(file);
        throw;
    }
}

inline void write(const ImageBitmap& bitmap, const std::string& path) {
    const int width = bitmap.getWidth();
    const int height = bitmap.getHeight();
    constexpr uint32_t offset = 14 + 124;
    if (width <= 0 || height <= 0 || uint64_t(width) * uint64_t(height) > (std::numeric_limits<uint32_t>::max() - offset) / 4)
        throw std::runtime_error("Invalid BMP dimensions");

    const uint32_t imageSize = uint32_t(uint64_t(width) * uint64_t(height) * 4);
    const uint32_t fileSize = offset + imageSize;
    FILE* file = fopen(path.c_str(), "wb");
    if (!file) throw std::runtime_error("Cannot open BMP");
    try {
        write16(file, 0x4D42);
        write32(file, fileSize);
        write16(file, 0);
        write16(file, 0);
        write32(file, offset);
        write32(file, 124);
        write32(file, width);
        write32(file, height);
        write16(file, 1);
        write16(file, 32);
        write32(file, 3);
        write32(file, imageSize);
        for (int i = 0; i < 4; ++i) write32(file, 0);
        write32(file, 0x00FF0000);
        write32(file, 0x0000FF00);
        write32(file, 0x000000FF);
        write32(file, 0xFF000000);
        write32(file, 0x73524742);
        for (int i = 0; i < 9; ++i) write32(file, 0);
        write32(file, 0);
        write32(file, 0);
        write32(file, 0);
        write32(file, 0);
        write32(file, 0);
        write32(file, 0);
        write32(file, 0);

        std::vector<uint8_t> row(static_cast<size_t>(width) * 4);
        for (int y = height - 1; y >= 0; --y) {
            const auto* input = bitmap.getPixels() + static_cast<size_t>(y) * width;
            for (int x = 0; x < width; ++x) {
                auto* pixel = row.data() + x * 4;
                pixel[0] = input[x].b;
                pixel[1] = input[x].g;
                pixel[2] = input[x].r;
                pixel[3] = input[x].a;
            }
            if (fwrite(row.data(), 1, row.size(), file) != row.size()) throw std::runtime_error("BMP write failed");
        }
        fclose(file);
    } catch (...) {
        fclose(file);
        throw;
    }
}
}
