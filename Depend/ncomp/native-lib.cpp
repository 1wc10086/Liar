#include <algorithm>
#include <array>
#include <cstddef>
#include <cstdint>
#include <cstring>
#include <limits>
#include <mutex>
#include <new>
#include <span>
#include <stdexcept>
#include <unordered_map>
#include <vector>

extern "C" {
#include "fastlz.h"
#include "heatshrink_decoder.h"
#include "heatshrink_encoder.h"
#include "libbsc.h"
#include "lzf.h"
#include "lzfse.h"
#include "microtar.h"
}

#include "libzpaq.h"

namespace {

constexpr int64_t failure = -1;

[[nodiscard]] constexpr bool int_size(size_t size) noexcept {
    return size <= static_cast<size_t>(std::numeric_limits<int>::max());
}

struct MemoryReader final : libzpaq::Reader {
    std::span<const uint8_t> input;
    size_t position{};

    int get() override { return position == input.size() ? -1 : input[position++]; }
};

struct MemoryWriter final : libzpaq::Writer {
    std::span<uint8_t> output;
    size_t position{};
    bool overflow{};

    void put(int value) override {
        if (position == output.size()) {
            overflow = true;
            return;
        }
        output[position++] = static_cast<uint8_t>(value);
    }
};

struct TarMemory {
    std::span<const uint8_t> input;
    unsigned position{};
};

struct TarWriter {
    std::span<uint8_t> output;
    unsigned position{};
};

struct TarEntry {
    const char* name;
    const void* data;
    size_t size;
    uint32_t type;
};

struct LzwCode {
    uint32_t prefix{};
    uint8_t suffix{};
};

[[nodiscard]] bool bsc_ready() noexcept {
    static std::once_flag once;
    static int status{};
    std::call_once(once, [] { status = bsc_init(LIBBSC_FEATURE_NONE); });
    return status == LIBBSC_NO_ERROR;
}

[[nodiscard]] bool lzw_code(std::span<const uint8_t> input, size_t& bit, uint8_t width, uint32_t& value) noexcept {
    if (width > 32 || bit > input.size() * 8 || width > input.size() * 8 - bit) return false;
    value = 0;
    for (uint8_t i = 0; i != width; ++i) value |= static_cast<uint32_t>((input[(bit + i) / 8] >> ((bit + i) % 8)) & 1) << i;
    bit += width;
    return true;
}

[[nodiscard]] int64_t lzw_decompress(std::span<uint8_t> output, std::span<const uint8_t> input) {
    constexpr uint32_t first_code = 256;
    constexpr uint32_t max_codes = 1u << 20;
    if (input.empty() || output.empty()) return failure;
    std::vector<LzwCode> dictionary(max_codes - first_code);
    std::vector<uint8_t> sequence;
    sequence.reserve(max_codes - first_code);
    size_t bit{};
    size_t written{};
    uint8_t width = 9;
    uint32_t next = first_code;
    uint32_t last{};
    if (!lzw_code(input, bit, width, last) || last >= first_code) return failure;
    output[written++] = static_cast<uint8_t>(last);
    uint8_t first = static_cast<uint8_t>(last);
    for (;;) {
        uint32_t code{};
        if (!lzw_code(input, bit, width, code)) break;
        while (code == ((1u << width) - 1) && width < 20) {
            ++width;
            if (!lzw_code(input, bit, width, code)) return failure;
        }
        if (code > next || code >= max_codes) return failure;
        sequence.clear();
        uint32_t cursor = code == next ? last : code;
        while (cursor >= first_code) {
            if (cursor >= next || sequence.size() == sequence.capacity()) return failure;
            const auto& item = dictionary[cursor - first_code];
            sequence.push_back(item.suffix);
            cursor = item.prefix;
        }
        first = static_cast<uint8_t>(cursor);
        const auto extra = code == next ? 1u : 0u;
        if (written > output.size() || sequence.size() + extra + 1 > output.size() - written) return failure;
        output[written++] = first;
        for (auto it = sequence.rbegin(); it != sequence.rend(); ++it) output[written++] = *it;
        if (code == next) output[written++] = first;
        if (next < max_codes) dictionary[next++]= {last, first};
        last = code;
    }
    return static_cast<int64_t>(written);
}

[[nodiscard]] bool lzw_write(std::span<uint8_t> output, size_t& bit, uint8_t width, uint32_t value) noexcept {
    if (bit > output.size() * 8 || width > output.size() * 8 - bit) return false;
    for (uint8_t i = 0; i != width; ++i) output[(bit + i) / 8] = static_cast<uint8_t>(output[(bit + i) / 8] | (((value >> i) & 1) << ((bit + i) % 8)));
    bit += width;
    return true;
}

[[nodiscard]] int64_t lzw_compress(std::span<uint8_t> output, std::span<const uint8_t> input) {
    constexpr uint32_t first_code = 256;
    constexpr uint32_t max_codes = 1u << 20;
    if (input.empty()) return 0;
    std::fill(output.begin(), output.end(), 0);
    std::unordered_map<uint64_t, uint32_t> dictionary;
    dictionary.reserve(std::min<size_t>(input.size(), max_codes - first_code));
    size_t bit{};
    uint8_t width = 9;
    uint32_t next = first_code;
    uint32_t code = input.front();
    for (size_t i = 1; i < input.size(); ++i) {
        const auto key = (static_cast<uint64_t>(code) << 8) | input[i];
        if (const auto it = dictionary.find(key); it != dictionary.end()) {
            code = it->second;
            continue;
        }
        while (code >= ((1u << width) - 1) && width < 20) {
            if (!lzw_write(output, bit, width, (1u << width) - 1)) return failure;
            ++width;
        }
        if (!lzw_write(output, bit, width, code)) return failure;
        if (next < max_codes) dictionary.emplace(key, next++);
        code = input[i];
    }
    if (!lzw_write(output, bit, width, code)) return failure;
    return static_cast<int64_t>((bit + 7) / 8);
}

[[nodiscard]] int64_t heatshrink_compress(std::span<uint8_t> output, std::span<const uint8_t> input, uint8_t window_bits, uint8_t lookahead_bits) noexcept {
    auto* encoder = heatshrink_encoder_alloc(window_bits, lookahead_bits);
    if (!encoder) return failure;
    size_t read{};
    size_t written{};
    auto poll = [&] {
        for (;;) {
            if (written == output.size()) return false;
            size_t produced{};
            const auto status = heatshrink_encoder_poll(encoder, output.data() + written, output.size() - written, &produced);
            if (status < 0) return false;
            written += produced;
            if (status == HSER_POLL_EMPTY) return true;
        }
    };
    while (read < input.size()) {
        size_t consumed{};
        if (heatshrink_encoder_sink(encoder, const_cast<uint8_t*>(input.data()) + read, input.size() - read, &consumed) < 0 || !consumed || !poll()) {
            heatshrink_encoder_free(encoder);
            return failure;
        }
        read += consumed;
    }
    for (;;) {
        const auto status = heatshrink_encoder_finish(encoder);
        if (status < 0 || !poll()) {
            heatshrink_encoder_free(encoder);
            return failure;
        }
        if (status == HSER_FINISH_DONE) break;
    }
    heatshrink_encoder_free(encoder);
    return static_cast<int64_t>(written);
}

int tar_read(mtar_t* tar, void* data, unsigned size) {
    auto& memory = *static_cast<TarMemory*>(tar->stream);
    if (size > memory.input.size() || memory.position > memory.input.size() - size) return MTAR_EREADFAIL;
    std::memcpy(data, memory.input.data() + memory.position, size);
    memory.position += size;
    return MTAR_ESUCCESS;
}

int tar_seek(mtar_t* tar, unsigned position) {
    auto& memory = *static_cast<TarMemory*>(tar->stream);
    if (position > memory.input.size()) return MTAR_ESEEKFAIL;
    memory.position = position;
    return MTAR_ESUCCESS;
}

int tar_write(mtar_t* tar, const void* data, unsigned size) {
    auto& writer = *static_cast<TarWriter*>(tar->stream);
    if (size > writer.output.size() || writer.position > writer.output.size() - size) return MTAR_EWRITEFAIL;
    std::memcpy(writer.output.data() + writer.position, data, size);
    writer.position += size;
    return MTAR_ESUCCESS;
}

}

