module;
#include <cstdint>
#include <algorithm>
#include <fstream>
#include <future>
#include <initializer_list>
#include <string>
#include <string_view>
#include <tuple>
#include <utility>
#include <vector>
#include "lib/quickjs_ng/quickjs.h"

module tool.shell.js_engine;

import tool.shell.js_utils;
import utility.requests.cpr.cpr_core;
import utility.io;

namespace kernelx {

using FnList = std::initializer_list<std::tuple<const char*, JSCFunction*, int>>;
JSValue object(JSContext* ctx, FnList funcs);

namespace {

struct Options {
    std::string method{"GET"};
    js::BytesView body;
    std::string text_body;
    std::vector<std::pair<std::string, std::string>> headers;
    int64_t timeout{};
    int64_t connect_timeout{};
    bool follow_redirects{true};
    bool verify_peer{true};
    int64_t upload_size{-1};
    std::vector<std::pair<std::string, std::string>> query;
};

std::string percentEncode(std::string_view input) {
    static constexpr char hex[] = "0123456789ABCDEF";
    std::string output;
    output.reserve(input.size());
    for (const auto byte : input) {
        const auto value = static_cast<unsigned char>(byte);
        if ((value >= 'a' && value <= 'z') || (value >= 'A' && value <= 'Z') || (value >= '0' && value <= '9') || value == '-' || value == '_' || value == '.' || value == '~') output.push_back(static_cast<char>(value));
        else { output.push_back('%'); output.push_back(hex[value >> 4]); output.push_back(hex[value & 0x0F]); }
    }
    return output;
}

std::string requestUrl(std::string url, const Options& options) {
    if (options.query.empty()) return url;
    url += url.contains('?') ? '&' : '?';
    for (size_t index{}; index < options.query.size(); ++index) {
        if (index) url.push_back('&');
        url += percentEncode(options.query[index].first);
        url.push_back('=');
        url += percentEncode(options.query[index].second);
    }
    return url;
}

struct OwnedRequest {
    std::string url;
    std::string method;
    std::vector<std::pair<std::string, std::string>> headers;
    std::vector<uint8_t> body;
    int64_t timeout{};
    int64_t connect_timeout{};
    bool follow_redirects{true};
    bool verify_peer{true};

