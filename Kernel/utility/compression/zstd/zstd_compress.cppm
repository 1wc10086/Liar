module;
#include <cstddef>
#include <cstdint>
#include <optional>
#include <span>
#include <vector>

export module utility.compression.zstd.zstd_compress;

import utility.compression.zstd.zstd_core;

export namespace zstd_ns {

class Compressor {
public:
    [[nodiscard]] static std::optional<buffer_type> compress(view_type input, int32_t level = 3) {
        buffer_type output(compress_bound(input.size()));
        const auto written = compress_to(output, input, level);
        if (is_error(written)) return std::nullopt;
        output.resize(written);
        return output;
    }

    [[nodiscard]] static std::optional<buffer_type> compress_using_dict(view_type input, view_type dict, int32_t level = 3) {
        buffer_type output(compress_bound(input.size()));
        const auto written = compress_using_dict_to(output, input, dict, level);
        if (is_error(written)) return std::nullopt;
        output.resize(written);
        return output;
    }

    [[nodiscard]] static std::optional<buffer_type> compress_advanced(view_type input, const AdvancedOptions& options = {}) {
        buffer_type output(compress_bound(input.size()));
        const auto written = compress_advanced_to(output, input, options);
        if (is_error(written)) return std::nullopt;
        output.resize(written);
        return output;
    }
};

}
