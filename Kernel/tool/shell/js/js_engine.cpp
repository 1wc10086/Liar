module;
#include <algorithm>
#include <atomic>
#include <cctype>
#include <filesystem>
#include <functional>
#include <future>
#include <optional>
#include <memory>
#include <mutex>
#include <string>
#include <string_view>
#include <system_error>
#include <thread>
#include <unordered_map>
#include <utility>
#include <vector>
#include <jni.h>
#include "lib/quickjs_ng/quickjs.h"

module tool.shell.js_engine;

import tool.shell.js_utils;
import utility.io;
import utility.io.concurrent;
import utility.json;
import tool.shell.config_manager;
import tool.shell.plugin_base;
import tool.shell.core_utils;

namespace fs = std::filesystem;

namespace {

size_t manifestBufferSize(JSContext* ctx, JSValueConst item, size_t def) {
    JSValue v = JS_GetPropertyStr(ctx, item, "buffer_size");
    if (JS_IsUndefined(v) || JS_IsNull(v)) {
        JS_FreeValue(ctx, v);
        return def;
    }
    auto s = js::toString(ctx, v);
    JS_FreeValue(ctx, v);
    return ShellCore::parseByteSize(s, def);
}

size_t runtimeBufferSize(const JsPluginManifest& manifest) noexcept {
    return manifest.bufferSize ? manifest.bufferSize : ConfigManager::get().getJsBufferSize();
}

std::string valueString(json::Value value) {
    if (value.is_str()) return std::string(value.get_str_view());
    if (value.is_bool()) return value.get_bool() ? "true" : "false";
    if (value.is_int()) return std::to_string(value.get_sint());
    if (value.is_real()) return std::to_string(value.get_real());
    return {};
}

bool valueBool(json::Value value, bool def) noexcept {
    return value.is_bool() ? value.get_bool() : def;
}

std::optional<std::string_view> staticManifestJson(std::string_view source) {
    constexpr std::string_view marker = "/* kernelx-manifest";
    const size_t markerStart = source.find(marker);
    if (markerStart == std::string_view::npos) return std::nullopt;
    const size_t jsonStart = markerStart + marker.size();
    const size_t commentEnd = source.find("*/", jsonStart);
    if (commentEnd == std::string_view::npos) return std::string_view{};
    auto payload = source.substr(jsonStart, commentEnd - jsonStart);
    while (!payload.empty() && std::isspace(static_cast<unsigned char>(payload.front()))) payload.remove_prefix(1);
    while (!payload.empty() && std::isspace(static_cast<unsigned char>(payload.back()))) payload.remove_suffix(1);
    return payload;
}

void appendLoadError(std::vector<JsPluginManifest>& manifests, std::string_view path, std::string message, std::string_view impl) {
    JsPluginManifest manifest;
    manifest.implementation = impl;
    manifest.scriptPath = path;
    manifest.id = "js.load-error." + fs::path(path).generic_string();
    manifest.loadError = std::string(path) + ": " + std::move(message);
    manifest.params.push_back({.name = "InputFile", .type = "file", .required = false});
    manifests.push_back(std::move(manifest));
}

bool appendStaticManifests(
    std::vector<JsPluginManifest>& manifests,
    std::string_view path,
    std::string_view source,
    std::string_view impl,
    const std::unordered_map<std::string, std::string>& locales) {

    const auto payload = staticManifestJson(source);
    if (!payload) return false;
    const auto document = json::Document::parse(*payload);
    if (!document || (!document.root().is_obj() && !document.root().is_arr())) {
        appendLoadError(manifests, path, "invalid kernelx-manifest JSON", impl);
        return true;
    }

    const auto root = document.root();
    const size_t count = root.is_arr() ? root.arr_size() : 1;
    const size_t manifestStart = manifests.size();
    for (size_t i = 0; i < count; ++i) {
        const auto item = root.is_arr() ? root.arr_get(i) : root;
        if (!item.is_obj()) continue;
        JsPluginManifest manifest;
        manifest.implementation = valueString(item.obj_get("implementation"));
        if (manifest.implementation.empty()) manifest.implementation = impl;
        manifest.scriptPath = path;
        manifest.bufferSize = ShellCore::parseByteSize(valueString(item.obj_get("buffer_size")), 0);
        manifest.id = valueString(item.obj_get("id"));
        if (manifest.id.empty()) manifest.id = valueString(item.obj_get("name"));
        if (manifest.id.empty() || manifest.implementation != impl) continue;

        if (const auto params = item.obj_get("params"); params.is_arr()) {
            manifest.params.reserve(params.arr_size());
            for (const auto parameter : params.array()) {
                if (!parameter.is_obj()) continue;
                JsPluginManifest::Param param;
                param.name = valueString(parameter.obj_get("name"));
                param.type = valueString(parameter.obj_get("type"));
                if (param.type.empty()) param.type = "string";
                param.languageKey = valueString(parameter.obj_get("language"));
                param.defaultValue = valueString(parameter.obj_get("defaultValue"));
                if (param.defaultValue.empty()) param.defaultValue = valueString(parameter.obj_get("default"));
                param.ui_no = valueString(parameter.obj_get("ui_no"));
                param.mapProvider = valueString(parameter.obj_get("mapProvider"));
                param.required = valueBool(parameter.obj_get("required"), true);
                param.folder = valueBool(parameter.obj_get("folder"), false);
                param.localizedName = param.languageKey.empty() ? param.name : locales.contains("executor.implement:" + manifest.id + ":" + param.languageKey)
                    ? locales.at("executor.implement:" + manifest.id + ":" + param.languageKey)
                    : param.name;
                if (const auto extensions = parameter.obj_get("extensions"); extensions.is_arr())
                    for (const auto value : extensions.array()) param.extensions.push_back(valueString(value));
                if (const auto list = parameter.obj_get("list"); list.is_arr())
                    for (const auto value : list.array()) param.listValues.push_back(valueString(value));
                if (const auto map = parameter.obj_get("map"); map.is_arr()) {
                    for (const auto entry : map.array()) {
                        if (!entry.is_obj()) continue;
                        auto display = valueString(entry.obj_get("display"));
                        if (!display.empty()) param.mapValues.emplace_back(std::move(display), valueString(entry.obj_get("value")));
                    }
                }
                manifest.params.push_back(std::move(param));
            }
        }
        manifests.push_back(std::move(manifest));
    }
    if (manifests.size() == manifestStart)
        appendLoadError(manifests, path, "no static manifest matches the configured implementation", impl);
    return true;
}

}

