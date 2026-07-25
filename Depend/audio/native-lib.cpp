#define DR_WAV_IMPLEMENTATION
#define DR_WAV_NO_STDIO
#define MINIMP3_IMPLEMENTATION
#define MINIMP3_FLOAT_OUTPUT
#define MINIMP3_NO_STDIO
#include "dr_wav.h"
#include "minimp3_ex.h"
#include "opus.h"
#ifndef restrict
#define restrict __restrict
#endif
#include "xm.h"
#include "FLAC/stream_decoder.h"
#include "FLAC/stream_encoder.h"
#include "neaacdec.h"
#include "ALACBitUtilities.h"
#include "ALACDecoder.h"
#include "ALACEncoder.h"

#include <algorithm>
#include <cstddef>
#include <cstdint>
#include <cstring>
#include <cmath>
#include <cstdlib>
#include <limits>
#include <memory>
#include <new>
#include <vector>

namespace WMADecoder { int decode_wma_memory(const uint8_t*, size_t, float*, uint32_t, uint32_t*, uint32_t*, uint32_t*); }

extern "C" {

struct stb_vorbis;
struct stb_vorbis_info { unsigned sample_rate; int channels; unsigned setup_memory_required; unsigned setup_temp_memory_required; unsigned temp_memory_required; int max_frame_size; };
stb_vorbis* stb_vorbis_open_memory(const unsigned char*, int, int*, const void*);
void stb_vorbis_close(stb_vorbis*);
stb_vorbis_info stb_vorbis_get_info(stb_vorbis*);
int stb_vorbis_get_samples_float_interleaved(stb_vorbis*, int, float*, int);
int stb_vorbis_seek(stb_vorbis*, unsigned);

}

#if defined(_WIN32)
#define KA_API extern "C" __declspec(dllexport)
#else
#define KA_API extern "C" __attribute__((visibility("default")))
#endif

