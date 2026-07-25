module;
#include <algorithm>
#include <cstdint>
#include <cstdlib>
#include <initializer_list>
#include <limits>
#include <span>
#include <string>
#include <tuple>
#include <utility>
#include <vector>
#include "lib/quickjs_ng/quickjs.h"
#include "lib/XMemCompression/XMemCompression/XMemCompression.h"

module tool.shell.js_engine;

import tool.shell.js_utils;
import tool.shell.config_manager;
import utility.io;
import utility.zlib.zlib_compress;
import utility.zlib.zlib_uncompress;
import utility.deflate.deflate_compress;
import utility.deflate.deflate_uncompress;
import utility.gzip.gzip_compress;
import utility.gzip.gzip_uncompress;
import utility.lz4.lz4_compress;
import utility.lz4.lz4_uncompress;
import utility.lzma.lzma_compress;
import utility.lzma.lzma_uncompress;
import utility.bzip2.bzip2_compress;
import utility.bzip2.bzip2_uncompress;
import utility.snappy.snappy_compress;
import utility.snappy.snappy_uncompress;
import utility.compression.zstd.zstd_compress;
import utility.compression.zstd.zstd_uncompress;
import utility.compression.zstd.zstd_stream;
import utility.compression.zip.zip_core;
import utility.compression.zip.zip_compress;
import utility.compression.zip.zip_uncompress;
import utility.compression.xz.xz_compress;
import utility.compression.xz.xz_uncompress;
import utility.compression.xz.xz_core;
import utility.compression.brotli.brotli_compress;
import utility.compression.brotli.brotli_uncompress;
import utility.compression.brotli.brotli_core;
import utility.compression.fastlz.fastlz_uncompress;
import utility.compression.fastlz.fastlz_compress;
import utility.compression.lzf.lzf_uncompress;
import utility.compression.lzf.lzf_compress;
import utility.compression.bsc.bsc_uncompress;
import utility.compression.bsc.bsc_compress;
import utility.compression.zpaq.zpaq_uncompress;
import utility.compression.zpaq.zpaq_compress;
import utility.compression.lzw.lzw_uncompress;
import utility.compression.lzw.lzw_compress;
import utility.compression.lzfse.lzfse_uncompress;
import utility.compression.lzfse.lzfse_compress;
import utility.compression.tar.tar_uncompress;
import utility.compression.tar.tar_compress;
import utility.compression.heatshrink.heatshrink_uncompress;
import utility.compression.ncomp_core;

