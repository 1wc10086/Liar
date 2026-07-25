#if !defined(_WIN32)
/* Liar : change - expose POSIX large-file APIs on non-Windows targets. */
#define _POSIX_C_SOURCE 200809L
#endif

#include "XMemCompression.h"

#include <inttypes.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <sys/types.h>

#define XCOMPRESS_FILE_IDENTIFIER_LZXTDECODE 0x0FF512EDu
#define XMEM_TD_FLAG 0x80000000u
#define XCOMPRESS_HEADER_SIZE 16u
#define XMEM_TD_DEFAULT_WINDOW_SIZE 0x20000u
#define XMEM_TD_DEFAULT_COMPRESSED_BLOCK_SIZE 0x20000u
#define XMEM_TD_MAX_SEGMENTS 0xFFFFu
#define XMEM_TD_MAX_PASSES 8u
#define S_OK ((HRESULT)0)
#define XMCD_E_BADPARAM ((HRESULT)0x81DE2001u)
#define XMEM_TRACE_PASSES 0

typedef enum td_endian_t {
    TD_ENDIAN_BIG = 0,
    TD_ENDIAN_LITTLE = 1
} td_endian_t;

typedef struct td_header_t {
    td_endian_t endian;
    uint32_t flags;
    uint32_t segment_count;
    uint32_t bits_per_size;
    uint32_t compressed_block_size;
    uint32_t window_size;
    size_t payload_offset;
} td_header_t;

static int fopen_read(FILE **fp, const char *path) {
#if defined(_WIN32)
    return fopen_s(fp, path, "rb");
#else
    /* Liar : change - portable replacement for fopen_s. */
    *fp = fopen(path, "rb");
    return *fp == NULL;
#endif
}

static int fopen_write(FILE **fp, const char *path) {
#if defined(_WIN32)
    return fopen_s(fp, path, "wb");
#else
    /* Liar : change - portable replacement for fopen_s. */
    *fp = fopen(path, "wb");
    return *fp == NULL;
#endif
}

static int file_seek_to_end(FILE *fp) {
#if defined(_WIN32)
    return _fseeki64(fp, 0, SEEK_END);
#else
    /* Liar : change - retain 64-bit file offsets on POSIX. */
    return fseeko(fp, 0, SEEK_END);
#endif
}

static int file_seek_to_start(FILE *fp) {
#if defined(_WIN32)
    return _fseeki64(fp, 0, SEEK_SET);
#else
    return fseeko(fp, 0, SEEK_SET);
#endif
}

static int file_tell(FILE *fp, uint64_t *size) {
#if defined(_WIN32)
    __int64 offset = _ftelli64(fp);
    if (offset < 0) {
        return 1;
    }
    *size = (uint64_t)offset;
    return 0;
#else
    /* Liar : change - retain 64-bit file offsets on POSIX. */
    off_t offset = ftello(fp);
    if (offset < 0) {
        return 1;
    }
    *size = (uint64_t)offset;
    return 0;
#endif
}

static uint32_t read_u32_be(const uint8_t *data) {
    return ((uint32_t)data[0] << 24)
        | ((uint32_t)data[1] << 16)
        | ((uint32_t)data[2] << 8)
        | (uint32_t)data[3];
}

static uint32_t read_u32_le(const uint8_t *data) {
    return ((uint32_t)data[3] << 24)
        | ((uint32_t)data[2] << 16)
        | ((uint32_t)data[1] << 8)
        | (uint32_t)data[0];
}

static uint32_t read_u32(const uint8_t *data, td_endian_t endian) {
    return endian == TD_ENDIAN_BIG ? read_u32_be(data) : read_u32_le(data);
}

static const char *base_name(const char *path) {
    const char *slash = strrchr(path, '\\');
    const char *alt_slash = strrchr(path, '/');
    const char *base = path;

    if (slash != NULL && slash + 1 > base) {
        base = slash + 1;
    }
    if (alt_slash != NULL && alt_slash + 1 > base) {
        base = alt_slash + 1;
    }
    return base;
}

