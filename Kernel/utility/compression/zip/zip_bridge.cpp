module;
#include <array>
#include <cstddef>
#include <cstdint>
#include <cstdio>
#include <dlfcn.h>
#include <mutex>
#include <string>
#include <string_view>
#include <utility>
#include <vector>

module utility.compression.zip.zip_core;

namespace zip_ns {
namespace {

struct CEntry { const char* name; const void* data; uint64_t size; uint32_t directory; };
struct CInfo { const char* name; uint64_t size; uint64_t compressed_size; uint16_t method; uint16_t encryption; uint32_t directory; };
struct Api {
    void* library{};
    const char* path{};
    const char* error{};
    uint32_t (*abi_version)(){};
    const char* (*version_string)(){};
    void (*free_memory)(void*){};
    int32_t (*compress)(const CEntry*, uint64_t, int32_t, const char*, void**, uint64_t*){};
    void* (*open)(const void*, uint64_t, const char*){};
    void (*close)(void*){};
    uint64_t (*entry_count)(void*){};
    int32_t (*entry_info)(void*, uint64_t, CInfo*){};
    void* (*entry_open)(void*, uint64_t, const char*){};
    int64_t (*entry_read)(void*, void*, uint64_t){};
    void (*entry_close)(void*){};
    int32_t (*pkware_supported)(){};
    [[nodiscard]] bool ready() const noexcept { return library && abi_version && version_string && free_memory && compress && open && close && entry_count && entry_info && entry_open && entry_read && entry_close && pkware_supported; }
};

Api api;
std::once_flag once;
std::array<char, 4096> fallback{};

template <class T>
void bind(T& destination, const char* name) noexcept { destination = reinterpret_cast<T>(dlsym(api.library, name)); }

void initialize() noexcept {
    Dl_info location{};
    if (dladdr(reinterpret_cast<const void*>(&initialize), &location) && location.dli_fname) {
        const std::string_view executable{location.dli_fname};
        const auto slash = executable.rfind('/');
        if (slash != std::string_view::npos) {
            const auto directory = executable.substr(0, slash);
            if (directory.size() + sizeof("/libzip.so") <= fallback.size()) {
                std::snprintf(fallback.data(), fallback.size(), "%.*s/libzip.so", static_cast<int>(directory.size()), directory.data());
                api.library = dlopen(fallback.data(), RTLD_NOW | RTLD_LOCAL);
                if (api.library) api.path = fallback.data();
            }
        }
    }
    if (!api.library) {
        for (const char* name : {"libzip.so", "libzip.so.1"}) {
            api.library = dlopen(name, RTLD_NOW | RTLD_LOCAL);
            if (api.library) { api.path = name; break; }
        }
    }
    if (!api.library) { api.error = dlerror(); return; }
    bind(api.abi_version, "kzip_abi_version"); bind(api.version_string, "kzip_version_string"); bind(api.free_memory, "kzip_free");
    bind(api.compress, "kzip_compress"); bind(api.open, "kzip_open"); bind(api.close, "kzip_close"); bind(api.entry_count, "kzip_entry_count");
    bind(api.entry_info, "kzip_entry_info"); bind(api.entry_open, "kzip_entry_open"); bind(api.entry_read, "kzip_entry_read"); bind(api.entry_close, "kzip_entry_close"); bind(api.pkware_supported, "kzip_pkware_supported");
    if (!api.ready()) api.error = "missing required kzip symbol";
}

Api& zip() noexcept { std::call_once(once, initialize); return api; }

}

uint32_t abi_version() noexcept { auto& a = zip(); return a.abi_version ? a.abi_version() : 0; }
std::string_view version_string() noexcept { auto& a = zip(); return a.version_string ? a.version_string() : std::string_view{}; }
bool loaded() noexcept { return zip().ready(); }
std::string_view library_path() noexcept { auto& a = zip(); return a.path ? std::string_view{a.path} : std::string_view{}; }
std::string_view load_error() noexcept { auto& a = zip(); return a.error ? std::string_view{a.error} : std::string_view{}; }
bool pkware_supported() noexcept { auto& a = zip(); return a.pkware_supported && a.pkware_supported() != 0; }
void release(void* data) noexcept { auto& a = zip(); if (data && a.free_memory) a.free_memory(data); }

int32_t compress_to(void** output, uint64_t& output_size, std::span<const InputEntry> entries, int32_t level, std::string_view password) noexcept {
    auto& a = zip();
    if (!a.compress || entries.size() > UINT64_MAX) return -1;
    std::vector<CEntry> input;
    std::vector<std::string> names;
    input.reserve(entries.size());
    names.reserve(entries.size());
    for (const auto& entry : entries) names.emplace_back(entry.name);
    for (size_t i{}; i < entries.size(); ++i) input.push_back({names[i].c_str(), entries[i].data.data(), entries[i].data.size(), entries[i].directory});
    const std::string secret{password};
    return a.compress(input.data(), input.size(), level, secret.empty() ? nullptr : secret.c_str(), output, &output_size);
}

Archive::Archive(view_type input, std::string_view password) noexcept { auto& a = zip(); const std::string secret{password}; if (a.open) handle_ = a.open(input.data(), input.size(), secret.empty() ? nullptr : secret.c_str()); }
Archive::Archive(Archive&& other) noexcept : handle_(std::exchange(other.handle_, nullptr)) {}
Archive& Archive::operator=(Archive&& other) noexcept { if (this != &other) { reset(); handle_ = std::exchange(other.handle_, nullptr); } return *this; }
Archive::~Archive() { reset(); }
uint64_t Archive::entry_count() const noexcept { auto& a = zip(); return handle_ && a.entry_count ? a.entry_count(handle_) : 0; }
bool Archive::entry_info(uint64_t index, EntryInfo& output) const noexcept { auto& a = zip(); CInfo info{}; if (!handle_ || !a.entry_info || a.entry_info(handle_, index, &info) != 0) return false; output = {info.name ? info.name : "", info.size, info.compressed_size, info.method, info.encryption, info.directory != 0}; return true; }
EntryStream Archive::open_entry(uint64_t index, std::string_view password) const noexcept { auto& a = zip(); const std::string secret{password}; return EntryStream{handle_ && a.entry_open ? a.entry_open(handle_, index, secret.empty() ? nullptr : secret.c_str()) : nullptr}; }
void Archive::reset() noexcept { auto& a = zip(); if (handle_ && a.close) a.close(std::exchange(handle_, nullptr)); else handle_ = nullptr; }

EntryStream::EntryStream(EntryStream&& other) noexcept : handle_(std::exchange(other.handle_, nullptr)) {}
EntryStream& EntryStream::operator=(EntryStream&& other) noexcept { if (this != &other) { reset(); handle_ = std::exchange(other.handle_, nullptr); } return *this; }
EntryStream::~EntryStream() { reset(); }
int64_t EntryStream::read(std::span<byte> output) noexcept { auto& a = zip(); return handle_ && a.entry_read ? a.entry_read(handle_, output.data(), output.size()) : -1; }
void EntryStream::reset() noexcept { auto& a = zip(); if (handle_ && a.entry_close) a.entry_close(std::exchange(handle_, nullptr)); else handle_ = nullptr; }

}
