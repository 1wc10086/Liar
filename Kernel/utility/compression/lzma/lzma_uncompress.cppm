module;
#include <vector>
#include <cstdint>
#include <span>
#include <optional>
#include <cstring>
#include "lib/lzma/LzmaLib.h"
export module utility.lzma.lzma_uncompress;

export {
namespace lzma_ns {
    class Decompressor {
    public:
        static std::optional<std::vector<uint8_t>> decompress(std::span<const uint8_t> input) {
            const size_t propsSize = LZMA_PROPS_SIZE;
            const size_t headerSz = propsSize + sizeof(uint64_t);
            if (input.size() < headerSz) return std::nullopt;

            uint64_t rawSize;
            std::memcpy(&rawSize, input.data() + propsSize, sizeof(uint64_t));
            if (rawSize == static_cast<uint64_t>(-1)) return std::nullopt;

            std::vector<uint8_t> output(static_cast<size_t>(rawSize));
            size_t outLen = static_cast<size_t>(rawSize);
            size_t payloadLen = input.size() - headerSz;

            int res = LzmaUncompress(output.data(), &outLen, input.data() + headerSz, &payloadLen, input.data(), propsSize);
            if (res != SZ_OK || outLen != static_cast<size_t>(rawSize)) return std::nullopt;

            return output;
        }
    };
}
}