namespace kernelx {

using FnList = std::initializer_list<std::tuple<const char*, JSCFunction*, int>>;

int32_t intArg(JSContext* ctx, JSValueConst value, int32_t fallback) noexcept;
int64_t int64Arg(JSContext* ctx, JSValueConst value, int64_t fallback) noexcept;
JSValue object(JSContext* ctx, FnList funcs);
JSValue stringArray(JSContext* ctx, const std::vector<std::string>& values);

namespace {

template <class Fn>
JSValue compress(JSContext* ctx, int argc, JSValueConst* argv, Fn fn) {
    if (argc < 1) return js::bytes(ctx, nullptr, 0);
    auto input = js::bytesView(ctx, argv[0]);
    if (!input) return JS_NULL;
    auto out = fn(input.span(), argc > 1 ? intArg(ctx, argv[1], 9) : 9);
    return out ? js::bytes(ctx, std::move(*out)) : JS_NULL;
}

template <class Fn>
JSValue decompress(JSContext* ctx, int argc, JSValueConst* argv, Fn fn) {
    if (argc < 1) return js::bytes(ctx, nullptr, 0);
    auto input = js::bytesView(ctx, argv[0]);
    if (!input) return JS_NULL;
    auto out = fn(input.span(), argc > 1 ? static_cast<size_t>(std::max<int64_t>(0, int64Arg(ctx, argv[1], 0))) : 0ull);
    return out ? js::bytes(ctx, std::move(*out)) : JS_NULL;
}

JSValue zlibC(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return compress(ctx, argc, argv, [](auto input, int level) { return zlib_ns::Compressor::compress(input, level); }); }
JSValue zlibD(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return decompress(ctx, argc, argv, [](auto input, size_t expected) { return zlib_ns::Decompressor::decompress(input, expected); }); }
JSValue deflateC(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return compress(ctx, argc, argv, [](auto input, int level) { return deflate_ns::Compressor::compress(input, level); }); }
JSValue deflateD(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return decompress(ctx, argc, argv, [](auto input, size_t expected) { return deflate_ns::Decompressor::decompress(input, expected); }); }
JSValue gzipC(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return compress(ctx, argc, argv, [](auto input, int level) { return gzip_ns::Compressor::compress(input, level); }); }
JSValue gzipD(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return decompress(ctx, argc, argv, [](auto input, size_t expected) { return gzip_ns::Decompressor::decompress(input, expected); }); }
JSValue lz4C(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return compress(ctx, argc, argv, [](auto input, int) { return lz4_ns::Compressor::compress(input); }); }
JSValue lz4D(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return decompress(ctx, argc, argv, [](auto input, size_t expected) { return lz4_ns::Decompressor::decompress(input, expected); }); }
JSValue lzmaC(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return compress(ctx, argc, argv, [](auto input, int level) { return lzma_ns::Compressor::compress(input, level); }); }
JSValue lzmaD(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return decompress(ctx, argc, argv, [](auto input, size_t) { return lzma_ns::Decompressor::decompress(input); }); }
JSValue bzip2C(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return compress(ctx, argc, argv, [](auto input, int level) { return bzip2_ns::Compressor::compress(input, level); }); }
JSValue bzip2D(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return decompress(ctx, argc, argv, [](auto input, size_t) { return bzip2_ns::Decompressor::decompress(input); }); }
JSValue snappyC(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return compress(ctx, argc, argv, [](auto input, int) { return snappy_ns::Compressor::compress(input); }); }
JSValue snappyD(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return decompress(ctx, argc, argv, [](auto input, size_t) { return snappy_ns::Decompressor::decompress(input); }); }
JSValue fastlzD(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return decompress(ctx, argc, argv, [](auto input, size_t expected) { return fastlz_ns::Decompressor::decompress(input, expected); }); }
JSValue lzfD(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return decompress(ctx, argc, argv, [](auto input, size_t expected) { return lzf_ns::Decompressor::decompress(input, expected); }); }
JSValue bscD(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return decompress(ctx, argc, argv, [](auto input, size_t expected) { return bsc_ns::Decompressor::decompress(input, expected); }); }
JSValue zpaqD(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return decompress(ctx, argc, argv, [](auto input, size_t expected) { return zpaq_ns::Decompressor::decompress(input, expected); }); }
JSValue lzwD(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return decompress(ctx, argc, argv, [](auto input, size_t expected) { return lzw_ns::Decompressor::decompress(input, expected); }); }
JSValue lzfseD(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return decompress(ctx, argc, argv, [](auto input, size_t expected) { return lzfse_ns::Decompressor::decompress(input, expected); }); }
JSValue xmemD(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 1) return js::bytes(ctx, nullptr, 0);
    auto input = js::bytesView(ctx, argv[0]);
    if (!input || input.size == 0) return JS_NULL;
    uint8_t* raw = nullptr;
    size_t size = 0;
    if (XMemDecompressLzxTdBuffer(input.data, input.size, &raw, &size) != 0 || !raw) return JS_NULL;
    std::vector<uint8_t> output(raw, raw + size);
    std::free(raw);
    return js::bytes(ctx, std::move(output));
}
JSValue xmemC(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 1) return js::bytes(ctx, nullptr, 0);
    auto input = js::bytesView(ctx, argv[0]);
    if (!input || input.size == 0) return JS_NULL;
    uint8_t* raw = nullptr;
    size_t size = 0;
    if (XMemCompressLzxTdBuffer(input.data, input.size, &raw, &size) != 0 || !raw) return JS_NULL;
    std::vector<uint8_t> output(raw, raw + size);
    std::free(raw);
    return js::bytes(ctx, std::move(output));
}
JSValue fastlzC(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return compress(ctx, argc, argv, [](auto input, int level) { return fastlz_ns::Compressor::compress(input, level); }); }
JSValue lzfC(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return compress(ctx, argc, argv, [](auto input, int level) { return lzf_ns::Compressor::compress(input, level); }); }
JSValue bscC(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return compress(ctx, argc, argv, [](auto input, int level) { return bsc_ns::Compressor::compress(input, level); }); }
JSValue zpaqC(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return compress(ctx, argc, argv, [](auto input, int level) { return zpaq_ns::Compressor::compress(input, level); }); }
JSValue lzwC(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return compress(ctx, argc, argv, [](auto input, int level) { return lzw_ns::Compressor::compress(input, level); }); }
JSValue lzfseC(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return compress(ctx, argc, argv, [](auto input, int level) { return lzfse_ns::Compressor::compress(input, level); }); }
JSValue heatshrinkD(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 4) return JS_NULL;
    auto input = js::bytesView(ctx, argv[0]);
    if (!input) return JS_NULL;
    const auto expected = static_cast<size_t>(std::max<int64_t>(0, int64Arg(ctx, argv[1], 0)));
    const auto window = static_cast<uint8_t>(std::clamp(intArg(ctx, argv[2], 8), 4, 15));
    const auto lookahead = static_cast<uint8_t>(std::clamp(intArg(ctx, argv[3], 4), 3, static_cast<int32_t>(window) - 1));
    if (!expected) return JS_NULL;
    heatshrink_ns::Decoder decoder{256, window, lookahead};
    if (!decoder) return JS_NULL;
    if (expected == std::numeric_limits<size_t>::max()) return JS_NULL;
    std::vector<uint8_t> output(expected + 1);
    size_t input_position{};
    size_t output_position{};
    while (input_position < input.size) {
        size_t consumed{};
        const auto sink = decoder.sink(input.span().subspan(input_position), consumed);
        if (sink < 0 || !consumed) return JS_NULL;
        input_position += consumed;
        for (;;) {
            if (output_position == output.size()) return JS_NULL;
            size_t written{};
            const auto poll = decoder.poll(std::span<uint8_t>{output}.subspan(output_position), written);
            if (poll < 0) return JS_NULL;
            output_position += written;
            if (poll == 0) break;
        }
    }
    for (;;) {
        const auto finish = decoder.finish();
        if (finish < 0) return JS_NULL;
        for (;;) {
            if (output_position == output.size()) return JS_NULL;
            size_t written{};
            const auto poll = decoder.poll(std::span<uint8_t>{output}.subspan(output_position), written);
            if (poll < 0) return JS_NULL;
            output_position += written;
            if (poll == 0) break;
        }
        if (finish == 0) break;
    }
    if (output_position != expected) return JS_NULL;
    output.resize(expected);
    return js::bytes(ctx, std::move(output));
}
JSValue heatshrinkC(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 1) return js::bytes(ctx, nullptr, 0);
    auto input = js::bytesView(ctx, argv[0]);
    if (!input) return JS_NULL;
    const auto window = static_cast<uint8_t>(std::clamp(argc > 1 ? intArg(ctx, argv[1], 8) : 8, 4, 15));
    const auto lookahead = static_cast<uint8_t>(std::clamp(argc > 2 ? intArg(ctx, argv[2], 4) : 4, 3, static_cast<int32_t>(window) - 1));
    auto output = ncomp_ns::compress(ncomp_ns::Algorithm::heatshrink, input.span(), (static_cast<int32_t>(window) << 8) | lookahead);
    return output ? js::bytes(ctx, std::move(*output)) : JS_NULL;
}

