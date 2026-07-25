#include <brotli/decode.h>
#include <brotli/encode.h>
#include <brotli/types.h>
#include <cstddef>
#include <cstdint>

#if defined(_WIN32)
#define KBROTLI_API extern "C" __declspec(dllexport)
#else
#define KBROTLI_API extern "C" __attribute__((visibility("default")))
#endif

KBROTLI_API uint32_t kbrotli_encoder_version() noexcept {
    return BrotliEncoderVersion();
}

KBROTLI_API uint32_t kbrotli_decoder_version() noexcept {
    return BrotliDecoderVersion();
}

KBROTLI_API size_t kbrotli_encoder_max_compressed_size(size_t input_size) noexcept {
    return BrotliEncoderMaxCompressedSize(input_size);
}

KBROTLI_API BrotliEncoderState* kbrotli_encoder_create_instance() noexcept {
    return BrotliEncoderCreateInstance(nullptr, nullptr, nullptr);
}

KBROTLI_API void kbrotli_encoder_destroy_instance(BrotliEncoderState* state) noexcept {
    BrotliEncoderDestroyInstance(state);
}

KBROTLI_API BrotliDecoderState* kbrotli_decoder_create_instance() noexcept {
    return BrotliDecoderCreateInstance(nullptr, nullptr, nullptr);
}

KBROTLI_API void kbrotli_decoder_destroy_instance(BrotliDecoderState* state) noexcept {
    BrotliDecoderDestroyInstance(state);
}

KBROTLI_API int kbrotli_encoder_set_parameter(BrotliEncoderState* state, int parameter, uint32_t value) noexcept {
    return state && BrotliEncoderSetParameter(state, static_cast<BrotliEncoderParameter>(parameter), value);
}

KBROTLI_API int kbrotli_decoder_set_parameter(BrotliDecoderState* state, int parameter, uint32_t value) noexcept {
    return state && BrotliDecoderSetParameter(state, static_cast<BrotliDecoderParameter>(parameter), value);
}

KBROTLI_API int kbrotli_encoder_is_finished(BrotliEncoderState* state) noexcept {
    return state && BrotliEncoderIsFinished(state);
}

KBROTLI_API int kbrotli_encoder_has_more_output(BrotliEncoderState* state) noexcept {
    return state && BrotliEncoderHasMoreOutput(state);
}

KBROTLI_API int kbrotli_decoder_has_more_output(BrotliDecoderState* state) noexcept {
    return state && BrotliDecoderHasMoreOutput(state);
}

KBROTLI_API int kbrotli_decoder_is_used(BrotliDecoderState* state) noexcept {
    return state && BrotliDecoderIsUsed(state);
}

KBROTLI_API int kbrotli_decoder_is_finished(BrotliDecoderState* state) noexcept {
    return state && BrotliDecoderIsFinished(state);
}

KBROTLI_API BrotliDecoderErrorCode kbrotli_decoder_get_error_code(BrotliDecoderState* state) noexcept {
    return state ? BrotliDecoderGetErrorCode(state) : BROTLI_DECODER_ERROR_INVALID_ARGUMENTS;
}

KBROTLI_API const char* kbrotli_decoder_error_string(BrotliDecoderErrorCode code) noexcept {
    return BrotliDecoderErrorString(code);
}

KBROTLI_API size_t kbrotli_compress(
    int quality,
    int lgwin,
    int mode,
    size_t input_size,
    const uint8_t* input_buffer,
    size_t* encoded_size,
    uint8_t* encoded_buffer) noexcept {

    return BrotliEncoderCompress(
        quality,
        lgwin,
        static_cast<BrotliEncoderMode>(mode),
        input_size,
        input_buffer,
        encoded_size,
        encoded_buffer);
}

KBROTLI_API size_t kbrotli_compress_stream(
    BrotliEncoderState* state,
    int op,
    size_t* available_in,
    const uint8_t** next_in,
    size_t* available_out,
    uint8_t** next_out,
    size_t* total_out) noexcept {

    if (!state || !available_in || !next_in || !available_out || !next_out) return BROTLI_FALSE;
    return BrotliEncoderCompressStream(
        state,
        static_cast<BrotliEncoderOperation>(op),
        available_in,
        next_in,
        available_out,
        next_out,
        total_out);
}

KBROTLI_API const uint8_t* kbrotli_encoder_take_output(BrotliEncoderState* state, size_t* size) noexcept {
    return state ? BrotliEncoderTakeOutput(state, size) : nullptr;
}

KBROTLI_API size_t kbrotli_decompress(
    size_t encoded_size,
    const uint8_t* encoded_buffer,
    size_t* decoded_size,
    uint8_t* decoded_buffer) noexcept {

    return BrotliDecoderDecompress(encoded_size, encoded_buffer, decoded_size, decoded_buffer);
}

KBROTLI_API size_t kbrotli_decompress_stream(
    BrotliDecoderState* state,
    size_t* available_in,
    const uint8_t** next_in,
    size_t* available_out,
    uint8_t** next_out,
    size_t* total_out) noexcept {

    if (!state || !available_in || !next_in || !available_out || !next_out) return BROTLI_DECODER_RESULT_ERROR;
    return BrotliDecoderDecompressStream(state, available_in, next_in, available_out, next_out, total_out);
}

KBROTLI_API const uint8_t* kbrotli_decoder_take_output(BrotliDecoderState* state, size_t* size) noexcept {
    return state ? BrotliDecoderTakeOutput(state, size) : nullptr;
}
