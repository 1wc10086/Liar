module;
#include <cstdint>
#include <string>
#include <string_view>
#include <utility>
#include "lib/quickjs_ng/quickjs.h"

module tool.shell.js_engine;

import tool.shell.js_utils;
import utility.encryption.botan.botan_core;
import utility.encryption.botan.botan_encryption;

namespace kernelx {
namespace {

botan_ns::view_type bytes(std::string_view value) noexcept {
    return {reinterpret_cast<const uint8_t*>(value.data()), value.size()};
}

JSValue botanHash(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 2) return JS_NULL;
    const auto input = js::bytesView(ctx, argv[0]);
    if (!input) return JS_NULL;
    const auto algorithm = js::toString(ctx, argv[1]);
    if (algorithm.empty()) return JS_NULL;
    auto output = botan_ns::Encryption::hash(algorithm, input.span());
    return output ? js::bytes(ctx, std::move(*output)) : JS_NULL;
}

JSValue botanCipher(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv, botan_ns::Direction direction) {
    if (argc < 4) return JS_NULL;
    const auto input = js::bytesView(ctx, argv[0]);
    const auto key = js::bytesView(ctx, argv[1]);
    const auto nonce = js::bytesView(ctx, argv[2]);
    if (!input || !key || !nonce) return JS_NULL;
    const auto algorithm = js::toString(ctx, argv[3]);
    if (algorithm.empty()) return JS_NULL;
    auto output = botan_ns::Encryption::cipher(algorithm, direction, input.span(), key.span(), nonce.span());
    return output ? js::bytes(ctx, std::move(*output)) : JS_NULL;
}

JSValue botanEncrypt(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return botanCipher(ctx, JS_UNDEFINED, argc, argv, botan_ns::Direction::encrypt); }
JSValue botanDecrypt(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return botanCipher(ctx, JS_UNDEFINED, argc, argv, botan_ns::Direction::decrypt); }

JSValue botanRsaEncrypt(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 3) return JS_NULL;
    const auto input = js::bytesView(ctx, argv[0]);
    if (!input) return JS_NULL;
    const auto public_key = js::toString(ctx, argv[1]);
    const auto padding = js::toString(ctx, argv[2]);
    if (public_key.empty() || padding.empty()) return JS_NULL;
    auto output = botan_ns::Encryption::rsa_encrypt(bytes(public_key), padding, input.span());
    return output ? js::bytes(ctx, std::move(*output)) : JS_NULL;
}

JSValue botanRsaDecrypt(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 3) return JS_NULL;
    const auto input = js::bytesView(ctx, argv[0]);
    if (!input) return JS_NULL;
    const auto private_key = js::toString(ctx, argv[1]);
    const auto padding = js::toString(ctx, argv[2]);
    const auto password = argc > 3 ? js::toString(ctx, argv[3]) : std::string{};
    if (private_key.empty() || padding.empty()) return JS_NULL;
    auto output = botan_ns::Encryption::rsa_decrypt(bytes(private_key), password, padding, input.span());
    return output ? js::bytes(ctx, std::move(*output)) : JS_NULL;
}

JSValue botanLoaded(JSContext* ctx, JSValueConst, int, JSValueConst*) { return JS_NewBool(ctx, botan_ns::loaded()); }
JSValue botanVersion(JSContext* ctx, JSValueConst, int, JSValueConst*) { return JS_NewUint32(ctx, botan_ns::abi_version()); }
JSValue botanError(JSContext* ctx, JSValueConst, int, JSValueConst*) { return js::string(ctx, botan_ns::last_error()); }

}

JSValue createBotan(JSContext* ctx) {
    JSValue object = JS_NewObject(ctx);
    js::setFunction(ctx, object, "hash", botanHash, 2);
    js::setFunction(ctx, object, "encrypt", botanEncrypt, 4);
    js::setFunction(ctx, object, "decrypt", botanDecrypt, 4);
    js::setFunction(ctx, object, "rsaEncrypt", botanRsaEncrypt, 3);
    js::setFunction(ctx, object, "rsaDecrypt", botanRsaDecrypt, 4);
    js::setFunction(ctx, object, "loaded", botanLoaded, 0);
    js::setFunction(ctx, object, "abiVersion", botanVersion, 0);
    js::setFunction(ctx, object, "error", botanError, 0);
    return object;
}

}
