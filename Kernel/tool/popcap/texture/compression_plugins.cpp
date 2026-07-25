import utility.io;
import tool.shell.plugin_base;
import utility.zlib.zlib_compress;
import utility.zlib.zlib_uncompress;
import utility.zlib.zlib_stream;
import utility.deflate.deflate_compress;
import utility.deflate.deflate_uncompress;
import utility.gzip.gzip_compress;
import utility.gzip.gzip_uncompress;
import utility.bzip2.bzip2_compress;
import utility.bzip2.bzip2_uncompress;
import utility.bzip2.bzip2_stream;
import utility.lzma.lzma_compress;
import utility.lzma.lzma_uncompress;
import utility.snappy.snappy_compress;
import utility.snappy.snappy_uncompress;
import utility.lz4.lz4_compress;
import utility.lz4.lz4_uncompress;
#include <cstdint>
#include <cstring>
#include <string>
#include <fcntl.h>
#include <sys/stat.h>
#include <unistd.h>

namespace {

static constexpr size_t kStreamingThreshold = 4ULL * 1024 * 1024;
static constexpr size_t kChunkSize         = 256ULL * 1024;

PluginResult result(bool ok) { return ok ? PluginResult::ok() : PluginResult::fail(); }

template<typename Fn>
PluginResult fileTransform(const Args& a, Fn&& compress) {
    auto in = FileUtils::readFileBytes(a.get("InputFile"));
    if (in.empty()) return PluginResult::ok();
    auto out = compress(std::span<const uint8_t>(in));
    return result(out && FileUtils::writeFileBytes(a.get("OutputFile"), *out));
}

static bool writeAll(int fd, const uint8_t* p, size_t n) {
    while (n) {
        auto r = ::write(fd, p, n);
        if (r < 0) { if (errno == EINTR) continue; return false; }
        p += static_cast<size_t>(r);
        n -= static_cast<size_t>(r);
    }
    return true;
}

template<typename S>
PluginResult streamCompress(const std::string& inPath, const std::string& outPath, S&& stream) {
    auto [mapData, mapSize] = FileUtils::mmapReadFile(inPath);
    if (!mapData) return result(false);
    int fd = ::open(outPath.c_str(), O_CREAT | O_TRUNC | O_WRONLY | O_CLOEXEC, 0644);
    if (fd < 0) { FileUtils::munmapFile(mapData, mapSize); return result(false); }
    bool ok = true;
    size_t off = 0;
    while (off < mapSize) {
        size_t n = std::min(kChunkSize, mapSize - off);
        auto chunk = stream.feed({mapData + off, n}, off + n >= mapSize);
        if (!writeAll(fd, chunk.data(), chunk.size())) { ok = false; break; }
        off += n;
    }
    ::close(fd);
    FileUtils::munmapFile(mapData, mapSize);
    return result(ok);
}

template<typename S>
PluginResult streamDecompress(const std::string& inPath, const std::string& outPath, S&& stream) {
    auto [mapData, mapSize] = FileUtils::mmapReadFile(inPath);
    if (!mapData) return result(false);
    int fd = ::open(outPath.c_str(), O_CREAT | O_TRUNC | O_WRONLY | O_CLOEXEC, 0644);
    if (fd < 0) { FileUtils::munmapFile(mapData, mapSize); return result(false); }
    bool ok = true;
    size_t off = 0;
    while (off < mapSize) {
        size_t n = std::min(kChunkSize, mapSize - off);
        auto chunk = stream.feed({mapData + off, n});
        if (!writeAll(fd, chunk.data(), chunk.size())) { ok = false; break; }
        off += n;
    }
    ::close(fd);
    FileUtils::munmapFile(mapData, mapSize);
    return result(ok);
}

PluginResult zlibCompress(const Args& a) {
    auto path = a.get("InputFile"), outPath = a.get("OutputFile");
    int level = a.getInt("Level", 9);
    if (static_cast<size_t>(FileUtils::getFileSize(path)) > kStreamingThreshold)
        return streamCompress(path, outPath, zlib_ns::StreamCompressor(level));
    return fileTransform(a, [=](auto d){ return zlib_ns::Compressor::compress(d, level); });
}
PluginResult zlibDecompress(const Args& a) {
    auto path = a.get("InputFile"), outPath = a.get("OutputFile");
    size_t es = static_cast<size_t>(a.getInt("ExpectedSize", 0));
    if (static_cast<size_t>(FileUtils::getFileSize(path)) > kStreamingThreshold)
        return streamDecompress(path, outPath, zlib_ns::StreamDecompressor());
    return fileTransform(a, [=](auto d){ return zlib_ns::Decompressor::decompress(d, es); });
}

PluginResult deflateCompress(const Args& a) {
    auto path = a.get("InputFile"), outPath = a.get("OutputFile");
    int level = a.getInt("Level", 9);
    if (static_cast<size_t>(FileUtils::getFileSize(path)) > kStreamingThreshold)
        return streamCompress(path, outPath, zlib_ns::StreamCompressor(level));
    return fileTransform(a, [=](auto d){ return deflate_ns::Compressor::compress(d, level); });
}
PluginResult deflateDecompress(const Args& a) {
    auto path = a.get("InputFile"), outPath = a.get("OutputFile");
    size_t es = static_cast<size_t>(a.getInt("ExpectedSize", 0));
    if (static_cast<size_t>(FileUtils::getFileSize(path)) > kStreamingThreshold)
        return streamDecompress(path, outPath, zlib_ns::StreamDecompressor());
    return fileTransform(a, [=](auto d){ return deflate_ns::Decompressor::decompress(d, es); });
}

PluginResult gzipCompress(const Args& a) {
    auto path = a.get("InputFile"), outPath = a.get("OutputFile");
    int level = a.getInt("Level", 9);
    if (static_cast<size_t>(FileUtils::getFileSize(path)) > kStreamingThreshold)
        return streamCompress(path, outPath, zlib_ns::StreamCompressor(level, 15 + 16));
    return fileTransform(a, [=](auto d){ return gzip_ns::Compressor::compress(d, level); });
}
PluginResult gzipDecompress(const Args& a) {
    auto path = a.get("InputFile"), outPath = a.get("OutputFile");
    size_t es = static_cast<size_t>(a.getInt("ExpectedSize", 0));
    if (static_cast<size_t>(FileUtils::getFileSize(path)) > kStreamingThreshold)
        return streamDecompress(path, outPath, zlib_ns::StreamDecompressor(15 + 16));
    return fileTransform(a, [=](auto d){ return gzip_ns::Decompressor::decompress(d, es); });
}

PluginResult bzip2Compress(const Args& a) {
    auto path = a.get("InputFile"), outPath = a.get("OutputFile");
    int blk = a.getInt("BlockSize", 9), wf = a.getInt("WorkFactor", 0);
    if (static_cast<size_t>(FileUtils::getFileSize(path)) > kStreamingThreshold)
        return streamCompress(path, outPath, bzip2_ns::StreamCompressor(blk, wf));
    return fileTransform(a, [=](auto d){ return bzip2_ns::Compressor::compress(d, blk, wf); });
}
PluginResult bzip2Decompress(const Args& a) {
    auto path = a.get("InputFile"), outPath = a.get("OutputFile");
    bool small = a.getBool("Small", false);
    if (static_cast<size_t>(FileUtils::getFileSize(path)) > kStreamingThreshold)
        return streamDecompress(path, outPath, bzip2_ns::StreamDecompressor(small));
    return fileTransform(a, [=](auto d){ return bzip2_ns::Decompressor::decompress(d, small); });
}

template<typename Fn>
PluginResult fileTransformWithArgs(const Args& a, Fn&& compress) {
    auto in = FileUtils::readFileBytes(a.get("InputFile"));
    if (in.empty()) return PluginResult::ok();
    auto out = compress(in);
    return result(out && FileUtils::writeFileBytes(a.get("OutputFile"), *out));
}

struct Registrar {
    Registrar() {
        auto& f = PluginFactory::get();

        f.reg("util.zlib.compress",       [](const Args& a){ return zlibCompress(a); });
        f.reg("util.zlib.uncompress",     [](const Args& a){ return zlibDecompress(a); });
        f.reg("util.deflate.compress",    [](const Args& a){ return deflateCompress(a); });
        f.reg("util.deflate.uncompress",  [](const Args& a){ return deflateDecompress(a); });
        f.reg("util.gzip.compress",       [](const Args& a){ return gzipCompress(a); });
        f.reg("util.gzip.uncompress",     [](const Args& a){ return gzipDecompress(a); });
        f.reg("util.bzip2.compress",      [](const Args& a){ return bzip2Compress(a); });
        f.reg("util.bzip2.uncompress",    [](const Args& a){ return bzip2Decompress(a); });
        f.reg("util.lzma.compress", [](const Args& a){ return fileTransform(a, [=](auto d){ return lzma_ns::Compressor::compress(d, a.getInt("Level", 9)); }); });
        f.reg("util.lzma.uncompress", [](const Args& a){ return fileTransform(a, [](auto d){ return lzma_ns::Decompressor::decompress(d); }); });
        f.reg("util.snappy.compress", [](const Args& a){ return fileTransform(a, [](auto d){ return snappy_ns::Compressor::compress(d); }); });
        f.reg("util.snappy.uncompress", [](const Args& a){ return fileTransform(a, [](auto d){ return snappy_ns::Decompressor::decompress(d); }); });
        f.reg("util.lz4.compress", [](const Args& a){ return fileTransform(a, [](auto d){ return lz4_ns::Compressor::compress(d); }); });
        f.reg("util.lz4.uncompress", [](const Args& a){
            size_t es = static_cast<size_t>(a.getInt("ExpectedSize", 0));
            return fileTransform(a, [es](auto d){ return lz4_ns::Decompressor::decompress(d, es); });
        });
        f.reg("util.popcapzlib.compress", [](const Args& a){
            bool is64 = a.getBool("Is64Bit");
            return fileTransform(a, [=](auto d){ return zlib_ns::PopCapCompressor::compress(d, is64, a.getInt("Level", 9)); });
        });
        f.reg("util.popcapzlib.decompress", [](const Args& a){
            bool is64 = a.getBool("Is64Bit");
            return fileTransform(a, [is64](auto d){ return zlib_ns::PopCapDecompressor::decompress(d, is64); });
        });
    }
};

[[maybe_unused]] const Registrar registrar;

}
