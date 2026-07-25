#include <jni.h>
#include <cstddef>
#include <cstdint>
#include <cstring>
#include <string>
#define ZSTD_STATIC_LINKING_ONLY
#include "zstd.h"
#include "zdict.h"

#if defined(_WIN32)
#define KZSTD_API extern "C" __declspec(dllexport)
#else
#define KZSTD_API extern "C" __attribute__((visibility("default")))
#endif

namespace {

struct KZSTD_CDict {
    ZSTD_CDict* dict{};
};

struct KZSTD_DDict {
    ZSTD_DDict* dict{};
};

struct KZSTD_CStream {
    ZSTD_CCtx* ctx{};
};

struct KZSTD_DStream {
    ZSTD_DCtx* ctx{};
};

template <class T>
T* makeHandle(auto* ptr) noexcept {
    if (!ptr) return nullptr;
    auto* handle = new T{};
    handle->ctx = ptr;
    return handle;
}

}

KZSTD_API uint32_t kzstd_abi_version() noexcept {
    return 1;
}

KZSTD_API uint32_t kzstd_version_number() noexcept {
    return ZSTD_versionNumber();
}

KZSTD_API const char* kzstd_version_string() noexcept {
    return ZSTD_versionString();
}

KZSTD_API uint64_t kzstd_content_size_unknown() noexcept {
    return ZSTD_CONTENTSIZE_UNKNOWN;
}

KZSTD_API uint64_t kzstd_content_size_error() noexcept {
    return ZSTD_CONTENTSIZE_ERROR;
}

KZSTD_API uint32_t kzstd_is_error(size_t code) noexcept {
    return ZSTD_isError(code);
}

KZSTD_API const char* kzstd_error_name(size_t code) noexcept {
    return ZSTD_getErrorName(code);
}

KZSTD_API size_t kzstd_compress_bound(size_t src_size) noexcept {
    return ZSTD_compressBound(src_size);
}

KZSTD_API unsigned long long kzstd_frame_content_size(const void* src, size_t src_size) noexcept {
    return ZSTD_getFrameContentSize(src, src_size);
}

KZSTD_API unsigned long long kzstd_find_decompressed_size(const void* src, size_t src_size) noexcept {
    return ZSTD_getFrameContentSize(src, src_size);
}

KZSTD_API uint32_t kzstd_frame_header_size(const void* src, size_t src_size, size_t* out_size) noexcept {
    if (!out_size) return 0;
    ZSTD_FrameHeader header{};
    const size_t n = ZSTD_getFrameHeader(&header, src, src_size);
    if (ZSTD_isError(n)) return 0;
    *out_size = header.headerSize;
    return 1;
}

KZSTD_API uint32_t kzstd_get_dict_id_from_dict(const void* dict, size_t dict_size) noexcept {
    return ZSTD_getDictID_fromDict(dict, dict_size);
}

KZSTD_API uint32_t kzstd_get_dict_id_from_frame(const void* src, size_t src_size) noexcept {
    return ZSTD_getDictID_fromFrame(src, src_size);
}

KZSTD_API size_t kzstd_compress(void* dst, size_t dst_capacity, const void* src, size_t src_size, int level) noexcept {
    return ZSTD_compress(dst, dst_capacity, src, src_size, level);
}

KZSTD_API size_t kzstd_decompress(void* dst, size_t dst_capacity, const void* src, size_t src_size) noexcept {
    return ZSTD_decompress(dst, dst_capacity, src, src_size);
}

KZSTD_API size_t kzstd_compress_using_dict(void* dst, size_t dst_capacity, const void* src, size_t src_size, const void* dict, size_t dict_size, int level) noexcept {
    ZSTD_CCtx* ctx = ZSTD_createCCtx();
    if (!ctx) return ZSTD_error_memory_allocation;
    const size_t ret = ZSTD_compress_usingDict(ctx, dst, dst_capacity, src, src_size, dict, dict_size, level);
    ZSTD_freeCCtx(ctx);
    return ret;
}