void JsEngine::cleanupCtx(JSContext* ctx) {
    if (auto* data = static_cast<CtxData*>(JS_GetContextOpaque(ctx))) {
        JS_FreeValue(ctx, data->ui8Ctor);
        delete data;
        JS_SetContextOpaque(ctx, nullptr);
    }
}

const std::string* JsEngine::getScriptSource(const std::string& scriptPath) const {
    if (const auto it = scriptSources_.find(scriptPath); it != scriptSources_.end()) return std::addressof(it->second);
    return nullptr;
}

JSRuntime* JsEngine::newRuntime(size_t memoryLimit) const {
    JSRuntime* rt = JS_NewRuntime();
    JS_SetMemoryLimit(rt, memoryLimit);
    JS_SetMaxStackSize(rt, 1024 * 1024);
    return rt;
}

static bool hasManifestMarker(std::string_view src) {
    return src.find("plugin") != std::string_view::npos || src.find("manifest") != std::string_view::npos;
}

static std::string loadErrorId(std::string_view path) {
    return "js.load-error." + fs::path(path).generic_string();
}

static JSValue getGlobalPath(JSContext* ctx, std::string_view path) {
    JSValue cur = JS_GetGlobalObject(ctx);
    size_t begin = 0;
    while (begin < path.size()) {
        const auto dot = path.find('.', begin);
        const auto part = path.substr(begin, dot == std::string_view::npos ? path.size() - begin : dot - begin);
        const std::string key(part);
        JSValue next = JS_GetPropertyStr(ctx, cur, key.c_str());
        JS_FreeValue(ctx, cur);
        cur = next;
        if (JS_IsUndefined(cur) || JS_IsNull(cur)) return cur;
        if (dot == std::string_view::npos) break;
        begin = dot + 1;
    }
    return cur;
}

