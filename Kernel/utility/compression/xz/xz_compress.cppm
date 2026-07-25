module;
#include <optional>
#include <vector>

export module utility.compression.xz.xz_compress;

import utility.compression.xz.xz_core;

export namespace xz_ns {

class Compressor {
public:
    [[nodiscard]] static std::optional<buffer_type> compress(view_type input, uint32_t preset = preset_default, uint32_t check = check_crc64) {
        buffer_type output(compress_bound(input));
        size_t written = 0;
        const auto code = compress_to(output, written, input, Options{preset, check});
        if (is_error(code)) return std::nullopt;
        output.resize(written);
        return output;
    }
};

}
