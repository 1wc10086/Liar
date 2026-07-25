module;
#include <algorithm>
#include <cstdint>
#include <initializer_list>
#include <limits>
#include <span>
#include <tuple>
#include <utility>
#include <vector>
#include "lib/quickjs_ng/quickjs.h"

module tool.shell.js_engine;

import tool.shell.js_utils;
import utility.texture.file.jpeg.jpeg_core;
import utility.texture.file.stb.stb_core;
import utility.texture.file.webp.webp_core;

namespace kernelx {

using FnList = std::initializer_list<std::tuple<const char*, JSCFunction*, int>>;
int32_t intArg(JSContext* ctx, JSValueConst value, int32_t fallback) noexcept;
int64_t int64Arg(JSContext* ctx, JSValueConst value, int64_t fallback) noexcept;
double doubleArg(JSContext* ctx, JSValueConst value, double fallback) noexcept;
JSValue object(JSContext* ctx, FnList funcs);

namespace {

bool image_size(uint32_t width, uint32_t height, uint32_t channels, size_t& result) {
    return width && height && channels && width <= std::numeric_limits<size_t>::max() / height && static_cast<size_t>(width) * height <= std::numeric_limits<size_t>::max() / channels && (result = static_cast<size_t>(width) * height * channels, true);
}

JSValue image_info(JSContext* ctx, uint32_t width, uint32_t height, uint32_t flags = 0) {
    JSValue output = JS_NewObject(ctx);
    JS_SetPropertyStr(ctx, output, "width", JS_NewUint32(ctx, width));
    JS_SetPropertyStr(ctx, output, "height", JS_NewUint32(ctx, height));
    JS_SetPropertyStr(ctx, output, "flags", JS_NewUint32(ctx, flags));
    return output;
}

JSValue webpInfo(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    auto input = argc ? js::bytesView(ctx, argv[0]) : js::BytesView{}; texture::webp::Info info{};
    return input && texture::webp::info(input.span(), info) ? image_info(ctx, info.width, info.height, info.flags) : JS_NULL;
}

JSValue webpDecode(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    auto input = argc ? js::bytesView(ctx, argv[0]) : js::BytesView{}; texture::webp::Info info{}; size_t size{};
    if (!input || !texture::webp::info(input.span(), info) || !image_size(info.width, info.height, 4, size)) return JS_NULL;
    std::vector<uint8_t> output(size);
    if (!texture::webp::decode_rgba(input.span(), output, info.width, info.height)) return JS_NULL;
    JSValue result = image_info(ctx, info.width, info.height, info.flags); JS_SetPropertyStr(ctx, result, "pixels", js::bytes(ctx, std::move(output))); return result;
}

JSValue webpEncode(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 3) return JS_NULL; const auto width = static_cast<uint32_t>(std::max(0, intArg(ctx, argv[0], 0))); const auto height = static_cast<uint32_t>(std::max(0, intArg(ctx, argv[1], 0))); auto input = js::bytesView(ctx, argv[2]); size_t raw{};
    if (!input || !image_size(width, height, 4, raw) || input.size < raw) return JS_NULL;
    const auto capacity = argc > 6 ? static_cast<size_t>(std::max<int64_t>(0, int64Arg(ctx, argv[6], 0))) : raw + raw / 2 + 65536;
    if (capacity < 64) return JS_NULL; std::vector<uint8_t> output(capacity); size_t written{};
    const texture::webp::EncodeOptions options{static_cast<float>(argc > 3 ? doubleArg(ctx, argv[3], 75) : 75), argc > 4 && intArg(ctx, argv[4], 0), argc > 5 ? intArg(ctx, argv[5], 4) : 4};
    if (!texture::webp::encode_rgba(input.span(), width, height, width * 4, options, output, written)) return JS_NULL;
    output.resize(written); return js::bytes(ctx, std::move(output));
}

JSValue jpegInfo(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    auto input = argc ? js::bytesView(ctx, argv[0]) : js::BytesView{}; texture::jpeg::Info info{};
    if (!input || !texture::jpeg::info(input.span(), info)) return JS_NULL; JSValue output = image_info(ctx, info.width, info.height); JS_SetPropertyStr(ctx, output, "subsampling", JS_NewInt32(ctx, info.subsampling)); JS_SetPropertyStr(ctx, output, "colorspace", JS_NewInt32(ctx, info.colorspace)); return output;
}

JSValue jpegDecode(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    auto input = argc ? js::bytesView(ctx, argv[0]) : js::BytesView{}; texture::jpeg::Info info{}; size_t size{};
    if (!input || !texture::jpeg::info(input.span(), info) || !image_size(info.width, info.height, 4, size)) return JS_NULL; std::vector<uint8_t> output(size);
    if (!texture::jpeg::decode(input.span(), output, info.width, info.height, info.width * 4)) return JS_NULL; JSValue result = image_info(ctx, info.width, info.height); JS_SetPropertyStr(ctx, result, "pixels", js::bytes(ctx, std::move(output))); return result;
}

JSValue jpegEncode(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 3) return JS_NULL; const auto width = static_cast<uint32_t>(std::max(0, intArg(ctx, argv[0], 0))); const auto height = static_cast<uint32_t>(std::max(0, intArg(ctx, argv[1], 0))); auto input = js::bytesView(ctx, argv[2]); size_t raw{};
    if (!input || !image_size(width, height, 4, raw) || input.size < raw) return JS_NULL; const auto capacity = argc > 5 ? static_cast<size_t>(std::max<int64_t>(0, int64Arg(ctx, argv[5], 0))) : raw + 65536; if (capacity < 64) return JS_NULL;
    std::vector<uint8_t> output(capacity); size_t written{}; const texture::jpeg::EncodeOptions options{argc > 3 ? intArg(ctx, argv[3], 2) : 2, argc > 4 ? intArg(ctx, argv[4], 90) : 90};
    if (!texture::jpeg::encode(input.span(), width, height, width * 4, texture::jpeg::pixel_rgba, options, output, written)) return JS_NULL; output.resize(written); return js::bytes(ctx, std::move(output));
}

