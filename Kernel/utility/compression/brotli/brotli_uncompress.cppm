module;
#include <algorithm>
#include <cstddef>
#include <cstdint>
#include <optional>
#include <span>
#include <vector>

export module utility.compression.brotli.brotli_uncompress;

import utility.compression.brotli.brotli_core;

export namespace brotli_ns {

class Decompressor {
public:
    [[nodiscard]] static std::optional<buffer_type> decompress(view_type input, size_t expected_size = 0) {
        if (expected_size) return decompress_exact(input, expected_size);
        return decompress_stream(input);
    }

    [[nodiscard]] static std::optional<buffer_type> decompress_stream(view_type input, size_t expected_size = 0) {
        DStream stream;
        if (!stream) return std::nullopt;

        buffer_type output(std::max<size_t>(expected_size, 65536));
        ConstBufferCursor src{input.data(), input.size(), 0};
        size_t written = 0;

        for (;;) {
            if (written == output.size()) output.resize(output.empty() ? 65536 : output.size() * 2);
            BufferCursor dst{output.data(), output.size(), written};
            const auto before_src = src.pos;
            const auto before_dst = dst.pos;
            const auto ret = stream.decompress(dst, src);
            written = dst.pos;

            if (ret == DecoderResult::success) {
                output.resize(written);
                return output;
            }
            if (ret == DecoderResult::error || (src.pos == before_src && written == before_dst && ret != DecoderResult::needs_more_output)) return std::nullopt;
        }
    }

private:
    [[nodiscard]] static std::optional<buffer_type> decompress_exact(view_type input, size_t capacity) {
        buffer_type output(capacity);
        size_t written = output.size();
        const auto result = decompress_to(output, written, input);
        if (result != DecoderResult::success) return std::nullopt;
        output.resize(written);
        return output;
    }
};

}
