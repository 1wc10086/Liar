module;
#include <algorithm>
#include <cstddef>
#include <cstdint>
#include <limits>
#include <optional>
#include <vector>

export module utility.compression.xz.xz_uncompress;

import utility.compression.xz.xz_core;

export namespace xz_ns {

class Decompressor {
public:
    [[nodiscard]] static std::optional<buffer_type> decompress(view_type input, size_t expected_size = 0, uint64_t memlimit = unlimited_memory, uint32_t flags = decoder_concatenated) {
        if (expected_size) return decompress_exact(input, expected_size, memlimit, flags);

        size_t capacity = std::max<size_t>(input.size() > std::numeric_limits<size_t>::max() / 4 ? std::numeric_limits<size_t>::max() : input.size() * 4, 65536);
        for (int32_t i = 0; i < 24 && capacity <= std::numeric_limits<size_t>::max() / 2; ++i) {
            if (auto output = decompress_exact(input, capacity, memlimit, flags)) return output;
            capacity *= 2;
        }
        return std::nullopt;
    }

private:
    [[nodiscard]] static std::optional<buffer_type> decompress_exact(view_type input, size_t capacity, uint64_t memlimit, uint32_t flags) {
        buffer_type output(capacity);
        size_t written = 0;
        size_t consumed = 0;
        const auto code = decompress_to(output, written, input, consumed, memlimit, flags);
        if (code != Code::ok || consumed != input.size()) return std::nullopt;
        output.resize(written);
        return output;
    }
};

}