struct TarCallbackState { JSContext* ctx; JSValueConst callback; };
bool tarEntry(const tar_ns::Entry& entry, void* opaque) noexcept {
    auto& state = *static_cast<TarCallbackState*>(opaque);
    if (entry.data.empty() && (entry.type == tar_ns::regular_file || entry.type == 0) && entry.size != 0) return true;
    JSValue value = JS_NewObject(state.ctx);
    JS_SetPropertyStr(state.ctx, value, "name", js::string(state.ctx, entry.name));
    JS_SetPropertyStr(state.ctx, value, "type", JS_NewUint32(state.ctx, entry.type));
    JS_SetPropertyStr(state.ctx, value, "size", JS_NewBigUint64(state.ctx, entry.size));
    JS_SetPropertyStr(state.ctx, value, "data", js::bytes(state.ctx, entry.data.data(), entry.data.size()));
    JSValue result = JS_Call(state.ctx, state.callback, JS_UNDEFINED, 1, &value);
    const bool ok = !JS_IsException(result);
    JS_FreeValue(state.ctx, result);
    return ok;
}
JSValue tarExtract(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 2 || !JS_IsFunction(ctx, argv[1])) return JS_FALSE;
    auto input = js::bytesView(ctx, argv[0]);
    if (!input) return JS_FALSE;
    TarCallbackState state{ctx, argv[1]};
    return JS_NewBool(ctx, tar_ns::Extractor::extract(input.span(), tarEntry, &state));
}

struct TarEventState {
    JSContext* ctx;
    JSValueConst onEntry;
    JSValueConst onData;
    JSValueConst onEnd;
    std::string_view activeName;
    uint32_t activeType{};
    uint64_t activeSize{};
    bool active{};
};

bool tarEventCall(TarEventState& state, JSValueConst callback, JSValue value) noexcept {
    if (!JS_IsFunction(state.ctx, callback)) { JS_FreeValue(state.ctx, value); return true; }
    JSValue result = JS_Call(state.ctx, callback, JS_UNDEFINED, 1, &value);
    JS_FreeValue(state.ctx, value);
    const bool ok = !JS_IsException(result) && JS_ToBool(state.ctx, result) != 0;
    if (JS_IsException(result)) js::clearException(state.ctx);
    JS_FreeValue(state.ctx, result);
    return ok;
}

bool tarEvent(const tar_ns::Entry& entry, void* opaque) noexcept {
    auto& state = *static_cast<TarEventState*>(opaque);
    if (!entry.data.empty()) {
        JSValue data = JS_NewObject(state.ctx);
        JS_SetPropertyStr(state.ctx, data, "name", js::string(state.ctx, entry.name));
        JS_SetPropertyStr(state.ctx, data, "data", js::bytes(state.ctx, entry.data.data(), entry.data.size()));
        return tarEventCall(state, state.onData, data);
    }
    if (state.active) {
        JSValue end = JS_NewObject(state.ctx);
        JS_SetPropertyStr(state.ctx, end, "name", js::string(state.ctx, state.activeName));
        if (!tarEventCall(state, state.onEnd, end)) return false;
    }
    state.activeName = entry.name;
    state.activeType = entry.type;
    state.activeSize = entry.size;
    state.active = true;
    JSValue info = JS_NewObject(state.ctx);
    JS_SetPropertyStr(state.ctx, info, "name", js::string(state.ctx, entry.name));
    JS_SetPropertyStr(state.ctx, info, "type", JS_NewUint32(state.ctx, entry.type));
    JS_SetPropertyStr(state.ctx, info, "size", JS_NewBigUint64(state.ctx, entry.size));
    return tarEventCall(state, state.onEntry, info);
}

JSValue tarExtractEvents(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 2 || !JS_IsObject(argv[1])) return JS_FALSE;
    auto input = js::bytesView(ctx, argv[0]);
    if (!input) return JS_FALSE;
    JSValue entry = JS_GetPropertyStr(ctx, argv[1], "entry");
    JSValue data = JS_GetPropertyStr(ctx, argv[1], "data");
    JSValue end = JS_GetPropertyStr(ctx, argv[1], "end");
    TarEventState state{ctx, entry, data, end};
    const bool ok = tar_ns::Extractor::extract(input.span(), tarEvent, &state);
    if (ok && state.active) {
        JSValue event = JS_NewObject(ctx);
        JS_SetPropertyStr(ctx, event, "name", js::string(ctx, state.activeName));
        if (!tarEventCall(state, state.onEnd, event)) { JS_FreeValue(ctx, entry); JS_FreeValue(ctx, data); JS_FreeValue(ctx, end); return JS_FALSE; }
    }
    JS_FreeValue(ctx, entry);
    JS_FreeValue(ctx, data);
    JS_FreeValue(ctx, end);
    return JS_NewBool(ctx, ok);
}
JSValue tarCreate(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 1 || !JS_IsArray(argv[0])) return JS_NULL;
    int64_t count{};
    JS_GetLength(ctx, argv[0], &count);
    if (count <= 0) return JS_NULL;
    std::vector<tar_ns::InputEntry> entries;
    entries.reserve(static_cast<size_t>(count));
    std::vector<js::BytesView> data;
    data.reserve(static_cast<size_t>(count));
    std::vector<std::string> names;
    names.reserve(static_cast<size_t>(count));
    for (int64_t i = 0; i < count; ++i) {
        JSValue item = JS_GetPropertyUint32(ctx, argv[0], static_cast<uint32_t>(i));
        JSValue name = JS_GetPropertyStr(ctx, item, "name");
        names.push_back(js::toString(ctx, name));
        JS_FreeValue(ctx, name);
        JSValue directory = JS_GetPropertyStr(ctx, item, "directory");
        const bool is_directory = JS_ToBool(ctx, directory) == 1;
        JS_FreeValue(ctx, directory);
        JSValue value = JS_GetPropertyStr(ctx, item, "data");
        data.push_back(js::bytesView(ctx, value));
        JS_FreeValue(ctx, value);
        JS_FreeValue(ctx, item);
        if (names.back().empty() || (!is_directory && !data.back())) return JS_NULL;
        entries.push_back({names.back(), is_directory ? tar_ns::view_type{} : data.back().span(), is_directory});
    }
    auto output = tar_ns::Compressor::create(entries);
    return output.empty() ? JS_NULL : js::bytes(ctx, std::move(output));
}