KZSTD_API size_t kzstd_decompress_using_dict(void* dst, size_t dst_capacity, const void* src, size_t src_size, const void* dict, size_t dict_size) noexcept {
    ZSTD_DCtx* ctx = ZSTD_createDCtx();
    if (!ctx) return ZSTD_error_memory_allocation;
    const size_t ret = ZSTD_decompress_usingDict(ctx, dst, dst_capacity, src, src_size, dict, dict_size);
    ZSTD_freeDCtx(ctx);
    return ret;
}

KZSTD_API KZSTD_CDict* kzstd_create_cdict(const void* dict, size_t dict_size, int level) noexcept {
    auto* raw = ZSTD_createCDict(dict, dict_size, level);
    if (!raw) return nullptr;
    auto* handle = new KZSTD_CDict{};
    handle->dict = raw;
    return handle;
}

KZSTD_API void kzstd_free_cdict(KZSTD_CDict* handle) noexcept {
    if (!handle) return;
    ZSTD_freeCDict(handle->dict);
    delete handle;
}

KZSTD_API KZSTD_DDict* kzstd_create_ddict(const void* dict, size_t dict_size) noexcept {
    auto* raw = ZSTD_createDDict(dict, dict_size);
    if (!raw) return nullptr;
    auto* handle = new KZSTD_DDict{};
    handle->dict = raw;
    return handle;
}

KZSTD_API void kzstd_free_ddict(KZSTD_DDict* handle) noexcept {
    if (!handle) return;
    ZSTD_freeDDict(handle->dict);
    delete handle;
}

KZSTD_API size_t kzstd_compress_cdict(KZSTD_CDict* dict, void* dst, size_t dst_capacity, const void* src, size_t src_size) noexcept {
    if (!dict || !dict->dict) return ZSTD_error_dictionary_wrong;
    ZSTD_CCtx* ctx = ZSTD_createCCtx();
    if (!ctx) return ZSTD_error_memory_allocation;
    const size_t ret = ZSTD_compress_usingCDict(ctx, dst, dst_capacity, src, src_size, dict->dict);
    ZSTD_freeCCtx(ctx);
    return ret;
}

KZSTD_API size_t kzstd_decompress_ddict(KZSTD_DDict* dict, void* dst, size_t dst_capacity, const void* src, size_t src_size) noexcept {
    if (!dict || !dict->dict) return ZSTD_error_dictionary_wrong;
    ZSTD_DCtx* ctx = ZSTD_createDCtx();
    if (!ctx) return ZSTD_error_memory_allocation;
    const size_t ret = ZSTD_decompress_usingDDict(ctx, dst, dst_capacity, src, src_size, dict->dict);
    ZSTD_freeDCtx(ctx);
    return ret;
}

KZSTD_API size_t kzstd_train_dictionary(void* dict, size_t dict_capacity, const void* samples_buffer, const size_t* sample_sizes, unsigned nb_samples) noexcept {
    return ZDICT_trainFromBuffer(dict, dict_capacity, samples_buffer, sample_sizes, nb_samples);
}

KZSTD_API size_t kzstd_get_train_samples_bound(const size_t* sample_sizes, unsigned nb_samples) noexcept {
    size_t total = 0;
    if (!sample_sizes) return 0;
    for (unsigned i = 0; i < nb_samples; ++i) total += sample_sizes[i];
    return total;
}