namespace libzpaq {
void error(const char* message) { throw std::runtime_error(message ? message : "zpaq error"); }
}

#if defined(_WIN32)
#define NCOMP_API extern "C" __declspec(dllexport)
#else
#define NCOMP_API extern "C" __attribute__((visibility("default")))
#endif

NCOMP_API uint32_t ncomp_abi_version() { return 1; }
NCOMP_API int64_t ncomp_fastlz_compress(void* output, size_t output_size, const void* input, size_t input_size, int32_t level) {
    if (!output || !input || !int_size(input_size) || input_size < 16 || output_size < std::max<size_t>(66, input_size + (input_size + 19) / 20)) return failure;
    const auto written = fastlz_compress_level(level == 1 ? 1 : 2, input, static_cast<int>(input_size), output);
    return written > 0 && static_cast<size_t>(written) <= output_size ? written : failure;
}
NCOMP_API int64_t ncomp_lzf_compress(void* output, size_t output_size, const void* input, size_t input_size, int32_t) {
    if (!output || !input || input_size > std::numeric_limits<unsigned>::max() || output_size > std::numeric_limits<unsigned>::max()) return failure;
    const auto written = lzf_compress(input, static_cast<unsigned>(input_size), output, static_cast<unsigned>(output_size));
    return written ? static_cast<int64_t>(written) : failure;
}
NCOMP_API int64_t ncomp_bsc_compress(void* output, size_t output_size, const void* input, size_t input_size, int32_t level) {
    if (!output || !input || !int_size(input_size) || output_size < input_size + LIBBSC_HEADER_SIZE || !bsc_ready()) return failure;
    const auto features = level <= 1 ? LIBBSC_FEATURE_FASTMODE : LIBBSC_FEATURE_NONE;
    auto written = bsc_compress(static_cast<const unsigned char*>(input), static_cast<unsigned char*>(output), static_cast<int>(input_size), LIBBSC_DEFAULT_LZPHASHSIZE, LIBBSC_DEFAULT_LZPMINLEN, LIBBSC_DEFAULT_BLOCKSORTER, LIBBSC_DEFAULT_CODER, features);
    if (written == LIBBSC_NOT_COMPRESSIBLE) written = bsc_store(static_cast<const unsigned char*>(input), static_cast<unsigned char*>(output), static_cast<int>(input_size), features);
    return written > 0 && static_cast<size_t>(written) <= output_size ? written : failure;
}
NCOMP_API int64_t ncomp_lzfse_compress(void* output, size_t output_size, const void* input, size_t input_size, int32_t) {
    if (!output || !input) return failure;
    const auto written = lzfse_encode_buffer(static_cast<uint8_t*>(output), output_size, static_cast<const uint8_t*>(input), input_size, nullptr);
    return written ? static_cast<int64_t>(written) : failure;
}
NCOMP_API int64_t ncomp_zpaq_compress(void* output, size_t output_size, const void* input, size_t input_size, int32_t level) {
    if (!output || !input) return failure;
    try {
        MemoryReader reader;
        reader.input = {static_cast<const uint8_t*>(input), input_size};
        MemoryWriter writer;
        writer.output = {static_cast<uint8_t*>(output), output_size};
        const char method[] = {static_cast<char>('0' + std::clamp(level, 0, 5)), 0};
        libzpaq::compress(&reader, &writer, method);
        return writer.overflow ? failure : static_cast<int64_t>(writer.position);
    } catch (...) {
        return failure;
    }
}
NCOMP_API int64_t ncomp_lzw_compress(void* output, size_t output_size, const void* input, size_t input_size, int32_t) {
    if (!output || !input) return failure;
    try {
        return lzw_compress({static_cast<uint8_t*>(output), output_size}, {static_cast<const uint8_t*>(input), input_size});
    } catch (...) {
        return failure;
    }
}
NCOMP_API int64_t ncomp_heatshrink_compress(void* output, size_t output_size, const void* input, size_t input_size, int32_t parameters) {
    const auto window_bits = static_cast<uint8_t>((parameters >> 8) & 0xff);
    const auto lookahead_bits = static_cast<uint8_t>(parameters & 0xff);
    return output && input ? heatshrink_compress({static_cast<uint8_t*>(output), output_size}, {static_cast<const uint8_t*>(input), input_size}, window_bits, lookahead_bits) : failure;
}
NCOMP_API int64_t ncomp_fastlz_decompress(void* output, size_t output_size, const void* input, size_t input_size) {
    if (!output || !input || !int_size(output_size) || !int_size(input_size)) return failure;
    const auto written = fastlz_decompress(input, static_cast<int>(input_size), output, static_cast<int>(output_size));
    return written > 0 ? written : failure;
}
NCOMP_API int64_t ncomp_lzf_decompress(void* output, size_t output_size, const void* input, size_t input_size) {
    if (!output || !input || !int_size(output_size) || !int_size(input_size)) return failure;
    const auto written = lzf_decompress(input, static_cast<unsigned>(input_size), output, static_cast<unsigned>(output_size));
    return written ? static_cast<int64_t>(written) : failure;
}
NCOMP_API int64_t ncomp_bsc_decompress(void* output, size_t output_size, const void* input, size_t input_size) {
    if (!output || !input || !int_size(output_size) || !int_size(input_size)) return failure;
    int block_size{};
    int data_size{};
    if (!bsc_ready() || bsc_block_info(static_cast<const unsigned char*>(input), static_cast<int>(input_size), &block_size, &data_size, LIBBSC_FEATURE_NONE) != LIBBSC_NO_ERROR || data_size < 0 || static_cast<size_t>(data_size) > output_size || static_cast<size_t>(block_size) > input_size) return failure;
    return bsc_decompress(static_cast<const unsigned char*>(input), block_size, static_cast<unsigned char*>(output), data_size, LIBBSC_FEATURE_NONE) == LIBBSC_NO_ERROR ? data_size : failure;
}
NCOMP_API int64_t ncomp_lzfse_decompress(void* output, size_t output_size, const void* input, size_t input_size) {
    if (!output || !input) return failure;
    const auto written = lzfse_decode_buffer(static_cast<uint8_t*>(output), output_size, static_cast<const uint8_t*>(input), input_size, nullptr);
    return written ? static_cast<int64_t>(written) : failure;
}
NCOMP_API int64_t ncomp_zpaq_decompress(void* output, size_t output_size, const void* input, size_t input_size) {
    if (!output || !input) return failure;
    try {
        MemoryReader reader;
        reader.input = {static_cast<const uint8_t*>(input), input_size};
        MemoryWriter writer;
        writer.output = {static_cast<uint8_t*>(output), output_size};
        libzpaq::decompress(&reader, &writer);
        return writer.overflow ? failure : static_cast<int64_t>(writer.position);
    } catch (...) {
        return failure;
    }
}
NCOMP_API int64_t ncomp_lzw_decompress(void* output, size_t output_size, const void* input, size_t input_size) {
    if (!output || !input) return failure;
    try {
        return lzw_decompress({static_cast<uint8_t*>(output), output_size}, {static_cast<const uint8_t*>(input), input_size});
    } catch (...) {
        return failure;
    }
}
NCOMP_API void* ncomp_heatshrink_create(uint16_t input_buffer_size, uint8_t window_bits, uint8_t lookahead_bits) {
    return heatshrink_decoder_alloc(input_buffer_size, window_bits, lookahead_bits);
}
NCOMP_API void ncomp_heatshrink_destroy(void* decoder) { if (decoder) heatshrink_decoder_free(static_cast<heatshrink_decoder*>(decoder)); }
NCOMP_API int32_t ncomp_heatshrink_reset(void* decoder) { if (!decoder) return -1; heatshrink_decoder_reset(static_cast<heatshrink_decoder*>(decoder)); return 0; }
NCOMP_API int32_t ncomp_heatshrink_sink(void* decoder, const void* input, size_t input_size, size_t* consumed) { return decoder && input && consumed ? heatshrink_decoder_sink(static_cast<heatshrink_decoder*>(decoder), const_cast<uint8_t*>(static_cast<const uint8_t*>(input)), input_size, consumed) : -1; }
NCOMP_API int32_t ncomp_heatshrink_poll(void* decoder, void* output, size_t output_size, size_t* written) { return decoder && output && written ? heatshrink_decoder_poll(static_cast<heatshrink_decoder*>(decoder), static_cast<uint8_t*>(output), output_size, written) : -1; }
NCOMP_API int32_t ncomp_heatshrink_finish(void* decoder) { return decoder ? heatshrink_decoder_finish(static_cast<heatshrink_decoder*>(decoder)) : -1; }
NCOMP_API int64_t ncomp_tar_create_file(void* output, size_t output_size, const char* name, const void* input, size_t input_size) {
    if (!output || !name || !input || input_size > std::numeric_limits<unsigned>::max() || output_size > std::numeric_limits<unsigned>::max() || std::strlen(name) >= 100) return failure;
    TarWriter writer{{static_cast<uint8_t*>(output), output_size}};
    mtar_t tar{};
    tar.write = tar_write;
    tar.stream = &writer;
    if (mtar_write_file_header(&tar, name, static_cast<unsigned>(input_size)) != MTAR_ESUCCESS || mtar_write_data(&tar, input, static_cast<unsigned>(input_size)) != MTAR_ESUCCESS || mtar_finalize(&tar) != MTAR_ESUCCESS) return failure;
    return writer.position;
}
NCOMP_API int64_t ncomp_tar_create(void* output, size_t output_size, const TarEntry* entries, size_t count) {
    if (!output || !entries || !count || output_size > std::numeric_limits<unsigned>::max()) return failure;
    TarWriter writer{{static_cast<uint8_t*>(output), output_size}};
    mtar_t tar{};
    tar.write = tar_write;
    tar.stream = &writer;
    for (size_t i = 0; i < count; ++i) {
        const auto& entry = entries[i];
        if (!entry.name || std::strlen(entry.name) >= 100 || entry.size > std::numeric_limits<unsigned>::max()) return failure;
        const auto directory = entry.type == MTAR_TDIR;
        if (directory ? mtar_write_dir_header(&tar, entry.name) != MTAR_ESUCCESS : !entry.data || mtar_write_file_header(&tar, entry.name, static_cast<unsigned>(entry.size)) != MTAR_ESUCCESS || mtar_write_data(&tar, entry.data, static_cast<unsigned>(entry.size)) != MTAR_ESUCCESS) return failure;
    }
    return mtar_finalize(&tar) == MTAR_ESUCCESS ? writer.position : failure;
}
NCOMP_API int64_t ncomp_tar_extract(const void* input, size_t input_size, void* context, int32_t (*entry)(void*, const char*, uint32_t, uint64_t), int32_t (*data)(void*, const void*, size_t)) {
    if (!input || input_size > std::numeric_limits<unsigned>::max() || !entry || !data) return failure;
    TarMemory memory{{static_cast<const uint8_t*>(input), input_size}};
    mtar_t tar{};
    tar.read = tar_read;
    tar.seek = tar_seek;
    tar.stream = &memory;
    mtar_header_t header{};
    int64_t count{};
    for (;;) {
        const auto status = mtar_read_header(&tar, &header);
        if (status == MTAR_ENULLRECORD) return count;
        if (status != MTAR_ESUCCESS || entry(context, header.name, header.type, header.size) != 0) return failure;
        if (header.type == MTAR_TREG || header.type == 0) {
            std::array<uint8_t, 65536> buffer;
            unsigned remaining = header.size;
            while (remaining) {
                const auto chunk = std::min<unsigned>(remaining, buffer.size());
                if (mtar_read_data(&tar, buffer.data(), chunk) != MTAR_ESUCCESS || data(context, buffer.data(), chunk) != 0) return failure;
                remaining -= chunk;
            }
        }
        if (mtar_next(&tar) != MTAR_ESUCCESS) return failure;
        ++count;
    }
}