JSValue zstdCompress(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 1) return js::bytes(ctx, nullptr, 0);
    auto input = js::bytesView(ctx, argv[0]);
    if (!input) return JS_NULL;
    const int level = argc > 1 ? intArg(ctx, argv[1], 3) : 3;
    auto out = zstd_ns::Compressor::compress(input.span(), level);
    return out ? js::bytes(ctx, std::move(*out)) : JS_NULL;
}

JSValue zstdFrameSize(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 1) return JS_NewBigUint64(ctx, 0);
    auto input = js::bytesView(ctx, argv[0]);
    if (!input) return JS_NewBigUint64(ctx, 0);
    const auto size = zstd_ns::frame_content_size(input.span());
    return JS_NewBigUint64(ctx, size);
}

JSValue zstdCompressBound(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    const auto size = argc > 0 ? static_cast<size_t>(std::max<int64_t>(0, int64Arg(ctx, argv[0], 0))) : 0ull;
    return JS_NewBigUint64(ctx, zstd_ns::compress_bound(size));
}

JSValue zstdDecompress(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 1) return js::bytes(ctx, nullptr, 0);
    auto input = js::bytesView(ctx, argv[0]);
    if (!input) return JS_NULL;
    const auto expected = argc > 1 ? static_cast<size_t>(std::max<int64_t>(0, int64Arg(ctx, argv[1], 0))) : 0ull;
    auto out = zstd_ns::Decompressor::decompress(input.span(), expected);
    return out ? js::bytes(ctx, std::move(*out)) : JS_NULL;
}

JSValue zstdValid(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 1) return JS_FALSE;
    auto input = js::bytesView(ctx, argv[0]);
    if (!input) return JS_FALSE;
    const auto frameSize = zstd_ns::frame_content_size(input.span());
    return JS_NewBool(ctx, frameSize != zstd_ns::content_size_error);
}

JSValue xzCompress(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 1) return js::bytes(ctx, nullptr, 0);
    auto input = js::bytesView(ctx, argv[0]);
    if (!input) return JS_NULL;
    const auto preset = argc > 1 ? static_cast<uint32_t>(std::max<int64_t>(0, int64Arg(ctx, argv[1], xz_ns::preset_default))) : xz_ns::preset_default;
    auto out = xz_ns::Compressor::compress(input.span(), preset);
    return out ? js::bytes(ctx, std::move(*out)) : JS_NULL;
}

JSValue xzCompressBound(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    const auto size = argc > 0 ? static_cast<size_t>(std::max<int64_t>(0, int64Arg(ctx, argv[0], 0))) : 0ull;
    return JS_NewBigUint64(ctx, xz_ns::compress_bound(size));
}

JSValue xzDecompress(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 1) return js::bytes(ctx, nullptr, 0);
    auto input = js::bytesView(ctx, argv[0]);
    if (!input) return JS_NULL;
    const auto expected = argc > 1 ? static_cast<size_t>(std::max<int64_t>(0, int64Arg(ctx, argv[1], 0))) : 0ull;
    auto out = xz_ns::Decompressor::decompress(input.span(), expected);
    return out ? js::bytes(ctx, std::move(*out)) : JS_NULL;
}

JSValue brotliCompress(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 1) return js::bytes(ctx, nullptr, 0);
    auto input = js::bytesView(ctx, argv[0]);
    if (!input) return JS_NULL;
    const int level = argc > 1 ? intArg(ctx, argv[1], brotli_ns::default_quality) : brotli_ns::default_quality;
    auto out = brotli_ns::Compressor::compress(input.span(), level);
    return out ? js::bytes(ctx, std::move(*out)) : JS_NULL;
}

JSValue brotliCompressBound(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    const auto size = argc > 0 ? static_cast<size_t>(std::max<int64_t>(0, int64Arg(ctx, argv[0], 0))) : 0ull;
    return JS_NewBigUint64(ctx, brotli_ns::compress_bound(size));
}

JSValue brotliDecompress(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 1) return js::bytes(ctx, nullptr, 0);
    auto input = js::bytesView(ctx, argv[0]);
    if (!input) return JS_NULL;
    const auto expected = argc > 1 ? static_cast<size_t>(std::max<int64_t>(0, int64Arg(ctx, argv[1], 0))) : 0ull;
    auto out = brotli_ns::Decompressor::decompress(input.span(), expected);
    return out ? js::bytes(ctx, std::move(*out)) : JS_NULL;
}

