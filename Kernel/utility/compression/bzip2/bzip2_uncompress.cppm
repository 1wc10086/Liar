module;
#include <vector>
#include <cstdint>
#include <span>
#include <optional>
#include <cstring>
#include "lib/bzip2/bzlib.h"
export module utility.bzip2.bzip2_uncompress;

export {
namespace bzip2_ns {
    class Decompressor {
    public:
        static std::optional<std::vector<uint8_t>> decompress(std::span<const uint8_t> input, bool small = false) {
            if (input.empty()) return std::vector<uint8_t>{};
            size_t maxOutputSize = input.size() * 4;
            std::vector<uint8_t> output(maxOutputSize);
            bz_stream stream{};
            stream.next_in = reinterpret_cast<char*>(const_cast<uint8_t*>(input.data()));
            stream.avail_in = static_cast<unsigned int>(input.size());
            stream.next_out = reinterpret_cast<char*>(output.data());
            stream.avail_out = static_cast<unsigned int>(output.size());
            if (BZ2_bzDecompressInit(&stream, 0, small ? 1 : 0) != BZ_OK) return std::nullopt;

            int ret;
            while (true) {
                ret = BZ2_bzDecompress(&stream);
                if (ret == BZ_STREAM_END) break;
                if (ret == BZ_OK) {
                    if (stream.avail_out == 0) {
                        size_t currentTotal = (static_cast<uint64_t>(stream.total_out_hi32) << 32) | stream.total_out_lo32;
                        output.resize(output.size() * 2);
                        stream.next_out = reinterpret_cast<char*>(output.data() + currentTotal);
                        stream.avail_out = static_cast<unsigned int>(output.size() - currentTotal);
                    } else if (stream.avail_in == 0) {
                        break;
                    }
                } else break;
            }
            BZ2_bzDecompressEnd(&stream);
            uint64_t totalOut = (static_cast<uint64_t>(stream.total_out_hi32) << 32) | stream.total_out_lo32;
            if (ret != BZ_STREAM_END && totalOut == 0) return std::nullopt;
            output.resize(static_cast<size_t>(totalOut));
            return output;
        }
    };
}
}
