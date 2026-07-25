#include "Wma_Decoder.h"
#include "Wma_avcodec.h"
#include "Wma_avformat.h"
#include "Wma_avio.h"

#include <algorithm>
#include <cstdint>
#include <cstring>
#include <limits>

namespace WMADECODER_NAMESPACE {
namespace {

struct MemoryInput { const uint8_t* data{}; size_t size{}; size_t position{}; };

int readMemory(void* opaque, uint8_t* destination, int capacity) {
    auto& input = *static_cast<MemoryInput*>(opaque);
    if (!destination || capacity <= 0 || input.position >= input.size) return 0;
    const auto count = std::min<size_t>(static_cast<size_t>(capacity), input.size - input.position);
    std::memcpy(destination, input.data + input.position, count);
    input.position += count;
    return static_cast<int>(count);
}

int seekMemory(void* opaque, offset_t offset, int whence) {
    auto& input = *static_cast<MemoryInput*>(opaque);
    const auto base = whence == SEEK_SET ? int64_t{} : whence == SEEK_CUR ? static_cast<int64_t>(input.position) : whence == SEEK_END ? static_cast<int64_t>(input.size) : -1;
    if (base < 0 || offset > std::numeric_limits<int64_t>::max() - base || offset < -base) return -1;
    const auto position = base + offset;
    if (position < 0 || static_cast<uint64_t>(position) > input.size) return -1;
    input.position = static_cast<size_t>(position);
    return 0;
}

}

int url_close(URLContext*) { return 0; }
offset_t url_filesize(URLContext*) { return -1; }

int decode_wma_memory(const uint8_t* input, size_t input_size, float* output, uint32_t frame_capacity, uint32_t* sample_rate, uint32_t* channels, uint32_t* written_frames) {
    if (!input || !input_size || !output || !frame_capacity || !sample_rate || !channels || !written_frames) return -1;
    static bool initialized = [] { av_register_all(); return true; }();
    static_cast<void>(initialized);
    auto* format = av_find_input_format("asf");
    if (!format) return -2;

    MemoryInput memory{input, input_size};
    ByteIOContext io{};
    auto* buffer = static_cast<uint8_t*>(av_malloc(32768));
    if (!buffer) return -3;
    init_put_byte(&io, buffer, 32768, 0, &memory, readMemory, nullptr, seekMemory);
    io.is_streamed = -1;

    AVFormatContext* container{};
    if (av_open_input_stream(&container, &io, "memory.asf", format, nullptr) < 0) { av_free(buffer); return -4; }
    AVCodecContext* context{};
    int stream_index = -1;
    for (int index{}; index < container->nb_streams; ++index) if (container->streams[index]->codec.codec_type == CODEC_TYPE_AUDIO) { context = &container->streams[index]->codec; stream_index = index; break; }
    if (!context || (context->codec_id != CODEC_ID_WMAV1 && context->codec_id != CODEC_ID_WMAV2)) { av_close_input_file(container); return -5; }
    auto* codec = avcodec_find_decoder(context->codec_id);
    if (!codec || avcodec_open(context, codec) < 0) { av_close_input_file(container); return -6; }

    const auto stream_channels = static_cast<uint32_t>(context->channels);
    if (!stream_channels || stream_channels > 2) { avcodec_close(context); av_close_input_file(container); return -7; }
    uint32_t written{};
    alignas(16) int16_t pcm[AVCODEC_MAX_AUDIO_FRAME_SIZE / sizeof(int16_t)];
    AVPacket packet{};
    while (av_read_frame(container, &packet) >= 0) {
        if (packet.stream_index != stream_index) { av_free_packet(&packet); continue; }
        auto* source = packet.data;
        int remaining = packet.size;
        while (remaining > 0) {
            int byte_count{};
            const auto consumed = avcodec_decode_audio(context, pcm, &byte_count, source, remaining);
            if (consumed <= 0) break;
            source += consumed; remaining -= consumed;
            if (!byte_count) continue;
            const auto frames = static_cast<uint32_t>(byte_count / (sizeof(int16_t) * stream_channels));
            if (frames > frame_capacity - written) { av_free_packet(&packet); avcodec_close(context); av_close_input_file(container); return -8; }
            for (uint32_t frame{}; frame < frames; ++frame) for (uint32_t channel{}; channel < stream_channels; ++channel) output[(static_cast<size_t>(written + frame) * stream_channels) + channel] = static_cast<float>(pcm[frame * stream_channels + channel]) * (1.0f / 32768.0f);
            written += frames;
        }
        av_free_packet(&packet);
    }
    *sample_rate = static_cast<uint32_t>(context->sample_rate); *channels = stream_channels; *written_frames = written;
    avcodec_close(context); av_close_input_file(container); return 0;
}

}
