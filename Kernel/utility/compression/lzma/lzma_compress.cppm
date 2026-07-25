module;
#include <vector>
#include <cstdint>
#include <span>
#include <optional>
#include <cstring>
#include "lib/lzma/LzmaLib.h"
export module utility.lzma.lzma_compress;

export {
namespace lzma_ns {
    class Compressor {
    public:
        static std::optional<std::vector<uint8_t>> compress(std::span<const uint8_t> input, int level = 9) {
            if (input.empty()) return std::vector<uint8_t>{};
            const size_t propsSize = LZMA_PROPS_SIZE; 
            const size_t headerSz = propsSize + sizeof(uint64_t);
            size_t estimate = headerSz + input.size() + (input.size() >> 2) + 256;
            
            std::vector<uint8_t> output(estimate);
            uint8_t* propsPtr = output.data();
            uint8_t* rawSizePtr = output.data() + propsSize;
            uint8_t* dstPtr = output.data() + headerSz;
            size_t destLen = output.size() - headerSz;
            size_t propSize = propsSize;

            uint64_t rawSize = static_cast<uint64_t>(input.size());
            std::memcpy(rawSizePtr, &rawSize, sizeof(rawSize));

            int res = LzmaCompress(dstPtr, &destLen, input.data(), input.size(), propsPtr, &propSize, level, 0, -1, -1, -1, -1, -1);
            if (res != SZ_OK) return std::nullopt;

            output.resize(headerSz + destLen);
            return output;
        }
    };
}
}
