module;
#include <vector>
#include <cstdint>
#include <span>
#include <optional>
#include <cstring>
#include "lib/snappy/snappy.h"
export module utility.snappy.snappy_uncompress;

export {
namespace snappy_ns {
    class Decompressor {
    public:
        static std::optional<std::vector<uint8_t>> decompress(std::span<const uint8_t> input) {
            if (input.empty()) return std::vector<uint8_t>{};
            
            size_t uncompressed_length;
            if (!snappy::GetUncompressedLength(reinterpret_cast<const char*>(input.data()), input.size(), &uncompressed_length)) {
                return std::nullopt;
            }
            
            std::vector<uint8_t> output(uncompressed_length);
            if (!snappy::RawUncompress(reinterpret_cast<const char*>(input.data()), input.size(), reinterpret_cast<char*>(output.data()))) {
                return std::nullopt;
            }
            
            return output;
        }
    };
}
}
