module;
#include <cstddef>
#include <cstdint>
#include <jni.h>
#include "lib/quickjs_ng/quickjs.h"
#include <filesystem>
#include <mutex>
#include <string>
#include <string_view>
#include <unordered_map>
#include <utility>
#include <vector>
export module tool.shell.js_engine;
import tool.shell.plugin_base;
export {
    struct JsPluginManifest {
    struct Param {
        std::string name;
        std::string type;
        std::string languageKey;
        std::string localizedName;
        std::string defaultValue;
        std::string ui_no;
        std::string mapProvider;
        bool required = true;
        bool folder = false;
        std::vector<std::string> extensions;
        std::vector<std::string> listValues;
        std::vector<std::pair<std::string, std::string>> mapValues;
    };
    std::string id;
    std::string implementation;
    std::string scriptPath;
    std::string loadError;
    size_t bufferSize = 0;
    std::vector<Param> params;
};

class JsEngine;

struct CtxData {
    JsEngine* engine = nullptr;
    JSValue ui8Ctor = JS_UNDEFINED;
    static CtxData* get(JSContext* ctx) { return static_cast<CtxData*>(JS_GetContextOpaque(ctx)); }
    static JsEngine* engineOf(JSContext* ctx) { auto* d = get(ctx); return d ? d->engine : nullptr; }
};

class JsEngine {
public:
    static JsEngine& get() { static JsEngine i; return i; }

    void scanAndRegister(const std::string& utilityDir, const std::string& impl,
                         const std::unordered_map<std::string, std::string>& locales);
    bool has(const std::string& id) const { return manifests_.contains(id); }
    const JsPluginManifest* getManifest(const std::string& id) const;
    const std::unordered_map<std::string, JsPluginManifest>& all() const { return manifests_; }

    PluginResult execute(const std::string& id, const PluginParamMap& params);
    std::pair<int, int> executeBatch(const std::string& id,
        const std::vector<std::string>& inFiles,
        const std::vector<std::string>& outFiles,
        const PluginParamMap& extra);
    std::vector<std::pair<std::string, std::string>> queryParamOptions(
        const std::string& id, const std::string& paramName);

    void initJni(JavaVM* vm);
    [[nodiscard]] bool revorb(const std::string& inputPath, const std::string& outputPath) const;
    static void cleanupCtx(JSContext* ctx);
    static void registerKernelx(JSContext* ctx, JsEngine* self);
    static void registerKtApi(JSContext* ctx, JsEngine* self);

private:
    JsEngine() = default;

    struct ScriptSource {
        std::string path;
        std::string source;
    };

    struct Bytecode {
        std::vector<uint8_t> data;
        size_t sourceHash = 0;
        size_t sourceSize = 0;
    };

    std::unordered_map<std::string, JsPluginManifest> manifests_;
    std::unordered_map<std::string, std::string> scriptSources_;
    std::unordered_map<std::string, Bytecode> bytecodeCache_;
    std::vector<ScriptSource> libScripts_;
    std::mutex bytecodeMutex_;
    std::string utilityDir_;

    [[nodiscard]] const std::string* getScriptSource(const std::string& scriptPath) const;
    [[nodiscard]] JSRuntime* newRuntime(size_t memoryLimit) const;
    [[nodiscard]] JSValue evalCached(JSContext* ctx, std::string_view path, std::string_view source);
    void loadLibsIntoCtx(JSContext* ctx);
    static void registerNatives(JSContext* ctx, JsEngine* self);
};

namespace kernelx {

class DynamicLibrary {
public:
    enum class ClosePolicy : uint8_t { automatic, manual };

    DynamicLibrary() = default;
    DynamicLibrary(uint64_t handle, std::string path, std::string error, ClosePolicy closePolicy) noexcept;
    DynamicLibrary(const DynamicLibrary&) = delete;
    DynamicLibrary& operator=(const DynamicLibrary&) = delete;
    DynamicLibrary(DynamicLibrary&& other) noexcept;
    DynamicLibrary& operator=(DynamicLibrary&& other) noexcept;
    ~DynamicLibrary();

    [[nodiscard]] static DynamicLibrary open(std::string path, ClosePolicy closePolicy = ClosePolicy::automatic);
    [[nodiscard]] static DynamicLibrary openConfigured(std::string_view name, const void* fallbackSymbol = nullptr, ClosePolicy closePolicy = ClosePolicy::automatic);

    [[nodiscard]] bool loaded() const noexcept { return handle_ != 0; }
    [[nodiscard]] uint64_t raw() const noexcept { return handle_; }
    [[nodiscard]] const std::string& path() const noexcept { return path_; }
    [[nodiscard]] const std::string& error() const noexcept { return error_; }
    [[nodiscard]] static DynamicLibrary adopt(uint64_t handle, std::string path = {}, std::string error = {}) noexcept;
    [[nodiscard]] void* symbol(std::string_view name) const noexcept;
    template <class T>
    [[nodiscard]] T symbol(std::string_view name) const noexcept {
        return reinterpret_cast<T>(symbol(name));
    }
    [[nodiscard]] void* pointer() const noexcept { return reinterpret_cast<void*>(handle_); }
    [[nodiscard]] uint64_t release() noexcept;
    void close() noexcept;
    void closeNow() noexcept;

private:
    uint64_t handle_ = 0;
    std::string path_;
    std::string error_;
    ClosePolicy closePolicy_ = ClosePolicy::automatic;
};

[[nodiscard]] std::string configuredLibraryPath(std::string_view name);
[[nodiscard]] std::string currentDlError();

}

}
