module;
#include <algorithm>
#include <cstdint>
#include <cstdio>
#include <cstring>
#include <initializer_list>
#include <setjmp.h>
#include <span>
#include <string>
#include <string_view>
#include <tuple>
#include <utility>
#include <vector>
#include <lib/libpng/png.h>
#include "lib/quickjs_ng/quickjs.h"

module tool.shell.js_engine;

import tool.shell.js_utils;
import utility.io;

namespace kernelx {

using FnList = std::initializer_list<std::tuple<const char*, JSCFunction*, int>>;

int32_t intArg(JSContext* ctx, JSValueConst value, int32_t fallback) noexcept;
int64_t int64Arg(JSContext* ctx, JSValueConst value, int64_t fallback) noexcept;
JSValue object(JSContext* ctx, FnList funcs);

namespace {

struct Image { uint32_t width{}; uint32_t height{}; std::vector<uint8_t> rgba; };
struct ReadState { const uint8_t* data{}; size_t size{}; size_t offset{}; };

void pngRead(png_structp png, png_bytep out, png_size_t count) {
    auto* state = static_cast<ReadState*>(png_get_io_ptr(png));
    if (!state || count > state->size - state->offset) png_error(png, "read");
    std::memcpy(out, state->data + state->offset, count);
    state->offset += count;
}

void pngWrite(png_structp png, png_bytep data, png_size_t count) {
    auto* out = static_cast<std::vector<uint8_t>*>(png_get_io_ptr(png));
    const auto old = out->size();
    out->resize(old + count);
    std::memcpy(out->data() + old, data, count);
}

void pngFlush(png_structp) {}

Image decode(std::span<const uint8_t> bytes) {
    if (bytes.size() < 8 || png_sig_cmp(bytes.data(), 0, 8) != 0) return {};
    ReadState state{bytes.data(), bytes.size(), 0};
    png_structp png = png_create_read_struct(PNG_LIBPNG_VER_STRING, nullptr, nullptr, nullptr);
    if (!png) return {};
    png_infop info = png_create_info_struct(png);
    if (!info) { png_destroy_read_struct(&png, nullptr, nullptr); return {}; }
    if (setjmp(png_jmpbuf(png))) { png_destroy_read_struct(&png, &info, nullptr); return {}; }
    png_set_read_fn(png, &state, pngRead);
    png_read_info(png, info);
    auto width = png_get_image_width(png, info);
    auto height = png_get_image_height(png, info);
    auto color = png_get_color_type(png, info);
    auto depth = png_get_bit_depth(png, info);
    if (depth == 16) png_set_strip_16(png);
    if (color == PNG_COLOR_TYPE_PALETTE) png_set_palette_to_rgb(png);
    if (color == PNG_COLOR_TYPE_GRAY && depth < 8) png_set_expand_gray_1_2_4_to_8(png);
    if (png_get_valid(png, info, PNG_INFO_tRNS)) png_set_tRNS_to_alpha(png);
    if (color == PNG_COLOR_TYPE_GRAY || color == PNG_COLOR_TYPE_GRAY_ALPHA) png_set_gray_to_rgb(png);
    if (color == PNG_COLOR_TYPE_RGB || color == PNG_COLOR_TYPE_GRAY || color == PNG_COLOR_TYPE_PALETTE) png_set_filler(png, 0xFF, PNG_FILLER_AFTER);
    png_read_update_info(png, info);
    Image image{width, height, std::vector<uint8_t>(static_cast<size_t>(width) * height * 4)};
    std::vector<png_bytep> rows(height);
    for (uint32_t y = 0; y < height; ++y) rows[y] = image.rgba.data() + static_cast<size_t>(y) * width * 4;
    png_read_image(png, rows.data());
    png_destroy_read_struct(&png, &info, nullptr);
    return image;
}

std::vector<uint8_t> encode(uint32_t width, uint32_t height, std::span<const uint8_t> rgba, int level) {
    if (!width || !height || rgba.size() < static_cast<size_t>(width) * height * 4) return {};
    std::vector<uint8_t> out;
    out.reserve(static_cast<size_t>(width) * height / 2);
    png_structp png = png_create_write_struct(PNG_LIBPNG_VER_STRING, nullptr, nullptr, nullptr);
    if (!png) return {};
    png_infop info = png_create_info_struct(png);
    if (!info) { png_destroy_write_struct(&png, nullptr); return {}; }
    if (setjmp(png_jmpbuf(png))) { png_destroy_write_struct(&png, &info); return {}; }
    png_set_write_fn(png, &out, pngWrite, pngFlush);
    png_set_IHDR(png, info, width, height, 8, PNG_COLOR_TYPE_RGBA, PNG_INTERLACE_NONE, PNG_COMPRESSION_TYPE_DEFAULT, PNG_FILTER_TYPE_DEFAULT);
    if (level >= 0 && level <= 9) png_set_compression_level(png, level);
    png_write_info(png, info);
    std::vector<png_bytep> rows(height);
    for (uint32_t y = 0; y < height; ++y) rows[y] = const_cast<png_bytep>(rgba.data() + static_cast<size_t>(y) * width * 4);
    png_write_image(png, rows.data());
    png_write_end(png, nullptr);
    png_destroy_write_struct(&png, &info);
    return out;
}

JSValue infoObject(JSContext* ctx, const Image& image) {
    JSValue obj = JS_NewObject(ctx);
    JS_SetPropertyStr(ctx, obj, "width", JS_NewUint32(ctx, image.width));
    JS_SetPropertyStr(ctx, obj, "height", JS_NewUint32(ctx, image.height));
    JS_SetPropertyStr(ctx, obj, "bytesPerPixel", JS_NewUint32(ctx, 4));
    JS_SetPropertyStr(ctx, obj, "byteLength", JS_NewInt64(ctx, static_cast<int64_t>(image.rgba.size())));
    return obj;
}

JSValue info(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 1) return JS_NULL;
    auto input = js::bytesView(ctx, argv[0]);
    if (!input) return JS_NULL;
    auto image = decode(input.span());
    return image.width && image.height ? infoObject(ctx, image) : JS_NULL;
}

JSValue decodeBytes(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 1) return JS_NULL;
    auto input = js::bytesView(ctx, argv[0]);
    if (!input) return JS_NULL;
    auto image = decode(input.span());
    if (!image.width || !image.height) return JS_NULL;
    JSValue obj = infoObject(ctx, image);
    JS_SetPropertyStr(ctx, obj, "pixels", js::bytes(ctx, std::move(image.rgba)));
    return obj;
}

