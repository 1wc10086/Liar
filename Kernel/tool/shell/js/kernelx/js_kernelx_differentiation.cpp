module;
#include <algorithm>
#include <cstdint>
#include <initializer_list>
#include <limits>
#include <tuple>
#include <utility>
#include <vector>
#include "lib/quickjs_ng/quickjs.h"

module tool.shell.js_engine;

import tool.shell.js_utils;
import utility.differentiation.vcdiff.vcdiff_core;

namespace kernelx {

using FnList = std::initializer_list<std::tuple<const char*, JSCFunction*, int>>;
int32_t intArg(JSContext* ctx, JSValueConst value, int32_t fallback) noexcept;
int64_t int64Arg(JSContext* ctx, JSValueConst value, int64_t fallback) noexcept;
JSValue object(JSContext* ctx, FnList funcs);

namespace {

size_t sizeArg(JSContext* ctx, JSValueConst value, size_t fallback) noexcept {
    const auto value64 = int64Arg(ctx, value, static_cast<int64_t>(std::min(fallback, static_cast<size_t>(std::numeric_limits<int64_t>::max()))));
    return value64 > 0 ? static_cast<size_t>(value64) : 0;
}

JSValue encode(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 2) return JS_NULL;
    auto dictionary = js::bytesView(ctx, argv[0]);
    auto input = js::bytesView(ctx, argv[1]);
    if (!dictionary || !input) return JS_NULL;
    const auto format = static_cast<vcdiff::Format>(argc > 2 ? std::max(0, intArg(ctx, argv[2], 0)) : 0);
    const auto target_matching = argc < 4 || intArg(ctx, argv[3], 1) != 0;
    const auto overhead = input.size / 16 + 4096;
    const auto minimum = input.size > std::numeric_limits<size_t>::max() - overhead ? 0 : input.size + overhead;
    const auto capacity = argc > 4 ? sizeArg(ctx, argv[4], minimum) : minimum;
    if (!capacity) return JS_NULL;
    std::vector<uint8_t> output(capacity);
    size_t written{};
    if (!vcdiff::encode_to(dictionary.span(), input.span(), output, written, format, target_matching)) return JS_NULL;
    output.resize(written);
    return js::bytes(ctx, std::move(output));
}

JSValue decode(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 3) return JS_NULL;
    auto dictionary = js::bytesView(ctx, argv[0]);
    auto input = js::bytesView(ctx, argv[1]);
    const auto capacity = sizeArg(ctx, argv[2], 0);
    if (!dictionary || !input || !capacity) return JS_NULL;
    const auto max_file = argc > 3 ? sizeArg(ctx, argv[3], capacity) : capacity;
    const auto max_window = argc > 4 ? sizeArg(ctx, argv[4], capacity) : capacity;
    const auto allow_target = argc < 6 || intArg(ctx, argv[5], 1) != 0;
    std::vector<uint8_t> output(capacity);
    size_t written{};
    if (!vcdiff::decode_to(dictionary.span(), input.span(), output, written, max_file, max_window, allow_target)) return JS_NULL;
    output.resize(written);
    return js::bytes(ctx, std::move(output));
}

JSValue loaded(JSContext* ctx, JSValueConst, int, JSValueConst*) { return JS_NewBool(ctx, vcdiff::loaded()); }
JSValue abiVersion(JSContext* ctx, JSValueConst, int, JSValueConst*) { return JS_NewUint32(ctx, vcdiff::abi_version()); }

}

JSValue createDifferentiation(JSContext* ctx) { return object(ctx, {{"encode", encode, 5}, {"decode", decode, 6}, {"loaded", loaded, 0}, {"abiVersion", abiVersion, 0}}); }

}
