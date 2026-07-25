module;
#include <vector>
#include <cstdint>
#include <span>
#include <optional>
#include <cstring>
#include <zlib.h>
export module utility.deflate.deflate_uncompress;

export {
namespace deflate_ns {
    class Decompressor {
    public:
        static std::optional<std::vector<uint8_t>> decompress(std::span<const uint8_t> input, size_t expectedSize = 0, int windowBits = 15) {
            if (input.empty()) return std::vector<uint8_t>{};
            std::vector<uint8_t> output(expectedSize == 0 ? input.size() * 4 : expectedSize);
            z_stream stream{};
            stream.next_in = const_cast<Bytef*>(input.data());
            stream.avail_in = static_cast<uInt>(input.size());
            stream.next_out = output.data();
            stream.avail_out = static_cast<uInt>(output.size());
            if (inflateInit2(&stream, windowBits) != Z_OK) return std::nullopt;

            int ret;
            while (true) {
                ret = inflate(&stream, Z_NO_FLUSH);
                if (ret == Z_STREAM_END) break;
                if (ret == Z_OK || ret == Z_BUF_ERROR) {
                    if (stream.avail_out == 0) {
                        size_t currentOut = stream.total_out;
                        output.resize(output.size() * 2);
                        stream.next_out = output.data() + currentOut;
                        stream.avail_out = static_cast<uInt>(output.size() - currentOut);
                    } else if (stream.avail_in == 0) {
                        break;
                    }
                } else break;
            }
            inflateEnd(&stream);
            if (ret != Z_STREAM_END && stream.total_out == 0) return std::nullopt;
            output.resize(stream.total_out);
            return output;
        }
    };
}
}
