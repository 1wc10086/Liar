module;
#include <charconv>
#include <functional>
#include <map>
#include <stdexcept>
#include <string>
#include <string_view>
#include <utility>
#include <vector>
export module tool.shell.plugin_base;

export struct FunctionParam {
    std::string name;
    std::string type;
    bool required = true;
    std::string languageKey;
    std::string localizedName;
    std::vector<std::string> extensions;
    bool folder = false;
    std::string defaultValue;
    std::string ui_no;
    std::vector<std::string> listValues;
    std::vector<std::pair<std::string, std::string>> mapValues;
};

export struct FunctionConfig {
    std::string functionId;
    std::string localizedName;
    std::vector<FunctionParam> params;

    [[nodiscard]] std::string generateOutputPath(std::string_view inputPath) const {
        if (params.size() < 2) return std::string(inputPath) + kDefaultExt;
        const auto& out = params[1];
        if (out.folder) return std::string(inputPath) + ".bundle";
        const std::string& ext = out.extensions.empty() ? kDefaultExt : out.extensions.front();
        auto pos = inputPath.rfind('.');
        if (pos != std::string_view::npos && pos != 0)
            return std::string(inputPath.substr(0, pos)) + ext;
        return std::string(inputPath) + ext;
    }

private:
    static inline const std::string kDefaultExt = ".out";
};

export struct PluginResult {
    bool success = false;
    std::string output;
    std::string error;
    [[nodiscard]] static PluginResult ok(std::string out = {}) { return {true, std::move(out), {}}; }
    [[nodiscard]] static PluginResult fail(std::string err = {}) { return {false, {}, std::move(err)}; }
};

export using PluginParamMap = std::map<std::string, std::string, std::less<>>;

export struct Args {
    const PluginParamMap& m;
    [[nodiscard]] const std::string& get(std::string_view key) const {
        auto it = m.find(key);
        if (it == m.end()) throw std::invalid_argument(std::string("Missing parameter: ") + std::string(key));
        return it->second;
    }
    [[nodiscard]] std::string_view opt(std::string_view key, std::string_view def = {}) const {
        auto it = m.find(key);
        return it == m.end() ? def : std::string_view(it->second);
    }
    [[nodiscard]] bool getBool(std::string_view key, bool def = false) const {
        auto it = m.find(key);
        return it == m.end() ? def : it->second == "true" || it->second == "1";
    }
    [[nodiscard]] int getInt(std::string_view key, int def = 0) const {
        auto it = m.find(key);
        if (it == m.end() || it->second.empty()) return def;
        int v = def;
        std::from_chars(it->second.data(), it->second.data() + it->second.size(), v);
        return v;
    }
};

export class PluginFactory {
public:
    using PluginFunc = std::function<PluginResult(const Args&)>;
    [[nodiscard]] static PluginFactory& get() {
        static PluginFactory i;
        return i;
    }
    void reg(std::string_view id, PluginFunc f) { registry_.insert_or_assign(std::string(id), std::move(f)); }
    [[nodiscard]] bool has(std::string_view id) const { return registry_.contains(id); }
    [[nodiscard]] PluginResult execute(std::string_view id, const PluginParamMap& params) const {
        auto it = registry_.find(id);
        if (it == registry_.end()) return PluginResult::fail("Plugin not found");
        try { return it->second(Args{params}); }
        catch (const std::exception& e) { return PluginResult::fail(e.what()); }
        catch (...) { return PluginResult::fail("Unknown exception"); }
    }
private:
    std::map<std::string, PluginFunc, std::less<>> registry_;
};