static JSValue getManifestValue(JSContext* ctx) {
    JSValue global = JS_GetGlobalObject(ctx);
    for (const char* name : {"plugins", "plugin", "manifests", "manifest"}) {
        JSValue value = JS_GetPropertyStr(ctx, global, name);
        if (!JS_IsUndefined(value) && !JS_IsNull(value)) {
            JS_FreeValue(ctx, global);
            return value;
        }
        JS_FreeValue(ctx, value);
    }
    JS_FreeValue(ctx, global);
    return JS_UNDEFINED;
}

static JSValue manifestArray(JSContext* ctx, JSValueConst value) {
    if (JS_IsArray(value)) return JS_DupValue(ctx, value);
    JSValue arr = JS_NewArray(ctx);
    JS_SetPropertyUint32(ctx, arr, 0, JS_DupValue(ctx, value));
    return arr;
}

void JsEngine::registerNatives(JSContext* ctx, JsEngine* self) {
    auto* data = new CtxData{};
    data->engine = self;
    JSValue global = JS_GetGlobalObject(ctx);
    data->ui8Ctor = JS_GetPropertyStr(ctx, global, "Uint8Array");
    JS_SetContextOpaque(ctx, data);
    registerKernelx(ctx, self);
    registerKtApi(ctx, self);
    JS_FreeValue(ctx, global);
}

void JsEngine::loadLibsIntoCtx(JSContext* ctx) {
    JSValue global = JS_GetGlobalObject(ctx);
    JSValue filename = JS_GetPropertyStr(ctx, global, "__filename");
    for (const auto& lib : libScripts_) {
        JS_SetPropertyStr(ctx, global, "__filename", JS_NewString(ctx, lib.path.c_str()));
        JSValue r = evalCached(ctx, lib.path, lib.source);
        if (JS_IsException(r)) JS_FreeValue(ctx, JS_GetException(ctx));
        JS_FreeValue(ctx, r);
    }
    JS_SetPropertyStr(ctx, global, "__filename", filename);
    JS_FreeValue(ctx, global);
}

JSValue JsEngine::evalCached(JSContext* ctx, std::string_view path, std::string_view source) {
    const std::string cacheKey(path);
    const size_t sourceHash = std::hash<std::string_view>{}(source);
    std::vector<uint8_t> bytecode;
    {
        std::lock_guard lock(bytecodeMutex_);
        if (const auto it = bytecodeCache_.find(cacheKey);
            it != bytecodeCache_.end() && it->second.sourceHash == sourceHash && it->second.sourceSize == source.size())
            bytecode = it->second.data;
    }
    if (!bytecode.empty()) {
        JSValue function = JS_ReadObject(ctx, bytecode.data(), bytecode.size(), JS_READ_OBJ_BYTECODE);
        if (!JS_IsException(function)) return JS_EvalFunction(ctx, function);
        JS_FreeValue(ctx, JS_GetException(ctx));
        JS_FreeValue(ctx, function);
    }

    JSValue function = JS_Eval(ctx, source.data(), source.size(), cacheKey.c_str(), JS_EVAL_TYPE_GLOBAL | JS_EVAL_FLAG_COMPILE_ONLY);
    if (JS_IsException(function)) return function;
    size_t bytecodeSize = 0;
    uint8_t* rawBytecode = JS_WriteObject(ctx, &bytecodeSize, function, JS_WRITE_OBJ_BYTECODE | JS_WRITE_OBJ_STRIP_SOURCE | JS_WRITE_OBJ_STRIP_DEBUG);
    if (rawBytecode) {
        std::vector<uint8_t> compiled(rawBytecode, rawBytecode + bytecodeSize);
        js_free(ctx, rawBytecode);
        std::lock_guard lock(bytecodeMutex_);
        bytecodeCache_.insert_or_assign(cacheKey, Bytecode{.data = std::move(compiled), .sourceHash = sourceHash, .sourceSize = source.size()});
    }
    return JS_EvalFunction(ctx, function);
}

const JsPluginManifest* JsEngine::getManifest(const std::string& id) const {
    const auto it = manifests_.find(id);
    return it == manifests_.end() ? nullptr : std::addressof(it->second);
}

