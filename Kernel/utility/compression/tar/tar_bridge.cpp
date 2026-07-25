module;
#include <array>
#include <cstddef>
#include <cstdint>
#include <cstring>
#include <cstdio>
#include <dlfcn.h>
#include <mutex>
#include <string_view>
#include <vector>

module utility.compression.tar.tar_core;

namespace tar_ns {
namespace {

struct Api {
    struct InputEntry { const char* name; const void* data; size_t size; uint32_t type; };
    using extract_t = int64_t (*)(const void*, size_t, void*, int32_t (*)(void*, const char*, uint32_t, uint64_t), int32_t (*)(void*, const void*, size_t));
    using create_file_t = int64_t (*)(void*, size_t, const char*, const void*, size_t);
    using create_t = int64_t (*)(void*, size_t, const InputEntry*, size_t);
    void* library{};
    extract_t extract{};
    create_file_t create_file{};
    create_t create{};
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
    if (!api.library) {
        api.error = dlerror();
        return;
    }
    api.extract = reinterpret_cast<Api::extract_t>(dlsym(api.library, "ncomp_tar_extract"));
    api.create_file = reinterpret_cast<Api::create_file_t>(dlsym(api.library, "ncomp_tar_create_file"));
    api.create = reinterpret_cast<Api::create_t>(dlsym(api.library, "ncomp_tar_create"));
    if (!api.extract || !api.create_file || !api.create) api.error = "missing required ncomp symbol";
}

Api& ncomp() noexcept { std::call_once(once, init); return api; }

struct CallbackState {
    EntryCallback callback{};
    void* context{};
    const char* name{};
    uint32_t type{};
    uint64_t size{};
    bool accepted{};
};

int32_t entry_callback(void* opaque, const char* name, uint32_t type, uint64_t size) noexcept {
    auto& state = *static_cast<CallbackState*>(opaque);
    state.name = name;
    state.type = type;
    state.size = size;
    state.accepted = true;
    return state.callback(Entry{state.name, state.type, state.size, {}}, state.context) ? 0 : -1;
}

int32_t data_callback(void* opaque, const void* data, size_t size) noexcept {
    auto& state = *static_cast<CallbackState*>(opaque);
    if (!state.accepted) return -1;
    return state.callback(Entry{state.name, state.type, state.size, {static_cast<const byte*>(data), size}}, state.context) ? 0 : -1;
}

}

bool loaded() noexcept { auto& a = ncomp(); return a.library && a.extract && a.create_file && a.create; }
std::string_view library_path() noexcept { auto& a = ncomp(); return a.path ? std::string_view{a.path} : std::string_view{}; }
std::string_view load_error() noexcept { auto& a = ncomp(); return a.error ? std::string_view{a.error} : std::string_view{}; }
bool extract(view_type input, EntryCallback callback, void* context) noexcept {
    auto& a = ncomp();
    if (!a.extract || !callback) return false;
    CallbackState state{callback, context};
    return a.extract(input.data(), input.size(), &state, entry_callback, data_callback) >= 0;
}

buffer_type create_file(view_type input, std::string_view name) {
    auto& a = ncomp();
    if (!a.create_file || name.empty() || name.size() >= 100 || input.size() > static_cast<size_t>(-1) - 1536) return {};
    const auto padded = (input.size() + 511) & ~size_t{511};
    buffer_type output(padded + 1536);
    std::array<char, 100> path{};
    std::memcpy(path.data(), name.data(), name.size());
    const auto written = a.create_file(output.data(), output.size(), path.data(), input.data(), input.size());
    if (written < 0 || static_cast<size_t>(written) > output.size()) return {};
    output.resize(static_cast<size_t>(written));
    return output;
}

buffer_type create(std::span<const InputEntry> entries) {
    auto& a = ncomp();
    if (!a.create || entries.empty()) return {};
    size_t capacity = 1024;
    std::vector<std::array<char, 100>> names(entries.size());
    std::vector<Api::InputEntry> native_entries;
    native_entries.reserve(entries.size());
    for (size_t i = 0; i < entries.size(); ++i) {
        const auto& entry = entries[i];
        if (entry.name.empty() || entry.name.size() >= names[i].size() || entry.data.size() > static_cast<size_t>(-1) - 512) return {};
        std::memcpy(names[i].data(), entry.name.data(), entry.name.size());
        const auto padded = (entry.data.size() + 511) & ~size_t{511};
        if (capacity > static_cast<size_t>(-1) - padded - 512) return {};
        capacity += padded + 512;
        native_entries.push_back({names[i].data(), entry.data.data(), entry.data.size(), entry.directory ? directory : regular_file});
    }
    buffer_type output(capacity);
    const auto written = a.create(output.data(), output.size(), native_entries.data(), native_entries.size());
    if (written < 0 || static_cast<size_t>(written) > output.size()) return {};
    output.resize(static_cast<size_t>(written));
    return output;
}

}