JSValue brotliDecompressChunks(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 1 || !JS_IsArray(argv[0])) return JS_NULL;

    int64_t count = 0;
    JS_GetLength(ctx, argv[0], &count);
    if (count <= 0) return js::bytes(ctx, nullptr, 0);

    brotli_ns::DStream stream;
    if (!stream) return JS_NULL;

    const auto expected = argc > 1 ? static_cast<size_t>(std::max<int64_t>(0, int64Arg(ctx, argv[1], 0))) : 0ull;
    std::vector<uint8_t> output(std::max<size_t>(expected, 65536));
    size_t written = 0;

    for (int64_t i = 0; i < count; ++i) {
        JSValue item = JS_GetPropertyUint32(ctx, argv[0], static_cast<uint32_t>(i));
        auto input = js::bytesView(ctx, item);
        if (!input) {
            JS_FreeValue(ctx, item);
            return JS_NULL;
        }

        brotli_ns::ConstBufferCursor src{input.data, input.size, 0};
        while (src.pos < src.size) {
            if (written == output.size()) output.resize(output.empty() ? 65536 : output.size() * 2);
            brotli_ns::BufferCursor dst{output.data(), output.size(), written};
            const auto before_src = src.pos;
            const auto before_dst = dst.pos;
            const auto ret = stream.decompress(dst, src);
            if (ret == brotli_ns::DecoderResult::error) {
                JS_FreeValue(ctx, item);
                return JS_NULL;
            }
            written = dst.pos;
            if (ret == brotli_ns::DecoderResult::success && i + 1 != count) {
                JS_FreeValue(ctx, item);
                return JS_NULL;
            }
            if (src.pos == before_src && written == before_dst) {
                if (ret == brotli_ns::DecoderResult::needs_more_output) output.resize(output.empty() ? 65536 : output.size() * 2);
                else break;
            }
        }
        JS_FreeValue(ctx, item);
    }

    if (!stream.finished()) return JS_NULL;
    output.resize(written);
    return js::bytes(ctx, std::move(output));
}

JSValue zstdDecompressChunks(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 1 || !JS_IsArray(argv[0])) return JS_NULL;

    int64_t count = 0;
    JS_GetLength(ctx, argv[0], &count);
    if (count <= 0) return js::bytes(ctx, nullptr, 0);

    zstd_ns::DStream stream;
    if (!stream) return JS_NULL;
    if (zstd_ns::is_error(stream.init())) return JS_NULL;

    const auto expected = argc > 1 ? static_cast<size_t>(std::max<int64_t>(0, int64Arg(ctx, argv[1], 0))) : 0ull;
    std::vector<uint8_t> output(std::max<size_t>(expected, 65536));
    size_t written = 0;

    for (int64_t i = 0; i < count; ++i) {
        JSValue item = JS_GetPropertyUint32(ctx, argv[0], static_cast<uint32_t>(i));
        auto input = js::bytesView(ctx, item);
        if (!input) {
            JS_FreeValue(ctx, item);
            return JS_NULL;
        }

        zstd_ns::ConstBufferCursor src{input.data, input.size, 0};
        while (src.pos < src.size) {
            if (written == output.size()) output.resize(output.size() * 2);
            zstd_ns::BufferCursor dst{output.data(), output.size(), written};
            const auto before_src = src.pos;
            const auto before_dst = dst.pos;
            const auto ret = stream.decompress(dst, src);
            if (zstd_ns::is_error(ret)) return JS_NULL;
            written = dst.pos;
            if (src.pos == before_src && written == before_dst) output.resize(output.size() * 2);
        }
        JS_FreeValue(ctx, item);
    }

    output.resize(written);
    return js::bytes(ctx, std::move(output));
}

JSValue zipCompress(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 1) return js::bytes(ctx, nullptr, 0);
    auto input = js::bytesView(ctx, argv[0]);
    if (!input) return JS_NULL;
    const auto name = argc > 1 ? js::toString(ctx, argv[1]) : std::string{"data"};
    const auto level = argc > 2 ? intArg(ctx, argv[2], 6) : 6;
    const auto password = argc > 3 ? js::toString(ctx, argv[3]) : std::string{};
    auto output = zip_ns::Compressor::compress(input.span(), name, level, password);
    return output ? js::bytes(ctx, std::move(*output)) : JS_NULL;
}

JSValue zipDecompress(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 1) return js::bytes(ctx, nullptr, 0);
    auto input = js::bytesView(ctx, argv[0]);
    if (!input) return JS_NULL;
    const auto index = argc > 1 ? static_cast<uint64_t>(std::max<int64_t>(0, int64Arg(ctx, argv[1], 0))) : 0;
    const auto password = argc > 2 ? js::toString(ctx, argv[2]) : std::string{};
    auto output = zip_ns::Decompressor::extract(input.span(), index, password);
    return output ? js::bytes(ctx, std::move(*output)) : JS_NULL;
}

JSValue zipCreate(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 1 || !JS_IsArray(argv[0])) return JS_NULL;
    int64_t count{};
    JS_GetLength(ctx, argv[0], &count);
    if (count < 0) return JS_NULL;
    std::vector<zip_ns::InputEntry> entries;
    std::vector<std::string> names;
    std::vector<js::BytesView> data;
    entries.reserve(static_cast<size_t>(count));
    names.reserve(static_cast<size_t>(count));
    data.reserve(static_cast<size_t>(count));
    for (int64_t i = 0; i < count; ++i) {
        JSValue item = JS_GetPropertyUint32(ctx, argv[0], static_cast<uint32_t>(i));
        JSValue value = JS_GetPropertyStr(ctx, item, "name");
        names.push_back(js::toString(ctx, value));
        JS_FreeValue(ctx, value);
        value = JS_GetPropertyStr(ctx, item, "directory");
        const bool directory = JS_ToBool(ctx, value) == 1;
        JS_FreeValue(ctx, value);
        value = JS_GetPropertyStr(ctx, item, "data");
        data.push_back(js::bytesView(ctx, value));
        JS_FreeValue(ctx, value);
        JS_FreeValue(ctx, item);
        if (names.back().empty() || (!directory && !data.back())) return JS_NULL;
        entries.push_back({names.back(), directory ? zip_ns::view_type{} : data.back().span(), directory});
    }
    const auto level = argc > 1 ? intArg(ctx, argv[1], 6) : 6;
    const auto password = argc > 2 ? js::toString(ctx, argv[2]) : std::string{};
    auto output = zip_ns::Compressor::compress(entries, level, password);
    return output ? js::bytes(ctx, std::move(*output)) : JS_NULL;
}

