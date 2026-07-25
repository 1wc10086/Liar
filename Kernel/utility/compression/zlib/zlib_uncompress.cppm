module;
#include <vector>
#include <cstdint>
#include <span>
#include <optional>
#include <cstring>
#include <zlib.h>
export module utility.zlib.zlib_uncompress;

export {
namespace zlib_ns {
    class Decompressor {
    public:
        static std::optional<std::vector<uint8_t>> decompress(std::span<const uint8_t> input, size_t expectedSize = 0) {
            if (input.empty()) return std::vector<uint8_t>{};
            std::vector<uint8_t> output(expectedSize == 0 ? input.size() * 4 : expectedSize);

            z_stream stream{};
            stream.next_in = const_cast<Bytef*>(input.data());
            stream.avail_in = static_cast<uInt>(input.size());
            stream.next_out = output.data();
            stream.avail_out = static_cast<uInt>(output.size());

            if (inflateInit(&stream) != Z_OK) return std::nullopt;

            int ret;
            while ((ret = inflate(&stream, Z_FINISH)) == Z_BUF_ERROR) {
                size_t newSize = output.size() * 2;
                output.resize(newSize);
                stream.next_out = output.data() + stream.total_out;
                stream.avail_out = static_cast<uInt>(output.size() - stream.total_out);
            }

            if (ret != Z_STREAM_END) {
                inflateEnd(&stream);
                return std::nullopt;
            }
            
            inflateEnd(&stream);
            output.resize(stream.total_out);
            return output;
        }
    };

    class PopCapDecompressor {
    public:
        static constexpr uint32_t kThFirst = 0xDEADFED4;

        static std::optional<std::vector<uint8_t>> decompress(std::span<const uint8_t> input, bool is64Bit = false) {
            size_t headerSize = is64Bit ? 16 : 8;
            if (input.size() < headerSize) return std::nullopt;

            uint32_t magic;
            std::memcpy(&magic, input.data(), 4);
            if (magic != kThFirst) return std::nullopt;

            uint32_t originalSize;
            if (is64Bit) {
                std::memcpy(&originalSize, input.data() + 8, 4);
            } else {
                std::memcpy(&originalSize, input.data() + 4, 4);
            }

            std::vector<uint8_t> output(originalSize);
            uLongf dstLen = static_cast<uLongf>(originalSize);
            int ret = uncompress(output.data(), &dstLen, input.data() + headerSize, static_cast<uLong>(input.size() - headerSize));
            
            if (ret != Z_OK) return std::nullopt;
            output.resize(dstLen);
            return output;
        }
    };
}
}
