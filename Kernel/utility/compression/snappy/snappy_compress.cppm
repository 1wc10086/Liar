module;
#include <vector>
#include <cstdint>
#include <span>
#include <optional>
#include <cstring>
#include "lib/snappy/snappy.h"
export module utility.snappy.snappy_compress;

export {
namespace snappy_ns {
    class Compressor {
    public:
        static std::optional<std::vector<uint8_t>> compress(std::span<const uint8_t> input) {
            if (input.empty()) return std::vector<uint8_t>{};
            std::vector<uint8_t> output(snappy::MaxCompressedLength(input.size()));
            size_t compressed_length;
            snappy::RawCompress(reinterpret_cast<const char*>(input.data()), input.size(), 
                                reinterpret_cast<char*>(output.data()), &compressed_length);
            output.resize(compressed_length);
            return output;
        }
    };
}
}
