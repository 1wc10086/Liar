#include <cstddef>
#include <cstdint>
#include <new>
#include <lzma.h>

#if defined(_WIN32)
#define KXZ_API extern "C" __declspec(dllexport)
#else
#define KXZ_API extern "C" __attribute__((visibility("default")))
#endif

namespace {

struct Cursor {
    const void* src;
    size_t src_size;
    size_t src_pos;
    void* dst;
    size_t dst_size;
    size_t dst_pos;
};

struct Stream {
    lzma_stream value = LZMA_STREAM_INIT;
};

[[nodiscard]] const char* error_name(const lzma_ret code) noexcept {
    switch (code) {
    case LZMA_OK: return "ok";
    case LZMA_STREAM_END: return "stream end";
    case LZMA_NO_CHECK: return "no integrity check";
    case LZMA_UNSUPPORTED_CHECK: return "unsupported integrity check";
    case LZMA_GET_CHECK: return "integrity check available";
    case LZMA_MEM_ERROR: return "memory allocation failed";
    case LZMA_MEMLIMIT_ERROR: return "memory limit exceeded";
    case LZMA_FORMAT_ERROR: return "unrecognized file format";
    case LZMA_OPTIONS_ERROR: return "unsupported options";
    case LZMA_DATA_ERROR: return "corrupt data";
    case LZMA_BUF_ERROR: return "insufficient input or output space";
    default: return "programming error";
    }
}

[[nodiscard]] Stream* make_stream() noexcept {
    return new (std::nothrow) Stream{};
}

[[nodiscard]] uint32_t code(Stream* stream, Cursor* cursor, const lzma_action action) noexcept {
    if (!stream || !cursor || cursor->src_pos > cursor->src_size || cursor->dst_pos > cursor->dst_size) return LZMA_PROG_ERROR;
    auto& value = stream->value;
    value.next_in = cursor->src ? static_cast<const uint8_t*>(cursor->src) + cursor->src_pos : nullptr;
    value.avail_in = cursor->src_size - cursor->src_pos;
    value.next_out = cursor->dst ? static_cast<uint8_t*>(cursor->dst) + cursor->dst_pos : nullptr;
    value.avail_out = cursor->dst_size - cursor->dst_pos;
    const auto result = lzma_code(&value, action);
    cursor->src_pos = cursor->src_size - value.avail_in;
    cursor->dst_pos = cursor->dst_size - value.avail_out;
    return result;
}

}

KXZ_API uint32_t kxz_abi_version() noexcept { return 1; }
KXZ_API uint32_t kxz_version_number() noexcept { return lzma_version_number(); }
KXZ_API const char* kxz_version_string() noexcept { return lzma_version_string(); }
KXZ_API const char* kxz_error_name(const uint32_t code) noexcept { return error_name(static_cast<lzma_ret>(code)); }
KXZ_API uint64_t kxz_memusage_decoder(const uint64_t, const uint32_t, const void*, const size_t) noexcept { return 0; }
KXZ_API uint64_t kxz_easy_encoder_memusage(const uint32_t preset) noexcept { return lzma_easy_encoder_memusage(preset); }
KXZ_API uint64_t kxz_easy_decoder_memusage(const uint32_t preset) noexcept { return lzma_easy_decoder_memusage(preset); }
KXZ_API size_t kxz_stream_buffer_bound(const size_t src_size) noexcept { return lzma_stream_buffer_bound(src_size); }

KXZ_API uint32_t kxz_stream_buffer_encode(void* dst, const size_t dst_size, size_t* dst_pos, const void* src, const size_t src_size, const uint32_t preset, const uint32_t check) noexcept {
    return !dst_pos ? LZMA_PROG_ERROR : lzma_easy_buffer_encode(preset, static_cast<lzma_check>(check), nullptr, static_cast<const uint8_t*>(src), src_size, static_cast<uint8_t*>(dst), dst_pos, dst_size);
}

KXZ_API uint32_t kxz_stream_buffer_decode(void* dst, const size_t dst_size, size_t* dst_pos, const void* src, const size_t src_size, size_t* src_pos, const uint64_t memlimit, const uint32_t flags) noexcept {
    if (!dst_pos || !src_pos) return LZMA_PROG_ERROR;
    auto limit = memlimit ? memlimit : UINT64_MAX;
    return lzma_stream_buffer_decode(&limit, flags, nullptr, static_cast<const uint8_t*>(src), src_pos, src_size, static_cast<uint8_t*>(dst), dst_pos, dst_size);
}

KXZ_API void* kxz_encoder_create(const uint32_t preset, const uint32_t check) noexcept {
    auto* stream = make_stream();
    if (!stream) return nullptr;
    if (lzma_easy_encoder(&stream->value, preset, static_cast<lzma_check>(check)) == LZMA_OK) return stream;
    delete stream;
    return nullptr;
}

KXZ_API void* kxz_decoder_create(const uint64_t memlimit, const uint32_t flags) noexcept {
    auto* stream = make_stream();
    if (!stream) return nullptr;
    if (lzma_stream_decoder(&stream->value, memlimit ? memlimit : UINT64_MAX, flags) == LZMA_OK) return stream;
    delete stream;
    return nullptr;
}

KXZ_API void kxz_stream_destroy(void* handle) noexcept {
    auto* stream = static_cast<Stream*>(handle);
    if (!stream) return;
    lzma_end(&stream->value);
    delete stream;
}

KXZ_API uint32_t kxz_stream_code(void* handle, Cursor* cursor, const uint32_t action) noexcept {
    return action > LZMA_FULL_BARRIER ? LZMA_PROG_ERROR : code(static_cast<Stream*>(handle), cursor, static_cast<lzma_action>(action));
}

KXZ_API uint64_t kxz_stream_total_in(void* handle) noexcept { return handle ? static_cast<Stream*>(handle)->value.total_in : 0; }
KXZ_API uint64_t kxz_stream_total_out(void* handle) noexcept { return handle ? static_cast<Stream*>(handle)->value.total_out : 0; }
