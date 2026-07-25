module;
#include <algorithm>
#include <array>
#include <cstddef>
#include <cstdint>
#include <cstdio>
#include <dlfcn.h>
#include <limits>
#include <mutex>
#include <optional>
#include <string_view>
#include <vector>

module utility.compression.ncomp_core;

namespace ncomp_ns {
namespace {

struct Api {
    using decompress_t = int64_t (*)(void*, size_t, const void*, size_t);
    using compress_t = int64_t (*)(void*, size_t, const void*, size_t, int32_t);
    void* library{};
    std::array<decompress_t, 7> decompress{};
    std::array<compress_t, 7> compress{};
    const char* path{};
    const char* error{};
};

Api api;
std::once_flag once;
std::array<char, 4096> fallback_path{};

void init() noexcept {
    api.library = dlopen("libncomp.so", RTLD_NOW | RTLD_LOCAL);
    if (api.library) api.path = "libncomp.so";
    if (!api.library) {
        Dl_info info{};
        if (dladdr(reinterpret_cast<const void*>(&init), &info) && info.dli_fname) {
            const std::string_view path{info.dli_fname};
            if (const auto slash = path.rfind('/'); slash != std::string_view::npos && slash + 12 < fallback_path.size()) {
                std::snprintf(fallback_path.data(), fallback_path.size(), "%.*s/libncomp.so", static_cast<int>(slash), path.data());
                api.library = dlopen(fallback_path.data(), RTLD_NOW | RTLD_LOCAL);
                if (api.library) api.path = fallback_path.data();
            }
        }
    }
    if (!api.library) { api.error = dlerror(); return; }
    constexpr std::array names{"ncomp_fastlz_decompress", "ncomp_lzf_decompress", "ncomp_bsc_decompress", "ncomp_zpaq_decompress", "ncomp_lzw_decompress", "ncomp_lzfse_decompress", "ncomp_heatshrink_decompress_unavailable"};
    for (size_t i = 0; i < names.size(); ++i) api.decompress[i] = reinterpret_cast<Api::decompress_t>(dlsym(api.library, names[i]));
    constexpr std::array compress_names{"ncomp_fastlz_compress", "ncomp_lzf_compress", "ncomp_bsc_compress", "ncomp_zpaq_compress", "ncomp_lzw_compress", "ncomp_lzfse_compress", "ncomp_heatshrink_compress"};
    for (size_t i = 0; i < compress_names.size(); ++i) api.compress[i] = reinterpret_cast<Api::compress_t>(dlsym(api.library, compress_names[i]));
    if (std::any_of(api.decompress.begin(), api.decompress.end() - 1, [](auto fn) { return !fn; }) || std::any_of(api.compress.begin(), api.compress.end(), [](auto fn) { return !fn; })) api.error = "missing required ncomp symbol";
}

Api& ncomp() noexcept { std::call_once(once, init); return api; }

}

bool loaded() noexcept { auto& a = ncomp(); return a.library && !a.error; }
std::string_view library_path() noexcept { auto& a = ncomp(); return a.path ? std::string_view{a.path} : std::string_view{}; }
std::string_view load_error() noexcept { auto& a = ncomp(); return a.error ? std::string_view{a.error} : std::string_view{}; }
int64_t decompress_to(Algorithm algorithm, std::span<byte> output, view_type input) noexcept {
    auto& a = ncomp();
    const auto fn = a.decompress[static_cast<size_t>(algorithm)];
    return fn ? fn(output.data(), output.size(), input.data(), input.size()) : -1;
}
int64_t compress_to(Algorithm algorithm, std::span<byte> output, view_type input, int32_t level) noexcept {
    auto& a = ncomp();
    const auto fn = a.compress[static_cast<size_t>(algorithm)];
    return fn ? fn(output.data(), output.size(), input.data(), input.size(), level) : -1;
}
std::optional<buffer_type> decompress(Algorithm algorithm, view_type input, size_t expected_size) {
    size_t capacity = expected_size ? expected_size : std::max<size_t>(input.size() * 4, 1024);
    for (int attempts = 0; attempts != 24 && capacity <= std::numeric_limits<size_t>::max() / 2; ++attempts) {
        buffer_type output(capacity);
        const auto written = decompress_to(algorithm, output, input);
        if (written >= 0 && static_cast<size_t>(written) <= output.size()) {
            if (expected_size || static_cast<size_t>(written) < output.size()) {
                output.resize(static_cast<size_t>(written));
                return output;
            }
        }
        if (expected_size) return std::nullopt;
        capacity *= 2;
    }
    return std::nullopt;
}

std::optional<buffer_type> compress(Algorithm algorithm, view_type input, int32_t level) {
    size_t capacity = std::max<size_t>(input.size() + 64, 1024);
    for (int attempts = 0; attempts != 24 && capacity <= std::numeric_limits<size_t>::max() / 2; ++attempts) {
        buffer_type output(capacity);
        const auto written = compress_to(algorithm, output, input, level);
        if (written >= 0 && static_cast<size_t>(written) <= output.size()) {
            output.resize(static_cast<size_t>(written));
            return output;
        }
        capacity *= 2;
    }
    return std::nullopt;
}

}