namespace {

constexpr uint32_t abi = 2;
constexpr uint64_t unknown_frames = std::numeric_limits<uint64_t>::max();
enum class Codec : uint32_t { wav = 1, mp3 = 2, vorbis = 3, xm = 4 };

struct Info { uint32_t sample_rate{}; uint32_t channels{}; uint64_t frames{unknown_frames}; };

struct Decoder {
    virtual ~Decoder() = default;
    virtual int64_t read(float* output, uint32_t frames) noexcept = 0;
    virtual bool seek(uint64_t frame) noexcept = 0;
    Info info{};
};

struct Wav final : Decoder {
    drwav value{};
    explicit Wav(const void* data, size_t size) { if (drwav_init_memory(&value, data, size, nullptr)) info = {value.sampleRate, value.channels, value.totalPCMFrameCount}; }
    ~Wav() override { drwav_uninit(&value); }
    int64_t read(float* output, uint32_t frames) noexcept override { return static_cast<int64_t>(drwav_read_pcm_frames_f32(&value, frames, output)); }
    bool seek(uint64_t frame) noexcept override { return drwav_seek_to_pcm_frame(&value, frame) != 0; }
    [[nodiscard]] explicit operator bool() const noexcept { return value.onRead != nullptr; }
};

struct Mp3 final : Decoder {
    mp3dec_ex_t value{};
    explicit Mp3(const uint8_t* data, size_t size) {
        if (!mp3dec_ex_open_buf(&value, data, size, MP3D_SEEK_TO_SAMPLE) && value.info.hz > 0 && value.info.channels > 0) info = {static_cast<uint32_t>(value.info.hz), static_cast<uint32_t>(value.info.channels), value.samples / static_cast<uint64_t>(value.info.channels)};
    }
    ~Mp3() override { mp3dec_ex_close(&value); }
    int64_t read(float* output, uint32_t frames) noexcept override { return static_cast<int64_t>(mp3dec_ex_read(&value, output, static_cast<size_t>(frames) * info.channels) / info.channels); }
    bool seek(uint64_t frame) noexcept override { return mp3dec_ex_seek(&value, frame * info.channels) == 0; }
    [[nodiscard]] explicit operator bool() const noexcept { return value.info.hz > 0 && value.info.channels > 0; }
};

struct Vorbis final : Decoder {
    stb_vorbis* value{};
    explicit Vorbis(const uint8_t* data, size_t size) {
        if (size <= static_cast<size_t>(std::numeric_limits<int>::max())) value = stb_vorbis_open_memory(data, static_cast<int>(size), nullptr, nullptr);
        if (value) { const auto meta = stb_vorbis_get_info(value); info = {meta.sample_rate, static_cast<uint32_t>(meta.channels), unknown_frames}; }
    }
    ~Vorbis() override { if (value) stb_vorbis_close(value); }
    int64_t read(float* output, uint32_t frames) noexcept override { return value ? stb_vorbis_get_samples_float_interleaved(value, static_cast<int>(info.channels), output, static_cast<int>(frames * info.channels)) : -1; }
    bool seek(uint64_t frame) noexcept override { return value && frame <= std::numeric_limits<unsigned>::max() && stb_vorbis_seek(value, static_cast<unsigned>(frame)); }
    [[nodiscard]] explicit operator bool() const noexcept { return value != nullptr; }
};

struct Xm final : Decoder {
    std::vector<std::max_align_t> pool;
    xm_context_t* value{};
    explicit Xm(const uint8_t* data, size_t size, uint32_t rate) {
        if (!data || !size || size > std::numeric_limits<uint32_t>::max()) return;
        std::vector<std::max_align_t> prescan((XM_PRESCAN_DATA_SIZE + sizeof(std::max_align_t) - 1) / sizeof(std::max_align_t));
        auto* scan = reinterpret_cast<xm_prescan_data_t*>(prescan.data());
        if (!xm_prescan_module(reinterpret_cast<const char*>(data), static_cast<uint32_t>(size), scan)) return;
        pool.resize((xm_size_for_context(scan) + sizeof(std::max_align_t) - 1) / sizeof(std::max_align_t));
        value = xm_create_context(reinterpret_cast<char*>(pool.data()), scan, reinterpret_cast<const char*>(data), static_cast<uint32_t>(size));
        xm_set_sample_rate(value, static_cast<uint16_t>(rate));
        info = {rate, 2, unknown_frames};
    }
    int64_t read(float* output, uint32_t frames) noexcept override {
        if (!value) return -1;
        const auto generated = frames;
        while (frames) { const auto block = static_cast<uint16_t>(std::min<uint32_t>(frames, std::numeric_limits<uint16_t>::max())); xm_generate_samples(value, output, block); output += static_cast<size_t>(block) * 2; frames -= block; }
        return generated;
    }
    bool seek(uint64_t) noexcept override { return false; }
    [[nodiscard]] explicit operator bool() const noexcept { return value != nullptr; }
};

struct FlacMemory { const uint8_t* input{}; size_t size{}; size_t position{}; float* output{}; uint32_t capacity{}; uint32_t written{}; Info info{}; bool failed{}; };
FLAC__StreamDecoderReadStatus flacRead(const FLAC__StreamDecoder*, FLAC__byte buffer[], size_t* bytes, void* opaque) noexcept {
    auto& state = *static_cast<FlacMemory*>(opaque); const auto available = state.size - state.position; const auto count = std::min(*bytes, available);
    if (!count) { *bytes = 0; return FLAC__STREAM_DECODER_READ_STATUS_END_OF_STREAM; }
    std::memcpy(buffer, state.input + state.position, count); state.position += count; *bytes = count; return FLAC__STREAM_DECODER_READ_STATUS_CONTINUE;
}
FLAC__StreamDecoderWriteStatus flacWrite(const FLAC__StreamDecoder*, const FLAC__Frame* frame, const FLAC__int32* const source[], void* opaque) noexcept {
    auto& state = *static_cast<FlacMemory*>(opaque); const auto channels = frame->header.channels; const auto frames = frame->header.blocksize;
    if (!channels || channels > 8 || frames > state.capacity - state.written) { state.failed = true; return FLAC__STREAM_DECODER_WRITE_STATUS_ABORT; }
    const auto scale = 1.0f / static_cast<float>(uint64_t{1} << (frame->header.bits_per_sample - 1));
    for (uint32_t frame_index{}; frame_index < frames; ++frame_index) for (uint32_t channel{}; channel < channels; ++channel) state.output[(static_cast<size_t>(state.written + frame_index) * channels) + channel] = static_cast<float>(source[channel][frame_index]) * scale;
    state.written += frames; return FLAC__STREAM_DECODER_WRITE_STATUS_CONTINUE;
}
void flacMetadata(const FLAC__StreamDecoder*, const FLAC__StreamMetadata* metadata, void* opaque) noexcept { if (metadata->type == FLAC__METADATA_TYPE_STREAMINFO) static_cast<FlacMemory*>(opaque)->info = {metadata->data.stream_info.sample_rate, metadata->data.stream_info.channels, metadata->data.stream_info.total_samples}; }
void flacError(const FLAC__StreamDecoder*, FLAC__StreamDecoderErrorStatus, void* opaque) noexcept { static_cast<FlacMemory*>(opaque)->failed = true; }
struct FlacOutput { std::vector<uint8_t> data; bool failed{}; };
FLAC__StreamEncoderWriteStatus flacOutput(const FLAC__StreamEncoder*, const FLAC__byte buffer[], size_t bytes, uint32_t, uint32_t, void* opaque) noexcept {
    try { auto& output = *static_cast<FlacOutput*>(opaque); output.data.insert(output.data.end(), buffer, buffer + bytes); return FLAC__STREAM_ENCODER_WRITE_STATUS_OK; } catch (...) { static_cast<FlacOutput*>(opaque)->failed = true; return FLAC__STREAM_ENCODER_WRITE_STATUS_FATAL_ERROR; }
}

template <class T, class... Args>
Decoder* open(Args&&... args) noexcept {
    try {
        auto value = std::make_unique<T>(static_cast<Args&&>(args)...);
        return *value ? value.release() : nullptr;
    } catch (...) { return nullptr; }
}

}