static int read_file(const char *path, uint8_t **data, size_t *size) {
    FILE *fp = NULL;
    uint8_t *buffer = NULL;
    uint64_t file_size = 0;

    if (fopen_read(&fp, path) != 0 || fp == NULL) {
        fprintf(stderr, "failed to open input: %s\n", path);
        return 1;
    }

    if (file_seek_to_end(fp) != 0) {
        fclose(fp);
        fprintf(stderr, "failed to seek input: %s\n", path);
        return 1;
    }

    if (file_tell(fp, &file_size) != 0) {
        fclose(fp);
        fprintf(stderr, "failed to get input size: %s\n", path);
        return 1;
    }

    if (file_seek_to_start(fp) != 0) {
        fclose(fp);
        fprintf(stderr, "failed to rewind input: %s\n", path);
        return 1;
    }

    if (file_size > SIZE_MAX) {
        fclose(fp);
        fprintf(stderr, "input is too large\n");
        return 1;
    }

    buffer = (uint8_t *)malloc((size_t)file_size);
    if (buffer == NULL) {
        fclose(fp);
        fprintf(stderr, "failed to allocate %" PRIu64 " bytes for input\n", file_size);
        return 1;
    }

    if (file_size != 0 && fread(buffer, 1, (size_t)file_size, fp) != (size_t)file_size) {
        free(buffer);
        fclose(fp);
        fprintf(stderr, "failed to read input: %s\n", path);
        return 1;
    }

    fclose(fp);
    *data = buffer;
    *size = (size_t)file_size;
    return 0;
}

static int write_file(const char *path, const uint8_t *data, size_t size) {
    FILE *fp = NULL;

    if (fopen_write(&fp, path) != 0 || fp == NULL) {
        fprintf(stderr, "failed to open output: %s\n", path);
        return 1;
    }

    if (size != 0 && fwrite(data, 1, size, fp) != size) {
        fclose(fp);
        fprintf(stderr, "failed to write output: %s\n", path);
        return 1;
    }

    fclose(fp);
    return 0;
}

static char *build_default_output_path(const char *input_path, const char *suffix) {
    size_t input_len = strlen(input_path);
    size_t suffix_len = strlen(suffix) + 1u;
    char *result = (char *)malloc(input_len + suffix_len);

    if (result == NULL) {
        return NULL;
    }

    memcpy(result, input_path, input_len);
    memcpy(result + input_len, suffix, suffix_len);
    return result;
}

static uint32_t bit_width_u32(uint32_t value) {
    uint32_t bits = 0;

    while (value != 0) {
        ++bits;
        value >>= 1;
    }

    return bits;
}

static uint32_t td_translation_entry_bits(uint32_t max_segment_bits) {
    return max_segment_bits <= 20u ? 20u : 32u;
}

static size_t td_header_size_for(uint32_t segment_count, uint32_t max_segment_bits) {
    uint32_t entry_bits = td_translation_entry_bits(max_segment_bits);
    return XCOMPRESS_HEADER_SIZE + ((((size_t)entry_bits * (size_t)segment_count) + 31u) >> 5u) * sizeof(uint32_t);
}

static int is_power_of_two_u32(uint32_t value) {
    return value != 0u && (value & (value - 1u)) == 0u;
}

static int is_supported_td_pitch(uint32_t compressed_block_size) {
    return compressed_block_size == 0x8000u
        || compressed_block_size == 0x10000u
        || compressed_block_size == 0x20000u
        || compressed_block_size == 0x40000u;
}