void JsEngine::scanAndRegister(
    const std::string& utilityDir,
    const std::string& impl,
    const std::unordered_map<std::string, std::string>& locales) {

    manifests_.clear();
    libScripts_.clear();
    scriptSources_.clear();
    {
        std::lock_guard lock(bytecodeMutex_);
        bytecodeCache_.clear();
    }
    utilityDir_ = utilityDir;

    std::vector<std::string> files;
    std::error_code ec;
    for (auto it = fs::recursive_directory_iterator(utilityDir, ec); !ec && it != fs::recursive_directory_iterator(); it.increment(ec)) {
        if (it->is_regular_file(ec) && it->path().extension() == ".js") files.push_back(it->path().string());
    }
    if (files.empty()) return;
    std::sort(files.begin(), files.end());

    std::vector<std::string> sources(files.size());
    constexpr size_t batch = 128;
    for (size_t base = 0; base < files.size(); base += batch) {
        const size_t end = std::min(base + batch, files.size());
        parallelFor(end - base, [&](size_t i) {
            try { sources[base + i] = FileUtils::readTextFile(files[base + i]); }
            catch (...) {}
        });
    }

    auto worker = [&](size_t begin, size_t end) {
        std::vector<JsPluginManifest> manifests;
        std::vector<ScriptSource> libs;
        JSRuntime* rt = nullptr;

        auto legacyRuntime = [&] {
            if (rt) return rt;
            rt = JS_NewRuntime();
            JS_SetMemoryLimit(rt, 16 * 1024 * 1024);
            JS_SetMaxStackSize(rt, 256 * 1024);
            return rt;
        };

        for (size_t i = begin; i < end; ++i) {
            const auto& src = sources[i];
            if (src.empty()) continue;
            if (appendStaticManifests(manifests, files[i], src, impl, locales)) continue;
            if (!hasManifestMarker(src)) {
                libs.push_back({files[i], src});
                continue;
            }

            JSContext* ctx = JS_NewContext(legacyRuntime());
            JSValue global = JS_GetGlobalObject(ctx);
            JS_SetPropertyStr(ctx, global, "__filename", JS_NewString(ctx, files[i].c_str()));
            JS_FreeValue(ctx, global);

            JSValue r = JS_Eval(ctx, src.data(), src.size(), files[i].c_str(), JS_EVAL_TYPE_GLOBAL);
            if (JS_IsException(r)) {
                auto error = js::errorString(ctx);
                JS_FreeValue(ctx, r);
                JsPluginManifest manifest;
                manifest.implementation = impl;
                manifest.scriptPath = files[i];
                manifest.id = loadErrorId(files[i]);
                manifest.loadError = files[i] + ": " + error;
                manifest.params.push_back({.name = "InputFile", .type = "file", .required = false});
                manifests.push_back(std::move(manifest));
                JsEngine::cleanupCtx(ctx);
                JS_FreeContext(ctx);
                continue;
            }
            JS_FreeValue(ctx, r);

            JSValue manifestValue = getManifestValue(ctx);
            if (!JS_IsUndefined(manifestValue) && !JS_IsNull(manifestValue)) {
                JSValue manifestList = manifestArray(ctx, manifestValue);
                int64_t manifestCount = 0;
                JS_GetLength(ctx, manifestList, &manifestCount);
                const size_t manifestStart = manifests.size();

                auto loadString = [&](JSValueConst obj, const char* key) {
                    JSValue v = JS_GetPropertyStr(ctx, obj, key);
                    auto s = (!JS_IsUndefined(v) && !JS_IsNull(v)) ? js::toString(ctx, v) : std::string{};
                    JS_FreeValue(ctx, v);
                    return s;
                };
                auto loadBool = [&](JSValueConst obj, const char* key, bool def) {
                    JSValue v = JS_GetPropertyStr(ctx, obj, key);
                    if (JS_IsUndefined(v)) { JS_FreeValue(ctx, v); return def; }
                    const bool b = JS_ToBool(ctx, v) == 1;
                    JS_FreeValue(ctx, v);
                    return b;
                };
                auto loadArrayStrings = [&](JSValueConst obj, const char* key, std::vector<std::string>& out) {
                    JSValue a = JS_GetPropertyStr(ctx, obj, key);
                    if (JS_IsArray(a)) {
                        int64_t len = 0;
                        JS_GetLength(ctx, a, &len);
                        out.reserve(out.size() + static_cast<size_t>(std::max<int64_t>(len, 0)));
                        for (int64_t j = 0; j < len; ++j) {
                            JSValue v = JS_GetPropertyUint32(ctx, a, static_cast<uint32_t>(j));
                            out.push_back(js::toString(ctx, v));
                            JS_FreeValue(ctx, v);
                        }
                    }
                    JS_FreeValue(ctx, a);
                };

                for (int64_t mi = 0; mi < manifestCount; ++mi) {
                    JSValue itemManifest = JS_GetPropertyUint32(ctx, manifestList, static_cast<uint32_t>(mi));
                    JsPluginManifest manifest;
                    manifest.implementation = loadString(itemManifest, "implementation");
                    if (manifest.implementation.empty()) manifest.implementation = impl;
                    manifest.scriptPath = files[i];
                    manifest.bufferSize = manifestBufferSize(ctx, itemManifest, 0);
                    manifest.id = loadString(itemManifest, "id");
                    if (manifest.id.empty()) manifest.id = loadString(itemManifest, "name");
                    if (manifest.id.empty() || manifest.implementation != impl) {
                        JS_FreeValue(ctx, itemManifest);
                        continue;
                    }

                    JSValue params = JS_GetPropertyStr(ctx, itemManifest, "params");
                    if (JS_IsArray(params)) {
                        int64_t len = 0;
                        JS_GetLength(ctx, params, &len);
                        manifest.params.reserve(static_cast<size_t>(std::max<int64_t>(len, 0)));
                        for (int64_t j = 0; j < len; ++j) {
                            JSValue item = JS_GetPropertyUint32(ctx, params, static_cast<uint32_t>(j));
                            JsPluginManifest::Param p;
                            p.name = loadString(item, "name");
                            p.type = loadString(item, "type");
                            if (p.type.empty()) p.type = "string";
                            p.languageKey = loadString(item, "language");
                            p.defaultValue = loadString(item, "defaultValue");
                            if (p.defaultValue.empty()) p.defaultValue = loadString(item, "default");
                            p.ui_no = loadString(item, "ui_no");
                            p.mapProvider = loadString(item, "mapProvider");
                            p.required = loadBool(item, "required", true);
                            p.folder = loadBool(item, "folder", false);
                            p.localizedName = p.languageKey.empty() ? p.name : locales.contains("executor.implement:" + manifest.id + ":" + p.languageKey)
                                ? locales.at("executor.implement:" + manifest.id + ":" + p.languageKey)
                                : p.name;
                            loadArrayStrings(item, "extensions", p.extensions);
                            loadArrayStrings(item, "list", p.listValues);
                            JSValue map = JS_GetPropertyStr(ctx, item, "map");
                            if (JS_IsArray(map)) {
                                int64_t mapLen = 0;
                                JS_GetLength(ctx, map, &mapLen);
                                for (int64_t k = 0; k < mapLen; ++k) {
                                    JSValue entry = JS_GetPropertyUint32(ctx, map, static_cast<uint32_t>(k));
                                    const auto display = loadString(entry, "display");
                                    const auto value = loadString(entry, "value");
                                    if (!display.empty()) p.mapValues.emplace_back(display, value);
                                    JS_FreeValue(ctx, entry);
                                }
                            }
                            JS_FreeValue(ctx, map);
                            JS_FreeValue(ctx, item);
                            manifest.params.push_back(std::move(p));
                        }
                    }
                    JS_FreeValue(ctx, params);
                    manifests.push_back(std::move(manifest));
                    JS_FreeValue(ctx, itemManifest);
                }
                JS_FreeValue(ctx, manifestList);
                JS_FreeValue(ctx, manifestValue);
                if (manifests.size() == manifestStart) {
                    JsPluginManifest manifest;
                    manifest.implementation = impl;
                    manifest.scriptPath = files[i];
                    manifest.id = loadErrorId(files[i]);
                    manifest.loadError = files[i] + ": no manifest matches the configured implementation";
                    manifest.params.push_back({.name = "InputFile", .type = "file", .required = false});
                    manifests.push_back(std::move(manifest));
                }
            }

            JsEngine::cleanupCtx(ctx);
            JS_FreeContext(ctx);
        }

        if (rt) {
            JS_RunGC(rt);
            JS_FreeRuntime(rt);
        }
        return std::pair{std::move(manifests), std::move(libs)};
    };

    auto cacheSources = [&] {
        scriptSources_.reserve(files.size());
        for (size_t i = 0; i < files.size(); ++i) {
            if (!sources[i].empty()) scriptSources_.emplace(std::move(files[i]), std::move(sources[i]));
        }
    };

    const size_t threads = std::clamp(std::max(1u, std::thread::hardware_concurrency()), 1u, 8u);
    if (threads == 1 || files.size() < 32) {
        auto [ms, libs] = worker(0, files.size());
        for (auto& m : ms) manifests_.emplace(m.id, std::move(m));
        for (auto& l : libs) libScripts_.push_back(std::move(l));
        cacheSources();
        return;
    }

    std::vector<std::future<std::pair<std::vector<JsPluginManifest>, std::vector<ScriptSource>>>> futures;
    const size_t chunk = (files.size() + threads - 1) / threads;
    futures.reserve(threads);
    for (size_t t = 0; t < threads; ++t) {
        const size_t begin = t * chunk;
        const size_t end = std::min(begin + chunk, files.size());
        if (begin >= end) break;
        futures.emplace_back(getGlobalPool().submit([&, begin, end] { return worker(begin, end); }));
    }
    for (auto& fut : futures) {
        auto [ms, libs] = fut.get();
        for (auto& m : ms) manifests_.emplace(m.id, std::move(m));
        for (auto& l : libs) libScripts_.push_back(std::move(l));
    }
    cacheSources();
}