KA_API uint32_t ka_audio_abi_version() noexcept { return abi; }
KA_API const char* ka_audio_version() noexcept { return "1"; }
KA_API int32_t ka_audio_open_memory(uint32_t codec, const uint8_t* data, size_t size, uint32_t sample_rate, void** handle, uint32_t* out_rate, uint32_t* out_channels, uint64_t* out_frames) noexcept {
    if (!data || !size || !handle || !out_rate || !out_channels || !out_frames) return -1;
    Decoder* decoder{};
    switch (static_cast<Codec>(codec)) {
        case Codec::wav: decoder = open<Wav>(data, size); break;
        case Codec::mp3: decoder = open<Mp3>(data, size); break;
        case Codec::vorbis: decoder = open<Vorbis>(data, size); break;
        case Codec::xm: decoder = open<Xm>(data, size, sample_rate ? sample_rate : 48000); break;
        default: return -2;
    }
    if (!decoder) return -3;
    *handle = decoder; *out_rate = decoder->info.sample_rate; *out_channels = decoder->info.channels; *out_frames = decoder->info.frames;
    return 0;
}
KA_API int64_t ka_audio_read_f32(void* handle, float* output, uint32_t frames) noexcept { return handle && output ? static_cast<Decoder*>(handle)->read(output, frames) : -1; }
KA_API int32_t ka_audio_seek_frame(void* handle, uint64_t frame) noexcept { return handle && static_cast<Decoder*>(handle)->seek(frame) ? 0 : -1; }
KA_API void ka_audio_close(void* handle) noexcept { delete static_cast<Decoder*>(handle); }
KA_API void ka_audio_free(void* memory) noexcept { drwav_free(memory, nullptr); }
KA_API int32_t ka_wav_encode_f32(const float* pcm, uint64_t frames, uint32_t rate, uint32_t channels, uint8_t** output, size_t* output_size) noexcept {
    if (!pcm || !frames || !rate || !channels || !output || !output_size || channels > std::numeric_limits<uint16_t>::max()) return -1;
    drwav wav{}; void* data{}; size_t size{}; const drwav_data_format format{drwav_container_riff, DR_WAVE_FORMAT_IEEE_FLOAT, channels, rate, 32};
    if (!drwav_init_memory_write_sequential_pcm_frames(&wav, &data, &size, &format, frames, nullptr)) return -2;
    const auto written = drwav_write_pcm_frames(&wav, frames, pcm); const auto result = drwav_uninit(&wav);
    if (written != frames || result != DRWAV_SUCCESS) { drwav_free(data, nullptr); return -3; }
    *output = static_cast<uint8_t*>(data); *output_size = size; return 0;
}
KA_API int32_t ka_flac_decode_f32(const uint8_t* input, size_t input_size, float* output, uint32_t frame_capacity, uint32_t* out_rate, uint32_t* out_channels, uint64_t* out_frames, uint32_t* written_frames) noexcept {
    if (!input || !input_size || !output || !frame_capacity || !out_rate || !out_channels || !out_frames || !written_frames) return -1;
    auto* decoder = FLAC__stream_decoder_new(); if (!decoder) return -2;
    FlacMemory state{input, input_size, 0, output, frame_capacity};
    const auto initialized = FLAC__stream_decoder_init_stream(decoder, flacRead, nullptr, nullptr, nullptr, nullptr, flacWrite, flacMetadata, flacError, &state) == FLAC__STREAM_DECODER_INIT_STATUS_OK;
    const auto decoded = initialized && FLAC__stream_decoder_process_until_end_of_stream(decoder) && !state.failed;
    FLAC__stream_decoder_finish(decoder); FLAC__stream_decoder_delete(decoder);
    if (!decoded || !state.info.sample_rate || !state.info.channels) return -3;
    *out_rate = state.info.sample_rate; *out_channels = state.info.channels; *out_frames = state.info.frames; *written_frames = state.written; return 0;
}
KA_API int32_t ka_flac_encode_f32(const float* pcm, uint64_t frames, uint32_t rate, uint32_t channels, uint32_t compression, uint8_t** output, size_t* output_size) noexcept {
    if (!pcm || !frames || !rate || !channels || channels > 8 || !output || !output_size || frames > std::numeric_limits<size_t>::max() / channels) return -1;
    try {
        auto* encoder = FLAC__stream_encoder_new(); if (!encoder) return -2;
        FlacOutput encoded{}; std::vector<FLAC__int32> samples(static_cast<size_t>(frames) * channels);
        constexpr auto scale = 8388607.0f;
        for (size_t index{}; index < samples.size(); ++index) samples[index] = static_cast<FLAC__int32>(std::lround(std::clamp(pcm[index], -1.0f, 1.0f) * scale));
        const auto configured = FLAC__stream_encoder_set_channels(encoder, channels) && FLAC__stream_encoder_set_bits_per_sample(encoder, 24) && FLAC__stream_encoder_set_sample_rate(encoder, rate) && FLAC__stream_encoder_set_total_samples_estimate(encoder, frames) && FLAC__stream_encoder_set_compression_level(encoder, std::min(compression, 8u));
        const auto initialized = configured && FLAC__stream_encoder_init_stream(encoder, flacOutput, nullptr, nullptr, nullptr, &encoded) == FLAC__STREAM_ENCODER_INIT_STATUS_OK;
        bool processed = initialized;
        for (uint64_t offset{}; processed && offset < frames;) { const auto count = static_cast<uint32_t>(std::min<uint64_t>(frames - offset, std::numeric_limits<uint32_t>::max())); processed = FLAC__stream_encoder_process_interleaved(encoder, samples.data() + static_cast<size_t>(offset) * channels, count); offset += count; }
        processed = processed && FLAC__stream_encoder_finish(encoder) && !encoded.failed;
        FLAC__stream_encoder_delete(encoder);
        if (!processed || encoded.data.empty()) return -3;
        auto* data = static_cast<uint8_t*>(std::malloc(encoded.data.size())); if (!data) return -4;
        std::memcpy(data, encoded.data.data(), encoded.data.size()); *output = data; *output_size = encoded.data.size(); return 0;
    } catch (...) { return -5; }
}
KA_API int32_t ka_wma_decode_f32(const uint8_t* input, size_t input_size, float* output, uint32_t frame_capacity, uint32_t* out_rate, uint32_t* out_channels, uint32_t* written_frames) noexcept {
    try { return WMADecoder::decode_wma_memory(input, input_size, output, frame_capacity, out_rate, out_channels, written_frames); } catch (...) { return -99; }
}
KA_API int32_t ka_aac_decode_f32(const uint8_t* input, size_t input_size, float* output, uint32_t frame_capacity, uint32_t* out_rate, uint32_t* out_channels, uint32_t* written_frames) noexcept {
    if (!input || !input_size || input_size > std::numeric_limits<unsigned long>::max() || !output || !frame_capacity || !out_rate || !out_channels || !written_frames) return -1;
    auto decoder = NeAACDecOpen(); if (!decoder) return -2;
    auto* config = NeAACDecGetCurrentConfiguration(decoder); config->outputFormat = FAAD_FMT_FLOAT; if (!NeAACDecSetConfiguration(decoder, config)) { NeAACDecClose(decoder); return -3; }
    unsigned long rate{}; unsigned char channels{}; const auto skip = NeAACDecInit(decoder, const_cast<unsigned char*>(input), static_cast<unsigned long>(input_size), &rate, &channels);
    if (skip < 0 || static_cast<size_t>(skip) >= input_size || !channels || channels > 8) { NeAACDecClose(decoder); return -4; }
    size_t position = static_cast<size_t>(skip); uint32_t written{};
    while (position < input_size) {
        NeAACDecFrameInfo frame{}; auto* pcm = static_cast<float*>(NeAACDecDecode(decoder, &frame, const_cast<unsigned char*>(input + position), static_cast<unsigned long>(input_size - position)));
        if (frame.error || !frame.bytesconsumed || frame.bytesconsumed > input_size - position) { NeAACDecClose(decoder); return -5; }
        position += frame.bytesconsumed;
        if (!frame.samples) continue;
        const auto frame_channels = static_cast<uint32_t>(frame.channels); const auto frames = static_cast<uint32_t>(frame.samples / frame_channels);
        if (!pcm || !frame_channels || frame_channels > 8 || frames > frame_capacity - written) { NeAACDecClose(decoder); return -6; }
        std::memcpy(output + static_cast<size_t>(written) * frame_channels, pcm, static_cast<size_t>(frame.samples) * sizeof(float)); written += frames; rate = frame.samplerate; channels = frame.channels;
    }
    NeAACDecClose(decoder); *out_rate = rate; *out_channels = channels; *written_frames = written; return 0;
}
KA_API int32_t ka_alac_decode_s16(const uint8_t* cookie, size_t cookie_size, const uint8_t* input, size_t input_size, int16_t* output, uint32_t frame_capacity, uint32_t* out_rate, uint32_t* out_channels, uint32_t* written_frames) noexcept {
    if (!cookie || cookie_size > UINT32_MAX || !input || input_size > UINT32_MAX || !output || !frame_capacity || !out_rate || !out_channels || !written_frames) return -1;
    try { ALACDecoder decoder; if (decoder.Init(const_cast<uint8_t*>(cookie), static_cast<uint32_t>(cookie_size))) return -2; BitBuffer bits{}; BitBufferInit(&bits, const_cast<uint8_t*>(input), static_cast<uint32_t>(input_size)); uint32_t frames{}; const auto result = decoder.Decode(&bits, reinterpret_cast<uint8_t*>(output), frame_capacity, decoder.mConfig.numChannels, &frames); if (result || frames > frame_capacity) return -3; *out_rate = decoder.mConfig.sampleRate; *out_channels = decoder.mConfig.numChannels; *written_frames = frames; return 0; } catch (...) { return -4; }
}
KA_API int32_t ka_alac_encode_s16(const int16_t* pcm, uint32_t frames, uint32_t rate, uint32_t channels, uint32_t frame_length, uint8_t* output, size_t output_capacity, size_t* output_size, uint8_t* cookie, size_t cookie_capacity, size_t* cookie_size) noexcept {
    if (!pcm || !frames || !rate || !channels || channels > 8 || !output || output_capacity > INT32_MAX || !output_size || !cookie || !cookie_size) return -1;
    const auto required = static_cast<size_t>(frames) * channels * 6 + 1; if (output_capacity < required) return -2;
    try {
        const auto pcm_flags = static_cast<uint32_t>(kALACFormatFlagsNativeEndian) | static_cast<uint32_t>(kALACFormatFlagIsSignedInteger) | static_cast<uint32_t>(kALACFormatFlagIsPacked);
        AudioFormatDescription pcm_format{static_cast<double>(rate), kALACFormatLinearPCM, pcm_flags, channels * 2, 1, channels * 2, channels, 16, 0};
        AudioFormatDescription alac_format{static_cast<double>(rate), kALACFormatAppleLossless, 1, 0, frame_length ? frame_length : 4096, 0, channels, 0, 0};
        ALACEncoder encoder; encoder.SetFrameSize(alac_format.mFramesPerPacket); if (encoder.InitializeEncoder(alac_format)) return -3;
        int32_t size = static_cast<int32_t>(static_cast<size_t>(frames) * channels * 2); if (encoder.Encode(pcm_format, alac_format, reinterpret_cast<unsigned char*>(const_cast<int16_t*>(pcm)), output, &size)) return -4;
        uint32_t size_cookie = static_cast<uint32_t>(cookie_capacity); encoder.GetMagicCookie(cookie, &size_cookie); if (!size_cookie) return -5; *output_size = static_cast<size_t>(size); *cookie_size = size_cookie; return 0;
    } catch (...) { return -6; }
}

