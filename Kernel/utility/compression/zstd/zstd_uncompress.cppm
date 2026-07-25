module;
#include <algorithm>
#include <cstddef>
#include <cstdint>
#include <limits>
#include <optional>
#include <span>
#include <vector>

export module utility.compression.zstd.zstd_uncompress;

import utility.compression.zstd.zstd_core;

export namespace zstd_ns {

class Decompressor {
public:
    [[nodiscard]] static std::optional<buffer_type> decompress(view_type input, size_t expected_size = 0) {
        if (!expected_size) {
            const auto size = frame_content_size(input);
            if (size != 0 && size != content_size_error && size != content_size_unknown && size <= std::numeric_limits<size_t>::max()) expected_size = static_cast<size_t>(size);
        }

        if (expected_size) return decompress_exact(input, expected_size);

        size_t capacity = std::max<size_t>(input.size() * 4, 1024);
        for (int32_t i = 0; i < 24; ++i) {
            if (auto out = decompress_exact(input, capacity)) return out;
            capacity *= 2;
        }
        return std::nullopt;
    }

    [[nodiscard]] static std::optional<buffer_type> decompress_using_dict(view_type input, view_type dict, size_t expected_size = 0) {
        if (!expected_size) {
            const auto size = frame_content_size(input);
            if (size != 0 && size != content_size_error && size != content_size_unknown && size <= std::numeric_limits<size_t>::max()) expected_size = static_cast<size_t>(size);
        }
        if (!expected_size) return std::nullopt;
        buffer_type output(expected_size);
        const auto written = decompress_using_dict_to(output, input, dict);
        if (is_error(written)) return std::nullopt;
        output.resize(written);
        return output;
    }

private:
    [[nodiscard]] static std::optional<buffer_type> decompress_exact(view_type input, size_t capacity) {
        buffer_type output(capacity);
        const auto written = decompress_to(output, input);
        if (is_error(written)) return std::nullopt;
        output.resize(written);
        return output;
    }
};

}