std::vector<std::pair<std::string, std::string>> JsEngine::queryParamOptions(const std::string& id, const std::string& paramName) {
    std::vector<std::pair<std::string, std::string>> out;
    const auto* manifest = getManifest(id);
    if (!manifest) return out;
    const JsPluginManifest::Param* target = nullptr;
    for (const auto& p : manifest->params) {
        if (p.name == paramName) {
            target = &p;
            break;
        }
    }
    if (!target || target->mapProvider.empty()) return out;
    if (!target->mapValues.empty()) return target->mapValues;

    std::string src;
    const auto* cached = getScriptSource(manifest->scriptPath);
    if (!cached) {
        try { src = FileUtils::readTextFile(manifest->scriptPath); }
        catch (...) { return out; }
    }

    JSRuntime* rt = newRuntime(runtimeBufferSize(*manifest));
    JSContext* ctx = JS_NewContext(rt);
    registerNatives(ctx, this);
    JSValue global = JS_GetGlobalObject(ctx);
    JS_SetPropertyStr(ctx, global, "__filename", JS_NewString(ctx, manifest->scriptPath.c_str()));
    JS_FreeValue(ctx, global);
    loadLibsIntoCtx(ctx);
    const std::string& code = cached ? *cached : src;
    JSValue r = evalCached(ctx, manifest->scriptPath, code);
    if (JS_IsException(r)) JS_FreeValue(ctx, JS_GetException(ctx));
    JS_FreeValue(ctx, r);

    JSValue fn = getGlobalPath(ctx, target->mapProvider);
    if (JS_IsFunction(ctx, fn)) {
        JSValue ret = JS_Call(ctx, fn, JS_UNDEFINED, 0, nullptr);
        if (JS_IsArray(ret)) {
            int64_t len = 0;
            JS_GetLength(ctx, ret, &len);
            out.reserve(static_cast<size_t>(std::max<int64_t>(len, 0)));
            for (int64_t i = 0; i < len; ++i) {
                JSValue item = JS_GetPropertyUint32(ctx, ret, static_cast<uint32_t>(i));
                JSValue d = JS_GetPropertyStr(ctx, item, "display");
                JSValue v = JS_GetPropertyStr(ctx, item, "value");
                auto display = js::toString(ctx, d);
                auto value = js::toString(ctx, v);
                if (!display.empty()) out.emplace_back(std::move(display), std::move(value));
                JS_FreeValue(ctx, d);
                JS_FreeValue(ctx, v);
                JS_FreeValue(ctx, item);
            }
        } else if (JS_IsException(ret)) {
            JS_FreeValue(ctx, JS_GetException(ctx));
        }
        JS_FreeValue(ctx, ret);
    }
    JS_FreeValue(ctx, fn);
    JsEngine::cleanupCtx(ctx);
    JS_FreeContext(ctx);
    JS_FreeRuntime(rt);
    return out;
}

