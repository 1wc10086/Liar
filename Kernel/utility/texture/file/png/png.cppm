module;
#include <cstdint>
#include <cstdio>
#include <setjmp.h>
#include <span>
#include <stdexcept>
#include <string>
#include <string_view>
#include <vector>
#include <lib/libpng/png.h>
export module utility.png.png;

export struct ImageColor {
    uint8_t r{}, g{}, b{}, a{255};
    constexpr ImageColor() = default;
    constexpr ImageColor(uint8_t r, uint8_t g, uint8_t b, uint8_t a = 255) : r(r), g(g), b(b), a(a) {}
    static constexpr ImageColor fromUInt32(uint32_t v) {
        return {uint8_t(v & 0xFF), uint8_t((v >> 8) & 0xFF), uint8_t((v >> 16) & 0xFF), uint8_t((v >> 24) & 0xFF)};
    }
    constexpr uint32_t toUInt32() const { return (a << 24) | (b << 16) | (g << 8) | r; }
};

export class ImageBitmap {
    int width_, height_;
    std::vector<ImageColor> pixels_;
public:
    ImageBitmap(int w, int h) : width_(w), height_(h), pixels_(static_cast<size_t>(w) * h) {}

    ImageBitmap(const std::string& path, int compressionLevel = -1) {
        FILE* fp = fopen(path.c_str(), "rb");
        if (!fp) throw std::runtime_error("");

        png_structp png = png_create_read_struct(PNG_LIBPNG_VER_STRING, nullptr, nullptr, nullptr);
        if (!png) { fclose(fp); throw std::runtime_error(""); }
        png_infop info = png_create_info_struct(png);
        if (!info) { png_destroy_read_struct(&png, nullptr, nullptr); fclose(fp); throw std::runtime_error(""); }

        if (setjmp(png_jmpbuf(png))) {
            png_destroy_read_struct(&png, &info, nullptr);
            fclose(fp);
            throw std::runtime_error("");
        }

        png_init_io(png, fp);
        png_read_info(png, info);

        width_  = static_cast<int>(png_get_image_width(png, info));
        height_ = static_cast<int>(png_get_image_height(png, info));
        auto color_type = png_get_color_type(png, info);
        auto bit_depth  = png_get_bit_depth(png, info);

        if (bit_depth == 16) png_set_strip_16(png);
        if (color_type == PNG_COLOR_TYPE_PALETTE) png_set_palette_to_rgb(png);
        if (color_type == PNG_COLOR_TYPE_GRAY && bit_depth < 8) png_set_expand_gray_1_2_4_to_8(png);
        if (png_get_valid(png, info, PNG_INFO_tRNS)) png_set_tRNS_to_alpha(png);
        if (color_type == PNG_COLOR_TYPE_GRAY || color_type == PNG_COLOR_TYPE_GRAY_ALPHA) png_set_gray_to_rgb(png);
        if (color_type == PNG_COLOR_TYPE_RGB || color_type == PNG_COLOR_TYPE_GRAY || color_type == PNG_COLOR_TYPE_PALETTE)
            png_set_filler(png, 0xFF, PNG_FILLER_AFTER);

        png_read_update_info(png, info);

        const size_t rowBytes = static_cast<size_t>(width_) * 4;
        pixels_.resize(static_cast<size_t>(width_) * height_);

        std::vector<png_bytep> rows(static_cast<size_t>(height_));
        auto* raw = reinterpret_cast<uint8_t*>(pixels_.data());
        for (int y = 0; y < height_; ++y)
            rows[y] = raw + static_cast<size_t>(y) * rowBytes;

        png_read_image(png, rows.data());
        png_destroy_read_struct(&png, &info, nullptr);
        fclose(fp);
    }

    int getWidth() const { return width_; }
    int getHeight() const { return height_; }
    int getSize() const { return width_ * height_; }
    ImageColor* getPixels() { return pixels_.data(); }
    const ImageColor* getPixels() const { return pixels_.data(); }

    void save(const std::string& path, int compressionLevel = -1) const {
        FILE* fp = fopen(path.c_str(), "wb");
        if (!fp) throw std::runtime_error("");

        png_structp png = png_create_write_struct(PNG_LIBPNG_VER_STRING, nullptr, nullptr, nullptr);
        if (!png) { fclose(fp); throw std::runtime_error(""); }
        png_infop info = png_create_info_struct(png);
        if (!info) { png_destroy_write_struct(&png, nullptr); fclose(fp); throw std::runtime_error(""); }

        if (setjmp(png_jmpbuf(png))) {
            png_destroy_write_struct(&png, &info);
            fclose(fp);
            throw std::runtime_error("");
        }

        png_init_io(png, fp);
        png_set_IHDR(png, info, width_, height_, 8, PNG_COLOR_TYPE_RGBA,
                     PNG_INTERLACE_NONE, PNG_COMPRESSION_TYPE_DEFAULT, PNG_FILTER_TYPE_DEFAULT);

        if (compressionLevel >= 0 && compressionLevel <= 9) {
            png_set_compression_level(png, compressionLevel);
        }

        png_write_info(png, info);

        const size_t rowBytes = static_cast<size_t>(width_) * 4;
        const auto* raw = reinterpret_cast<const uint8_t*>(pixels_.data());
        std::vector<png_bytep> rows(static_cast<size_t>(height_));
        for (int y = 0; y < height_; ++y)
            rows[y] = const_cast<png_bytep>(raw + static_cast<size_t>(y) * rowBytes);

        png_write_image(png, rows.data());
        png_write_end(png, nullptr);
        png_destroy_write_struct(&png, &info);
        fclose(fp);
    }

    static ImageBitmap* create(int w, int h) { return new ImageBitmap(w, h); }
    static ImageBitmap* create(const std::string& p) { return new ImageBitmap(p); }
};
