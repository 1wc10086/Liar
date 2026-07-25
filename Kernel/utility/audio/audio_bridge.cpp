module;
#include <array>
#include <cstddef>
#include <cstdint>
#include <cstdio>
#include <dlfcn.h>
#include <limits>
#include <memory>
#include <mutex>
#include <span>
#include <string_view>
#include <utility>
#include <vector>

module utility.audio.audio_core;

namespace audio_ns {
namespace {

struct Api {
    using abi_t = uint32_t (*)(); using version_t = const char* (*)(); using open_t = int32_t (*)(uint32_t, const uint8_t*, size_t, uint32_t, void**, uint32_t*, uint32_t*, uint64_t*); using read_t = int64_t (*)(void*, float*, uint32_t); using seek_t = int32_t (*)(void*, uint64_t); using close_t = void (*)(void*); using encode_t = int32_t (*)(const float*, uint64_t, uint32_t, uint32_t, uint32_t, uint8_t**, size_t*); using wav_t = int32_t (*)(const float*, uint64_t, uint32_t, uint32_t, uint8_t**, size_t*); using flac_t = int32_t (*)(const uint8_t*, size_t, float*, uint32_t, uint32_t*, uint32_t*, uint64_t*, uint32_t*); using wma_t = int32_t (*)(const uint8_t*, size_t, float*, uint32_t, uint32_t*, uint32_t*, uint32_t*); using aac_t = wma_t; using alac_t = int32_t (*)(const uint8_t*, size_t, const uint8_t*, size_t, int16_t*, uint32_t, uint32_t*, uint32_t*, uint32_t*); using alac_encode_t = int32_t (*)(const int16_t*, uint32_t, uint32_t, uint32_t, uint32_t, uint8_t*, size_t, size_t*, uint8_t*, size_t, size_t*); using free_t = void (*)(void*);
    void* library{}; const char* path{}; const char* error{}; abi_t abi{}; version_t version{}; open_t open{}; read_t read{}; seek_t seek{}; close_t close{}; wav_t wav{}; encode_t flac_encode{}; flac_t flac{}; wma_t wma{}; aac_t aac{}; alac_t alac{}; alac_encode_t alac_encode{}; free_t free{};
    [[nodiscard]] bool ready() const noexcept { return library && abi && version && open && read && seek && close && wav && flac_encode && flac && wma && aac && alac && alac_encode && free && abi() == 2; }
};

Api api; std::once_flag once; std::array<char, 4096> fallback{};
template <class T> void bind(T& value, const char* name) noexcept { value = reinterpret_cast<T>(dlsym(api.library, name)); }
void initialize() noexcept {
    api.library = dlopen("libaudio.so", RTLD_NOW | RTLD_LOCAL); if (api.library) api.path = "libaudio.so";
    if (!api.library) { Dl_info location{}; if (dladdr(reinterpret_cast<const void*>(&initialize), &location) && location.dli_fname) { const std::string_view source{location.dli_fname}; if (const auto slash = source.rfind('/'); slash != source.npos && slash + sizeof("/libaudio.so") <= fallback.size()) { const auto directory = source.substr(0, slash); std::snprintf(fallback.data(), fallback.size(), "%.*s/libaudio.so", static_cast<int>(directory.size()), directory.data()); api.library = dlopen(fallback.data(), RTLD_NOW | RTLD_LOCAL); if (api.library) api.path = fallback.data(); } } }
    if (!api.library) { api.error = dlerror(); return; }
    bind(api.abi, "ka_audio_abi_version"); bind(api.version, "ka_audio_version"); bind(api.open, "ka_audio_open_memory"); bind(api.read, "ka_audio_read_f32"); bind(api.seek, "ka_audio_seek_frame"); bind(api.close, "ka_audio_close"); bind(api.wav, "ka_wav_encode_f32"); bind(api.flac_encode, "ka_flac_encode_f32"); bind(api.flac, "ka_flac_decode_f32"); bind(api.wma, "ka_wma_decode_f32"); bind(api.aac, "ka_aac_decode_f32"); bind(api.alac, "ka_alac_decode_s16"); bind(api.alac_encode, "ka_alac_encode_s16"); bind(api.free, "ka_audio_free");
    if (!api.ready()) api.error = "missing required audio ABI symbol or unsupported ABI";
}
Api& instance() noexcept { std::call_once(once, initialize); return api; }

}

Decoder::Decoder(Decoder&& other) noexcept : handle_(std::exchange(other.handle_, nullptr)), info_(other.info_) {}
Decoder& Decoder::operator=(Decoder&& other) noexcept { if (this != &other) { if (handle_) instance().close(handle_); handle_ = std::exchange(other.handle_, nullptr); info_ = other.info_; } return *this; }
Decoder::~Decoder() { if (handle_) instance().close(handle_); }
Decoder::operator bool() const noexcept { return handle_ != nullptr; }
Info Decoder::info() const noexcept { return info_; }
int64_t Decoder::read(std::span<float> output) noexcept { const auto channels = info_.channels; return handle_ && channels && output.size() / channels <= UINT32_MAX ? instance().read(handle_, output.data(), static_cast<uint32_t>(output.size() / channels)) : -1; }
bool Decoder::seek(uint64_t frame) noexcept { return handle_ && instance().seek(handle_, frame) == 0; }

bool loaded() noexcept { return instance().ready(); }
std::string_view library_path() noexcept { auto& value = instance(); return value.path ? value.path : ""; }
std::string_view load_error() noexcept { auto& value = instance(); return value.error ? value.error : ""; }
std::unique_ptr<Decoder> open(Codec codec, bytes input, uint32_t sample_rate) noexcept {
    auto& value = instance(); if (!value.ready() || input.empty()) return {};
    try {
        auto decoder = std::make_unique<Decoder>(); uint32_t rate{}, channels{}; uint64_t frames{};
        if (value.open(static_cast<uint32_t>(codec), input.data(), input.size(), sample_rate, &decoder->handle_, &rate, &channels, &frames) != 0) return {};
        decoder->info_ = {rate, channels, frames}; return decoder;
    } catch (...) { return {}; }
}
Chunk decode(Codec codec, bytes input, uint32_t capacity, uint32_t sample_rate) noexcept {
    try {
        Chunk result{}; auto decoder = open(codec, input, sample_rate); if (!decoder || !capacity || !decoder->info().channels || capacity > std::numeric_limits<size_t>::max() / decoder->info().channels) return result; result.info = decoder->info(); result.samples.resize(static_cast<size_t>(capacity) * result.info.channels); const auto frames = decoder->read(result.samples); if (frames < 0 || static_cast<uint64_t>(frames) > capacity) return {}; result.info.frames = static_cast<uint64_t>(frames); result.samples.resize(static_cast<size_t>(frames) * result.info.channels); return result;
    } catch (...) { return {}; }
}
Chunk decode_flac(bytes input, uint32_t capacity) noexcept {
    try {
        Chunk result{}; auto& value = instance(); if (!value.ready() || input.empty() || !capacity || capacity > std::numeric_limits<size_t>::max() / 8) return result; result.samples.resize(static_cast<size_t>(capacity) * 8); uint32_t written{};
        if (value.flac(input.data(), input.size(), result.samples.data(), capacity, &result.info.sample_rate, &result.info.channels, &result.info.frames, &written) != 0 || !result.info.channels || written > capacity) return {};
        result.samples.resize(static_cast<size_t>(written) * result.info.channels); return result;
    } catch (...) { return {}; }
}
Chunk decode_wma(bytes input, uint32_t capacity) noexcept {
    try {
        Chunk result{}; auto& value = instance(); if (!value.ready() || input.empty() || !capacity || capacity > std::numeric_limits<size_t>::max() / 2) return result; result.samples.resize(static_cast<size_t>(capacity) * 2); uint32_t written{};
        if (value.wma(input.data(), input.size(), result.samples.data(), capacity, &result.info.sample_rate, &result.info.channels, &written) != 0 || !result.info.channels || result.info.channels > 2 || written > capacity) return {};
        result.info.frames = unknown_frames; result.samples.resize(static_cast<size_t>(written) * result.info.channels); return result;
    } catch (...) { return {}; }
}
Chunk decode_aac(bytes input, uint32_t capacity) noexcept {
    try {
        Chunk result{}; auto& value = instance(); if (!value.ready() || input.empty() || !capacity || capacity > std::numeric_limits<size_t>::max() / 8) return result; result.samples.resize(static_cast<size_t>(capacity) * 8); uint32_t written{};
        if (value.aac(input.data(), input.size(), result.samples.data(), capacity, &result.info.sample_rate, &result.info.channels, &written) != 0 || !result.info.channels || result.info.channels > 8 || written > capacity) return {};
        result.info.frames = unknown_frames; result.samples.resize(static_cast<size_t>(written) * result.info.channels); return result;
    } catch (...) { return {}; }
}
std::vector<int16_t> decode_alac_s16(bytes cookie, bytes input, uint32_t capacity, Info& info) noexcept {
    try {
        info = {}; auto& value = instance(); if (!value.ready() || cookie.empty() || input.empty() || !capacity || capacity > std::numeric_limits<size_t>::max() / 8) return {}; std::vector<int16_t> output(static_cast<size_t>(capacity) * 8); uint32_t written{};
        if (value.alac(cookie.data(), cookie.size(), input.data(), input.size(), output.data(), capacity, &info.sample_rate, &info.channels, &written) != 0 || !info.channels || info.channels > 8 || written > capacity) { info = {}; return {}; }
        info.frames = written; output.resize(static_cast<size_t>(written) * info.channels); return output;
    } catch (...) { info = {}; return {}; }
}
AlacFrame encode_alac_s16(std::span<const int16_t> pcm, uint32_t sample_rate, uint32_t channels, uint32_t frame_length) noexcept {
    try {
        AlacFrame result{}; auto& value = instance(); if (!value.ready() || !sample_rate || !channels || channels > 8 || pcm.empty() || pcm.size() % channels || pcm.size() / channels > UINT32_MAX) return result;
        result.data.resize((pcm.size() / channels) * channels * 6 + 1); result.cookie.resize(64); size_t data_size{}, cookie_size{};
        if (value.alac_encode(pcm.data(), static_cast<uint32_t>(pcm.size() / channels), sample_rate, channels, frame_length, result.data.data(), result.data.size(), &data_size, result.cookie.data(), result.cookie.size(), &cookie_size) != 0 || !data_size || !cookie_size) return {};
        result.data.resize(data_size); result.cookie.resize(cookie_size); return result;
    } catch (...) { return {}; }
}
std::vector<uint8_t> encode_wav_f32(std::span<const float> pcm, uint32_t sample_rate, uint32_t channels) noexcept {
    try {
        auto& value = instance(); if (!value.ready() || !sample_rate || !channels || pcm.empty() || pcm.size() % channels) return {}; uint8_t* data{}; size_t size{};
        if (value.wav(pcm.data(), pcm.size() / channels, sample_rate, channels, &data, &size) != 0 || !data) return {};
        std::vector<uint8_t> output(data, data + size); value.free(data); return output;
    } catch (...) { return {}; }
}
std::vector<uint8_t> encode_flac_f32(std::span<const float> pcm, uint32_t sample_rate, uint32_t channels, uint32_t compression) noexcept {
    try {
        auto& value = instance(); if (!value.ready() || !sample_rate || !channels || pcm.empty() || pcm.size() % channels) return {}; uint8_t* data{}; size_t size{};
        if (value.flac_encode(pcm.data(), pcm.size() / channels, sample_rate, channels, compression, &data, &size) != 0 || !data) return {};
        std::vector<uint8_t> output(data, data + size); value.free(data); return output;
    } catch (...) { return {}; }
}

}
