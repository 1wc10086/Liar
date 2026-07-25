module;
#include <array>
#include <algorithm>
#include <cstddef>
#include <cstdint>
#include <exception>
#include <memory>
#include <mutex>
#include <optional>
#include <span>
#include <utility>
#include <vector>
#include <cstring>
#include "lib/quickjs_ng/quickjs.h"
#include "lib/lzma/Alloc.h"
#include "lib/lzma/LzmaDec.h"

module tool.shell.js_engine;

import tool.shell.js_utils;
import utility.compression.zstd.zstd_core;
import utility.zlib.zlib_stream;
import utility.gzip.gzip_stream;
import utility.bzip2.bzip2_stream;
import utility.compression.brotli.brotli_core;
import utility.compression.xz.xz_core;

namespace kernelx {

int32_t intArg(JSContext* ctx, JSValueConst value, int32_t fallback) noexcept;

namespace {

constexpr size_t kChunkSize = 64 * 1024;
JSClassID zstdCompressorClass{};
JSClassID zstdDecompressorClass{};
JSClassID bufferedStreamClass{};
JSClassID brotliStreamClass{};
JSClassID xzStreamClass{};
JSClassID lzmaDecoderClass{};
std::once_flag zstdClassIds;

struct ZstdCompressor { zstd_ns::CStream stream; bool finished{}; };
struct ZstdDecompressor { zstd_ns::DStream stream; bool finished{}; };

enum class BufferedAlgorithm : uint8_t { zlib, deflate, gzip, bzip2 };
struct BufferedStream {
    BufferedAlgorithm algorithm;
    bool compression;
    bool finished{};
    std::unique_ptr<zlib_ns::StreamCompressor> zlibCompressor;
    std::unique_ptr<zlib_ns::StreamDecompressor> zlibDecompressor;
    std::unique_ptr<gzip_ns::StreamCompressor> gzipCompressor;
    std::unique_ptr<gzip_ns::StreamDecompressor> gzipDecompressor;
    std::unique_ptr<bzip2_ns::StreamCompressor> bzip2Compressor;
    std::unique_ptr<bzip2_ns::StreamDecompressor> bzip2Decompressor;
};
struct BrotliStream { bool compression; bool finished{}; brotli_ns::CStream compressor; brotli_ns::DStream decompressor; };
struct XzStream {
    bool compression;
    bool finished{};
    bool completed{};
    xz_ns::CStream compressor;
    xz_ns::DStream decompressor;

