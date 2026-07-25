module;
#include <vector>
#include <cstdint>
#include <span>
#include <optional>
#include <zlib.h>

export module utility.gzip.gzip_uncompress;

export {
namespace gzip_ns {
    class Decompressor {
    public:
        static std::optional<std::vector<uint8_t>> decompress(
            std::span<const uint8_t> input,
            size_t expectedSize = 0,
            int windowBits = 15) {
            if (input.empty()) return std::vector<uint8_t>{};

            std::vector<uint8_t> output(expectedSize == 0 ? input.size() * 4 : expectedSize);
            if (output.empty()) output.resize(256);

            z_stream stream{};
            stream.next_in = const_cast<Bytef*>(reinterpret_cast<const Bytef*>(input.data()));
            stream.avail_in = static_cast<uInt>(input.size());
            stream.next_out = reinterpret_cast<Bytef*>(output.data());
            stream.avail_out = static_cast<uInt>(output.size());

            if (inflateInit2(&stream, windowBits + 16) != Z_OK)
                return std::nullopt;

            int ret = Z_OK;
            while (true) {
                ret = inflate(&stream, Z_NO_FLUSH);
                if (ret == Z_STREAM_END) break;

                if (ret == Z_OK || ret == Z_BUF_ERROR) {
                    if (stream.avail_out == 0) {
                        const size_t current = stream.total_out;
                        output.resize(output.size() * 2);
                        stream.next_out = reinterpret_cast<Bytef*>(output.data() + current);
                        stream.avail_out = static_cast<uInt>(output.size() - current);
                    } else if (stream.avail_in == 0) {
                        break;
                    }
                } else {
                    inflateEnd(&stream);
                    return std::nullopt;
                }
            }

            inflateEnd(&stream);
            if (ret != Z_STREAM_END && stream.total_out == 0) return std::nullopt;
            output.resize(stream.total_out);
            return output;
        }
    };
}
}
