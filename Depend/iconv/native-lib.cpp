#include <cerrno>
#include <cstddef>
#include <iconv.h>

#if defined(__GNUC__)
#define KICONV_EXPORT __attribute__((visibility("default")))
#else
#define KICONV_EXPORT
#endif

extern "C" KICONV_EXPORT int kiconv_convert(
    const char* to_encoding,
    const char* from_encoding,
    const void* input,
    size_t input_size,
    void* output,
    size_t output_capacity,
    size_t* input_consumed,
    size_t* output_written
) noexcept {
    if (!to_encoding || !from_encoding || (!input && input_size) || (!output && output_capacity) || !input_consumed || !output_written) return EINVAL;

    *input_consumed = 0;
    *output_written = 0;

    const iconv_t descriptor = iconv_open(to_encoding, from_encoding);
    if (descriptor == reinterpret_cast<iconv_t>(-1)) return errno;

    auto* input_cursor = const_cast<char*>(static_cast<const char*>(input));
    auto input_left = input_size;
    auto* output_cursor = static_cast<char*>(output);
    auto output_left = output_capacity;
    int result = 0;

    if (iconv(descriptor, &input_cursor, &input_left, &output_cursor, &output_left) == static_cast<size_t>(-1)) result = errno;
    if (!result && iconv(descriptor, nullptr, nullptr, &output_cursor, &output_left) == static_cast<size_t>(-1)) result = errno;

    *input_consumed = input_size - input_left;
    *output_written = output_capacity - output_left;
    iconv_close(descriptor);
    return result;
}
