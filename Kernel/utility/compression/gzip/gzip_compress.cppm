module;
#include <vector>
#include <cstdint>
#include <span>
#include <optional>
#include <zlib.h>

export module utility.gzip.gzip_compress;

export {
namespace gzip_ns {
    class Compressor {
    public:
        static std::optional<std::vector<uint8_t>> compress(
            std::span<const uint8_t> input,
            int level = 9,
            int windowBits = 15,
            int memoryLevel = 8,
            int strategy = Z_DEFAULT_STRATEGY) {
            if (input.empty()) return std::vector<uint8_t>{};

            size_t estimate = input.size() + (input.size() >> 12) + (input.size() >> 14) + 64 + 12;
            std::vector<uint8_t> output(estimate);

            z_stream stream{};
            stream.next_in = const_cast<Bytef*>(reinterpret_cast<const Bytef*>(input.data()));
            stream.avail_in = static_cast<uInt>(input.size());
            stream.next_out = reinterpret_cast<Bytef*>(output.data());
            stream.avail_out = static_cast<uInt>(output.size());

            if (deflateInit2(&stream, level, Z_DEFLATED, windowBits + 16, memoryLevel, strategy) != Z_OK)
                return std::nullopt;

            int ret = deflate(&stream, Z_FINISH);
            if (ret != Z_STREAM_END) {
                deflateEnd(&stream);
                return std::nullopt;
            }

            deflateEnd(&stream);
            output.resize(stream.total_out);
            return output;
        }
    };
}
}