PluginResult JsEngine::execute(const std::string& id, const PluginParamMap& params) {
    const auto* manifest = getManifest(id);
    if (!manifest) return PluginResult::fail("Plugin not found: " + id);
    if (!manifest->loadError.empty()) return PluginResult::fail(manifest->loadError);
    std::string src;
    const auto* cached = getScriptSource(manifest->scriptPath);
    if (!cached) {
        try { src = FileUtils::readTextFile(manifest->scriptPath); }
        catch (const std::exception& e) { return PluginResult::fail(e.what()); }
    }

    JSRuntime* rt = newRuntime(runtimeBufferSize(*manifest));
    JSContext* ctx = JS_NewContext(rt);
    registerNatives(ctx, this);
    JSValue fileGlobal = JS_GetGlobalObject(ctx);
    JS_SetPropertyStr(ctx, fileGlobal, "__filename", JS_NewString(ctx, manifest->scriptPath.c_str()));
    JS_FreeValue(ctx, fileGlobal);
    loadLibsIntoCtx(ctx);

    auto cleanup = [&](PluginResult result) {
        cleanupCtx(ctx);
        JS_FreeContext(ctx);
        JS_FreeRuntime(rt);
        return result;
    };

    const std::string& code = cached ? *cached : src;
    JSValue r = evalCached(ctx, manifest->scriptPath, code);
    if (JS_IsException(r)) {
        auto msg = js::errorString(ctx);
        JS_FreeValue(ctx, r);
        return cleanup(PluginResult::fail(std::move(msg)));
    }
    JS_FreeValue(ctx, r);

    JSValue global = JS_GetGlobalObject(ctx);
    JSValue fn = JS_GetPropertyStr(ctx, global, "execute");
    JS_FreeValue(ctx, global);
    if (!JS_IsFunction(ctx, fn)) {
        JS_FreeValue(ctx, fn);
        return cleanup(PluginResult::fail("No execute function"));
    }

    JSValue paramObj = JS_NewObject(ctx);
    for (const auto& [k, v] : params) JS_SetPropertyStr(ctx, paramObj, k.c_str(), JS_NewStringLen(ctx, v.data(), v.size()));
    JSValue argv[1] = { paramObj };
    JSValue ret = JS_Call(ctx, fn, JS_UNDEFINED, 1, argv);
    JS_FreeValue(ctx, fn);
    JS_FreeValue(ctx, paramObj);

    PluginResult result;
    if (JS_IsException(ret)) {
        result = PluginResult::fail(js::errorString(ctx));
    } else if (JS_IsObject(ret)) {
        JSValue success = JS_GetPropertyStr(ctx, ret, "success");
        result.success = JS_ToBool(ctx, success) == 1;
        JS_FreeValue(ctx, success);
        JSValue output = JS_GetPropertyStr(ctx, ret, "output");
        if (!JS_IsUndefined(output)) result.output = js::toString(ctx, output);
        JS_FreeValue(ctx, output);
        JSValue error = JS_GetPropertyStr(ctx, ret, "error");
        if (!JS_IsUndefined(error)) result.error = js::toString(ctx, error);
        JS_FreeValue(ctx, error);
    }
    JS_FreeValue(ctx, ret);
    return cleanup(result);
}