static int parse_td_header(const uint8_t *input, size_t input_size, td_header_t *header) {
    static const uint32_t bits_per_size_table[4] = { 20u, 32u, 0u, 0u };
    uint32_t identifier_be;
    uint32_t identifier_le;
    uint32_t bits_per_entry;

    if (input_size < XCOMPRESS_HEADER_SIZE) {
        fprintf(stderr, "input is smaller than the XMem TD header\n");
        return 1;
    }

    identifier_be = read_u32_be(input);
    identifier_le = read_u32_le(input);
    if (identifier_be == XCOMPRESS_FILE_IDENTIFIER_LZXTDECODE) {
        header->endian = TD_ENDIAN_BIG;
    } else if (identifier_le == XCOMPRESS_FILE_IDENTIFIER_LZXTDECODE) {
        header->endian = TD_ENDIAN_LITTLE;
    } else {
        fprintf(stderr, "unsupported header: expected 0x%08X\n", XCOMPRESS_FILE_IDENTIFIER_LZXTDECODE);
        return 1;
    }

    header->flags = read_u32(input + 12, header->endian);
    header->segment_count = (header->flags >> 6) & 0xFFFFu;
    header->bits_per_size = bits_per_size_table[(header->flags >> 22) & 0x3u];
    header->compressed_block_size = 0x8000u << ((header->flags >> 4) & 0x3u);
    header->window_size = 1u << ((header->flags & 0xFu) + 15u);

    if (header->segment_count == 0) {
        fprintf(stderr, "invalid TD header: segment count is zero\n");
        return 1;
    }
    if (header->bits_per_size == 0) {
        fprintf(stderr, "unsupported TD header: unknown size encoding\n");
        return 1;
    }

    bits_per_entry = 20u + ((header->flags & 0x00C00000u) ? 12u : 0u);
    header->payload_offset = XCOMPRESS_HEADER_SIZE
        + ((((size_t)bits_per_entry * (size_t)header->segment_count) + 31u) >> 5u) * sizeof(uint32_t);

    if (header->payload_offset > input_size) {
        fprintf(stderr, "invalid TD header: size table exceeds input size\n");
        return 1;
    }

    return 0;
}

static int decode_segment_sizes(
    const uint8_t *input,
    size_t input_size,
    const td_header_t *header,
    uint32_t **segment_sizes,
    size_t *total_output_size) {
    uint32_t *sizes = NULL;
    uint64_t total = 0;
    size_t cursor = XCOMPRESS_HEADER_SIZE;
    uint32_t bits = 0;
    uint32_t old = 0;
    uint32_t num = 0;
    uint32_t index = 0;

    sizes = (uint32_t *)malloc((size_t)header->segment_count * sizeof(uint32_t));
    if (sizes == NULL) {
        fprintf(stderr, "failed to allocate segment table\n");
        return 1;
    }

    for (index = 0; index < header->segment_count; ++index) {
        uint32_t size = 0;

        if ((header->bits_per_size & 31u) != 0) {
            bits = (bits + header->bits_per_size) & 31u;
            if (bits > 0 && bits <= header->bits_per_size) {
                if (cursor + sizeof(uint32_t) > input_size) {
                    free(sizes);
                    fprintf(stderr, "segment table is truncated\n");
                    return 1;
                }
                num = read_u32(input + cursor, header->endian);
                cursor += sizeof(uint32_t);
            } else {
                num = old;
                old = 0;
            }

            if (bits == 0) {
                size = num;
                old = 0;
            } else {
                size = (num >> (32u - bits)) | (old << bits);
                old = num & ((1u << (32u - bits)) - 1u);
            }
        } else {
            if (cursor + sizeof(uint32_t) > input_size) {
                free(sizes);
                fprintf(stderr, "segment table is truncated\n");
                return 1;
            }
            size = read_u32(input + cursor, header->endian);
            cursor += sizeof(uint32_t);
        }

        sizes[index] = size;
        total += size;
    }

    if (cursor != header->payload_offset) {
        free(sizes);
        fprintf(stderr, "segment table size does not match header\n");
        return 1;
    }

    if (total > SIZE_MAX) {
        free(sizes);
        fprintf(stderr, "output is too large for this build\n");
        return 1;
    }

    *segment_sizes = sizes;
    *total_output_size = (size_t)total;
    return 0;
}

