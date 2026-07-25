module;
#include <algorithm>
#include <cstdint>
#include <filesystem>
#include <string>
#include <vector>
#include <unordered_map>
module tool.shell.config_manager;
import utility.io;
import utility.json;
import tool.shell.plugin_base;
import tool.shell.core_utils;
namespace fs = std::filesystem;

bool ConfigManager::initialize(const std::string& scriptDirPath) {
    configs_.clear();
    locales_.clear();
    settings_.clear();
    functionOrder_.clear();
    jsBufferSize_ = 64ull * 1024ull * 1024ull;

    fs::path baseDir(scriptDirPath);
    auto mainData = FileUtils::readFileBytes((baseDir / "main.json").string());
    if (mainData.empty()) return false;

    auto doc = json::Document::parse({reinterpret_cast<const char*>(mainData.data()), mainData.size()}, json::ReadFlag::AllowComments | json::ReadFlag::AllowTrailingCommas);
    if (!doc) return false;

    auto root    = doc.root();
    for (const char* key : {"ptx_bmp", "astc_quality", "png_compression_level"}) {
        if (auto val = root.obj_get(key); val) {
            if (val.is_bool()) settings_[key] = val.get_bool() ? "true" : "false";
            else if (val.is_int()) settings_[key] = std::to_string(val.get_sint());
            else if (val.is_str()) settings_[key] = val.get_str();
        }
    }
    auto version = root.obj_get("version");
    if (version.is_obj()) {
        if (auto value = version.obj_get("kernel"); value.is_str()) sysInfo_.kernelVer = value.get_str();
        if (auto value = version.obj_get("script"); value.is_str()) sysInfo_.scriptVer = value.get_str();
        if (auto value = version.obj_get("shell"); value.is_str()) sysInfo_.shellVer = value.get_str();
    }
    if (auto value = root.obj_get("arch"); value.is_str()) sysInfo_.arch = value.get_str();

    auto paths = root.obj_get("paths");
    std::string execImplPath;
    std::string settingsPath;
    std::string langDirPath;
    if (paths.is_obj()) {
        if (auto value = paths.obj_get("executor_implementation"); value.is_str()) execImplPath = value.get_str();
        if (auto value = paths.obj_get("settings"); value.is_str()) settingsPath = value.get_str();
        if (auto value = paths.obj_get("language_dir"); value.is_str()) langDirPath = value.get_str();
    }

    if (!settingsPath.empty() && !loadLocale(baseDir, baseDir / settingsPath, baseDir / langDirPath)) return false;

    configs_.clear();
    if (!execImplPath.empty()) loadExecutors(baseDir / execImplPath);
    scriptDir_ = std::move(baseDir);
    return true;
}

bool ConfigManager::loadLocale(const fs::path& ,
                                const fs::path& settingsPath,
                                const fs::path& langDir) {
    auto setData = FileUtils::readFileBytes(settingsPath.string());
    if (setData.empty()) return false;

    auto sdoc = json::Document::parse({reinterpret_cast<const char*>(setData.data()), setData.size()}, json::ReadFlag::AllowComments | json::ReadFlag::AllowTrailingCommas);
    if (!sdoc) return false;

    for (auto [key, val] : sdoc.root().object()) {
        std::string k = key.get_str();
        if (k == "language") continue;
        if (val.is_bool()) settings_[k] = val.get_bool() ? "true" : "false";
        else if (val.is_int()) settings_[k] = std::to_string(val.get_sint());
        else if (val.is_str()) settings_[k] = val.get_str();
    }

    std::string langName;
    if (auto value = sdoc.root().obj_get("language"); value.is_str()) langName = value.get_str();
    if (langName.empty()) return false;
    if (auto value = sdoc.root().obj_get("js_buffer_size"); value.is_str())
        jsBufferSize_ = ShellCore::parseByteSize(value.get_str_view(), jsBufferSize_);

    auto langData = FileUtils::readFileBytes((langDir / (langName + ".json")).string());
    if (langData.empty()) return false;

    auto ldoc = json::Document::parse({reinterpret_cast<const char*>(langData.data()), langData.size()}, json::ReadFlag::AllowComments | json::ReadFlag::AllowTrailingCommas);
    if (!ldoc) return false;

    locales_.clear();
    functionOrder_.clear();

    static const std::string prefix = "executor.implement:";
    int orderIndex = 0;

    for (auto [key, val] : ldoc.root().object()) {
        std::string k = key.get_str();
        locales_[k] = val.get_str();

        if (k.compare(0, prefix.size(), prefix) == 0) {
            size_t start = prefix.size();
            size_t end   = k.find(':', start);
            std::string funcId = k.substr(start, end - start);
            if (functionOrder_.find(funcId) == functionOrder_.end())
                functionOrder_[funcId] = orderIndex++;
        }
    }
    return true;
}

void ConfigManager::loadExecutors(const fs::path& executorDir) {
    std::error_code ec;
    for (const auto& entry : fs::directory_iterator(executorDir, ec)) {
        if (!entry.is_regular_file()) continue;
        if (entry.path().extension() != ".json") continue;

        std::string filename = entry.path().filename().string();
        std::string funcId   = filename.substr(0, filename.size() - 5);

        auto data = FileUtils::readFileBytes(entry.path().string());
        auto doc  = json::Document::parse({reinterpret_cast<const char*>(data.data()), data.size()}, json::ReadFlag::AllowComments | json::ReadFlag::AllowTrailingCommas);
        if (!doc) continue;

        FunctionConfig cfg;
        cfg.functionId    = funcId;
        cfg.localizedName = getLoc("executor.implement:" + funcId);

        for (auto paramObj : doc.root().obj_get("parameters").array()) {
            FunctionParam p;
            p.name = paramObj.obj_get("name").get_str();
            p.type = paramObj.obj_get("type").get_str();

            if (auto v = paramObj.obj_get("required"))    p.required = v.get_bool();

            if (auto v = paramObj.obj_get("language")) {
                p.languageKey   = v.get_str();
                p.localizedName = getLoc("executor.implement:" + funcId + ":" + p.languageKey);
            } else {
                p.localizedName = p.name;
            }

            if (auto v = paramObj.obj_get("ui_no"))       p.ui_no = v.get_str();

            if (auto v = paramObj.obj_get("extensions"))
                for (auto e : v.array()) p.extensions.push_back(e.get_str());

            if (auto v = paramObj.obj_get("folder"))      p.folder = v.get_bool();

            if (auto v = paramObj.obj_get("default")) {
                if      (v.is_bool()) p.defaultValue = v.get_bool() ? "true" : "false";
                else if (v.is_int())  p.defaultValue = std::to_string(v.get_sint());
                else if (v.is_str())  p.defaultValue = v.get_str();
            }

            if (auto v = paramObj.obj_get("list"))
                for (auto e : v.array()) p.listValues.push_back(e.get_str());

            if (auto v = paramObj.obj_get("map")) {
                for (auto [key, val] : v.object()) {
                    p.mapValues.emplace_back(key.get_str(), val.get_str());
                }
            }

            cfg.params.push_back(std::move(p));
        }
        configs_.push_back(std::move(cfg));
    }

    std::sort(configs_.begin(), configs_.end(), [this](const FunctionConfig& a, const FunctionConfig& b) {
        auto itA = functionOrder_.find(a.functionId);
        auto itB = functionOrder_.find(b.functionId);
        int oA = (itA != functionOrder_.end()) ? itA->second : 999999;
        int oB = (itB != functionOrder_.end()) ? itB->second : 999999;
        return oA != oB ? oA < oB : a.functionId < b.functionId;
    });
}
