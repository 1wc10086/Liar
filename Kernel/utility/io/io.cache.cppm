module;
#include <filesystem>
#include <mutex>
#include <shared_mutex>
#include <string>
#include <string_view>
#include <unordered_set>
export module utility.io.cache;

export struct StringViewHash {
    using is_transparent = void;
    [[nodiscard]] size_t operator()(std::string_view s) const noexcept { return std::hash<std::string_view>{}(s); }
};

export class DirectoryCache {
public:
    void ensure(std::string_view dirPath) {
        if (dirPath.empty()) return;
        {
            std::shared_lock lk(mu_);
            if (known_.contains(dirPath)) return;
        }
        std::lock_guard lk(mu_);
        if (!known_.emplace(dirPath).second) return;
        std::error_code ec;
        std::filesystem::create_directories(dirPath, ec);
    }

    void clear() {
        std::lock_guard lk(mu_);
        known_.clear();
    }

private:
    std::unordered_set<std::string, StringViewHash, std::equal_to<>> known_;
    std::shared_mutex mu_;
};

export inline DirectoryCache& getGlobalDirCache() {
    static DirectoryCache cache;
    return cache;
}