static int decompress_td_stream(
    const uint8_t *input,
    size_t input_size,
    const td_header_t *header,
    const uint32_t *segment_sizes,
    uint8_t **output_data,
    size_t *output_size) {
    XMEMDECOMPRESSION_CONTEXT context = NULL;
    XMEMCODEC_PARAMETERS_LZX params;
    uint8_t *output = NULL;
    size_t written = 0;
    HRESULT hr;
    uint32_t index = 0;

    params.Flags = 0;
    params.WindowSize = header->window_size;
    params.CompressionPartitionSize = 0;

    hr = XMemCreateDecompressionContext(XMEMCODEC_DEFAULT, &params, XMEM_TD_FLAG, &context);
    if (hr != S_OK) {
        fprintf(stderr, "XMemCreateDecompressionContext failed: 0x%08" PRIX32 "\n", (uint32_t)hr);
        return 1;
    }

    output = (uint8_t *)malloc(*output_size);
    if (output == NULL) {
        XMemDestroyDecompressionContext(context);
        fprintf(stderr, "failed to allocate %zu bytes for output\n", *output_size);
        return 1;
    }

    for (index = 0; index < header->segment_count; ++index) {
        size_t block_output_size = segment_sizes[index];
        size_t segment_base_offset = (size_t)index * header->compressed_block_size;
        size_t segment_data_offset = segment_base_offset;
        size_t segment_end_offset;
        const uint8_t *segment;
        size_t offset = 0;

        if (segment_base_offset > input_size) {
            free(output);
            XMemDestroyDecompressionContext(context);
            fprintf(stderr, "segment %u base exceeds input size\n", index);
            return 1;
        }

        if (index == 0) {
            segment_data_offset = header->payload_offset;
        }

        segment_end_offset = segment_base_offset + header->compressed_block_size;
        if (segment_end_offset > input_size) {
            segment_end_offset = input_size;
        }
        if (segment_data_offset > segment_end_offset) {
            free(output);
            XMemDestroyDecompressionContext(context);
            fprintf(stderr, "segment %u payload exceeds segment bounds\n", index);
            return 1;
        }

        segment = input + segment_data_offset;

        while (offset < block_output_size) {
            size_t chunk_size = block_output_size - offset;
            size_t src_size;
            HRESULT chunk_result;

            if (chunk_size > header->compressed_block_size) {
                chunk_size = header->compressed_block_size;
            }

            src_size = segment_end_offset - segment_data_offset;
#if defined(_MSC_VER)
            __try {
                chunk_result = XMemDecompressSegmentTD(
                    context,
                    output + written,
                    &chunk_size,
                    segment,
                    src_size,
                    block_output_size,
                    offset);
            } __except (EXCEPTION_EXECUTE_HANDLER) {
                free(output);
                XMemDestroyDecompressionContext(context);
                fprintf(
                    stderr,
                    "XMemDecompressSegmentTD raised an exception at segment %u offset %zu "
                    "(segment size %zu, src size %zu, segment_data_offset %zu, segment_end_offset %zu)\n",
                    index,
                    offset,
                    block_output_size,
                    src_size,
                    segment_data_offset,
                    segment_end_offset);
                return 1;
            }
#else
            chunk_result = XMemDecompressSegmentTD(
                context,
                output + written,
                &chunk_size,
                segment,
                src_size,
                block_output_size,
                offset);
#endif
            if (chunk_result != S_OK) {
                free(output);
                XMemDestroyDecompressionContext(context);
                fprintf(
                    stderr,
                    "XMemDecompressSegmentTD failed at segment %u offset %zu: 0x%08" PRIX32 "\n",
                    index,
                    offset,
                    (uint32_t)chunk_result);
                return 1;
            }
            if (chunk_size == 0) {
                free(output);
                XMemDestroyDecompressionContext(context);
                fprintf(
                    stderr,
                    "XMemDecompressSegmentTD returned zero bytes at segment %u offset %zu (segment size %zu, src size %zu)\n",
                    index,
                    offset,
                    block_output_size,
                    src_size);
                return 1;
            }

            written += chunk_size;
            offset += chunk_size;
        }
    }

    XMemDestroyDecompressionContext(context);
    *output_data = output;
    *output_size = written;
    return 0;
}

int XMemDecompressLzxTdBuffer(
    const uint8_t *input,
    size_t input_size,
    uint8_t **output_data,
    size_t *output_size) {
    uint32_t *segment_sizes = NULL;
    td_header_t header;
    size_t decompressed_size = 0;
    int result = 1;

    if (input == NULL || output_data == NULL || output_size == NULL) {
        fprintf(stderr, "invalid arguments for TD decompression\n");
        return 1;
    }

    *output_data = NULL;
    *output_size = 0;

    if (parse_td_header(input, input_size, &header) != 0) {
        goto cleanup;
    }

    if (decode_segment_sizes(input, input_size, &header, &segment_sizes, &decompressed_size) != 0) {
        goto cleanup;
    }

    if (decompress_td_stream(input, input_size, &header, segment_sizes, output_data, &decompressed_size) != 0) {
        goto cleanup;
    }

    *output_size = decompressed_size;
    result = 0;

cleanup:
    if (result != 0) {
        free(*output_data);
        *output_data = NULL;
        *output_size = 0;
    }
    free(segment_sizes);
    return result;
}