KA_API int32_t ka_opus_decoder_create(uint32_t rate, uint32_t channels, void** handle) noexcept {
    if (!handle || (channels != 1 && channels != 2)) return -1;
    int error{}; auto* value = opus_decoder_create(static_cast<opus_int32>(rate), static_cast<int>(channels), &error); if (!value) return error; *handle = value; return 0;
}
KA_API void ka_opus_decoder_destroy(void* handle) noexcept { opus_decoder_destroy(static_cast<OpusDecoder*>(handle)); }
KA_API int32_t ka_opus_decode_f32(void* handle, const uint8_t* packet, int32_t packet_size, float* pcm, int32_t max_frames, int32_t fec) noexcept { return handle && pcm ? opus_decode_float(static_cast<OpusDecoder*>(handle), packet, packet_size, pcm, max_frames, fec) : -1; }
KA_API int32_t ka_opus_encoder_create(uint32_t rate, uint32_t channels, int32_t application, void** handle) noexcept {
    if (!handle || (channels != 1 && channels != 2)) return -1;
    int error{}; auto* value = opus_encoder_create(static_cast<opus_int32>(rate), static_cast<int>(channels), application, &error); if (!value) return error; *handle = value; return 0;
}
KA_API void ka_opus_encoder_destroy(void* handle) noexcept { opus_encoder_destroy(static_cast<OpusEncoder*>(handle)); }
KA_API int32_t ka_opus_encode_f32(void* handle, const float* pcm, int32_t frames, uint8_t* packet, int32_t packet_capacity) noexcept { return handle && pcm && packet ? opus_encode_float(static_cast<OpusEncoder*>(handle), pcm, frames, packet, packet_capacity) : -1; }