JSValue zipExtractArchive(JSContext* ctx, zip_ns::view_type input, JSValueConst callback, std::string_view password) {
    zip_ns::Archive archive{input, password};
    if (!archive) return JS_FALSE;
    for (uint64_t index{}; index < archive.entry_count(); ++index) {
        zip_ns::EntryInfo info;
        if (!archive.entry_info(index, info)) return JS_FALSE;
        JSValue entry = JS_NewObject(ctx);
        JS_SetPropertyStr(ctx, entry, "name", js::string(ctx, info.name));
        JS_SetPropertyStr(ctx, entry, "size", JS_NewBigUint64(ctx, info.size));
        JS_SetPropertyStr(ctx, entry, "compressedSize", JS_NewBigUint64(ctx, info.compressed_size));
        JS_SetPropertyStr(ctx, entry, "method", JS_NewUint32(ctx, info.method));
        JS_SetPropertyStr(ctx, entry, "encryption", JS_NewUint32(ctx, info.encryption));
        JS_SetPropertyStr(ctx, entry, "directory", JS_NewBool(ctx, info.directory));
        if (!info.directory) {
            if (info.size > std::numeric_limits<size_t>::max()) { JS_FreeValue(ctx, entry); return JS_FALSE; }
            auto stream = archive.open_entry(index, password);
            if (!stream) { JS_FreeValue(ctx, entry); return JS_FALSE; }
            std::vector<uint8_t> data(static_cast<size_t>(info.size));
            size_t position{};
            while (position < data.size()) {
                const auto read = stream.read(std::span<uint8_t>{data}.subspan(position));
                if (read <= 0 || static_cast<uint64_t>(read) > data.size() - position) { JS_FreeValue(ctx, entry); return JS_FALSE; }
                position += static_cast<size_t>(read);
            }
            JS_SetPropertyStr(ctx, entry, "data", js::bytes(ctx, std::move(data)));
        }
        JSValue result = JS_Call(ctx, callback, JS_UNDEFINED, 1, &entry);
        const bool success = !JS_IsException(result);
        JS_FreeValue(ctx, result);
        if (!success) return JS_FALSE;
    }
    return JS_TRUE;
}

JSValue zipExtract(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 2 || !JS_IsFunction(ctx, argv[1])) return JS_FALSE;
    auto input = js::bytesView(ctx, argv[0]);
    if (!input) return JS_FALSE;
    const auto password = argc > 2 ? js::toString(ctx, argv[2]) : std::string{};
    return zipExtractArchive(ctx, input.span(), argv[1], password);
}

JSClassID zipArchiveClass{};
JSClassID zipEntryClass{};

struct ZipArchiveObject { JSValue owner{JS_UNDEFINED}; zip_ns::Archive archive; ZipArchiveObject(JSContext* ctx, JSValueConst input, zip_ns::view_type data, std::string_view password) : owner(JS_DupValue(ctx, input)), archive(data, password) {} };
struct ZipEntryObject { JSValue archive{JS_UNDEFINED}; zip_ns::EntryStream stream; uint64_t remaining{}; ZipEntryObject(JSContext* ctx, JSValueConst owner, zip_ns::EntryStream value, uint64_t size) : archive(JS_DupValue(ctx, owner)), stream(std::move(value)), remaining(size) {} };

void zipArchiveFinalizer(JSRuntime* runtime, JSValue value) { if (auto* archive = static_cast<ZipArchiveObject*>(JS_GetOpaque(value, zipArchiveClass))) { JS_FreeValueRT(runtime, archive->owner); delete archive; } }
void zipEntryFinalizer(JSRuntime* runtime, JSValue value) { if (auto* entry = static_cast<ZipEntryObject*>(JS_GetOpaque(value, zipEntryClass))) { JS_FreeValueRT(runtime, entry->archive); delete entry; } }
const JSClassDef zipArchiveClassDef{"KernelxZipArchive", zipArchiveFinalizer, nullptr, nullptr, nullptr};
const JSClassDef zipEntryClassDef{"KernelxZipEntry", zipEntryFinalizer, nullptr, nullptr, nullptr};

ZipArchiveObject* zipArchive(JSContext* ctx, JSValueConst value) { return static_cast<ZipArchiveObject*>(JS_GetOpaque2(ctx, value, zipArchiveClass)); }
ZipEntryObject* zipEntry(JSContext* ctx, JSValueConst value) { return static_cast<ZipEntryObject*>(JS_GetOpaque2(ctx, value, zipEntryClass)); }

void initializeZipClasses(JSContext* ctx) {
    auto* runtime = JS_GetRuntime(ctx);
    static std::once_flag classIds;
    std::call_once(classIds, [&] { JS_NewClassID(runtime, &zipArchiveClass); JS_NewClassID(runtime, &zipEntryClass); });
    JS_NewClass(runtime, zipArchiveClass, &zipArchiveClassDef);
    JS_NewClass(runtime, zipEntryClass, &zipEntryClassDef);
}

JSValue zipEntryRead(JSContext* ctx, JSValueConst self, int argc, JSValueConst* argv) {
    auto* entry = zipEntry(ctx, self);
    if (!entry || !entry->stream || !entry->remaining) return js::bytes(ctx, nullptr, 0);
    const auto requested = argc ? static_cast<size_t>(std::max<int64_t>(1, int64Arg(ctx, argv[0], 65536))) : 65536ull;
    const auto size = static_cast<size_t>(std::min<uint64_t>(requested, entry->remaining));
    std::vector<uint8_t> output(size);
    const auto read = entry->stream.read(output);
    if (read <= 0 || static_cast<uint64_t>(read) > entry->remaining) return JS_NULL;
    entry->remaining -= static_cast<uint64_t>(read);
    output.resize(static_cast<size_t>(read));
    return js::bytes(ctx, std::move(output));
}