typedef struct td_compress_pass_t {
    uint8_t *data;
    size_t size;
    uint32_t segment_count;
    uint32_t max_segment_bits;
    size_t header_size;
} td_compress_pass_t;

static void free_compress_pass(td_compress_pass_t *pass) {
    if (pass == NULL) {
        return;
    }

    free(pass->data);
    pass->data = NULL;
    pass->size = 0;
    pass->segment_count = 0;
    pass->max_segment_bits = 0;
    pass->header_size = XCOMPRESS_HEADER_SIZE;
}

static int ensure_output_capacity(uint8_t **data, size_t *capacity, size_t required) {
    uint8_t *resized = NULL;
    size_t new_capacity = *capacity;

    if (required <= *capacity) {
        return 0;
    }

    if (new_capacity == 0) {
        new_capacity = required;
    }

    while (new_capacity < required) {
        if (new_capacity > (SIZE_MAX / 2u)) {
            new_capacity = required;
            break;
        }
        new_capacity *= 2u;
    }

    resized = (uint8_t *)realloc(*data, new_capacity);
    if (resized == NULL) {
        fprintf(stderr, "failed to allocate %zu bytes for compressed output\n", new_capacity);
        return 1;
    }

    *data = resized;
    *capacity = new_capacity;
    return 0;
}

