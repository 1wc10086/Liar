module;
#include <vector>
#include <cstdint>
#include <span>
#include <optional>
#include <cstring>
#include "lib/etcpak/lz4/lz4.h"
export module utility.lz4.lz4_uncompress;

export {
namespace lz4_ns {
    class Decompressor {
    public:
        static std::optional<std::vector<uint8_t>> decompress(
            std::span<const uint8_t> input,
            size_t expectedSize)
        {
            if (input.empty()) return std::vector<uint8_t>{};
            if (expectedSize == 0) return std::nullopt;

            std::vector<uint8_t> output(expectedSize);

            const int ret = LZ4_decompress_safe(
                reinterpret_cast<const char*>(input.data()),
                reinterpret_cast<char*>(output.data()),
                static_cast<int>(input.size()),
                static_cast<int>(expectedSize));

            if (ret < 0) return std::nullopt;

            output.resize(static_cast<size_t>(ret));
            return output;
        }
    };
}
}