KZSTD_API size_t kzstd_compress_advanced(
    void* dst,
    size_t dst_capacity,
    const void* src,
    size_t src_size,
    int level,
    int checksum,
    int content_size_flag,
    int nb_workers,
    int window_log,
    int chain_log,
    int hash_log,
    int search_log,
    int min_match,
    int target_length,
    int strategy) noexcept {

    ZSTD_CCtx* ctx = ZSTD_createCCtx();
    if (!ctx) return ZSTD_error_memory_allocation;

    auto set = [&](ZSTD_cParameter p, int v) -> size_t {
        return v ? ZSTD_CCtx_setParameter(ctx, p, v) : 0;
    };

    size_t ret = 0;
    if (!ZSTD_isError(ret)) ret = ZSTD_CCtx_setParameter(ctx, ZSTD_c_compressionLevel, level);
    if (!ZSTD_isError(ret)) ret = ZSTD_CCtx_setParameter(ctx, ZSTD_c_checksumFlag, checksum ? 1 : 0);
    if (!ZSTD_isError(ret)) ret = ZSTD_CCtx_setParameter(ctx, ZSTD_c_contentSizeFlag, content_size_flag ? 1 : 0);
    if (!ZSTD_isError(ret)) ret = set(ZSTD_c_nbWorkers, nb_workers);
    if (!ZSTD_isError(ret)) ret = set(ZSTD_c_windowLog, window_log);
    if (!ZSTD_isError(ret)) ret = set(ZSTD_c_chainLog, chain_log);
    if (!ZSTD_isError(ret)) ret = set(ZSTD_c_hashLog, hash_log);
    if (!ZSTD_isError(ret)) ret = set(ZSTD_c_searchLog, search_log);
    if (!ZSTD_isError(ret)) ret = set(ZSTD_c_minMatch, min_match);
    if (!ZSTD_isError(ret)) ret = set(ZSTD_c_targetLength, target_length);
    if (!ZSTD_isError(ret) && strategy) ret = ZSTD_CCtx_setParameter(ctx, ZSTD_c_strategy, strategy);
    if (!ZSTD_isError(ret)) ret = ZSTD_compress2(ctx, dst, dst_capacity, src, src_size);

    ZSTD_freeCCtx(ctx);
    return ret;
}

KZSTD_API KZSTD_CStream* kzstd_create_cstream() noexcept {
    auto* raw = ZSTD_createCCtx();
    if (!raw) return nullptr;
    auto* handle = new KZSTD_CStream{};
    handle->ctx = raw;
    return handle;
}

KZSTD_API void kzstd_free_cstream(KZSTD_CStream* stream) noexcept {
    if (!stream) return;
    ZSTD_freeCCtx(stream->ctx);
    delete stream;
}

KZSTD_API size_t kzstd_cstream_init(KZSTD_CStream* stream, int level) noexcept {
    if (!stream || !stream->ctx) return ZSTD_error_GENERIC;
    size_t ret = ZSTD_CCtx_reset(stream->ctx, ZSTD_reset_session_only);
    if (!ZSTD_isError(ret)) ret = ZSTD_CCtx_setParameter(stream->ctx, ZSTD_c_compressionLevel, level);
    return ret;
}

KZSTD_API size_t kzstd_cstream_set_parameter(KZSTD_CStream* stream, int parameter, int value) noexcept {
    if (!stream || !stream->ctx) return ZSTD_error_GENERIC;
    return ZSTD_CCtx_setParameter(stream->ctx, static_cast<ZSTD_cParameter>(parameter), value);
}

KZSTD_API size_t kzstd_cstream_load_cdict(KZSTD_CStream* stream, KZSTD_CDict* dict) noexcept {
    if (!stream || !stream->ctx || !dict || !dict->dict) return ZSTD_error_dictionary_wrong;
    return ZSTD_CCtx_refCDict(stream->ctx, dict->dict);
}

KZSTD_API size_t kzstd_cstream_compress(
    KZSTD_CStream* stream,
    void* dst,
    size_t dst_capacity,
    size_t* dst_pos,
    const void* src,
    size_t src_size,
    size_t* src_pos) noexcept {

    if (!stream || !stream->ctx || !dst_pos || !src_pos) return ZSTD_error_GENERIC;
    ZSTD_outBuffer out{dst, dst_capacity, *dst_pos};
    ZSTD_inBuffer in{src, src_size, *src_pos};
    const size_t ret = ZSTD_compressStream2(stream->ctx, &out, &in, ZSTD_e_continue);
    *dst_pos = out.pos;
    *src_pos = in.pos;
    return ret;
}