static int compress_td_pass(
    XMEMCOMPRESSION_CONTEXT context,
    const uint8_t *input,
    size_t input_size,
    uint32_t compressed_block_size,
    float threshold,
    td_compress_pass_t *pass,
    uint32_t pass_index) {
    const uint8_t *input_cursor = input;
    size_t input_remaining = input_size;
    size_t output_capacity = 0;
    HRESULT hr;

    memset(pass, 0, sizeof(*pass));
    pass->header_size = XCOMPRESS_HEADER_SIZE;

    if (ensure_output_capacity(&pass->data, &output_capacity, compressed_block_size) != 0) {
        return 1;
    }

    hr = XMemBeginCompressionTD(context, compressed_block_size);
    if (hr != S_OK) {
        fprintf(stderr, "XMemBeginCompressionTD failed: 0x%08" PRIX32 "\n", (uint32_t)hr);
        free_compress_pass(pass);
        return 1;
    }

    if (XMEM_TRACE_PASSES) {
        fprintf(
            stderr,
            "[trace pass] pass=%" PRIu32 " begin input=%zu block=%" PRIu32 " threshold=%.3f\n",
            pass_index,
            input_size,
            compressed_block_size,
            threshold);
    }

    while (input_remaining != 0) {
        size_t segment_offset;
        size_t dest_size;
        size_t src_size;
        uint32_t segment_bits;

        if (pass->segment_count >= XMEM_TD_MAX_SEGMENTS) {
            fprintf(stderr, "too many TD segments; maximum is %u\n", XMEM_TD_MAX_SEGMENTS);
            free_compress_pass(pass);
            return 1;
        }

        segment_offset = (size_t)pass->segment_count * (size_t)compressed_block_size;
        if (segment_offset > SIZE_MAX - compressed_block_size) {
            fprintf(stderr, "compressed output is too large for this build\n");
            free_compress_pass(pass);
            return 1;
        }

        if (ensure_output_capacity(&pass->data, &output_capacity, segment_offset + compressed_block_size) != 0) {
            free_compress_pass(pass);
            return 1;
        }

        dest_size = compressed_block_size;
        src_size = input_remaining;
        if (XMEM_TRACE_PASSES) {
            fprintf(
                stderr,
                "[trace pass] pass=%" PRIu32 " seg=%" PRIu32 " src_off=%zu remaining=%zu\n",
                pass_index,
                pass->segment_count,
                (size_t)(input_cursor - input),
                input_remaining);
        }
        hr = XMemCompressSegmentTD(
            context,
            pass->data + segment_offset,
            &dest_size,
            input_cursor,
            &src_size,
            threshold);
        if (hr != S_OK) {
            fprintf(stderr, "XMemCompressSegmentTD failed at segment %u: 0x%08" PRIX32 "\n",
                pass->segment_count,
                (uint32_t)hr);
            free_compress_pass(pass);
            return 1;
        }
        if (XMEM_TRACE_PASSES) {
            fprintf(
                stderr,
                "[trace pass] pass=%" PRIu32 " seg=%" PRIu32 " done src=%zu dest=%zu hr=0x%08" PRIX32 "\n",
                pass_index,
                pass->segment_count,
                src_size,
                dest_size,
                (uint32_t)hr);
        }
        if (src_size == 0) {
            fprintf(stderr, "XMemCompressSegmentTD consumed zero bytes at segment %u\n", pass->segment_count);
            free_compress_pass(pass);
            return 1;
        }
        if (src_size > UINT32_MAX) {
            fprintf(stderr, "TD segment is too large\n");
            free_compress_pass(pass);
            return 1;
        }
        if (dest_size > compressed_block_size) {
            fprintf(stderr, "XMemCompressSegmentTD returned an oversized segment\n");
            free_compress_pass(pass);
            return 1;
        }

        pass->size = segment_offset + dest_size;
        segment_bits = bit_width_u32((uint32_t)src_size);
        if (segment_bits > pass->max_segment_bits) {
            pass->max_segment_bits = segment_bits;
        }

        input_cursor += src_size;
        input_remaining -= src_size;
        ++pass->segment_count;
    }

    pass->header_size = td_header_size_for(pass->segment_count, pass->max_segment_bits);
    if (pass->header_size > compressed_block_size) {
        fprintf(stderr, "TD header is larger than the compressed block size\n");
        free_compress_pass(pass);
        return 1;
    }

    if (pass->size < pass->header_size) {
        if (ensure_output_capacity(&pass->data, &output_capacity, pass->header_size) != 0) {
            free_compress_pass(pass);
            return 1;
        }
        pass->size = pass->header_size;
    }

    {
        size_t header_size = compressed_block_size;
        hr = XMemEndCompressionTD(context, pass->data, &header_size, threshold);
        if (hr != S_OK) {
            if ((hr == XMCD_E_BADPARAM) && (header_size == 0u)) {
                if (XMEM_TRACE_PASSES) {
                    fprintf(
                        stderr,
                        "[trace pass] pass=%" PRIu32 " retry header_size=%zu segments=%" PRIu32 " size=%zu\n",
                        pass_index,
                        header_size,
                        pass->segment_count,
                        pass->size);
                }
                free_compress_pass(pass);
                return 2;
            }

            fprintf(stderr, "XMemEndCompressionTD failed: 0x%08" PRIX32 "\n", (uint32_t)hr);
            free_compress_pass(pass);
            return 1;
        }
        pass->header_size = header_size;
    }

    if (XMEM_TRACE_PASSES) {
        fprintf(
            stderr,
            "[trace pass] pass=%" PRIu32 " complete segments=%" PRIu32 " header=%zu size=%zu bits=%" PRIu32 "\n",
            pass_index,
            pass->segment_count,
            pass->header_size,
            pass->size,
            pass->max_segment_bits);
    }

    return 0;
}