std::pair<int, int> JsEngine::executeBatch(
    const std::string& id,
    const std::vector<std::string>& inFiles,
    const std::vector<std::string>& outFiles,
    const PluginParamMap& extra) {

    const auto* manifest = getManifest(id);
    if (!manifest) return {0, static_cast<int>(inFiles.size())};
    if (!manifest->loadError.empty()) return {0, static_cast<int>(inFiles.size())};
    if (inFiles.empty()) return {0, 0};

    std::string src;
    const auto* cached = getScriptSource(manifest->scriptPath);
    if (!cached) {
        try { src = FileUtils::readTextFile(manifest->scriptPath); }
        catch (...) { return {0, static_cast<int>(inFiles.size())}; }
    }
    const std::string& code = cached ? *cached : src;

    const std::string inKey = manifest->params.size() > 0 ? manifest->params[0].name : "InputFile";
    const std::string outKey = manifest->params.size() > 1 ? manifest->params[1].name : "OutputFile";
    std::atomic<int> succ{0};
    std::atomic<int> fail{0};
    auto libs = std::make_shared<std::vector<ScriptSource>>(libScripts_);
    constexpr size_t batch = 64;
    const size_t batches = (inFiles.size() + batch - 1) / batch;
    Latch latch(static_cast<int>(batches));

    for (size_t b = 0; b < batches; ++b) {
        const size_t begin = b * batch;
        const size_t end = std::min(begin + batch, inFiles.size());
        getGlobalPool().submitVoid([=, &code, &inFiles, &outFiles, &extra, &succ, &fail, &latch, this] {
            JSRuntime* rt = newRuntime(runtimeBufferSize(*manifest));
            JSContext* ctx = JS_NewContext(rt);
            registerNatives(ctx, this);
            JSValue global = JS_GetGlobalObject(ctx);
            JS_SetPropertyStr(ctx, global, "__filename", JS_NewString(ctx, manifest->scriptPath.c_str()));
            JS_FreeValue(ctx, global);
            for (const auto& lib : *libs) {
                JSValue r = evalCached(ctx, lib.path, lib.source);
                if (JS_IsException(r)) JS_FreeValue(ctx, JS_GetException(ctx));
                JS_FreeValue(ctx, r);
            }
            JSValue sr = evalCached(ctx, manifest->scriptPath, code);
            if (JS_IsException(sr)) JS_FreeValue(ctx, JS_GetException(ctx));
            JS_FreeValue(ctx, sr);

            JSValue fnGlobal = JS_GetGlobalObject(ctx);
            JSValue fn = JS_GetPropertyStr(ctx, fnGlobal, "execute");
            JS_FreeValue(ctx, fnGlobal);
            if (JS_IsFunction(ctx, fn)) {
                for (size_t i = begin; i < end; ++i) {
                    JSValue pObj = js::paramsObject(ctx, extra);
                    JS_SetPropertyStr(ctx, pObj, inKey.c_str(), JS_NewStringLen(ctx, inFiles[i].data(), inFiles[i].size()));
                    JS_SetPropertyStr(ctx, pObj, outKey.c_str(), JS_NewStringLen(ctx, outFiles[i].data(), outFiles[i].size()));
                    JSValue argv[2] = { pObj, JS_NewString(ctx, id.c_str()) };
                    JSValue ret = JS_Call(ctx, fn, JS_UNDEFINED, 2, argv);
                    JS_FreeValue(ctx, argv[1]);
                    JS_FreeValue(ctx, pObj);
                    bool ok = false;
                    if (JS_IsObject(ret)) {
                        JSValue success = JS_GetPropertyStr(ctx, ret, "success");
                        ok = JS_ToBool(ctx, success) == 1;
                        JS_FreeValue(ctx, success);
                    } else if (JS_IsException(ret)) {
                        JS_FreeValue(ctx, JS_GetException(ctx));
                    }
                    JS_FreeValue(ctx, ret);
                    (ok ? succ : fail).fetch_add(1, std::memory_order_relaxed);
                }
            } else {
                fail.fetch_add(static_cast<int>(end - begin), std::memory_order_relaxed);
            }
            JS_FreeValue(ctx, fn);
            cleanupCtx(ctx);
            JS_FreeContext(ctx);
            JS_FreeRuntime(rt);
            latch.countDown();
        });
    }
    latch.wait();
    return {succ.load(), fail.load()};
}