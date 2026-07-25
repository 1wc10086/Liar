module;
#include <algorithm>
#include <cctype>
#include <charconv>
#include <cstdio>
#include <limits>
#include <string>
#include <string_view>
export module tool.shell.core_utils;

export struct SystemInfo {
    std::string kernelVer;
    std::string scriptVer;
    std::string shellVer;
    std::string arch;
};

export namespace ShellCore {

enum class ResultCode : int {
    Success = 0,
    FileNotExist = 1,
    FuncNotFound = 2,
    ExecFailed = 3
};

inline std::string makeResult(int code, double t = 0.0, const std::string& out = {}) {
    char buf[64];
    std::snprintf(buf, sizeof(buf), "%d:%.4f", code, t);
    return out.empty() ? std::string(buf) : std::string(buf) + ':' + out;
}

inline bool endsWithIC(std::string_view s, std::string_view suffix) {
    if (suffix.empty()) return true;
    if (s.size() < suffix.size()) return false;
    return std::equal(suffix.rbegin(), suffix.rend(), s.rbegin(), [](unsigned char a, unsigned char b) {
        return std::tolower(a) == std::tolower(b);
    });
}

[[nodiscard]] inline size_t parseByteSize(std::string_view text, size_t fallback) noexcept {
    while (!text.empty() && std::isspace(static_cast<unsigned char>(text.front()))) text.remove_prefix(1);
    while (!text.empty() && std::isspace(static_cast<unsigned char>(text.back()))) text.remove_suffix(1);
    if (text.empty()) return fallback;

    size_t multiplier = 1;
    switch (static_cast<char>(std::tolower(static_cast<unsigned char>(text.back())))) {
    case 'k': multiplier = 1024ull; text.remove_suffix(1); break;
    case 'm': multiplier = 1024ull * 1024ull; text.remove_suffix(1); break;
    case 'g': multiplier = 1024ull * 1024ull * 1024ull; text.remove_suffix(1); break;
    default: break;
    }
    while (!text.empty() && std::isspace(static_cast<unsigned char>(text.back()))) text.remove_suffix(1);
    if (text.empty()) return fallback;

    const auto dot = text.find('.');
    const auto wholeText = text.substr(0, dot);
    const auto fracText = dot == std::string_view::npos ? std::string_view{} : text.substr(dot + 1);
    size_t whole = 0;
    if (!wholeText.empty()) {
        const auto r = std::from_chars(wholeText.data(), wholeText.data() + wholeText.size(), whole);
        if (r.ec != std::errc{} || r.ptr != wholeText.data() + wholeText.size()) return fallback;
    }
    if (whole > std::numeric_limits<size_t>::max() / multiplier) return fallback;

    size_t result = whole * multiplier;
    if (!fracText.empty()) {
        size_t frac = 0;
        size_t div = 1;
        const auto r = std::from_chars(fracText.data(), fracText.data() + fracText.size(), frac);
        if (r.ec != std::errc{} || r.ptr != fracText.data() + fracText.size()) return fallback;
        for (size_t i = 0; i < fracText.size(); ++i) {
            if (div > std::numeric_limits<size_t>::max() / 10) return fallback;
            div *= 10;
        }
        result += (multiplier / div) * frac;
    }
    return result ? result : fallback;
}

}