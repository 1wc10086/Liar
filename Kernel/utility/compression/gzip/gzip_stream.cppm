module;
#include <cstdint>
#include <span>
#include <vector>
export module utility.gzip.gzip_stream;
import utility.zlib.zlib_stream;

export namespace gzip_ns {

class StreamCompressor {
public:
    explicit StreamCompressor(int level = 9) : zlc_(level, 15 + 16) {}
    [[nodiscard]] std::vector<uint8_t> feed(std::span<const uint8_t> in, bool finish = false) { return zlc_.feed(in, finish); }
private:
    zlib_ns::StreamCompressor zlc_;
};

class StreamDecompressor {
public:
    StreamDecompressor() : zld_(15 + 16) {}
    [[nodiscard]] std::vector<uint8_t> feed(std::span<const uint8_t> in) { return zld_.feed(in); }
    [[nodiscard]] bool done() const noexcept { return zld_.done(); }
private:
    zlib_ns::StreamDecompressor zld_;
};

}