KZSTD_API size_t kzstd_cstream_flush(KZSTD_CStream* stream, void* dst, size_t dst_capacity, size_t* dst_pos) noexcept {
    if (!stream || !stream->ctx || !dst_pos) return ZSTD_error_GENERIC;
    ZSTD_outBuffer out{dst, dst_capacity, *dst_pos};
    ZSTD_inBuffer in{nullptr, 0, 0};
    const size_t ret = ZSTD_compressStream2(stream->ctx, &out, &in, ZSTD_e_flush);
    *dst_pos = out.pos;
    return ret;
}

KZSTD_API size_t kzstd_cstream_end(KZSTD_CStream* stream, void* dst, size_t dst_capacity, size_t* dst_pos) noexcept {
    if (!stream || !stream->ctx || !dst_pos) return ZSTD_error_GENERIC;
    ZSTD_outBuffer out{dst, dst_capacity, *dst_pos};
    ZSTD_inBuffer in{nullptr, 0, 0};
    const size_t ret = ZSTD_compressStream2(stream->ctx, &out, &in, ZSTD_e_end);
    *dst_pos = out.pos;
    return ret;
}

KZSTD_API KZSTD_DStream* kzstd_create_dstream() noexcept {
    auto* raw = ZSTD_createDCtx();
    if (!raw) return nullptr;
    auto* handle = new KZSTD_DStream{};
    handle->ctx = raw;
    return handle;
}

KZSTD_API void kzstd_free_dstream(KZSTD_DStream* stream) noexcept {
    if (!stream) return;
    ZSTD_freeDCtx(stream->ctx);
    delete stream;
}

KZSTD_API size_t kzstd_dstream_init(KZSTD_DStream* stream) noexcept {
    if (!stream || !stream->ctx) return ZSTD_error_GENERIC;
    return ZSTD_DCtx_reset(stream->ctx, ZSTD_reset_session_only);
}

KZSTD_API size_t kzstd_dstream_load_ddict(KZSTD_DStream* stream, KZSTD_DDict* dict) noexcept {
    if (!stream || !stream->ctx || !dict || !dict->dict) return ZSTD_error_dictionary_wrong;
    return ZSTD_DCtx_refDDict(stream->ctx, dict->dict);
}

KZSTD_API size_t kzstd_dstream_set_parameter(KZSTD_DStream* stream, int parameter, int value) noexcept {
    if (!stream || !stream->ctx) return ZSTD_error_GENERIC;
    return ZSTD_DCtx_setParameter(stream->ctx, static_cast<ZSTD_dParameter>(parameter), value);
}

KZSTD_API size_t kzstd_dstream_decompress(
    KZSTD_DStream* stream,
    void* dst,
    size_t dst_capacity,
    size_t* dst_pos,
    const void* src,
    size_t src_size,
    size_t* src_pos) noexcept {

    if (!stream || !stream->ctx || !dst_pos || !src_pos) return ZSTD_error_GENERIC;
    ZSTD_outBuffer out{dst, dst_capacity, *dst_pos};
    ZSTD_inBuffer in{src, src_size, *src_pos};
    const size_t ret = ZSTD_decompressStream(stream->ctx, &out, &in);
    *dst_pos = out.pos;
    *src_pos = in.pos;
    return ret;
}

extern "C" JNIEXPORT jstring JNICALL
Java_com_example_mynative_mynativezstd_MainActivity_stringFromJNI(JNIEnv* env, jobject) {
    std::string text = "zstd native bridge ";
    text += ZSTD_versionString();
    return env->NewStringUTF(text.c_str());
}
