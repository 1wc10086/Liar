module;
#include <cstddef>
#include <cstdint>
#include <array>
#include <cstdio>
#include <dlfcn.h>
#include <mutex>
#include <span>
#include <string_view>
#include <utility>

module utility.compression.heatshrink.heatshrink_core;

namespace heatshrink_ns {
namespace {
struct Api {
    using create_t = void* (*)(uint16_t, uint8_t, uint8_t);
    using destroy_t = void (*)(void*);
    using reset_t = int32_t (*)(void*);
    using sink_t = int32_t (*)(void*, const void*, size_t, size_t*);
    using poll_t = int32_t (*)(void*, void*, size_t, size_t*);
    using finish_t = int32_t (*)(void*);
    void* library{}; create_t create{}; destroy_t destroy{}; reset_t reset{}; sink_t sink{}; poll_t poll{}; finish_t finish{};
};
Api api; std::once_flag once;
void init() noexcept {
    api.library = dlopen("libncomp.so", RTLD_NOW | RTLD_LOCAL);
    if (!api.library) {
        Dl_info info{};
        if (dladdr(reinterpret_cast<const void*>(&init), &info) && info.dli_fname) {
            const std::string_view path{info.dli_fname};
            std::array<char, 4096> fallback{};
            if (const auto slash = path.rfind('/'); slash != std::string_view::npos && slash + 12 < fallback.size()) {
                std::snprintf(fallback.data(), fallback.size(), "%.*s/libncomp.so", static_cast<int>(slash), path.data());
                api.library = dlopen(fallback.data(), RTLD_NOW | RTLD_LOCAL);
            }
        }
    }
    if (!api.library) return;
    api.create = reinterpret_cast<Api::create_t>(dlsym(api.library, "ncomp_heatshrink_create"));
    api.destroy = reinterpret_cast<Api::destroy_t>(dlsym(api.library, "ncomp_heatshrink_destroy"));
    api.reset = reinterpret_cast<Api::reset_t>(dlsym(api.library, "ncomp_heatshrink_reset"));
    api.sink = reinterpret_cast<Api::sink_t>(dlsym(api.library, "ncomp_heatshrink_sink"));
    api.poll = reinterpret_cast<Api::poll_t>(dlsym(api.library, "ncomp_heatshrink_poll"));
    api.finish = reinterpret_cast<Api::finish_t>(dlsym(api.library, "ncomp_heatshrink_finish"));
}
Api& ncomp() noexcept { std::call_once(once, init); return api; }
}
bool loaded() noexcept { auto& a = ncomp(); return a.create && a.destroy && a.reset && a.sink && a.poll && a.finish; }
Decoder::Decoder(uint16_t input_buffer_size, uint8_t window_bits, uint8_t lookahead_bits) noexcept { auto& a = ncomp(); if (a.create) handle_ = a.create(input_buffer_size, window_bits, lookahead_bits); }
Decoder::~Decoder() { auto& a = ncomp(); if (handle_ && a.destroy) a.destroy(std::exchange(handle_, nullptr)); }
int32_t Decoder::sink(view_type input, size_t& consumed) noexcept { auto& a = ncomp(); return handle_ && a.sink ? a.sink(handle_, input.data(), input.size(), &consumed) : -1; }
int32_t Decoder::poll(std::span<byte> output, size_t& written) noexcept { auto& a = ncomp(); return handle_ && a.poll ? a.poll(handle_, output.data(), output.size(), &written) : -1; }
int32_t Decoder::finish() noexcept { auto& a = ncomp(); return handle_ && a.finish ? a.finish(handle_) : -1; }
void Decoder::reset() noexcept { auto& a = ncomp(); if (handle_ && a.reset) a.reset(handle_); }
}