JSValue encodeBytes(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 3) return JS_NULL;
    const auto width = static_cast<uint32_t>(std::max(0, intArg(ctx, argv[0], 0)));
    const auto height = static_cast<uint32_t>(std::max(0, intArg(ctx, argv[1], 0)));
    auto rgba = js::bytesView(ctx, argv[2]);
    if (!rgba) return JS_NULL;
    auto out = encode(width, height, rgba.span(), argc > 3 ? intArg(ctx, argv[3], 6) : 6);
    return out.empty() ? JS_NULL : js::bytes(ctx, std::move(out));
}

JSValue read(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 1) return JS_NULL;
    try { auto bytes = FileUtils::readFileBytes(js::toString(ctx, argv[0])); JSValue arg = js::bytes(ctx, std::move(bytes)); JSValue out = decodeBytes(ctx, JS_UNDEFINED, 1, &arg); JS_FreeValue(ctx, arg); return out; }
    catch (...) { return JS_NULL; }
}

JSValue write(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 4) return JS_FALSE;
    JSValue bytes = encodeBytes(ctx, JS_UNDEFINED, argc - 1, argv + 1);
    if (JS_IsNull(bytes)) return JS_FALSE;
    auto view = js::bytesView(ctx, bytes);
    const bool ok = view && FileUtils::writeFileBytes(js::toString(ctx, argv[0]), view.span());
    JS_FreeValue(ctx, bytes);
    return JS_NewBool(ctx, ok);
}

JSValue createBuffer(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 2) return js::bytes(ctx, nullptr, 0);
    const auto width = static_cast<size_t>(std::max<int64_t>(0, int64Arg(ctx, argv[0], 0)));
    const auto height = static_cast<size_t>(std::max<int64_t>(0, int64Arg(ctx, argv[1], 0)));
    uint32_t fill = argc > 2 ? static_cast<uint32_t>(int64Arg(ctx, argv[2], 0)) : 0;
    std::vector<uint8_t> out(width * height * 4);
    for (size_t i = 0; i < out.size(); i += 4) {
        out[i] = static_cast<uint8_t>(fill & 0xFF);
        out[i + 1] = static_cast<uint8_t>((fill >> 8) & 0xFF);
        out[i + 2] = static_cast<uint8_t>((fill >> 16) & 0xFF);
        out[i + 3] = static_cast<uint8_t>((fill >> 24) & 0xFF);
    }
    return js::bytes(ctx, std::move(out));
}

}

JSValue createPng(JSContext* ctx) {
    return object(ctx, {
        {"info", info, 1}, {"decode", decodeBytes, 1}, {"encode", encodeBytes, 4},
        {"read", read, 1}, {"write", write, 5}, {"createBuffer", createBuffer, 3},
    });
}

}
