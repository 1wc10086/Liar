module;
#include <vector>
#include <cstdint>
#include <span>
#include <optional>
#include <cstring>
#include <zlib.h>
export module utility.zlib.zlib_compress;

export {
namespace zlib_ns {
    class Compressor {
    public:
        static std::optional<std::vector<uint8_t>> compress(std::span<const uint8_t> input, int level = 9) {
            if (input.empty()) return std::vector<uint8_t>{};
            size_t maxCompressedSize = compressBound(static_cast<uLong>(input.size()));
            std::vector<uint8_t> output(maxCompressedSize);
            
            z_stream stream{};
            stream.next_in = const_cast<Bytef*>(input.data());
            stream.avail_in = static_cast<uInt>(input.size());
            stream.next_out = output.data();
            stream.avail_out = static_cast<uInt>(output.size());

            if (deflateInit(&stream, level) != Z_OK) return std::nullopt;
            if (deflate(&stream, Z_FINISH) != Z_STREAM_END) {
                deflateEnd(&stream);
                return std::nullopt;
            }
            deflateEnd(&stream);
            output.resize(stream.total_out);
            return output;
        }
    };

    class PopCapCompressor {
    public:
        static constexpr uint32_t kThFirst = 0xDEADFED4;

        static std::optional<std::vector<uint8_t>> compress(std::span<const uint8_t> input, bool is64Bit = false, int level = 9) {
            size_t headerSize = is64Bit ? 16 : 8;
            size_t estimate = headerSize + compressBound(static_cast<uLong>(input.size()));
            std::vector<uint8_t> output(estimate);

            uint32_t magic = kThFirst;
            std::memcpy(output.data(), &magic, 4);

            uint32_t srcSz = static_cast<uint32_t>(input.size());
            if (is64Bit) {
                uint32_t zero = 0;
                std::memcpy(output.data() + 4, &zero, 4);
                std::memcpy(output.data() + 8, &srcSz, 4);
                std::memcpy(output.data() + 12, &zero, 4);
            } else {
                std::memcpy(output.data() + 4, &srcSz, 4);
            }

            z_stream stream{};
            stream.next_in = const_cast<Bytef*>(input.data());
            stream.avail_in = static_cast<uInt>(input.size());
            stream.next_out = output.data() + headerSize;
            stream.avail_out = static_cast<uInt>(output.size() - headerSize);

            if (deflateInit(&stream, level) != Z_OK) return std::nullopt;
            if (deflate(&stream, Z_FINISH) != Z_STREAM_END) {
                deflateEnd(&stream);
                return std::nullopt;
            }
            deflateEnd(&stream);

            output.resize(headerSize + stream.total_out);
            return output;
        }
    };
}
}
