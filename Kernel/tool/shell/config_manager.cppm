module;
#include <charconv>
#include <filesystem>
#include <functional>
#include <cstdint>
#include <string>
#include <string_view>
#include <unordered_map>
#include <vector>
export module tool.shell.config_manager;
import tool.shell.plugin_base;
import tool.shell.core_utils;

export class ConfigManager {
public:
    [[nodiscard]] static ConfigManager& get() { static ConfigManager instance; return instance; }
    bool initialize(const std::string& scriptDirPath);
    [[nodiscard]] const std::vector<FunctionConfig>& getAllConfigs() const { return configs_; }
    [[nodiscard]] const std::unordered_map<std::string, std::string>& getAllLocales() const { return locales_; }
    [[nodiscard]] const SystemInfo& getSysInfo() const { return sysInfo_; }
    [[nodiscard]] const std::filesystem::path& getScriptDir() const noexcept { return scriptDir_; }
    [[nodiscard]] size_t getJsBufferSize() const noexcept { return jsBufferSize_; }
    [[nodiscard]] std::string getLoc(const std::string& key) const {
        if (auto it = locales_.find(key); it != locales_.end()) return it->second;
        return key;
    }
    [[nodiscard]] std::string getSetting(std::string_view key, std::string_view def = {}) const {
        if (auto it = settings_.find(key); it != settings_.end()) return it->second;
        return std::string(def);
    }
    [[nodiscard]] int getSettingInt(std::string_view key, int def = 0) const {
        if (auto it = settings_.find(key); it != settings_.end()) {
            int v = def;
            auto r = std::from_chars(it->second.data(), it->second.data() + it->second.size(), v);
            if (r.ec == std::errc{}) return v;
        }
        return def;
    }
private:
    struct TransparentStringHash {
        using is_transparent = void;
        [[nodiscard]] size_t operator()(std::string_view s) const noexcept { return std::hash<std::string_view>{}(s); }
    };
    using SettingsMap = std::unordered_map<std::string, std::string, TransparentStringHash, std::equal_to<>>;

    std::vector<FunctionConfig> configs_;
    std::unordered_map<std::string, std::string> locales_;
    SettingsMap settings_;
    std::unordered_map<std::string, int> functionOrder_;
    SystemInfo sysInfo_;
    std::filesystem::path scriptDir_;
    size_t jsBufferSize_ = 64ull * 1024ull * 1024ull;
    bool loadLocale(const std::filesystem::path& scriptDir, const std::filesystem::path& settingsPath, const std::filesystem::path& langDir);
    void loadExecutors(const std::filesystem::path& executorDir);
};
