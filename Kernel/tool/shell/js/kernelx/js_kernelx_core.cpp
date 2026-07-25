module;
#include <algorithm>
#include <cstdint>
#include <initializer_list>
#include <string>
#include <string_view>
#include <tuple>
#include <vector>
#include "lib/quickjs_ng/quickjs.h"

module tool.shell.js_engine;

import tool.shell.config_manager;
import tool.shell.js_utils;
import utility.io;

namespace kernelx {

using FnList = std::initializer_list<std::tuple<const char*, JSCFunction*, int>>;

JSValue createIo(JSContext* ctx);
JSValue createPath(JSContext* ctx);
JSValue createPng(JSContext* ctx);
JSValue createWebp(JSContext* ctx);
JSValue createJpeg(JSContext* ctx);
JSValue createStb(JSContext* ctx);
JSValue createTexture(JSContext* ctx);
JSValue createJson(JSContext* ctx);
JSValue createXml(JSContext* ctx);
JSValue createTomlplusplus(JSContext* ctx);
JSValue createRapidyaml(JSContext* ctx);
JSValue createLexbor(JSContext* ctx);
JSValue createCompress(JSContext* ctx);
JSValue createZlib(JSContext* ctx);
JSValue createDeflate(JSContext* ctx);
JSValue createGzip(JSContext* ctx);
JSValue createLz4(JSContext* ctx);
JSValue createLzma(JSContext* ctx);
JSValue createBzip2(JSContext* ctx);
JSValue createSnappy(JSContext* ctx);
JSValue createFastlz(JSContext* ctx);
JSValue createLzf(JSContext* ctx);
JSValue createBsc(JSContext* ctx);
JSValue createZpaq(JSContext* ctx);
JSValue createLzw(JSContext* ctx);
JSValue createLzfse(JSContext* ctx);
JSValue createXmem(JSContext* ctx);
JSValue createTar(JSContext* ctx);
JSValue createHeatshrink(JSContext* ctx);
JSValue createZstd(JSContext* ctx);
JSValue createIconv(JSContext* ctx);
JSValue createZip(JSContext* ctx);
JSValue createBrotli(JSContext* ctx);
JSValue createXz(JSContext* ctx);
JSValue createScript(JSContext* ctx);
JSValue createLibrary(JSContext* ctx);
JSValue createBotan(JSContext* ctx);
JSValue createDifferentiation(JSContext* ctx);
JSValue createRequests(JSContext* ctx);
JSValue createAudio(JSContext* ctx);
JSValue createBinary(JSContext* ctx);
JSValue createZstdStream(JSContext* ctx);
JSValue createZlibStream(JSContext* ctx);
JSValue createDeflateStream(JSContext* ctx);
JSValue createGzipStream(JSContext* ctx);
JSValue createBzip2Stream(JSContext* ctx);
JSValue createBrotliStream(JSContext* ctx);
JSValue createXzStream(JSContext* ctx);
JSValue createLzmaStream(JSContext* ctx);

int32_t intArg(JSContext* ctx, JSValueConst value, int32_t fallback) noexcept {
    int32_t out = fallback;
    JS_ToInt32(ctx, &out, value);
    return out;
}

int64_t int64Arg(JSContext* ctx, JSValueConst value, int64_t fallback) noexcept {
    int64_t out = fallback;
    JS_ToInt64(ctx, &out, value);
    return out;
}

double doubleArg(JSContext* ctx, JSValueConst value, double fallback) noexcept {
    double out = fallback;
    JS_ToFloat64(ctx, &out, value);
    return out;
}

bool boolArg(JSContext* ctx, JSValueConst value, bool fallback) noexcept {
    return JS_IsUndefined(value) || JS_IsNull(value) ? fallback : JS_ToBool(ctx, value) == 1;
}

std::string stringArg(JSContext* ctx, JSValueConst value, std::string_view fallback) {
    return JS_IsUndefined(value) || JS_IsNull(value) ? std::string(fallback) : js::toString(ctx, value);
}

std::vector<std::string> stringList(JSContext* ctx, JSValueConst value) {
    std::vector<std::string> out;
    if (!JS_IsArray(value)) return out;
    int64_t len = 0;
    JS_GetLength(ctx, value, &len);
    out.reserve(static_cast<size_t>(std::max<int64_t>(len, 0)));
    for (int64_t i = 0; i < len; ++i) {
        JSValue item = JS_GetPropertyUint32(ctx, value, static_cast<uint32_t>(i));
        out.push_back(js::toString(ctx, item));
        JS_FreeValue(ctx, item);
    }
    return out;
}

JSValue stringArray(JSContext* ctx, const std::vector<std::string>& values) {
    JSValue arr = JS_NewArray(ctx);
    for (uint32_t i = 0; i < values.size(); ++i) JS_SetPropertyUint32(ctx, arr, i, js::string(ctx, values[i]));
    return arr;
}

JSValue object(JSContext* ctx, FnList funcs) {
    JSValue obj = JS_NewObject(ctx);
    for (auto [name, fn, argc] : funcs) js::setFunction(ctx, obj, name, fn, argc);
    return obj;
}

void setConst(JSContext* ctx, JSValueConst obj, const char* name, JSValue value) {
    JS_SetPropertyStr(ctx, obj, name, value);
}

}

