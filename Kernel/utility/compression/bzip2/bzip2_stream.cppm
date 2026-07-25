module;
#include <cstdint>
#include <cstring>
#include <span>
#include <stdexcept>
#include <vector>
#include "lib/bzip2/bzlib.h"
export module utility.bzip2.bzip2_stream;

export namespace bzip2_ns {

class StreamCompressor {
public:
    explicit StreamCompressor(int blockSize = 9, int workFactor = 0) {
        std::memset(&stream_, 0, sizeof(stream_));
        if (BZ2_bzCompressInit(&stream_, blockSize, 0, workFactor) != BZ_OK)
            throw std::runtime_error("bzip2 stream compress init failed");
        init_ = true;
    }
    ~StreamCompressor() { if (init_) BZ2_bzCompressEnd(&stream_); }
    StreamCompressor(const StreamCompressor&) = delete;
    StreamCompressor& operator=(const StreamCompressor&) = delete;

    [[nodiscard]] std::vector<uint8_t> feed(std::span<const uint8_t> input, bool finish = false) {
        stream_.next_in = reinterpret_cast<char*>(const_cast<uint8_t*>(input.data()));
        stream_.avail_in = static_cast<unsigned int>(input.size());
        std::vector<uint8_t> out(input.size() / 2 + 4096);
        for (;;) {
            stream_.next_out = reinterpret_cast<char*>(out.data() + offset_);
            stream_.avail_out = static_cast<unsigned int>(out.size() - offset_);
            int ret = BZ2_bzCompress(&stream_, finish ? BZ_FINISH : BZ_RUN);
            if (ret == BZ_STREAM_END || ret == BZ_RUN_OK || ret == BZ_FINISH_OK) {
                uint64_t total = (static_cast<uint64_t>(stream_.total_out_hi32) << 32) | stream_.total_out_lo32;
                offset_ = static_cast<size_t>(total) - baseTotal_;
                if (ret == BZ_STREAM_END) break;
                if (stream_.avail_out != 0) break;
                out.resize(out.size() * 2);
            } else {
                throw std::runtime_error("bzip2 compress stream error");
            }
        }
        out.resize(offset_);
        baseTotal_ = (static_cast<uint64_t>(stream_.total_out_hi32) << 32) | stream_.total_out_lo32;
        offset_ = 0;
        return out;
    }

private:
    bz_stream stream_{};
    bool init_ = false;
    size_t baseTotal_ = 0;
    size_t offset_ = 0;
};

class StreamDecompressor {
public:
    explicit StreamDecompressor(bool small = false) {
        std::memset(&stream_, 0, sizeof(stream_));
        if (BZ2_bzDecompressInit(&stream_, 0, small ? 1 : 0) != BZ_OK)
            throw std::runtime_error("bzip2 stream decompress init failed");
        init_ = true;
    }
    ~StreamDecompressor() { if (init_) BZ2_bzDecompressEnd(&stream_); }
    StreamDecompressor(const StreamDecompressor&) = delete;
    StreamDecompressor& operator=(const StreamDecompressor&) = delete;

    [[nodiscard]] std::vector<uint8_t> feed(std::span<const uint8_t> input) {
        stream_.next_in = reinterpret_cast<char*>(const_cast<uint8_t*>(input.data()));
        stream_.avail_in = static_cast<unsigned int>(input.size());
        std::vector<uint8_t> out(16384);
        for (;;) {
            stream_.next_out = reinterpret_cast<char*>(out.data() + offset_);
            stream_.avail_out = static_cast<unsigned int>(out.size() - offset_);
            int ret = BZ2_bzDecompress(&stream_);
            if (ret == BZ_STREAM_END || ret == BZ_OK) {
                uint64_t total = (static_cast<uint64_t>(stream_.total_out_hi32) << 32) | stream_.total_out_lo32;
                offset_ = static_cast<size_t>(total) - baseTotal_;
                if (ret == BZ_STREAM_END || stream_.avail_out != 0) break;
                out.resize(out.size() * 2);
            } else {
                throw std::runtime_error("bzip2 decompress stream error");
            }
        }
        out.resize(offset_);
        baseTotal_ = (static_cast<uint64_t>(stream_.total_out_hi32) << 32) | stream_.total_out_lo32;
        offset_ = 0;
        return out;
    }

private:
    bz_stream stream_{};
    bool init_ = false;
    size_t baseTotal_ = 0;
    size_t offset_ = 0;
};

}