int XMemCompressLzxTdBufferEx(
    const uint8_t *input,
    size_t input_size,
    uint8_t **output_data,
    size_t *output_size,
    uint32_t window_size,
    uint32_t compressed_block_size,
    float threshold) {
    XMEMCOMPRESSION_CONTEXT context = NULL;
    XMEMCODEC_PARAMETERS_LZX params;
    td_compress_pass_t pass;
    HRESULT hr;
    uint32_t pass_index;

    if (input == NULL || output_data == NULL || output_size == NULL) {
        fprintf(stderr, "invalid arguments for TD compression\n");
        return 1;
    }

    *output_data = NULL;
    *output_size = 0;
    memset(&pass, 0, sizeof(pass));
    pass.header_size = XCOMPRESS_HEADER_SIZE;

    if (input_size == 0) {
        fprintf(stderr, "TD compression of an empty buffer is not supported by this wrapper\n");
        return 1;
    }
    if (!is_power_of_two_u32(window_size) || window_size < 0x8000u) {
        fprintf(stderr, "invalid LZX window size: 0x%08" PRIX32 "\n", window_size);
        return 1;
    }
    if (!is_supported_td_pitch(compressed_block_size)) {
        fprintf(stderr, "invalid TD compressed block size: 0x%08" PRIX32 "\n", compressed_block_size);
        return 1;
    }
    if (threshold <= 0.0f) {
        fprintf(stderr, "invalid compression threshold: %f\n", threshold);
        return 1;
    }

    params.Flags = 0;
    params.WindowSize = window_size;
    params.CompressionPartitionSize = 0;

    hr = XMemCreateCompressionContext(XMEMCODEC_DEFAULT, &params, XMEM_TD_FLAG, &context);
    if (hr != S_OK) {
        fprintf(stderr, "XMemCreateCompressionContext failed: 0x%08" PRIX32 "\n", (uint32_t)hr);
        return 1;
    }

    hr = XMemResetCompressionContext(context);
    if (hr != S_OK) {
        fprintf(stderr, "XMemResetCompressionContext failed: 0x%08" PRIX32 "\n", (uint32_t)hr);
        XMemDestroyCompressionContext(context);
        return 1;
    }

    for (pass_index = 0; pass_index < XMEM_TD_MAX_PASSES; ++pass_index) {
        int pass_result;
        free_compress_pass(&pass);

        pass_result = compress_td_pass(context, input, input_size, compressed_block_size, threshold, &pass, pass_index);
        if (pass_result == 0) {
            break;
        }
        if (pass_result == 2) {
            continue;
        }

        if (pass_result != 0) {
            XMemDestroyCompressionContext(context);
            return 1;
        }
    }

    XMemDestroyCompressionContext(context);

    if (pass.data == NULL) {
        fprintf(stderr, "TD compression did not converge on a stable header size\n");
        free_compress_pass(&pass);
        return 1;
    }

    *output_data = pass.data;
    *output_size = pass.size;
    pass.data = NULL;
    free_compress_pass(&pass);
    return 0;
}

int XMemCompressLzxTdBuffer(
    const uint8_t *input,
    size_t input_size,
    uint8_t **output_data,
    size_t *output_size) {
    return XMemCompressLzxTdBufferEx(
        input,
        input_size,
        output_data,
        output_size,
        XMEM_TD_DEFAULT_WINDOW_SIZE,
        XMEM_TD_DEFAULT_COMPRESSED_BLOCK_SIZE,
        1.0f);
}

#if defined(XMEMCOMPRESSION_STANDALONE)
int main(int argc, char **argv) {
    const char *input_path = NULL;
    const char *output_path = NULL;
    char *default_output_path = NULL;
    uint8_t *input_data = NULL;
    uint8_t *output_data = NULL;
    size_t input_size = 0;
    size_t output_size = 0;
    int compress_mode = 0;
    int arg_index = 1;
    int result = 1;

    if (argc >= 2 && (strcmp(argv[1], "-c") == 0 || strcmp(argv[1], "--compress") == 0)) {
        compress_mode = 1;
        arg_index = 2;
    } else if (argc >= 2 && (strcmp(argv[1], "-d") == 0 || strcmp(argv[1], "--decompress") == 0)) {
        compress_mode = 0;
        arg_index = 2;
    }

    if (argc > arg_index) {
        input_path = argv[arg_index];
    } else {
        fprintf(stderr, "failed to read input path\n");
        return 1;
    }

    if (argc > arg_index + 1) {
        output_path = argv[arg_index + 1];
    } else {
        default_output_path = build_default_output_path(input_path, compress_mode ? ".cmp" : ".dec");
        if (default_output_path == NULL) {
            fprintf(stderr, "failed to build default output path\n");
            return 1;
        }
        output_path = default_output_path;
    }

    if (read_file(input_path, &input_data, &input_size) != 0) {
        goto cleanup;
    }

    if (compress_mode) {
        if (XMemCompressLzxTdBuffer(input_data, input_size, &output_data, &output_size) != 0) {
            goto cleanup;
        }
    } else {
        if (XMemDecompressLzxTdBuffer(input_data, input_size, &output_data, &output_size) != 0) {
            goto cleanup;
        }
    }

    if (write_file(output_path, output_data, output_size) != 0) {
        goto cleanup;
    }

    printf("%s %s -> %s (%zu bytes)\n",
        compress_mode ? "compressed" : "decompressed",
        input_path,
        output_path,
        output_size);
    result = 0;

cleanup:
    free(output_data);
    free(input_data);
    free(default_output_path);
    return result;
}
#endif
