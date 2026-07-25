#include <jni.h>
import utility.io;
import utility.json;
import tool.shell.plugin_base;
import tool.shell.config_manager;
import tool.shell.shell_processor;
import tool.shell.js_engine;
#include <algorithm>
#include <chrono>
#include <cctype>
#include <filesystem>
#include <string>
#include <vector>
namespace fs = std::filesystem;

static constexpr int kSuccess = 0;
static constexpr int kErrFileNotExist = 1;
static constexpr int kErrFuncNotFound = 2;
static constexpr int kErrExecFailed = 3;

static std::string makeResult(int code, double t = 0.0, const std::string& out = "") {
    char buf[64];
    snprintf(buf, sizeof(buf), "%d:%.4f", code, t);
    return out.empty() ? std::string(buf) : std::string(buf) + ":" + out;
}

static bool endsWithIC(const std::string& s, const std::string& suffix) {
    if (s.size() < suffix.size()) return false;
    return std::equal(suffix.rbegin(), suffix.rend(), s.rbegin(), [](unsigned char a, unsigned char b) {
        return std::tolower(a) == std::tolower(b);
    });
}

static auto buildJsParamsArr(json::MutDocument& doc, const JsPluginManifest& manifest) {
    auto pArr = doc.mut_arr();
    for (const auto& p : manifest.params) {
        auto pObj = doc.mut_obj();
        doc.obj_add_str(pObj, "name", p.name);
        doc.obj_add_str(pObj, "type", p.type);
        doc.obj_add_bool(pObj, "required", p.required);
        doc.obj_add_str(pObj, "localizedName", p.localizedName);
        doc.obj_add_str(pObj, "ui_no", p.ui_no);
        doc.obj_add_str(pObj, "defaultValue", p.defaultValue);
        doc.obj_add_bool(pObj, "folder", p.folder);
        doc.obj_add_str(pObj, "mapProvider", p.mapProvider);
        if (!p.extensions.empty()) {
            auto eArr = doc.mut_arr();
            for (const auto& e : p.extensions) eArr.arr_append(doc.mut_strdup(e));
            pObj.obj_add(doc.mut_str("extensions"), eArr);
        }
        if (!p.listValues.empty()) {
            auto lArr = doc.mut_arr();
            for (const auto& v : p.listValues) lArr.arr_append(doc.mut_strdup(v));
            pObj.obj_add(doc.mut_str("list"), lArr);
        }
        if (!p.mapValues.empty()) {
            auto mArr = doc.mut_arr();
            for (const auto& [display, value] : p.mapValues) {
                auto entry = doc.mut_obj();
                doc.obj_add_str(entry, "display", display);
                doc.obj_add_str(entry, "value", value);
                mArr.arr_append(entry);
            }
            pObj.obj_add(doc.mut_str("map"), mArr);
        }
        pArr.arr_append(pObj);
    }
    return pArr;
}

static auto buildNativeParamsArr(json::MutDocument& doc, const FunctionConfig& cfg) {
    auto pArr = doc.mut_arr();
    for (const auto& param : cfg.params) {
        auto pObj = doc.mut_obj();
        doc.obj_add_str(pObj, "name", param.name);
        doc.obj_add_str(pObj, "type", param.type);
        doc.obj_add_bool(pObj, "required", param.required);
        doc.obj_add_str(pObj, "localizedName", param.localizedName);
        doc.obj_add_str(pObj, "ui_no", param.ui_no);
        doc.obj_add_str(pObj, "defaultValue", param.defaultValue);
        doc.obj_add_bool(pObj, "folder", param.folder);
        if (!param.extensions.empty()) {
            auto eArr = doc.mut_arr();
            for (const auto& e : param.extensions) eArr.arr_append(doc.mut_strdup(e));
            pObj.obj_add(doc.mut_str("extensions"), eArr);
        }
        if (!param.listValues.empty()) {
            auto lArr = doc.mut_arr();
            for (const auto& v : param.listValues) lArr.arr_append(doc.mut_strdup(v));
            pObj.obj_add(doc.mut_str("list"), lArr);
        }
        if (!param.mapValues.empty()) {
            auto mArr = doc.mut_arr();
            for (const auto& [display, value] : param.mapValues) {
                auto entry = doc.mut_obj();
                doc.obj_add_str(entry, "display", display);
                doc.obj_add_str(entry, "value", value);
                mArr.arr_append(entry);
            }
            pObj.obj_add(doc.mut_str("map"), mArr);
        }
        pArr.arr_append(pObj);
    }
    return pArr;
}

