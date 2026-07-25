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
import utility.audio.audio_core;

namespace kernelx {

using FnList = std::initializer_list<std::tuple<const char*, JSCFunction*, int>>;
int32_t intArg(JSContext* ctx, JSValueConst value, int32_t fallback) noexcept;
int64_t int64Arg(JSContext* ctx, JSValueConst value, int64_t fallback) noexcept;
JSValue object(JSContext* ctx, FnList funcs);

namespace {

JSValue float32(JSContext* ctx, std::vector<float>&& value) {
    JSValue bytes = JS_NewArrayBufferCopy(ctx, reinterpret_cast<const uint8_t*>(value.data()), value.size() * sizeof(float));
    if (JS_IsException(bytes)) return bytes;
    JSValue args[]{bytes, JS_NewInt64(ctx, 0), JS_NewInt64(ctx, static_cast<int64_t>(value.size()))};
    JSValue result = JS_NewTypedArray(ctx, 3, args, JS_TYPED_ARRAY_FLOAT32);
    JS_FreeValue(ctx, bytes);
    JS_FreeValue(ctx, args[1]);
    JS_FreeValue(ctx, args[2]);
    return result;
}
JSValue int16(JSContext* ctx, std::vector<int16_t>&& value) {
    JSValue bytes = JS_NewArrayBufferCopy(ctx, reinterpret_cast<const uint8_t*>(value.data()), value.size() * sizeof(int16_t));
    if (JS_IsException(bytes)) return bytes;
    JSValue args[]{bytes, JS_NewInt64(ctx, 0), JS_NewInt64(ctx, static_cast<int64_t>(value.size()))};
    JSValue result = JS_NewTypedArray(ctx, 3, args, JS_TYPED_ARRAY_INT16);
    JS_FreeValue(ctx, bytes);
    JS_FreeValue(ctx, args[1]);
    JS_FreeValue(ctx, args[2]);
    return result;
}

JSValue decode(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv, audio_ns::Codec codec) {
    if (!argc) return JS_NULL;
    auto input = js::bytesView(ctx, argv[0]);
    if (!input) return JS_NULL;
    const auto capacity = static_cast<uint32_t>(std::clamp<int64_t>(argc > 1 ? int64Arg(ctx, argv[1], 48000) : 48000, 1, std::numeric_limits<uint32_t>::max()));
    const auto rate = static_cast<uint32_t>(std::clamp<int64_t>(argc > 2 ? int64Arg(ctx, argv[2], 48000) : 48000, 1, 192000));
    auto output = audio_ns::decode(codec, input.span(), capacity, rate);
    if (!output.info.channels) return JS_NULL;
    JSValue result = JS_NewObject(ctx);
    JS_SetPropertyStr(ctx, result, "sampleRate", JS_NewUint32(ctx, output.info.sample_rate));
    JS_SetPropertyStr(ctx, result, "channels", JS_NewUint32(ctx, output.info.channels));
    JS_SetPropertyStr(ctx, result, "frames", JS_NewBigUint64(ctx, output.info.frames));
    JS_SetPropertyStr(ctx, result, "pcm", float32(ctx, std::move(output.samples)));
    return result;
}

JSValue wavDecode(JSContext* ctx, JSValueConst t, int argc, JSValueConst* argv) { return decode(ctx, t, argc, argv, audio_ns::Codec::wav); }
JSValue mp3Decode(JSContext* ctx, JSValueConst t, int argc, JSValueConst* argv) { return decode(ctx, t, argc, argv, audio_ns::Codec::mp3); }
JSValue vorbisDecode(JSContext* ctx, JSValueConst t, int argc, JSValueConst* argv) { return decode(ctx, t, argc, argv, audio_ns::Codec::vorbis); }
JSValue xmDecode(JSContext* ctx, JSValueConst t, int argc, JSValueConst* argv) { return decode(ctx, t, argc, argv, audio_ns::Codec::xm); }
JSValue flacDecode(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (!argc) return JS_NULL; auto input = js::bytesView(ctx, argv[0]); if (!input) return JS_NULL; const auto capacity = static_cast<uint32_t>(std::clamp<int64_t>(argc > 1 ? int64Arg(ctx, argv[1], 48000) : 48000, 1, std::numeric_limits<uint32_t>::max())); auto output = audio_ns::decode_flac(input.span(), capacity); if (!output.info.channels) return JS_NULL;
    JSValue result = JS_NewObject(ctx); JS_SetPropertyStr(ctx, result, "sampleRate", JS_NewUint32(ctx, output.info.sample_rate)); JS_SetPropertyStr(ctx, result, "channels", JS_NewUint32(ctx, output.info.channels)); JS_SetPropertyStr(ctx, result, "frames", JS_NewBigUint64(ctx, output.info.frames)); JS_SetPropertyStr(ctx, result, "pcm", float32(ctx, std::move(output.samples))); return result;
}
JSValue wmaDecode(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (!argc) return JS_NULL; auto input = js::bytesView(ctx, argv[0]); if (!input) return JS_NULL; const auto capacity = static_cast<uint32_t>(std::clamp<int64_t>(argc > 1 ? int64Arg(ctx, argv[1], 48000) : 48000, 1, std::numeric_limits<uint32_t>::max())); auto output = audio_ns::decode_wma(input.span(), capacity); if (!output.info.channels) return JS_NULL;
    JSValue result = JS_NewObject(ctx); JS_SetPropertyStr(ctx, result, "sampleRate", JS_NewUint32(ctx, output.info.sample_rate)); JS_SetPropertyStr(ctx, result, "channels", JS_NewUint32(ctx, output.info.channels)); JS_SetPropertyStr(ctx, result, "pcm", float32(ctx, std::move(output.samples))); return result;
}
JSValue aacDecode(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (!argc) return JS_NULL; auto input = js::bytesView(ctx, argv[0]); if (!input) return JS_NULL; const auto capacity = static_cast<uint32_t>(std::clamp<int64_t>(argc > 1 ? int64Arg(ctx, argv[1], 48000) : 48000, 1, std::numeric_limits<uint32_t>::max())); auto output = audio_ns::decode_aac(input.span(), capacity); if (!output.info.channels) return JS_NULL;
    JSValue result = JS_NewObject(ctx); JS_SetPropertyStr(ctx, result, "sampleRate", JS_NewUint32(ctx, output.info.sample_rate)); JS_SetPropertyStr(ctx, result, "channels", JS_NewUint32(ctx, output.info.channels)); JS_SetPropertyStr(ctx, result, "pcm", float32(ctx, std::move(output.samples))); return result;
}
JSValue alacDecode(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 2) return JS_NULL; auto cookie = js::bytesView(ctx, argv[0]); auto input = js::bytesView(ctx, argv[1]); if (!cookie || !input) return JS_NULL; const auto capacity = static_cast<uint32_t>(std::clamp<int64_t>(argc > 2 ? int64Arg(ctx, argv[2], 4096) : 4096, 1, std::numeric_limits<uint32_t>::max())); audio_ns::Info info{}; auto output = audio_ns::decode_alac_s16(cookie.span(), input.span(), capacity, info); if (!info.channels) return JS_NULL;
    JSValue result = JS_NewObject(ctx); JS_SetPropertyStr(ctx, result, "sampleRate", JS_NewUint32(ctx, info.sample_rate)); JS_SetPropertyStr(ctx, result, "channels", JS_NewUint32(ctx, info.channels)); JS_SetPropertyStr(ctx, result, "frames", JS_NewBigUint64(ctx, info.frames)); JS_SetPropertyStr(ctx, result, "pcm", int16(ctx, std::move(output))); return result;
}
JSValue alacEncode(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 3) return JS_NULL; auto input = js::bytesView(ctx, argv[0]); const auto rate = static_cast<uint32_t>(std::clamp(intArg(ctx, argv[1], 44100), 1, 192000)); const auto channels = static_cast<uint32_t>(std::clamp(intArg(ctx, argv[2], 2), 1, 8)); const auto frame_length = static_cast<uint32_t>(std::clamp(argc > 3 ? intArg(ctx, argv[3], 4096) : 4096, 1, 65535)); if (!input || input.size % sizeof(int16_t) || input.size / sizeof(int16_t) % channels) return JS_NULL; auto encoded = audio_ns::encode_alac_s16(std::span<const int16_t>{reinterpret_cast<const int16_t*>(input.data), input.size / sizeof(int16_t)}, rate, channels, frame_length); if (encoded.data.empty()) return JS_NULL; JSValue result = JS_NewObject(ctx); JS_SetPropertyStr(ctx, result, "cookie", js::bytes(ctx, std::move(encoded.cookie))); JS_SetPropertyStr(ctx, result, "data", js::bytes(ctx, std::move(encoded.data))); return result;
}
JSValue wavEncode(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 3) return JS_NULL; auto input = js::bytesView(ctx, argv[0]); const auto rate = static_cast<uint32_t>(std::clamp(intArg(ctx, argv[1], 48000), 1, 192000)); const auto channels = static_cast<uint32_t>(std::clamp(intArg(ctx, argv[2], 2), 1, 8)); if (!input || input.size % sizeof(float) || input.size / sizeof(float) % channels) return JS_NULL; auto wav = audio_ns::encode_wav_f32(std::span<const float>{reinterpret_cast<const float*>(input.data), input.size / sizeof(float)}, rate, channels); return wav.empty() ? JS_NULL : js::bytes(ctx, std::move(wav));
}
JSValue flacEncode(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 3) return JS_NULL; auto input = js::bytesView(ctx, argv[0]); const auto rate = static_cast<uint32_t>(std::clamp(intArg(ctx, argv[1], 48000), 1, 192000)); const auto channels = static_cast<uint32_t>(std::clamp(intArg(ctx, argv[2], 2), 1, 8)); const auto compression = static_cast<uint32_t>(std::clamp(argc > 3 ? intArg(ctx, argv[3], 5) : 5, 0, 8)); if (!input || input.size % sizeof(float) || input.size / sizeof(float) % channels) return JS_NULL; auto flac = audio_ns::encode_flac_f32(std::span<const float>{reinterpret_cast<const float*>(input.data), input.size / sizeof(float)}, rate, channels, compression); return flac.empty() ? JS_NULL : js::bytes(ctx, std::move(flac));
}
JSValue status(JSContext* ctx, JSValueConst, int, JSValueConst*) { JSValue result = JS_NewObject(ctx); JS_SetPropertyStr(ctx, result, "loaded", JS_NewBool(ctx, audio_ns::loaded())); JS_SetPropertyStr(ctx, result, "path", js::string(ctx, audio_ns::library_path())); JS_SetPropertyStr(ctx, result, "error", js::string(ctx, audio_ns::load_error())); return result; }
JSValue loaded(JSContext* ctx, JSValueConst, int, JSValueConst*) { return JS_NewBool(ctx, audio_ns::loaded()); }

}

JSValue createAudio(JSContext* ctx) { return object(ctx, {{"wav", wavDecode, 3}, {"encodeWav", wavEncode, 3}, {"mp3", mp3Decode, 3}, {"vorbis", vorbisDecode, 3}, {"flac", flacDecode, 2}, {"encodeFlac", flacEncode, 4}, {"wma", wmaDecode, 2}, {"aac", aacDecode, 2}, {"alac", alacDecode, 3}, {"encodeAlac", alacEncode, 4}, {"xm", xmDecode, 3}, {"status", status, 0}, {"loaded", loaded, 0}}); }

}