JSValue resize(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 5) return JS_NULL; auto input = js::bytesView(ctx, argv[0]); const auto iw = static_cast<uint32_t>(std::max(0, intArg(ctx, argv[1], 0))); const auto ih = static_cast<uint32_t>(std::max(0, intArg(ctx, argv[2], 0))); const auto ow = static_cast<uint32_t>(std::max(0, intArg(ctx, argv[3], 0))); const auto oh = static_cast<uint32_t>(std::max(0, intArg(ctx, argv[4], 0))); const auto layout = static_cast<texture::stb::Layout>(argc > 5 ? intArg(ctx, argv[5], 5) : 5); const auto channels = layout == texture::stb::Layout::one ? 1u : layout == texture::stb::Layout::two ? 2u : layout == texture::stb::Layout::rgb || layout == texture::stb::Layout::bgr ? 3u : 4u; size_t in_size{}, out_size{};
    if (!input || !image_size(iw, ih, channels, in_size) || !image_size(ow, oh, channels, out_size) || input.size < in_size) return JS_NULL; std::vector<uint8_t> output(out_size);
    if (!texture::stb::resize_u8(input.span(), iw, ih, iw * channels, output, ow, oh, ow * channels, layout, argc > 6 && intArg(ctx, argv[6], 0))) return JS_NULL; return js::bytes(ctx, std::move(output));
}

JSValue textureLoaded(JSContext* ctx, JSValueConst, int, JSValueConst*) { return JS_NewBool(ctx, texture::webp::loaded() && texture::jpeg::loaded() && texture::stb::loaded()); }

}

JSValue createWebp(JSContext* ctx) { return object(ctx, {{"info", webpInfo, 1}, {"decode", webpDecode, 1}, {"encode", webpEncode, 7}, {"loaded", textureLoaded, 0}}); }
JSValue createJpeg(JSContext* ctx) { return object(ctx, {{"info", jpegInfo, 1}, {"decode", jpegDecode, 1}, {"encode", jpegEncode, 6}, {"loaded", textureLoaded, 0}}); }
JSValue createStb(JSContext* ctx) { return object(ctx, {{"resize", resize, 7}, {"loaded", textureLoaded, 0}}); }
JSValue createTexture(JSContext* ctx) { JSValue api = JS_NewObject(ctx); JS_SetPropertyStr(ctx, api, "webp", createWebp(ctx)); JS_SetPropertyStr(ctx, api, "jpeg", createJpeg(ctx)); JS_SetPropertyStr(ctx, api, "stb", createStb(ctx)); js::setFunction(ctx, api, "loaded", textureLoaded, 0); return api; }

}
