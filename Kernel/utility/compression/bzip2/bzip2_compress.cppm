module;
#include <vector>
#include <cstdint>
#include <span>
#include <optional>
#include <cstring>
#include "lib/bzip2/bzlib.h"
export module utility.bzip2.bzip2_compress;

export {
namespace bzip2_ns {
    class Compressor {
    public:
        static std::optional<std::vector<uint8_t>> compress(std::span<const uint8_t> input, int blockSize = 9, int workFactor = 0) {
            if (input.empty()) return std::vector<uint8_t>{};
            size_t maxOutputSize = static_cast<size_t>(input.size() * 1.1) + 1024;
            std::vector<uint8_t> output(maxOutputSize);
            bz_stream stream{};
            stream.next_in = reinterpret_cast<char*>(const_cast<uint8_t*>(input.data()));
            stream.avail_in = static_cast<unsigned int>(input.size());
            stream.next_out = reinterpret_cast<char*>(output.data());
            stream.avail_out = static_cast<unsigned int>(output.size());
            if (BZ2_bzCompressInit(&stream, blockSize, 0, workFactor) != BZ_OK) return std::nullopt;
            if (BZ2_bzCompress(&stream, BZ_FINISH) != BZ_STREAM_END) {
                BZ2_bzCompressEnd(&stream);
                return std::nullopt;
            }
            BZ2_bzCompressEnd(&stream);
            uint64_t totalOut = (static_cast<uint64_t>(stream.total_out_hi32) << 32) | stream.total_out_lo32;
            output.resize(static_cast<size_t>(totalOut));
            return output;
        }
    };
}
}
