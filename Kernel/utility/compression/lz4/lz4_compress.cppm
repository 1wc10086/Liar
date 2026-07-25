module;
#include <vector>
#include <cstdint>
#include <span>
#include <optional>
#include <cstring>
#include "lib/etcpak/lz4/lz4.h"
export module utility.lz4.lz4_compress;

export {
namespace lz4_ns {
    class Compressor {
    public:
        static std::optional<std::vector<uint8_t>> compress(
            std::span<const uint8_t> input)
        {
            if (input.empty()) return std::vector<uint8_t>{};

            const int srcSize = static_cast<int>(input.size());
            const int maxDstSize = LZ4_compressBound(srcSize);
            if (maxDstSize <= 0) return std::nullopt;

            std::vector<uint8_t> output(static_cast<size_t>(maxDstSize));

            int compressedSize = 0;
                compressedSize = LZ4_compress_default(
                    reinterpret_cast<const char*>(input.data()),
                    reinterpret_cast<char*>(output.data()),
                    srcSize, maxDstSize);

            if (compressedSize <= 0) return std::nullopt;

            output.resize(static_cast<size_t>(compressedSize));
            return output;
        }
    };
}
}