JSValue zipOpenEntry(JSContext* ctx, JSValueConst self, int argc, JSValueConst* argv) {
    auto* archive = zipArchive(ctx, self);
    if (!archive || argc < 1) return JS_NULL;
    uint64_t index{};
    if (JS_ToBigUint64(ctx, &index, argv[0])) { js::clearException(ctx); int64_t number{}; if (JS_ToInt64(ctx, &number, argv[0]) || number < 0) return JS_NULL; index = static_cast<uint64_t>(number); }
    zip_ns::EntryInfo info;
    if (!archive->archive.entry_info(index, info) || info.directory || info.size > SIZE_MAX) return JS_NULL;
    const auto password = argc > 1 ? js::toString(ctx, argv[1]) : std::string{};
    auto stream = archive->archive.open_entry(index, password);
    if (!stream) return JS_NULL;
    JSValue object = JS_NewObjectClass(ctx, zipEntryClass);
    if (JS_IsException(object)) return object;
    JS_SetOpaque(object, new ZipEntryObject{ctx, self, std::move(stream), info.size});
    js::setFunction(ctx, object, "read", zipEntryRead, 1);
    js::setFunction(ctx, object, "remaining", [](JSContext* ctx, JSValueConst self, int, JSValueConst*) -> JSValue { const auto* entry = zipEntry(ctx, self); return JS_NewBigUint64(ctx, entry ? entry->remaining : 0); }, 0);
    return object;
}

JSValue zipOpen(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 1) return JS_NULL;
    auto input = js::bytesView(ctx, argv[0]);
    if (!input) return JS_NULL;
    initializeZipClasses(ctx);
    const auto password = argc > 1 ? js::toString(ctx, argv[1]) : std::string{};
    JSValue object = JS_NewObjectClass(ctx, zipArchiveClass);
    if (JS_IsException(object)) return object;
    auto* archive = new ZipArchiveObject{ctx, argv[0], input.span(), password};
    if (!archive->archive) { delete archive; JS_FreeValue(ctx, object); return JS_NULL; }
    JS_SetOpaque(object, archive);
    js::setFunction(ctx, object, "entryCount", [](JSContext* ctx, JSValueConst self, int, JSValueConst*) -> JSValue { const auto* archive = zipArchive(ctx, self); return JS_NewBigUint64(ctx, archive ? archive->archive.entry_count() : 0); }, 0);
    js::setFunction(ctx, object, "openEntry", zipOpenEntry, 2);
    return object;
}

JSValue zipExtractChunks(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 2 || !JS_IsArray(argv[0]) || !JS_IsFunction(ctx, argv[1])) return JS_FALSE;
    int64_t count{};
    JS_GetLength(ctx, argv[0], &count);
    if (count < 0) return JS_FALSE;
    std::vector<uint8_t> input;
    for (int64_t i = 0; i < count; ++i) {
        JSValue item = JS_GetPropertyUint32(ctx, argv[0], static_cast<uint32_t>(i));
        auto chunk = js::bytesView(ctx, item);
        JS_FreeValue(ctx, item);
        if (!chunk || chunk.size > std::numeric_limits<size_t>::max() - input.size()) return JS_FALSE;
        input.insert(input.end(), chunk.data, chunk.data + chunk.size);
    }
    const auto password = argc > 2 ? js::toString(ctx, argv[2]) : std::string{};
    return zipExtractArchive(ctx, input, argv[1], password);
}

JSValue xzDecompressChunks(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 1 || !JS_IsArray(argv[0])) return JS_NULL;

    int64_t count = 0;
    JS_GetLength(ctx, argv[0], &count);
    if (count <= 0) return js::bytes(ctx, nullptr, 0);

    xz_ns::DStream stream;
    if (!stream) return JS_NULL;

    const auto expected = argc > 1 ? static_cast<size_t>(std::max<int64_t>(0, int64Arg(ctx, argv[1], 0))) : 0ull;
    std::vector<uint8_t> output(std::max<size_t>(expected, 65536));
    size_t written = 0;
    bool ended = false;

    for (int64_t i = 0; i < count; ++i) {
        JSValue item = JS_GetPropertyUint32(ctx, argv[0], static_cast<uint32_t>(i));
        auto input = js::bytesView(ctx, item);
        if (!input) {
            JS_FreeValue(ctx, item);
            return JS_NULL;
        }

        xz_ns::ConstBufferCursor src{input.data, input.size, 0};
        while (src.pos < src.size) {
            if (written == output.size()) output.resize(output.empty() ? 65536 : output.size() * 2);
            xz_ns::BufferCursor dst{output.data(), output.size(), written};
            const auto before_src = src.pos;
            const auto before_dst = dst.pos;
            const auto ret = stream.code(dst, src);
            written = dst.pos;
            if (ret == xz_ns::Code::stream_end) ended = true;
            else if (xz_ns::is_error(ret) && ret != xz_ns::Code::buf_error) {
                JS_FreeValue(ctx, item);
                return JS_NULL;
            }
            if (src.pos == before_src && written == before_dst) {
                if (ret == xz_ns::Code::buf_error && dst.pos != dst.size) break;
                output.resize(output.empty() ? 65536 : output.size() * 2);
            }
        }

        JS_FreeValue(ctx, item);
    }

    xz_ns::ConstBufferCursor finish_src{nullptr, 0, 0};
    while (!ended) {
        if (written == output.size()) output.resize(output.empty() ? 65536 : output.size() * 2);
        xz_ns::BufferCursor dst{output.data(), output.size(), written};
        const auto before_dst = dst.pos;
        const auto ret = stream.code(dst, finish_src, xz_ns::Action::finish);
        written = dst.pos;
        if (ret == xz_ns::Code::stream_end) ended = true;
        else if (xz_ns::is_error(ret)) return JS_NULL;
        if (!ended && written == before_dst) return JS_NULL;
    }

    output.resize(written);
    return js::bytes(ctx, std::move(output));
}