void JsEngine::registerKernelx(JSContext* ctx, JsEngine*) {
    JSValue kernelx = JS_NewObject(ctx);

    JSValue io = kernelx::createIo(ctx);
    JS_SetPropertyStr(ctx, kernelx, "io", JS_DupValue(ctx, io));
    JS_SetPropertyStr(ctx, kernelx, "fs", io);
    JS_SetPropertyStr(ctx, kernelx, "path", kernelx::createPath(ctx));
    JS_SetPropertyStr(ctx, kernelx, "png", kernelx::createPng(ctx));
    JS_SetPropertyStr(ctx, kernelx, "webp", kernelx::createWebp(ctx));
    JS_SetPropertyStr(ctx, kernelx, "jpeg", kernelx::createJpeg(ctx));
    JS_SetPropertyStr(ctx, kernelx, "stb", kernelx::createStb(ctx));
    JS_SetPropertyStr(ctx, kernelx, "texture", kernelx::createTexture(ctx));
    JS_SetPropertyStr(ctx, kernelx, "json", kernelx::createJson(ctx));
    JS_SetPropertyStr(ctx, kernelx, "xml", kernelx::createXml(ctx));
    JS_SetPropertyStr(ctx, kernelx, "toml", kernelx::createTomlplusplus(ctx));
    JS_SetPropertyStr(ctx, kernelx, "tomlplusplus", kernelx::createTomlplusplus(ctx));
    JS_SetPropertyStr(ctx, kernelx, "yaml", kernelx::createRapidyaml(ctx));
    JS_SetPropertyStr(ctx, kernelx, "rapidyaml", kernelx::createRapidyaml(ctx));
    JS_SetPropertyStr(ctx, kernelx, "lexbor", kernelx::createLexbor(ctx));
    JSValue zlib = kernelx::createZlib(ctx);
    JS_SetPropertyStr(ctx, zlib, "stream", kernelx::createZlibStream(ctx));
    JS_SetPropertyStr(ctx, kernelx, "zlib", zlib);
    JSValue deflate = kernelx::createDeflate(ctx);
    JS_SetPropertyStr(ctx, deflate, "stream", kernelx::createDeflateStream(ctx));
    JS_SetPropertyStr(ctx, kernelx, "deflate", deflate);
    JSValue gzip = kernelx::createGzip(ctx);
    JS_SetPropertyStr(ctx, gzip, "stream", kernelx::createGzipStream(ctx));
    JS_SetPropertyStr(ctx, kernelx, "gzip", gzip);
    JS_SetPropertyStr(ctx, kernelx, "lz4", kernelx::createLz4(ctx));
    JSValue lzma = kernelx::createLzma(ctx);
    JS_SetPropertyStr(ctx, lzma, "stream", kernelx::createLzmaStream(ctx));
    JS_SetPropertyStr(ctx, kernelx, "lzma", lzma);
    JSValue bzip2 = kernelx::createBzip2(ctx);
    JS_SetPropertyStr(ctx, bzip2, "stream", kernelx::createBzip2Stream(ctx));
    JS_SetPropertyStr(ctx, kernelx, "bzip2", bzip2);
    JS_SetPropertyStr(ctx, kernelx, "snappy", kernelx::createSnappy(ctx));
    JS_SetPropertyStr(ctx, kernelx, "fastlz", kernelx::createFastlz(ctx));
    JS_SetPropertyStr(ctx, kernelx, "lzf", kernelx::createLzf(ctx));
    JS_SetPropertyStr(ctx, kernelx, "bsc", kernelx::createBsc(ctx));
    JS_SetPropertyStr(ctx, kernelx, "zpaq", kernelx::createZpaq(ctx));
    JS_SetPropertyStr(ctx, kernelx, "lzw", kernelx::createLzw(ctx));
    JS_SetPropertyStr(ctx, kernelx, "lzfse", kernelx::createLzfse(ctx));
    JS_SetPropertyStr(ctx, kernelx, "xmem", kernelx::createXmem(ctx));
    JS_SetPropertyStr(ctx, kernelx, "tar", kernelx::createTar(ctx));
    JS_SetPropertyStr(ctx, kernelx, "heatshrink", kernelx::createHeatshrink(ctx));
    JSValue zstd = kernelx::createZstd(ctx);
    JS_SetPropertyStr(ctx, zstd, "stream", kernelx::createZstdStream(ctx));
    JS_SetPropertyStr(ctx, kernelx, "zstd", zstd);
    JS_SetPropertyStr(ctx, kernelx, "iconv", kernelx::createIconv(ctx));
    JS_SetPropertyStr(ctx, kernelx, "zip", kernelx::createZip(ctx));
    JSValue brotli = kernelx::createBrotli(ctx);
    JS_SetPropertyStr(ctx, brotli, "stream", kernelx::createBrotliStream(ctx));
    JS_SetPropertyStr(ctx, kernelx, "brotli", brotli);
    JSValue xz = kernelx::createXz(ctx);
    JS_SetPropertyStr(ctx, xz, "stream", kernelx::createXzStream(ctx));
    JS_SetPropertyStr(ctx, kernelx, "xz", xz);
    JS_SetPropertyStr(ctx, kernelx, "compress", kernelx::createCompress(ctx));
    JS_SetPropertyStr(ctx, kernelx, "script", kernelx::createScript(ctx));
    JS_SetPropertyStr(ctx, kernelx, "library", kernelx::createLibrary(ctx));
    JS_SetPropertyStr(ctx, kernelx, "botan", kernelx::createBotan(ctx));
    JS_SetPropertyStr(ctx, kernelx, "differentiation", kernelx::createDifferentiation(ctx));
    JS_SetPropertyStr(ctx, kernelx, "requests", kernelx::createRequests(ctx));
    JS_SetPropertyStr(ctx, kernelx, "audio", kernelx::createAudio(ctx));
    JS_SetPropertyStr(ctx, kernelx, "binary", kernelx::createBinary(ctx));

    JSValue global = JS_GetGlobalObject(ctx);
    const auto libraryBaseSetting = ConfigManager::get().getSetting("library");
    const auto libraryBase = libraryBaseSetting.empty()
        ? std::string{}
        : FileUtils::normalizeFsPath(FileUtils::joinPath(ConfigManager::get().getScriptDir().string(), libraryBaseSetting));
    JS_SetPropertyStr(ctx, global, "__kernelx_library__", js::string(ctx, libraryBase));
    JS_SetPropertyStr(ctx, global, "kernelx", kernelx);
    JS_FreeValue(ctx, global);
}
