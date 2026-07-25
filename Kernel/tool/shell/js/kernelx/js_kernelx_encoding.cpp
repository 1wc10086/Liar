module;
#include <string>
#include <utility>
#include "lib/quickjs_ng/quickjs.h"

module tool.shell.js_engine;

import tool.shell.js_utils;
import utility.encoding.iconv.iconv_encoding;

namespace kernelx {
namespace {

JSValue iconvConvert(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 3) return JS_NULL;
    const auto input = js::bytesView(ctx, argv[0]);
    if (!input) return JS_NULL;
    const auto from_encoding = js::toString(ctx, argv[1]);
    const auto to_encoding = js::toString(ctx, argv[2]);
    if (from_encoding.empty() || to_encoding.empty()) return JS_NULL;
    auto output = iconv_ns::Encoding::convert(input.span(), from_encoding.c_str(), to_encoding.c_str());
    return output ? js::bytes(ctx, std::move(*output)) : JS_NULL;
}

}

JSValue createIconv(JSContext* ctx) {
    JSValue object = JS_NewObject(ctx);
    js::setFunction(ctx, object, "convert", iconvConvert, 3);
    return object;
}

}