JSValue pair(JSContext* ctx, JSCFunction* c, JSCFunction* d) {
    return object(ctx, {{"compress", c, 2}, {"decompress", d, 2}, {"encode", c, 2}, {"decode", d, 2}});
}

}

JSValue createZlib(JSContext* ctx) { return pair(ctx, zlibC, zlibD); }
JSValue createDeflate(JSContext* ctx) { return pair(ctx, deflateC, deflateD); }
JSValue createGzip(JSContext* ctx) { return pair(ctx, gzipC, gzipD); }
JSValue createLz4(JSContext* ctx) { return pair(ctx, lz4C, lz4D); }
JSValue createLzma(JSContext* ctx) { return pair(ctx, lzmaC, lzmaD); }
JSValue createBzip2(JSContext* ctx) { return pair(ctx, bzip2C, bzip2D); }
JSValue createSnappy(JSContext* ctx) { return pair(ctx, snappyC, snappyD); }
JSValue createFastlz(JSContext* ctx) { return pair(ctx, fastlzC, fastlzD); }
JSValue createLzf(JSContext* ctx) { return pair(ctx, lzfC, lzfD); }
JSValue createBsc(JSContext* ctx) { return pair(ctx, bscC, bscD); }
JSValue createZpaq(JSContext* ctx) { return pair(ctx, zpaqC, zpaqD); }
JSValue createLzw(JSContext* ctx) { return pair(ctx, lzwC, lzwD); }
JSValue createLzfse(JSContext* ctx) { return pair(ctx, lzfseC, lzfseD); }
JSValue createXmem(JSContext* ctx) { return object(ctx, {{"compress", xmemC, 1}, {"encode", xmemC, 1}, {"decompress", xmemD, 1}, {"decode", xmemD, 1}}); }
JSValue createHeatshrink(JSContext* ctx) { return object(ctx, {{"compress", heatshrinkC, 3}, {"encode", heatshrinkC, 3}, {"decompress", heatshrinkD, 4}, {"decode", heatshrinkD, 4}}); }
JSValue createTar(JSContext* ctx) { return object(ctx, {{"create", tarCreate, 1}, {"compress", tarCreate, 1}, {"extract", tarExtract, 2}, {"streamExtract", tarExtract, 2}, {"extractEvents", tarExtractEvents, 2}}); }
JSValue createZstd(JSContext* ctx) {
    return object(ctx, {
        {"compress", zstdCompress, 2}, {"encode", zstdCompress, 2}, {"decompress", zstdDecompress, 2}, {"decode", zstdDecompress, 2},
        {"decompressChunks", zstdDecompressChunks, 2}, {"streamDecompress", zstdDecompressChunks, 2},
        {"frameSize", zstdFrameSize, 1}, {"compressBound", zstdCompressBound, 1}, {"bound", zstdCompressBound, 1}, {"valid", zstdValid, 1},
    });
}

JSValue createZip(JSContext* ctx) {
    return object(ctx, {
        {"compress", zipCompress, 4}, {"encode", zipCompress, 4}, {"decompress", zipDecompress, 3}, {"decode", zipDecompress, 3},
        {"create", zipCreate, 3}, {"extract", zipExtract, 3}, {"streamExtract", zipExtractChunks, 3}, {"extractChunks", zipExtractChunks, 3}, {"open", zipOpen, 2},
    });
}

JSValue createBrotli(JSContext* ctx) {
    return object(ctx, {
        {"compress", brotliCompress, 2}, {"encode", brotliCompress, 2}, {"decompress", brotliDecompress, 2}, {"decode", brotliDecompress, 2},
        {"decompressChunks", brotliDecompressChunks, 2}, {"streamDecompress", brotliDecompressChunks, 2},
        {"compressBound", brotliCompressBound, 1}, {"bound", brotliCompressBound, 1},
    });
}

JSValue createXz(JSContext* ctx) {
    return object(ctx, {
        {"compress", xzCompress, 2}, {"encode", xzCompress, 2}, {"decompress", xzDecompress, 2}, {"decode", xzDecompress, 2},
        {"decompressChunks", xzDecompressChunks, 2}, {"streamDecompress", xzDecompressChunks, 2},
        {"compressBound", xzCompressBound, 1}, {"bound", xzCompressBound, 1},
    });
}

JSValue createCompress(JSContext* ctx) {
    return object(ctx, {
        {"zlib", zlibC, 2}, {"unzlib", zlibD, 2}, {"deflate", deflateC, 2}, {"inflate", deflateD, 2},
        {"gzip", gzipC, 2}, {"gunzip", gzipD, 2}, {"lz4", lz4C, 2}, {"unlz4", lz4D, 2},
        {"lzma", lzmaC, 2}, {"unlzma", lzmaD, 2}, {"bzip2", bzip2C, 2}, {"unbzip2", bzip2D, 2},
        {"snappy", snappyC, 2}, {"unsnappy", snappyD, 2}, {"zstd", zstdCompress, 2}, {"unzstd", zstdDecompress, 2},
        {"zip", zipCompress, 4}, {"unzip", zipDecompress, 3},
        {"brotli", brotliCompress, 2}, {"unbrotli", brotliDecompress, 2}, {"xz", xzCompress, 2}, {"unxz", xzDecompress, 2},
        {"unfastlz", fastlzD, 2}, {"unlzf", lzfD, 2}, {"unbsc", bscD, 2}, {"unzpaq", zpaqD, 2}, {"unlzw", lzwD, 2}, {"unlzfse", lzfseD, 2},
        {"xmem", xmemC, 1}, {"unxmem", xmemD, 1},
        {"fastlz", fastlzC, 2}, {"lzf", lzfC, 2}, {"bsc", bscC, 2}, {"zpaq", zpaqC, 2}, {"lzw", lzwC, 2}, {"lzfse", lzfseC, 2},
        {"unheatshrink", heatshrinkD, 4},
        {"heatshrink", heatshrinkC, 3},
    });
}

}