    [[nodiscard]] cpr_ns::Response perform() const {
        std::vector<cpr_ns::Header> views;
        views.reserve(headers.size());
        for (const auto& [name, value] : headers) views.push_back({name, value});
        return cpr_ns::request({url, method, views, body, timeout, connect_timeout, follow_redirects, verify_peer});
    }
};

JSValue property(JSContext* ctx, JSValueConst object, const char* name) {
    auto value = JS_GetPropertyStr(ctx, object, name);
    if (JS_IsException(value)) { js::clearException(ctx); return JS_UNDEFINED; }
    return value;
}

bool boolean(JSContext* ctx, JSValueConst object, const char* name, bool fallback) {
    auto value = property(ctx, object, name);
    const auto result = JS_IsUndefined(value) || JS_IsNull(value) ? fallback : JS_ToBool(ctx, value) == 1;
    JS_FreeValue(ctx, value);
    return result;
}

int64_t integer(JSContext* ctx, JSValueConst object, const char* name) {
    auto value = property(ctx, object, name);
    int64_t result{};
    JS_ToInt64(ctx, &result, value);
    JS_FreeValue(ctx, value);
    return result;
}

void read_headers(JSContext* ctx, JSValueConst object, Options& options) {
    auto headers = property(ctx, object, "headers");
    if (!JS_IsObject(headers)) { JS_FreeValue(ctx, headers); return; }
    JSPropertyEnum* properties{};
    uint32_t count{};
    if (JS_GetOwnPropertyNames(ctx, &properties, &count, headers, JS_GPN_STRING_MASK | JS_GPN_ENUM_ONLY) == 0) {
        options.headers.reserve(count);
        for (uint32_t index{}; index < count; ++index) {
            const char* key = JS_AtomToCString(ctx, properties[index].atom);
            auto value = JS_GetProperty(ctx, headers, properties[index].atom);
            if (key) { options.headers.emplace_back(key, js::toString(ctx, value)); JS_FreeCString(ctx, key); }
            JS_FreeValue(ctx, value);
            JS_FreeAtom(ctx, properties[index].atom);
        }
        js_free(ctx, properties);
    }
    JS_FreeValue(ctx, headers);
}

void read_query(JSContext* ctx, JSValueConst object, Options& options) {
    JSValue query = property(ctx, object, "query");
    if (!JS_IsObject(query)) { JS_FreeValue(ctx, query); return; }
    JSPropertyEnum* properties{};
    uint32_t count{};
    if (JS_GetOwnPropertyNames(ctx, &properties, &count, query, JS_GPN_STRING_MASK | JS_GPN_ENUM_ONLY) == 0) {
        options.query.reserve(count);
        for (uint32_t index{}; index < count; ++index) {
            const char* key = JS_AtomToCString(ctx, properties[index].atom);
            JSValue value = JS_GetProperty(ctx, query, properties[index].atom);
            if (key) { options.query.emplace_back(key, js::toString(ctx, value)); JS_FreeCString(ctx, key); }
            JS_FreeValue(ctx, value);
            JS_FreeAtom(ctx, properties[index].atom);
        }
        js_free(ctx, properties);
    }
    JS_FreeValue(ctx, query);
}

Options read_options(JSContext* ctx, JSValueConst value, std::string_view method) {
    Options options; options.method = method;
    if (!JS_IsObject(value)) return options;
    auto custom_method = property(ctx, value, "method");
    if (!JS_IsUndefined(custom_method) && !JS_IsNull(custom_method)) options.method = js::toString(ctx, custom_method);
    JS_FreeValue(ctx, custom_method);
    auto body = property(ctx, value, "body");
    options.body = js::bytesView(ctx, body);
    if (!options.body && !JS_IsUndefined(body) && !JS_IsNull(body)) options.text_body = js::toString(ctx, body);
    JS_FreeValue(ctx, body);
    read_headers(ctx, value, options);
    read_query(ctx, value, options);
    options.timeout = integer(ctx, value, "timeout");
    options.connect_timeout = integer(ctx, value, "connectTimeout");
    options.follow_redirects = boolean(ctx, value, "followRedirects", true);
    options.verify_peer = boolean(ctx, value, "verifyPeer", true);
    auto upload_size = property(ctx, value, "uploadSize");
    if (!JS_IsUndefined(upload_size) && !JS_IsNull(upload_size)) JS_ToInt64(ctx, &options.upload_size, upload_size);
    JS_FreeValue(ctx, upload_size);
    return options;
}

JSValue response(JSContext* ctx, cpr_ns::Response&& result) {
    JSValue output = JS_NewObject(ctx);
    JS_SetPropertyStr(ctx, output, "ok", JS_NewBool(ctx, result.code == 0 && result.status >= 200 && result.status < 400));
    JS_SetPropertyStr(ctx, output, "code", JS_NewInt32(ctx, result.code));
    JS_SetPropertyStr(ctx, output, "status", JS_NewInt64(ctx, result.status));
    JS_SetPropertyStr(ctx, output, "elapsed", JS_NewFloat64(ctx, result.elapsed));
    JS_SetPropertyStr(ctx, output, "body", js::bytes(ctx, std::move(result.body)));
    JS_SetPropertyStr(ctx, output, "headers", js::bytes(ctx, std::move(result.headers)));
    JS_SetPropertyStr(ctx, output, "error", js::string(ctx, result.error()));
    return output;
}

JSValue request(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv, std::string_view method) {
    if (argc < 1) return JS_NULL;
    const auto url = js::toString(ctx, argv[0]);
    if (url.empty()) return JS_NULL;
    auto options = read_options(ctx, argc > 1 ? argv[1] : JS_UNDEFINED, method);
    std::vector<cpr_ns::Header> headers;
    headers.reserve(options.headers.size());
    for (const auto& [name, value] : options.headers) headers.push_back({name, value});
    const auto body = options.body ? options.body.span() : cpr_ns::bytes_view{reinterpret_cast<const uint8_t*>(options.text_body.data()), options.text_body.size()};
    const auto full_url = requestUrl(url, options);
    const cpr_ns::Request input{full_url, options.method.empty() ? std::string_view{"GET"} : std::string_view{options.method}, headers, body, options.timeout, options.connect_timeout, options.follow_redirects, options.verify_peer};
    return response(ctx, cpr_ns::request(input));
}

JSValue streamRequest(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 3 || !JS_IsObject(argv[2])) return JS_NULL;
    const auto url = js::toString(ctx, argv[0]);
    if (url.empty()) return JS_NULL;
    auto options = read_options(ctx, argc > 1 ? argv[1] : JS_UNDEFINED, "GET");
    std::vector<cpr_ns::Header> headers;
    headers.reserve(options.headers.size());
    for (const auto& [name, value] : options.headers) headers.push_back({name, value});
    const auto body = options.body ? options.body.span() : cpr_ns::bytes_view{reinterpret_cast<const uint8_t*>(options.text_body.data()), options.text_body.size()};
    const auto full_url = requestUrl(url, options);
    const cpr_ns::Request input{full_url, options.method.empty() ? std::string_view{"GET"} : std::string_view{options.method}, headers, body, options.timeout, options.connect_timeout, options.follow_redirects, options.verify_peer};

