module;
#include <cstdint>
#include <cstring>
#include <span>
#include <stdexcept>
#include <vector>
#include <zlib.h>
export module utility.zlib.zlib_stream;

export namespace zlib_ns {

class StreamCompressor {
public:
    explicit StreamCompressor(int level = 9, int windowBits = 15, int memLevel = 8, int strategy = Z_DEFAULT_STRATEGY) {
        std::memset(&stream_, 0, sizeof(stream_));
        if (deflateInit2(&stream_, level, Z_DEFLATED, windowBits, memLevel, strategy) != Z_OK)
            throw std::runtime_error("zlib stream compress init failed");
        init_ = true;
    }
    ~StreamCompressor() { if (init_) deflateEnd(&stream_); }
    StreamCompressor(const StreamCompressor&) = delete;
    StreamCompressor& operator=(const StreamCompressor&) = delete;

    [[nodiscard]] std::vector<uint8_t> feed(std::span<const uint8_t> input, bool finish = false) {
        stream_.next_in = const_cast<uint8_t*>(input.data());
        stream_.avail_in = static_cast<uInt>(input.size());
        std::vector<uint8_t> out(input.size() / 2 + 4096);
        for (;;) {
            stream_.next_out = out.data() + stream_.total_out - saved_;
            stream_.avail_out = static_cast<uInt>(out.size() - (stream_.total_out - saved_));
            int ret = deflate(&stream_, finish ? Z_FINISH : Z_NO_FLUSH);
            if (ret == Z_STREAM_ERROR) throw std::runtime_error("zlib compress stream error");
            if (stream_.avail_out != 0) break;
            out.resize(out.size() * 2);
        }
        size_t produced = stream_.total_out - saved_;
        saved_ = stream_.total_out;
        out.resize(produced);
        return out;
    }

private:
    z_stream stream_{};
    bool init_ = false;
    size_t saved_ = 0;
};

class StreamDecompressor {
public:
    explicit StreamDecompressor(int windowBits = 15) {
        std::memset(&stream_, 0, sizeof(stream_));
        if (inflateInit2(&stream_, windowBits) != Z_OK)
            throw std::runtime_error("zlib stream decompress init failed");
        init_ = true;
    }
    ~StreamDecompressor() { if (init_) inflateEnd(&stream_); }
    StreamDecompressor(const StreamDecompressor&) = delete;
    StreamDecompressor& operator=(const StreamDecompressor&) = delete;

    [[nodiscard]] std::vector<uint8_t> feed(std::span<const uint8_t> input) {
        stream_.next_in = const_cast<uint8_t*>(input.data());
        stream_.avail_in = static_cast<uInt>(input.size());
        std::vector<uint8_t> out(16384);
        for (;;) {
            stream_.next_out = out.data() + stream_.total_out - saved_;
            stream_.avail_out = static_cast<uInt>(out.size() - (stream_.total_out - saved_));
            int ret = inflate(&stream_, Z_NO_FLUSH);
            if (ret == Z_STREAM_END || ret == Z_BUF_ERROR || ret == Z_OK) {
                if (stream_.avail_out != 0) break;
                out.resize(out.size() * 2);
            } else {
                throw std::runtime_error("zlib decompress stream error");
            }
        }
        size_t produced = stream_.total_out - saved_;
        saved_ = stream_.total_out;
        out.resize(produced);
        return out;
    }

    [[nodiscard]] bool done() const noexcept { return stream_.avail_in == 0; }

private:
    z_stream stream_{};
    bool init_ = false;
    size_t saved_ = 0;
};

}