extern "C" {

JNIEXPORT jint JNICALL JNI_OnLoad(JavaVM* vm, void*) {
    JniHelper::g_vm = vm;
    JsEngine::get().initJni(vm);
    return JNI_VERSION_1_6;
}

JNIEXPORT jstring JNICALL
Java_com_liar_byzymztools_util_native_ShellNative_initKernel(JNIEnv* env, jclass, jstring jScriptDir) {
    std::string scriptDir = JniHelper::jstringToString(env, jScriptDir);
    bool ok = ConfigManager::get().initialize(scriptDir);

    if (ok) {
        std::string implFile;
        try {
            auto mainData = FileUtils::readFileBytes(scriptDir + "/main.json");
            auto doc = json::Document::parse({reinterpret_cast<const char*>(mainData.data()), mainData.size()}, json::ReadFlag::AllowComments | json::ReadFlag::AllowTrailingCommas);
            if (doc) {
                auto paths = doc.root().obj_get("paths");
                if (paths.is_obj()) {
                    auto execPath = paths.obj_get("executor_implementation");
                    if (execPath.is_str()) implFile = fs::path(execPath.get_str()).filename().string();
                }
            }
        } catch (...) {}
        JsEngine::get().scanAndRegister(scriptDir + "/utility", implFile, ConfigManager::get().getAllLocales());
    }

    json::MutDocument doc;
    auto root = doc.mut_obj();
    doc.set_root(root);
    doc.obj_add_bool(root, "isSuccess", ok);
    if (ok) {
        auto sysObj = doc.mut_obj();
        const auto& sys = ConfigManager::get().getSysInfo();
        doc.obj_add_str(sysObj, "kernel", sys.kernelVer);
        doc.obj_add_str(sysObj, "shell", sys.shellVer);
        doc.obj_add_str(sysObj, "script", sys.scriptVer);
        doc.obj_add_str(sysObj, "arch", sys.arch);
        root.obj_add(doc.mut_str("sysInfo"), sysObj);
        auto transObj = doc.mut_obj();
        for (const auto& [k, v] : ConfigManager::get().getAllLocales())
            transObj.obj_add(doc.mut_strdup(k), doc.mut_strdup(v));
        root.obj_add(doc.mut_str("translations"), transObj);
    } else {
        auto transObj = doc.mut_obj();
        root.obj_add(doc.mut_str("translations"), transObj);
    }
    return JniHelper::stringToJstring(env, doc.write());
}

JNIEXPORT jstring JNICALL
Java_com_liar_byzymztools_util_native_ShellNative_matchFunctions(JNIEnv* env, jclass, jstring jPath) {
    auto path = JniHelper::jstringToString(env, jPath);
    if (!FileUtils::fileExists(path))
        return JniHelper::stringToJstring(env, "[{\"errorCode\": 1}]");
    bool isDir = FileUtils::isDirectory(path);

    json::MutDocument doc;
    auto arr = doc.mut_arr();
    doc.set_root(arr);

    for (const auto& cfg : ConfigManager::get().getAllConfigs()) {
        if (cfg.params.empty()) continue;
        bool match = isDir ? true : [&] {
            const auto& p1 = cfg.params[0];
            if (p1.folder) return false;
            if (p1.extensions.empty()) return true;
            for (const auto& ext : p1.extensions) if (endsWithIC(path, ext)) return true;
            return false;
        }();
        if (!match) continue;
        auto mObj = doc.mut_obj();
        doc.obj_add_str(mObj, "funcName", cfg.functionId);
        doc.obj_add_str(mObj, "localizedName", isDir ? "[*] " + cfg.localizedName : cfg.localizedName);
        mObj.obj_add(doc.mut_str("params"), buildNativeParamsArr(doc, cfg));
        arr.arr_append(mObj);
    }

    for (const auto& [id, manifest] : JsEngine::get().all()) {
        if (manifest.params.empty()) continue;
        bool match = isDir ? true : [&] {
            const auto& p1 = manifest.params[0];
            if (p1.folder) return false;
            if (p1.extensions.empty()) return true;
            for (const auto& ext : p1.extensions) if (endsWithIC(path, ext)) return true;
            return false;
        }();
        if (!match) continue;
        auto mObj = doc.mut_obj();
        std::string dispName = manifest.loadError.empty() ? manifest.id : "JS load error: " + fs::path(manifest.scriptPath).filename().string();
        auto locIt = ConfigManager::get().getAllLocales().find("executor.implement:" + manifest.id);
        if (locIt != ConfigManager::get().getAllLocales().end()) dispName = locIt->second;
        doc.obj_add_str(mObj, "funcName", manifest.id);
        doc.obj_add_str(mObj, "localizedName", isDir ? "[*] " + dispName : dispName);
        mObj.obj_add(doc.mut_str("params"), buildJsParamsArr(doc, manifest));
        arr.arr_append(mObj);
    }

    return JniHelper::stringToJstring(env, doc.write());
}

JNIEXPORT jstring JNICALL
Java_com_liar_byzymztools_util_native_ShellNative_executeFunction(
    JNIEnv* env, jclass, jstring jFuncName, jobjectArray jParams) {

    auto funcName = JniHelper::jstringToString(env, jFuncName);
    PluginParamMap params;
    jsize n = env->GetArrayLength(jParams);
    for (jsize i = 0; i < n; ++i) {
        auto js = (jstring)env->GetObjectArrayElement(jParams, i);
        auto s = JniHelper::jstringToString(env, js);
        env->DeleteLocalRef(js);
        if (auto pos = s.find(':'); pos != std::string::npos)
            params[s.substr(0, pos)] = s.substr(pos + 1);
    }

    if (JsEngine::get().has(funcName)) {
        auto t0 = std::chrono::high_resolution_clock::now();
        auto res = JsEngine::get().execute(funcName, params);
        auto t1 = std::chrono::high_resolution_clock::now();
        double elapsed = std::chrono::duration<double>(t1 - t0).count();
        std::string out = res.success ? res.output : res.error;
        return JniHelper::stringToJstring(env, makeResult(res.success ? kSuccess : kErrExecFailed, elapsed, out));
    }

    const FunctionConfig* cfg = nullptr;
    for (const auto& c : ConfigManager::get().getAllConfigs())
        if (c.functionId == funcName) { cfg = &c; break; }
    if (!cfg) return JniHelper::stringToJstring(env, makeResult(kErrFuncNotFound));

    if (!cfg->params.empty() && params.count(cfg->params[0].name))
        if (!FileUtils::fileExists(params.at(cfg->params[0].name)))
            return JniHelper::stringToJstring(env, makeResult(kErrFileNotExist));

    double elapsed = 0.0;
    bool ok = ShellProcessor::executeFunction(*cfg, params, elapsed);
    return JniHelper::stringToJstring(env, makeResult(ok ? kSuccess : kErrExecFailed, elapsed));
}

JNIEXPORT jstring JNICALL
Java_com_liar_byzymztools_util_native_ShellNative_executeBatchFunction(
    JNIEnv* env, jclass, jstring jFuncName, jstring jInputFolder, jstring jOutputFolder, jobjectArray jExtraParams) {

    auto funcName = JniHelper::jstringToString(env, jFuncName);
    auto inFolder = JniHelper::jstringToString(env, jInputFolder);
    auto outFolder = JniHelper::jstringToString(env, jOutputFolder);

    PluginParamMap extra;
    jsize n = env->GetArrayLength(jExtraParams);
    for (jsize i = 0; i < n; ++i) {
        auto js = (jstring)env->GetObjectArrayElement(jExtraParams, i);
        auto s = JniHelper::jstringToString(env, js);
        env->DeleteLocalRef(js);
        if (auto pos = s.find(':'); pos != std::string::npos)
            extra[s.substr(0, pos)] = s.substr(pos + 1);
    }

    if (JsEngine::get().has(funcName)) {
        if (!FileUtils::fileExists(inFolder)) {
            char buf[64];
            snprintf(buf, sizeof(buf), "%d:0.0000:0:0", kErrFileNotExist);
            return JniHelper::stringToJstring(env, buf);
        }
        auto inFiles = FileUtils::collectFiles(inFolder);
        std::vector<std::string> outFiles;
        outFiles.reserve(inFiles.size());
        size_t prefixLen = inFolder.size();
        FileUtils::createDirectory(outFolder);
        for (const auto& f : inFiles) {
            size_t skip = (f.size() > prefixLen && (f[prefixLen] == '/' || f[prefixLen] == '\\')) ? 1 : 0;
            outFiles.push_back(FileUtils::joinPath(outFolder, f.substr(prefixLen + skip)));
        }
        auto t0 = std::chrono::high_resolution_clock::now();
        auto [succ, fail] = JsEngine::get().executeBatch(funcName, inFiles, outFiles, extra);
        double elapsed = std::chrono::duration<double>(std::chrono::high_resolution_clock::now() - t0).count();
        char buf[128];
        snprintf(buf, sizeof(buf), "%d:%.4f:%d:%d", kSuccess, elapsed, succ, fail);
        return JniHelper::stringToJstring(env, buf);
    }

    const FunctionConfig* cfg = nullptr;
    for (const auto& c : ConfigManager::get().getAllConfigs())
        if (c.functionId == funcName) { cfg = &c; break; }
    if (!cfg) return JniHelper::stringToJstring(env, makeResult(kErrFuncNotFound));

    if (!FileUtils::fileExists(inFolder))
        return JniHelper::stringToJstring(env, makeResult(kErrFileNotExist));

    auto res = ShellProcessor::executeBatch(*cfg, inFolder, outFolder, extra);
    char buf[128];
    snprintf(buf, sizeof(buf), "%d:%.4f:%d:%d", kSuccess, res.elapsed, res.successCount, res.failCount);
    return JniHelper::stringToJstring(env, buf);
}

JNIEXPORT jstring JNICALL
Java_com_liar_byzymztools_util_native_ShellNative_queryParamOptions(
    JNIEnv* env, jclass, jstring jFuncName, jstring jParamName) {
    auto funcName = JniHelper::jstringToString(env, jFuncName);
    auto paramName = JniHelper::jstringToString(env, jParamName);

    auto options = JsEngine::get().queryParamOptions(funcName, paramName);

    json::MutDocument doc;
    auto arr = doc.mut_arr();
    doc.set_root(arr);

    for (const auto& [display, value] : options) {
        auto o = doc.mut_obj();
        doc.obj_add_str(o, "display", display);
        doc.obj_add_str(o, "value", value);
        arr.arr_append(o);
    }

    return JniHelper::stringToJstring(env, doc.write());
}

}