    JSValue on_data = property(ctx, argv[2], "data");
    JSValue on_header = property(ctx, argv[2], "header");
    JSValue on_progress = property(ctx, argv[2], "progress");
    JSValue on_read = property(ctx, argv[2], "read");
    if (!JS_IsFunction(ctx, on_data)) { JS_FreeValue(ctx, on_data); JS_FreeValue(ctx, on_header); JS_FreeValue(ctx, on_progress); JS_FreeValue(ctx, on_read); return JS_NULL; }

    auto call = [ctx](JSValueConst callback, int count, JSValue* values) {
        if (!JS_IsFunction(ctx, callback)) return true;
        JSValue result = JS_Call(ctx, callback, JS_UNDEFINED, count, values);
        const bool ok = !JS_IsException(result) && JS_ToBool(ctx, result) != 0;
        if (JS_IsException(result)) js::clearException(ctx);
        JS_FreeValue(ctx, result);
        return ok;
    };
    cpr_ns::StreamCallbacks callbacks;
    callbacks.upload_size = options.upload_size;
    callbacks.write = [&](cpr_ns::bytes_view bytes) { JSValue value = js::bytes(ctx, bytes.data(), bytes.size()); const bool ok = call(on_data, 1, &value); JS_FreeValue(ctx, value); return ok; };
    callbacks.header = [&](cpr_ns::bytes_view bytes) { JSValue value = js::bytes(ctx, bytes.data(), bytes.size()); const bool ok = call(on_header, 1, &value); JS_FreeValue(ctx, value); return ok; };
    callbacks.progress = [&](int64_t total_down, int64_t now_down, int64_t total_up, int64_t now_up) {
        JSValue values[]{JS_NewInt64(ctx, total_down), JS_NewInt64(ctx, now_down), JS_NewInt64(ctx, total_up), JS_NewInt64(ctx, now_up)};
        const bool ok = call(on_progress, 4, values);
        for (auto& value : values) JS_FreeValue(ctx, value);
        return ok;
    };
    callbacks.read = [&](std::span<uint8_t> output) -> size_t {
        JSValue capacity = JS_NewBigUint64(ctx, output.size());
        JSValue result = JS_Call(ctx, on_read, JS_UNDEFINED, 1, &capacity);
        JS_FreeValue(ctx, capacity);
        if (JS_IsException(result)) { js::clearException(ctx); return output.size() + 1; }
        auto bytes = js::bytesView(ctx, result);
        if (!bytes || bytes.size > output.size()) { JS_FreeValue(ctx, result); return output.size() + 1; }
        std::copy_n(bytes.data, bytes.size, output.data());
        JS_FreeValue(ctx, result);
        return bytes.size;
    };
    if (!JS_IsFunction(ctx, on_read)) callbacks.read = {};
    auto result = cpr_ns::request_stream(input, callbacks);
    JS_FreeValue(ctx, on_data);
    JS_FreeValue(ctx, on_header);
    JS_FreeValue(ctx, on_progress);
    JS_FreeValue(ctx, on_read);
    return response(ctx, std::move(result));
}

JSValue download(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 2) return JS_NULL;
    const auto url = js::toString(ctx, argv[0]);
    const auto path = js::toString(ctx, argv[1]);
    if (url.empty() || path.empty()) return JS_NULL;
    auto options = read_options(ctx, argc > 2 ? argv[2] : JS_UNDEFINED, "GET");
    if (!FileUtils::writeFileBytes(path, {})) return JS_NULL;
    std::vector<cpr_ns::Header> headers;
    headers.reserve(options.headers.size());
    for (const auto& [name, value] : options.headers) headers.push_back({name, value});
    const auto body = options.body ? options.body.span() : cpr_ns::bytes_view{reinterpret_cast<const uint8_t*>(options.text_body.data()), options.text_body.size()};
    const auto full_url = requestUrl(url, options);
    const cpr_ns::Request input{full_url, options.method.empty() ? std::string_view{"GET"} : std::string_view{options.method}, headers, body, options.timeout, options.connect_timeout, options.follow_redirects, options.verify_peer};
    cpr_ns::StreamCallbacks callbacks;
    callbacks.write = [&path](cpr_ns::bytes_view bytes) { return FileUtils::appendFileBytes(path, bytes); };
    return response(ctx, cpr_ns::request_stream(input, callbacks));
}

