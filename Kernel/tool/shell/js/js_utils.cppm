module;
#include <cstdint>
#include <memory>
#include <span>
#include <string>
#include <string_view>
#include <utility>
#include <vector>
#include "lib/quickjs_ng/quickjs.h"

export module tool.shell.js_utils;

export namespace js {

inline void freeVectorBuffer(JSRuntime*, void* opaque, void*) {
    delete static_cast<std::vector<uint8_t>*>(opaque);
}

struct BytesView {
    JSContext* ctx{};
    const uint8_t* data{};
    size_t size{};
    JSValue owner = JS_UNDEFINED;

    BytesView() = default;
    BytesView(JSContext* ctx, const uint8_t* data, size_t size, JSValue owner = JS_UNDEFINED) noexcept
        : ctx(ctx), data(data), size(size), owner(owner) {}
    BytesView(const BytesView&) = delete;
    BytesView& operator=(const BytesView&) = delete;
    BytesView(BytesView&& other) noexcept
        : ctx(std::exchange(other.ctx, nullptr)), data(std::exchange(other.data, nullptr)), size(std::exchange(other.size, 0)), owner(std::exchange(other.owner, JS_UNDEFINED)) {}
    BytesView& operator=(BytesView&& other) noexcept {
        if (this != &other) {
            reset();
            ctx = std::exchange(other.ctx, nullptr);
            data = std::exchange(other.data, nullptr);
            size = std::exchange(other.size, 0);
            owner = std::exchange(other.owner, JS_UNDEFINED);
        }
        return *this;
    }
    ~BytesView() { reset(); }

    [[nodiscard]] explicit operator bool() const noexcept { return data || size == 0; }
    [[nodiscard]] std::span<const uint8_t> span() const noexcept { return {data, size}; }

    void reset() noexcept {
        if (ctx && !JS_IsUndefined(owner)) JS_FreeValue(ctx, owner);
        ctx = nullptr;
        data = nullptr;
        size = 0;
        owner = JS_UNDEFINED;
    }
};

inline void clearException(JSContext* ctx) {
    JSValue ex = JS_GetException(ctx);
    JS_FreeValue(ctx, ex);
}

[[nodiscard]] inline std::string toString(JSContext* ctx, JSValueConst value) {
    size_t len = 0;
    const char* p = JS_ToCStringLen(ctx, &len, value);
    if (!p) return {};
    std::string out(p, len);
    JS_FreeCString(ctx, p);
    return out;
}

[[nodiscard]] inline JSValue string(JSContext* ctx, std::string_view value) {
    return JS_NewStringLen(ctx, value.data(), value.size());
}

[[nodiscard]] inline JSValue bytes(JSContext* ctx, const uint8_t* data, size_t size) {
    return JS_NewUint8ArrayCopy(ctx, data, size);
}

[[nodiscard]] inline JSValue bytes(JSContext* ctx, const std::vector<uint8_t>& value) {
    return bytes(ctx, value.empty() ? nullptr : value.data(), value.size());
}

[[nodiscard]] inline JSValue bytes(JSContext* ctx, std::vector<uint8_t>&& value) {
    if (value.empty()) return JS_NewUint8ArrayCopy(ctx, nullptr, 0);
    auto* owner = new std::vector<uint8_t>(std::move(value));
    JSValue out = JS_NewUint8Array(ctx, owner->data(), owner->size(), freeVectorBuffer, owner, false);
    if (JS_IsException(out)) {
        delete owner;
        return out;
    }
    return out;
}

[[nodiscard]] inline BytesView bytesView(JSContext* ctx, JSValueConst value) {
    size_t size = 0;
    uint8_t* data = JS_GetUint8Array(ctx, &size, value);
    if (data || JS_GetTypedArrayType(value) == JS_TYPED_ARRAY_UINT8) return {ctx, data, size};
    clearException(ctx);

    data = JS_GetArrayBuffer(ctx, &size, value);
    if (data || JS_IsArrayBuffer(value)) return {ctx, data, size};

    size_t offset = 0;
    size_t length = 0;
    size_t bpe = 0;
    JSValue owner = JS_GetTypedArrayBuffer(ctx, value, &offset, &length, &bpe);
    if (JS_IsException(owner)) {
        clearException(ctx);
        return {};
    }
    size_t bufferSize = 0;
    uint8_t* buffer = JS_GetArrayBuffer(ctx, &bufferSize, owner);
    if ((buffer || bufferSize == 0) && offset <= bufferSize && length <= bufferSize - offset) return {ctx, buffer + offset, length, owner};
    JS_FreeValue(ctx, owner);
    return {};
}

inline void setFunction(JSContext* ctx, JSValueConst object, const char* name, JSCFunction* fn, int argc) {
    JS_SetPropertyStr(ctx, object, name, JS_NewCFunction(ctx, fn, name, argc));
}

[[nodiscard]] inline JSValue parseJson(JSContext* ctx, std::string_view value, const char* filename = "<json>") {
    JSValue result = JS_ParseJSON(ctx, value.data(), value.size(), filename);
    if (JS_IsException(result)) {
        clearException(ctx);
        return JS_NULL;
    }
    return result;
}

[[nodiscard]] inline std::string stringify(JSContext* ctx, JSValueConst value, std::string_view fallback = "{}") {
    JSValue json = JS_JSONStringify(ctx, value, JS_UNDEFINED, JS_UNDEFINED);
    if (JS_IsException(json)) {
        clearException(ctx);
        return std::string(fallback);
    }
    auto out = toString(ctx, json);
    JS_FreeValue(ctx, json);
    return out.empty() ? std::string(fallback) : out;
}

[[nodiscard]] inline std::string base64(JSContext* ctx, JSValueConst value) {
    auto view = bytesView(ctx, value);
    if (!view || view.size == 0) return {};
    static constexpr char table[] = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
    std::string out;
    out.reserve(((view.size + 2) / 3) * 4);
    for (size_t i = 0; i < view.size; i += 3) {
        uint32_t n = static_cast<uint32_t>(view.data[i]) << 16;
        if (i + 1 < view.size) n |= static_cast<uint32_t>(view.data[i + 1]) << 8;
        if (i + 2 < view.size) n |= static_cast<uint32_t>(view.data[i + 2]);
        out.push_back(table[(n >> 18) & 0x3F]);
        out.push_back(table[(n >> 12) & 0x3F]);
        out.push_back(i + 1 < view.size ? table[(n >> 6) & 0x3F] : '=');
        out.push_back(i + 2 < view.size ? table[n & 0x3F] : '=');
    }
    return out;
}

[[nodiscard]] inline std::string errorString(JSContext* ctx) {
    JSValue ex = JS_GetException(ctx);
    auto out = toString(ctx, ex);
    JS_FreeValue(ctx, ex);
    return out;
}

[[nodiscard]] inline JSValue paramsObject(JSContext* ctx, const auto& params) {
    JSValue object = JS_NewObject(ctx);
    for (const auto& [key, value] : params) JS_SetPropertyStr(ctx, object, key.c_str(), string(ctx, value));
    return object;
}

}