    XzStream(bool isCompression, uint32_t preset)
        : compression(isCompression), compressor(xz_ns::Options{.preset = preset}), decompressor() {}
};
struct LzmaDecoder {
    CLzmaDec decoder{};
    std::array<uint8_t, LZMA_PROPS_SIZE + sizeof(uint64_t)> header{};
    size_t headerSize{};
    uint64_t expected{};
    uint64_t written{};
    ELzmaStatus status{LZMA_STATUS_NOT_SPECIFIED};
    bool initialized{};
    bool finished{};
    LzmaDecoder() { LzmaDec_Construct(&decoder); }
    ~LzmaDecoder() { LzmaDec_Free(&decoder, &g_Alloc); }
};

void compressorFinalizer(JSRuntime*, JSValue value) { delete static_cast<ZstdCompressor*>(JS_GetOpaque(value, zstdCompressorClass)); }
void decompressorFinalizer(JSRuntime*, JSValue value) { delete static_cast<ZstdDecompressor*>(JS_GetOpaque(value, zstdDecompressorClass)); }
void bufferedStreamFinalizer(JSRuntime*, JSValue value) { delete static_cast<BufferedStream*>(JS_GetOpaque(value, bufferedStreamClass)); }
void brotliStreamFinalizer(JSRuntime*, JSValue value) { delete static_cast<BrotliStream*>(JS_GetOpaque(value, brotliStreamClass)); }
void xzStreamFinalizer(JSRuntime*, JSValue value) { delete static_cast<XzStream*>(JS_GetOpaque(value, xzStreamClass)); }
void lzmaDecoderFinalizer(JSRuntime*, JSValue value) { delete static_cast<LzmaDecoder*>(JS_GetOpaque(value, lzmaDecoderClass)); }
const JSClassDef compressorClassDef{"KernelxZstdCompressor", compressorFinalizer, nullptr, nullptr, nullptr};
const JSClassDef decompressorClassDef{"KernelxZstdDecompressor", decompressorFinalizer, nullptr, nullptr, nullptr};
const JSClassDef bufferedStreamClassDef{"KernelxBufferedStream", bufferedStreamFinalizer, nullptr, nullptr, nullptr};
const JSClassDef brotliStreamClassDef{"KernelxBrotliStream", brotliStreamFinalizer, nullptr, nullptr, nullptr};
const JSClassDef xzStreamClassDef{"KernelxXzStream", xzStreamFinalizer, nullptr, nullptr, nullptr};
const JSClassDef lzmaDecoderClassDef{"KernelxLzmaDecoder", lzmaDecoderFinalizer, nullptr, nullptr, nullptr};

ZstdCompressor* compressor(JSContext* ctx, JSValueConst value) { return static_cast<ZstdCompressor*>(JS_GetOpaque2(ctx, value, zstdCompressorClass)); }
ZstdDecompressor* decompressor(JSContext* ctx, JSValueConst value) { return static_cast<ZstdDecompressor*>(JS_GetOpaque2(ctx, value, zstdDecompressorClass)); }
BufferedStream* bufferedStream(JSContext* ctx, JSValueConst value) { return static_cast<BufferedStream*>(JS_GetOpaque2(ctx, value, bufferedStreamClass)); }
BrotliStream* brotliStream(JSContext* ctx, JSValueConst value) { return static_cast<BrotliStream*>(JS_GetOpaque2(ctx, value, brotliStreamClass)); }
XzStream* xzStream(JSContext* ctx, JSValueConst value) { return static_cast<XzStream*>(JS_GetOpaque2(ctx, value, xzStreamClass)); }
LzmaDecoder* lzmaDecoder(JSContext* ctx, JSValueConst value) { return static_cast<LzmaDecoder*>(JS_GetOpaque2(ctx, value, lzmaDecoderClass)); }

void initialize(JSContext* ctx) {
    auto* runtime = JS_GetRuntime(ctx);
    std::call_once(zstdClassIds, [&] {
        JS_NewClassID(runtime, &zstdCompressorClass);
        JS_NewClassID(runtime, &zstdDecompressorClass);
        JS_NewClassID(runtime, &bufferedStreamClass);
        JS_NewClassID(runtime, &brotliStreamClass);
        JS_NewClassID(runtime, &xzStreamClass);
        JS_NewClassID(runtime, &lzmaDecoderClass);
    });
    JS_NewClass(runtime, zstdCompressorClass, &compressorClassDef);
    JS_NewClass(runtime, zstdDecompressorClass, &decompressorClassDef);
    JS_NewClass(runtime, bufferedStreamClass, &bufferedStreamClassDef);
    JS_NewClass(runtime, brotliStreamClass, &brotliStreamClassDef);
    JS_NewClass(runtime, xzStreamClass, &xzStreamClassDef);
    JS_NewClass(runtime, lzmaDecoderClass, &lzmaDecoderClassDef);
}

JSValue lzmaPush(JSContext* ctx, JSValueConst self, int argc, JSValueConst* argv) {
    auto* value = lzmaDecoder(ctx, self);
    if (!value || value->finished || argc < 1) return JS_NULL;
    auto input = js::bytesView(ctx, argv[0]);
    if (!input) return JS_NULL;
    size_t offset{};
    const auto headerBytes = value->header.size();
    while (!value->initialized && offset < input.size) {
        value->header[value->headerSize++] = input.data[offset++];
        if (value->headerSize != headerBytes) continue;
        std::memcpy(&value->expected, value->header.data() + LZMA_PROPS_SIZE, sizeof(value->expected));
        if (value->expected == UINT64_MAX || value->expected > SIZE_MAX || LzmaDec_Allocate(&value->decoder, value->header.data(), LZMA_PROPS_SIZE, &g_Alloc) != SZ_OK) return JS_NULL;
        LzmaDec_Init(&value->decoder);
        value->initialized = true;
    }
    if (!value->initialized) return js::bytes(ctx, nullptr, 0);
    std::vector<uint8_t> output;
    std::array<uint8_t, kChunkSize> buffer{};
    while (offset < input.size && value->written < value->expected) {
        SizeT sourceSize = input.size - offset;
        const auto capacity = static_cast<SizeT>(std::min<uint64_t>(buffer.size(), value->expected - value->written));
        SizeT destinationSize = capacity;
        const auto result = LzmaDec_DecodeToBuf(&value->decoder, buffer.data(), &destinationSize, input.data + offset, &sourceSize, value->written + destinationSize == value->expected ? LZMA_FINISH_END : LZMA_FINISH_ANY, &value->status);
        if (result != SZ_OK) return JS_NULL;
        offset += sourceSize;
        value->written += destinationSize;
        output.insert(output.end(), buffer.begin(), buffer.begin() + static_cast<std::ptrdiff_t>(destinationSize));
        if (!sourceSize && !destinationSize) break;
    }
    return js::bytes(ctx, std::move(output));
}

JSValue lzmaFinish(JSContext* ctx, JSValueConst self, int, JSValueConst*) {
    auto* value = lzmaDecoder(ctx, self);
    if (!value || value->finished || !value->initialized || value->written != value->expected) return JS_NULL;
    value->finished = true;
    return value->status == LZMA_STATUS_FINISHED_WITH_MARK || value->status == LZMA_STATUS_MAYBE_FINISHED_WITHOUT_MARK
        ? js::bytes(ctx, nullptr, 0)
        : JS_NULL;
}

JSValue createLzmaDecoder(JSContext* ctx, JSValueConst, int, JSValueConst*) {
    JSValue object = JS_NewObjectClass(ctx, lzmaDecoderClass);
    if (JS_IsException(object)) return object;
    JS_SetOpaque(object, new LzmaDecoder{});
    js::setFunction(ctx, object, "push", lzmaPush, 1);
    js::setFunction(ctx, object, "finish", lzmaFinish, 0);
    return object;
}

template <class Source, class Destination, class Process>
std::optional<std::vector<uint8_t>> processChunks(Source& source, Process process) {
    std::vector<uint8_t> output;
    std::array<uint8_t, kChunkSize> buffer{};
    for (;;) {
        Destination destination{buffer.data(), buffer.size(), 0};
        const auto sourceBefore = source.pos;
        const auto result = process(destination, source);
        if (!result) return std::nullopt;
        output.insert(output.end(), buffer.begin(), buffer.begin() + static_cast<std::ptrdiff_t>(destination.pos));
        if (source.pos == source.size && destination.pos < destination.size) return output;
        if (source.pos == sourceBefore && destination.pos == 0) return std::nullopt;
    }
}

JSValue brotliPush(JSContext* ctx, JSValueConst self, int argc, JSValueConst* argv) {
    auto* value = brotliStream(ctx, self);
    if (!value || value->finished || argc < 1) return JS_NULL;
    auto input = js::bytesView(ctx, argv[0]);
    if (!input) return JS_NULL;
    brotli_ns::ConstBufferCursor source{input.data, input.size, 0};
    if (value->compression) {
        auto output = processChunks<brotli_ns::ConstBufferCursor, brotli_ns::BufferCursor>(source, [&](auto& dst, auto& src) { return value->compressor.compress(dst, src); });
        return output ? js::bytes(ctx, std::move(*output)) : JS_NULL;
    }
    auto output = processChunks<brotli_ns::ConstBufferCursor, brotli_ns::BufferCursor>(source, [&](auto& dst, auto& src) { return !brotli_ns::is_error(value->decompressor.decompress(dst, src)); });
    return output ? js::bytes(ctx, std::move(*output)) : JS_NULL;
}

JSValue brotliFinish(JSContext* ctx, JSValueConst self, int, JSValueConst*) {
    auto* value = brotliStream(ctx, self);
    if (!value || value->finished) return JS_NULL;
    value->finished = true;
    if (!value->compression) return value->decompressor.finished() ? js::bytes(ctx, nullptr, 0) : JS_NULL;
    brotli_ns::ConstBufferCursor source{};
    std::vector<uint8_t> output;
    std::array<uint8_t, kChunkSize> buffer{};
    while (!value->compressor.finished()) {
        brotli_ns::BufferCursor destination{buffer.data(), buffer.size(), 0};
        if (!value->compressor.compress(destination, source, brotli_ns::EncoderOperation::finish)) return JS_NULL;
        output.insert(output.end(), buffer.begin(), buffer.begin() + static_cast<std::ptrdiff_t>(destination.pos));
        if (!destination.pos && !value->compressor.finished()) return JS_NULL;
    }
    return js::bytes(ctx, std::move(output));
}

JSValue createBrotli(JSContext* ctx, bool compression, int argc, JSValueConst* argv) {
    JSValue object = JS_NewObjectClass(ctx, brotliStreamClass);
    if (JS_IsException(object)) return object;
    auto* value = new BrotliStream{.compression = compression};
    if ((!value->compressor || !value->decompressor) || (compression && !value->compressor.set_options({.quality = argc ? intArg(ctx, argv[0], brotli_ns::default_quality) : brotli_ns::default_quality}))) { delete value; JS_FreeValue(ctx, object); return JS_NULL; }
    JS_SetOpaque(object, value);
    js::setFunction(ctx, object, "push", brotliPush, 1);
    js::setFunction(ctx, object, "finish", brotliFinish, 0);
    return object;
}

JSValue createBrotliCompressor(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return createBrotli(ctx, true, argc, argv); }
JSValue createBrotliDecompressor(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return createBrotli(ctx, false, argc, argv); }

JSValue xzPush(JSContext* ctx, JSValueConst self, int argc, JSValueConst* argv) {
    auto* value = xzStream(ctx, self);
    if (!value || value->finished || argc < 1) return JS_NULL;
    auto input = js::bytesView(ctx, argv[0]);
    if (!input) return JS_NULL;
    xz_ns::ConstBufferCursor source{input.data, input.size, 0};
    std::vector<uint8_t> output;
    std::array<uint8_t, kChunkSize> buffer{};
    for (;;) {
        xz_ns::BufferCursor destination{buffer.data(), buffer.size(), 0};
        const auto sourceBefore = source.pos;
        const auto result = value->compression ? value->compressor.code(destination, source) : value->decompressor.code(destination, source);
        if (xz_ns::is_error(result) && result != xz_ns::Code::buf_error) return JS_NULL;
        if (!value->compression && result == xz_ns::Code::stream_end) value->completed = true;
        output.insert(output.end(), buffer.begin(), buffer.begin() + static_cast<std::ptrdiff_t>(destination.pos));
        if (source.pos == source.size && destination.pos < destination.size) break;
        if (source.pos == sourceBefore && destination.pos == 0) return JS_NULL;
    }
    return js::bytes(ctx, std::move(output));
}

JSValue xzFinish(JSContext* ctx, JSValueConst self, int, JSValueConst*) {
    auto* value = xzStream(ctx, self);
    if (!value || value->finished) return JS_NULL;
    value->finished = true;
    if (!value->compression) return value->completed ? js::bytes(ctx, nullptr, 0) : JS_NULL;
    xz_ns::ConstBufferCursor source{};
    std::vector<uint8_t> output;
    std::array<uint8_t, kChunkSize> buffer{};
    for (;;) {
        xz_ns::BufferCursor destination{buffer.data(), buffer.size(), 0};
        const auto result = value->compressor.code(destination, source, xz_ns::Action::finish);
        if (xz_ns::is_error(result)) return JS_NULL;
        output.insert(output.end(), buffer.begin(), buffer.begin() + static_cast<std::ptrdiff_t>(destination.pos));
        if (result == xz_ns::Code::stream_end) break;
        if (!destination.pos) return JS_NULL;
    }
    return js::bytes(ctx, std::move(output));
}

JSValue createXz(JSContext* ctx, bool compression, int argc, JSValueConst* argv) {
    JSValue object = JS_NewObjectClass(ctx, xzStreamClass);
    if (JS_IsException(object)) return object;
    const auto preset = static_cast<uint32_t>(std::max<int32_t>(0, argc ? intArg(ctx, argv[0], xz_ns::preset_default) : xz_ns::preset_default));
    auto* value = new XzStream{compression, preset};
    if ((!compression && !value->decompressor) || (compression && !value->compressor)) { delete value; JS_FreeValue(ctx, object); return JS_NULL; }
    JS_SetOpaque(object, value);
    js::setFunction(ctx, object, "push", xzPush, 1);
    js::setFunction(ctx, object, "finish", xzFinish, 0);
    return object;
}

JSValue createXzCompressor(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return createXz(ctx, true, argc, argv); }
JSValue createXzDecompressor(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return createXz(ctx, false, argc, argv); }

std::vector<uint8_t> feed(BufferedStream& value, std::span<const uint8_t> input, bool finish) {
    switch (value.algorithm) {
    case BufferedAlgorithm::zlib:
    case BufferedAlgorithm::deflate:
        return value.compression ? value.zlibCompressor->feed(input, finish) : value.zlibDecompressor->feed(input);
    case BufferedAlgorithm::gzip:
        return value.compression ? value.gzipCompressor->feed(input, finish) : value.gzipDecompressor->feed(input);
    case BufferedAlgorithm::bzip2:
        return value.compression ? value.bzip2Compressor->feed(input, finish) : value.bzip2Decompressor->feed(input);
    }
    return {};
}

JSValue bufferedPush(JSContext* ctx, JSValueConst self, int argc, JSValueConst* argv) {
    auto* value = bufferedStream(ctx, self);
    if (!value || value->finished || argc < 1) return JS_NULL;
    auto input = js::bytesView(ctx, argv[0]);
    if (!input) return JS_NULL;
    try { return js::bytes(ctx, feed(*value, input.span(), false)); }
    catch (const std::exception&) { return JS_NULL; }
}

JSValue bufferedFinish(JSContext* ctx, JSValueConst self, int, JSValueConst*) {
    auto* value = bufferedStream(ctx, self);
    if (!value || value->finished) return JS_NULL;
    value->finished = true;
    if (!value->compression) return js::bytes(ctx, nullptr, 0);
    try { return js::bytes(ctx, feed(*value, {}, true)); }
    catch (const std::exception&) { return JS_NULL; }
}

JSValue createBuffered(JSContext* ctx, BufferedAlgorithm algorithm, bool compression, int argc, JSValueConst* argv) {
    JSValue object = JS_NewObjectClass(ctx, bufferedStreamClass);
    if (JS_IsException(object)) return object;
    try {
        auto* value = new BufferedStream{.algorithm = algorithm, .compression = compression};
        const int level = argc ? intArg(ctx, argv[0], 9) : 9;
        switch (algorithm) {
        case BufferedAlgorithm::zlib:
        case BufferedAlgorithm::deflate:
            if (compression) value->zlibCompressor = std::make_unique<zlib_ns::StreamCompressor>(level);
            else value->zlibDecompressor = std::make_unique<zlib_ns::StreamDecompressor>();
            break;
        case BufferedAlgorithm::gzip:
            if (compression) value->gzipCompressor = std::make_unique<gzip_ns::StreamCompressor>(level);
            else value->gzipDecompressor = std::make_unique<gzip_ns::StreamDecompressor>();
            break;
        case BufferedAlgorithm::bzip2:
            if (compression) value->bzip2Compressor = std::make_unique<bzip2_ns::StreamCompressor>(std::clamp(level, 1, 9));
            else value->bzip2Decompressor = std::make_unique<bzip2_ns::StreamDecompressor>();
            break;
        }
        JS_SetOpaque(object, value);
    } catch (const std::exception&) {
        JS_FreeValue(ctx, object);
        return JS_NULL;
    }
    js::setFunction(ctx, object, "push", bufferedPush, 1);
    js::setFunction(ctx, object, "finish", bufferedFinish, 0);
    return object;
}

JSValue createZlibCompressor(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return createBuffered(ctx, BufferedAlgorithm::zlib, true, argc, argv); }
JSValue createZlibDecompressor(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return createBuffered(ctx, BufferedAlgorithm::zlib, false, argc, argv); }
JSValue createDeflateCompressor(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return createBuffered(ctx, BufferedAlgorithm::deflate, true, argc, argv); }
JSValue createDeflateDecompressor(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return createBuffered(ctx, BufferedAlgorithm::deflate, false, argc, argv); }
JSValue createGzipCompressor(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return createBuffered(ctx, BufferedAlgorithm::gzip, true, argc, argv); }
JSValue createGzipDecompressor(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return createBuffered(ctx, BufferedAlgorithm::gzip, false, argc, argv); }
JSValue createBzip2Compressor(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return createBuffered(ctx, BufferedAlgorithm::bzip2, true, argc, argv); }
JSValue createBzip2Decompressor(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return createBuffered(ctx, BufferedAlgorithm::bzip2, false, argc, argv); }

JSValue compressPush(JSContext* ctx, JSValueConst self, int argc, JSValueConst* argv) {
    auto* value = compressor(ctx, self);
    if (!value || value->finished || argc < 1) return JS_NULL;
    auto input = js::bytesView(ctx, argv[0]);
    if (!input) return JS_NULL;
    zstd_ns::ConstBufferCursor source{input.data, input.size, 0};
    std::vector<uint8_t> output;
    std::array<uint8_t, kChunkSize> buffer{};
    for (;;) {
        zstd_ns::BufferCursor destination{buffer.data(), buffer.size(), 0};
        const auto before = source.pos;
        const auto result = value->stream.compress(destination, source);
        if (zstd_ns::is_error(result)) return JS_NULL;
        output.insert(output.end(), buffer.begin(), buffer.begin() + static_cast<std::ptrdiff_t>(destination.pos));
        if (source.pos == source.size && destination.pos < destination.size) break;
        if (source.pos == before && destination.pos == 0) return JS_NULL;
    }
    return js::bytes(ctx, std::move(output));
}

JSValue compressFinish(JSContext* ctx, JSValueConst self, int, JSValueConst*) {
    auto* value = compressor(ctx, self);
    if (!value || value->finished) return JS_NULL;
    std::vector<uint8_t> output;
    std::array<uint8_t, kChunkSize> buffer{};
    size_t result{};
    do {
        zstd_ns::BufferCursor destination{buffer.data(), buffer.size(), 0};
        result = value->stream.end(destination);
        if (zstd_ns::is_error(result)) return JS_NULL;
        output.insert(output.end(), buffer.begin(), buffer.begin() + static_cast<std::ptrdiff_t>(destination.pos));
    } while (result != 0);
    value->finished = true;
    return js::bytes(ctx, std::move(output));
}

JSValue decompressPush(JSContext* ctx, JSValueConst self, int argc, JSValueConst* argv) {
    auto* value = decompressor(ctx, self);
    if (!value || value->finished || argc < 1) return JS_NULL;
    auto input = js::bytesView(ctx, argv[0]);
    if (!input) return JS_NULL;
    zstd_ns::ConstBufferCursor source{input.data, input.size, 0};
    std::vector<uint8_t> output;
    std::array<uint8_t, kChunkSize> buffer{};
    for (;;) {
        zstd_ns::BufferCursor destination{buffer.data(), buffer.size(), 0};
        const auto before = source.pos;
        const auto result = value->stream.decompress(destination, source);
        if (zstd_ns::is_error(result)) return JS_NULL;
        output.insert(output.end(), buffer.begin(), buffer.begin() + static_cast<std::ptrdiff_t>(destination.pos));
        if (source.pos == source.size && destination.pos < destination.size) break;
        if (source.pos == before && destination.pos == 0) return JS_NULL;
    }
    return js::bytes(ctx, std::move(output));
}

JSValue decompressFinish(JSContext* ctx, JSValueConst self, int, JSValueConst*) {
    auto* value = decompressor(ctx, self);
    if (!value || value->finished) return JS_FALSE;
    value->finished = true;
    return JS_TRUE;
}

JSValue createCompressor(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    JSValue object = JS_NewObjectClass(ctx, zstdCompressorClass);
    if (JS_IsException(object)) return object;
    auto* value = new ZstdCompressor{};
    const int32_t level = argc ? intArg(ctx, argv[0], 3) : 3;
    if (!value->stream || zstd_ns::is_error(value->stream.init(level))) { delete value; JS_FreeValue(ctx, object); return JS_NULL; }
    JS_SetOpaque(object, value);
    js::setFunction(ctx, object, "push", compressPush, 1);
    js::setFunction(ctx, object, "finish", compressFinish, 0);
    return object;
}

JSValue createDecompressor(JSContext* ctx, JSValueConst, int, JSValueConst*) {
    JSValue object = JS_NewObjectClass(ctx, zstdDecompressorClass);
    if (JS_IsException(object)) return object;
    auto* value = new ZstdDecompressor{};
    if (!value->stream || zstd_ns::is_error(value->stream.init())) { delete value; JS_FreeValue(ctx, object); return JS_NULL; }
    JS_SetOpaque(object, value);
    js::setFunction(ctx, object, "push", decompressPush, 1);
    js::setFunction(ctx, object, "finish", decompressFinish, 0);
    return object;
}

}

JSValue createZstdStream(JSContext* ctx) {
    initialize(ctx);
    JSValue api = JS_NewObject(ctx);
    js::setFunction(ctx, api, "compressor", createCompressor, 1);
    js::setFunction(ctx, api, "decompressor", createDecompressor, 0);
    return api;
}

JSValue createBufferedApi(JSContext* ctx, JSCFunction* compressor, JSCFunction* decompressor) {
    initialize(ctx);
    JSValue api = JS_NewObject(ctx);
    js::setFunction(ctx, api, "compressor", compressor, 1);
    js::setFunction(ctx, api, "decompressor", decompressor, 0);
    return api;
}

JSValue createZlibStream(JSContext* ctx) { return createBufferedApi(ctx, createZlibCompressor, createZlibDecompressor); }
JSValue createDeflateStream(JSContext* ctx) { return createBufferedApi(ctx, createDeflateCompressor, createDeflateDecompressor); }
JSValue createGzipStream(JSContext* ctx) { return createBufferedApi(ctx, createGzipCompressor, createGzipDecompressor); }
JSValue createBzip2Stream(JSContext* ctx) { return createBufferedApi(ctx, createBzip2Compressor, createBzip2Decompressor); }
JSValue createBrotliStream(JSContext* ctx) { return createBufferedApi(ctx, createBrotliCompressor, createBrotliDecompressor); }
JSValue createXzStream(JSContext* ctx) { return createBufferedApi(ctx, createXzCompressor, createXzDecompressor); }
JSValue createLzmaStream(JSContext* ctx) {
    initialize(ctx);
    JSValue api = JS_NewObject(ctx);
    js::setFunction(ctx, api, "decompressor", createLzmaDecoder, 0);
    return api;
}

}
