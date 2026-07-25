module;
#include <algorithm>
#include <charconv>
#include <cstdint>
#include <functional>
#include <mutex>
#include <ranges>
#include <string>
#include <string_view>
#include <unordered_map>
export module tool.popcap.trail.utils;
import tool.popcap.trail.core;

export namespace Trail {

[[nodiscard]] inline std::string_view curveName(int32_t value) noexcept {
    return value >= 0 && static_cast<size_t>(value) < kCurveNames.size() ? kCurveNames[value] : kCurveNames[1];
}

[[nodiscard]] inline int32_t curveValue(std::string_view name) noexcept {
    if (auto it = std::ranges::find(kCurveNames, name); it != kCurveNames.end())
        return static_cast<int32_t>(it - kCurveNames.begin());
    return 1;
}

[[nodiscard]] inline Format parseFormat(std::string_view value) noexcept {
    if (value == "GameConsole") return Format::GameConsole;
    if (value == "PC") return Format::PC;
    if (value == "Phone32") return Format::Phone32;
    if (value == "Phone64") return Format::Phone64;
    if (value == "TV") return Format::TV;
    if (value == "WP") return Format::WP;
    if (value == "Json") return Format::Json;
    if (value == "RawXml") return Format::RawXml;
    return Format::Unknown;
}

class ImageMap {
public:
    static void clear() {
        std::lock_guard lock(mutex_);
        toId_.clear();
        toPath_.clear();
        initialized_ = false;
    }

    static void add(std::string_view path, int32_t id) {
        std::lock_guard lock(mutex_);
        addUnlocked(path, id);
    }

    [[nodiscard]] static std::string fromId(int32_t id) {
        ensureInitialized();
        std::lock_guard lock(mutex_);
        if (auto it = toPath_.find(id); it != toPath_.end()) return it->second;
        return {};
    }

    [[nodiscard]] static int32_t toId(std::string_view path) {
        ensureInitialized();
        if (path.empty()) return -1;
        std::lock_guard lock(mutex_);
        if (auto it = toId_.find(path); it != toId_.end()) return it->second;
        int32_t id{};
        const auto [ptr, ec] = std::from_chars(path.data(), path.data() + path.size(), id);
        return ec == std::errc{} && ptr == path.data() + path.size() ? id : -1;
    }

private:
    static void ensureInitialized() {
        std::lock_guard lock(mutex_);
        if (!initialized_) {
            addUnlocked("PopStudioExample", 99999);
            initialized_ = true;
        }
    }

    static void addUnlocked(std::string_view path, int32_t id) {
        auto key = std::string(path);
        toId_.insert_or_assign(key, id);
        toPath_.insert_or_assign(id, std::move(key));
    }

    struct StringHash {
        using is_transparent = void;
        [[nodiscard]] size_t operator()(std::string_view value) const noexcept { return std::hash<std::string_view>{}(value); }
    };

    inline static std::unordered_map<std::string, int32_t, StringHash, std::equal_to<>> toId_;
    inline static std::unordered_map<int32_t, std::string> toPath_;
    inline static std::mutex mutex_;
    inline static bool initialized_ = false;
};

}
