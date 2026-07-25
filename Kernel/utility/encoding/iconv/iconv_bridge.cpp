module;
#include <algorithm>
#include <array>
#include <cerrno>
#include <cstddef>
#include <cstdio>
#include <dlfcn.h>
#include <mutex>
#include <string_view>

module utility.encoding.iconv.iconv_core;

namespace iconv_ns {
namespace {

struct Api {
    using convert_t = int (*)(const char*, const char*, const void*, size_t, void*, size_t, size_t*, size_t*);

    void* library{};
    const char* path{};
    const char* error{};
    convert_t convert{};

    [[nodiscard]] bool ready() const noexcept { return library && convert; }
};

Api api;
std::once_flag api_once;
std::array<char, 4096> fallback_path{};

void initialize() noexcept {
    constexpr const char* names[] = {"libiconv.so"};
    for (const auto* name : names) {
        api.library = dlopen(name, RTLD_NOW | RTLD_LOCAL);
        if (api.library) {
            api.path = name;
            break;
        }
    }

    if (!api.library) {
        Dl_info info{};
        if (dladdr(reinterpret_cast<const void*>(&initialize), &info) && info.dli_fname) {
            const std::string_view path{info.dli_fname};
            if (const auto slash = path.rfind('/'); slash != std::string_view::npos && slash + 12 < fallback_path.size()) {
                const auto directory = path.substr(0, slash);
                if (directory.size() + 12 < fallback_path.size()) {
                    std::snprintf(fallback_path.data(), fallback_path.size(), "%.*s/libiconv.so", static_cast<int>(directory.size()), directory.data());
                    api.library = dlopen(fallback_path.data(), RTLD_NOW | RTLD_LOCAL);
                    if (api.library) api.path = fallback_path.data();
                }
            }
        }
    }

    if (!api.library) {
        api.error = dlerror();
        return;
    }

    api.convert = reinterpret_cast<Api::convert_t>(dlsym(api.library, "kiconv_convert"));
    if (!api.ready()) api.error = "missing required kiconv_convert symbol";
}

Api& iconv() noexcept {
    std::call_once(api_once, initialize);
    return api;
}

}

bool loaded() noexcept { return iconv().ready(); }
std::string_view library_path() noexcept { const auto& value = iconv(); return value.path ? std::string_view{value.path} : std::string_view{}; }
std::string_view load_error() noexcept { const auto& value = iconv(); return value.error ? std::string_view{value.error} : std::string_view{}; }

ConversionResult convert_to(const char* to_encoding, const char* from_encoding, view_type input, std::span<byte> output) noexcept {
    const auto& value = iconv();
    if (!value.convert || !to_encoding || !from_encoding) return {EINVAL, 0, 0};

    ConversionResult result{};
    result.error = value.convert(to_encoding, from_encoding, input.data(), input.size(), output.data(), output.size(), &result.input_consumed, &result.output_written);
    return result;
}

ConversionResult convert_to(std::string_view to_encoding, std::string_view from_encoding, view_type input, std::span<byte> output) noexcept {
    if (to_encoding.find('\0') != std::string_view::npos || from_encoding.find('\0') != std::string_view::npos || to_encoding.size() >= 128 || from_encoding.size() >= 128) return {EINVAL, 0, 0};
    std::array<char, 128> to_name{};
    std::array<char, 128> from_name{};
    std::copy(to_encoding.begin(), to_encoding.end(), to_name.begin());
    std::copy(from_encoding.begin(), from_encoding.end(), from_name.begin());
    return convert_to(to_name.data(), from_name.data(), input, output);
}

}
