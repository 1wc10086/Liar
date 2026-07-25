module;
#include <cstddef>
#include <cstdint>
#include <optional>
#include <span>
#include <vector>

export module utility.compression.brotli.brotli_compress;

import utility.compression.brotli.brotli_core;

export namespace brotli_ns {

class Compressor {
public:
    [[nodiscard]] static std::optional<buffer_type> compress(view_type input, int32_t level = default_quality) {
        return compress(input, Options{.quality = level});
    }

    [[nodiscard]] static std::optional<buffer_type> compress(view_type input, const Options& options) {
        buffer_type output(compress_bound(input.size()));
        size_t written = output.size();
        if (!compress_to(output, written, input, options)) return std::nullopt;
        output.resize(written);
        return output;
    }
};

}