JSValue upload(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 2) return JS_NULL;
    const auto url = js::toString(ctx, argv[0]);
    const auto path = js::toString(ctx, argv[1]);
    if (url.empty() || path.empty()) return JS_NULL;
    const auto size = FileUtils::getFileSize(path);
    if (size < 0) return JS_NULL;
    std::ifstream input(path, std::ios::binary);
    if (!input) return JS_NULL;
    auto options = read_options(ctx, argc > 2 ? argv[2] : JS_UNDEFINED, "PUT");
    options.upload_size = size;
    std::vector<cpr_ns::Header> headers;
    headers.reserve(options.headers.size());
    for (const auto& [name, value] : options.headers) headers.push_back({name, value});
    const auto full_url = requestUrl(url, options);
    const cpr_ns::Request request{full_url, options.method.empty() ? std::string_view{"PUT"} : std::string_view{options.method}, headers, {}, options.timeout, options.connect_timeout, options.follow_redirects, options.verify_peer};
    cpr_ns::StreamCallbacks callbacks;
    callbacks.upload_size = size;
    callbacks.write = [](cpr_ns::bytes_view) { return true; };
    callbacks.read = [&input](std::span<uint8_t> output) -> size_t {
        input.read(reinterpret_cast<char*>(output.data()), static_cast<std::streamsize>(output.size()));
        return static_cast<size_t>(input.gcount());
    };
    return response(ctx, cpr_ns::request_stream(request, callbacks));
}

JSValue batch(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 1 || !JS_IsArray(argv[0])) return JS_NewArray(ctx);
    int64_t count{};
    JS_GetLength(ctx, argv[0], &count);
    if (count <= 0) return JS_NewArray(ctx);
    auto options = read_options(ctx, argc > 1 ? argv[1] : JS_UNDEFINED, "GET");
    std::vector<OwnedRequest> requests;
    requests.reserve(static_cast<size_t>(count));
    const auto body = options.body ? options.body.span() : cpr_ns::bytes_view{reinterpret_cast<const uint8_t*>(options.text_body.data()), options.text_body.size()};
    for (int64_t index{}; index < count; ++index) {
        JSValue item = JS_GetPropertyUint32(ctx, argv[0], static_cast<uint32_t>(index));
        const auto url = js::toString(ctx, item);
        JS_FreeValue(ctx, item);
        if (url.empty()) continue;
        requests.push_back({requestUrl(url, options), options.method, options.headers, {body.begin(), body.end()}, options.timeout, options.connect_timeout, options.follow_redirects, options.verify_peer});
    }
    int64_t requested{};
    if (argc > 2) JS_ToInt64(ctx, &requested, argv[2]);
    const size_t concurrency = static_cast<size_t>(std::clamp<int64_t>(requested > 0 ? requested : 4, 1, 64));
    std::vector<cpr_ns::Response> results(requests.size());
    for (size_t begin{}; begin < requests.size(); begin += concurrency) {
        const auto end = std::min(begin + concurrency, requests.size());
        std::vector<std::future<cpr_ns::Response>> futures;
        futures.reserve(end - begin);
        for (size_t index = begin; index < end; ++index) futures.emplace_back(std::async(std::launch::async, [&requests, index] { return requests[index].perform(); }));
        for (size_t index = begin; index < end; ++index) results[index] = futures[index - begin].get();
    }
    JSValue output = JS_NewArray(ctx);
    for (uint32_t index{}; index < results.size(); ++index) JS_SetPropertyUint32(ctx, output, index, response(ctx, std::move(results[index])));
    return output;
}

JSValue any(JSContext* ctx, JSValueConst this_value, int argc, JSValueConst* argv) { return request(ctx, this_value, argc, argv, "GET"); }
JSValue get(JSContext* ctx, JSValueConst this_value, int argc, JSValueConst* argv) { return request(ctx, this_value, argc, argv, "GET"); }
JSValue post(JSContext* ctx, JSValueConst this_value, int argc, JSValueConst* argv) { return request(ctx, this_value, argc, argv, "POST"); }
JSValue put(JSContext* ctx, JSValueConst this_value, int argc, JSValueConst* argv) { return request(ctx, this_value, argc, argv, "PUT"); }
JSValue patch(JSContext* ctx, JSValueConst this_value, int argc, JSValueConst* argv) { return request(ctx, this_value, argc, argv, "PATCH"); }
JSValue remove(JSContext* ctx, JSValueConst this_value, int argc, JSValueConst* argv) { return request(ctx, this_value, argc, argv, "DELETE"); }
JSValue head(JSContext* ctx, JSValueConst this_value, int argc, JSValueConst* argv) { return request(ctx, this_value, argc, argv, "HEAD"); }
JSValue loaded(JSContext* ctx, JSValueConst, int, JSValueConst*) { return JS_NewBool(ctx, cpr_ns::loaded()); }
JSValue version(JSContext* ctx, JSValueConst, int, JSValueConst*) { return js::string(ctx, cpr_ns::version()); }

}

JSValue createRequests(JSContext* ctx) {
    return object(ctx, {{"request", any, 2}, {"get", get, 2}, {"post", post, 2}, {"put", put, 2}, {"patch", patch, 2}, {"delete", remove, 2}, {"head", head, 2}, {"stream", streamRequest, 3}, {"download", download, 3}, {"upload", upload, 3}, {"batch", batch, 3}, {"loaded", loaded, 0}, {"version", version, 0}});
}

}
