module;
#include <algorithm>
#include <array>
#include <cctype>
#include <cstddef>
#include <cerrno>
#include <cstdint>
#include <cstring>
#include <filesystem>
#include <fstream>
#include <functional>
#include <span>
#include <string>
#include <string_view>
#include <system_error>
#include <utility>
#include <vector>
#include <fcntl.h>
#include <sys/mman.h>
#include <sys/stat.h>
#include <sys/uio.h>
#include <unistd.h>

module utility.io.fs;
import utility.io.concurrent;
import utility.io.cache;

namespace {
    bool writeAll(int fd, const uint8_t* p, size_t n) noexcept {
        while (n) {
            auto r = ::write(fd, p, n);
            if (r < 0) { if (errno == EINTR) continue; return false; }
            p += static_cast<size_t>(r);
            n -= static_cast<size_t>(r);
        }
        return true;
    }

    bool endsWithIC(std::string_view s, std::string_view x) noexcept {
        if (x.empty()) return true;
        if (s.size() < x.size()) return false;
        auto b = s.end() - static_cast<std::ptrdiff_t>(x.size());
        return std::equal(b, s.end(), x.begin(), x.end(), [](unsigned char a, unsigned char b) { return std::tolower(a) == std::tolower(b); });
    }

    void ensureParent(const std::string& path) {
        auto p = std::filesystem::path(path).parent_path();
        if (!p.empty()) getGlobalDirCache().ensure(p.string());
    }
}

std::vector<uint8_t> FileUtils::readFileBytes(const std::string& path) {
    auto [p, n] = mmapReadFile(path);
    if (p) {
        std::vector<uint8_t> v(p, p + n);
        munmapFile(p, n);
        return v;
    }
    std::ifstream in(path, std::ios::binary | std::ios::ate);
    if (!in) return {};
    auto len = in.tellg();
    if (len <= 0) return {};
    std::vector<uint8_t> v(static_cast<size_t>(len));
    in.seekg(0);
    in.read(reinterpret_cast<char*>(v.data()), static_cast<std::streamsize>(v.size()));
    return in ? v : std::vector<uint8_t>{};
}

bool FileUtils::writeFileBytes(const std::string& path, std::span<const uint8_t> data) {
    ensureParent(path);
    size_t sz = data.size();
    if (sz >= 1024 * 1024) {
        int fd = ::open(path.c_str(), O_RDWR | O_CREAT | O_TRUNC | O_CLOEXEC, 0644);
        if (fd < 0) return false;
        if (::ftruncate(fd, static_cast<off_t>(sz)) != 0) { ::close(fd); return false; }
        void* p = ::mmap(nullptr, sz, PROT_READ | PROT_WRITE, MAP_SHARED, fd, 0);
        if (p == MAP_FAILED) { ::close(fd); return false; }
        std::memcpy(p, data.data(), sz);
        ::msync(p, sz, MS_SYNC);
        ::munmap(p, sz);
        ::close(fd);
        return true;
    }
    int fd = ::open(path.c_str(), O_CREAT | O_TRUNC | O_WRONLY | O_CLOEXEC, 0644);
    if (fd < 0) return false;
    bool ok = writeAll(fd, data.data(), sz);
    ok = (::close(fd) == 0) && ok;
    return ok;
}

bool FileUtils::appendFileBytes(const std::string& path, std::span<const uint8_t> data) {
    ensureParent(path);
    const int fd = ::open(path.c_str(), O_CREAT | O_APPEND | O_WRONLY | O_CLOEXEC, 0644);
    if (fd < 0) return false;
    const bool ok = writeAll(fd, data.data(), data.size());
    return ::close(fd) == 0 && ok;
}

std::string FileUtils::readTextFile(const std::string& path) {
    auto v = readFileBytes(path);
    return {reinterpret_cast<const char*>(v.data()), v.size()};
}

bool FileUtils::writeTextFile(const std::string& path, std::string_view content) {
    return writeFileBytes(path, {reinterpret_cast<const uint8_t*>(content.data()), content.size()});
}

bool FileUtils::appendTextFile(const std::string& path, std::string_view content) {
    return appendFileBytes(path, {reinterpret_cast<const uint8_t*>(content.data()), content.size()});
}

void FileUtils::posixWrite(const std::string& path, const void* data, size_t size) {
    writeFileBytes(path, {static_cast<const uint8_t*>(data), size});
}

void FileUtils::posixWriteV(const std::string& path, const void* hdr, size_t hdrLen, const void* body, size_t bodyLen) {
    ensureParent(path);
    int fd = ::open(path.c_str(), O_CREAT | O_TRUNC | O_WRONLY | O_CLOEXEC, 0644);
    if (fd < 0) return;
    iovec v[2]{{const_cast<void*>(hdr), hdrLen}, {const_cast<void*>(body), bodyLen}};
    while (::writev(fd, v, 2) < 0 && errno == EINTR) {}
    ::close(fd);
}

bool FileUtils::posixReadInto(const std::string& path, uint8_t* buf, size_t size, int64_t fileOffset) noexcept {
    int fd = ::open(path.c_str(), O_RDONLY | O_CLOEXEC);
    if (fd < 0) return false;
    size_t done = 0;
    while (done < size) {
        auto r = ::pread(fd, buf + done, size - done, static_cast<off_t>(fileOffset) + static_cast<off_t>(done));
        if (r < 0) { if (errno == EINTR) continue; ::close(fd); return false; }
        if (r == 0) break;
        done += static_cast<size_t>(r);
    }
    ::close(fd);
    return done == size;
}

int64_t FileUtils::posixFileSize(const std::string& path) noexcept {
    struct stat st{};
    return ::stat(path.c_str(), &st) == 0 ? static_cast<int64_t>(st.st_size) : -1;
}

std::pair<const uint8_t*, size_t> FileUtils::mmapReadFile(const std::string& path) {
    int fd = ::open(path.c_str(), O_RDONLY | O_CLOEXEC);
    if (fd < 0) return {nullptr, 0};
    struct stat st{};
    if (::fstat(fd, &st) != 0 || st.st_size <= 0) { ::close(fd); return {nullptr, 0}; }
    size_t sz = static_cast<size_t>(st.st_size);
    void* p = ::mmap(nullptr, sz, PROT_READ, MAP_PRIVATE, fd, 0);
    ::close(fd);
    if (p == MAP_FAILED) return {nullptr, 0};
    ::madvise(p, sz, MADV_SEQUENTIAL);
    return {static_cast<const uint8_t*>(p), sz};
}

void FileUtils::munmapFile(const uint8_t* addr, size_t length) {
    if (addr && length) ::munmap(const_cast<uint8_t*>(addr), length);
}

bool FileUtils::streamReadFile(const std::string& path, size_t chunkSize, std::function<bool(const uint8_t*, size_t)> cb) {
    std::ifstream in(path, std::ios::binary);
    if (!in) return false;
    chunkSize = std::max<size_t>(chunkSize, 4096);
    std::vector<uint8_t> buf(chunkSize);
    while (in) {
        in.read(reinterpret_cast<char*>(buf.data()), static_cast<std::streamsize>(buf.size()));
        auto n = static_cast<size_t>(in.gcount());
        if (n && !cb(buf.data(), n)) return false;
    }
    return true;
}

std::vector<std::vector<uint8_t>> FileUtils::batchReadFiles(const std::vector<std::string>& paths) {
    std::vector<std::vector<uint8_t>> out(paths.size());
    parallelForBatched(paths.size(), 64, [&](size_t i) { out[i] = readFileBytes(paths[i]); });
    return out;
}

bool FileUtils::createFile(const std::string& path) { ensureParent(path); std::ofstream f(path, std::ios::app | std::ios::binary); return static_cast<bool>(f); }
bool FileUtils::deleteFile(const std::string& path) { std::error_code ec; return std::filesystem::remove(path, ec); }
bool FileUtils::deleteDirectory(const std::string& path) { std::error_code ec; return std::filesystem::remove_all(path, ec) > 0; }
bool FileUtils::copyFile(const std::string& src, const std::string& dst) { ensureParent(dst); std::error_code ec; return std::filesystem::copy_file(src, dst, std::filesystem::copy_options::overwrite_existing, ec); }
bool FileUtils::moveFile(const std::string& src, const std::string& dst) { ensureParent(dst); std::error_code ec; std::filesystem::rename(src, dst, ec); if (!ec) return true; return copyFile(src, dst) && deleteFile(src); }
bool FileUtils::truncateFile(const std::string& path) { ensureParent(path); std::ofstream f(path, std::ios::binary | std::ios::trunc); return static_cast<bool>(f); }
int64_t FileUtils::getFileSize(const std::string& path) { std::error_code ec; auto n = std::filesystem::file_size(path, ec); return ec ? -1 : static_cast<int64_t>(n); }
int64_t FileUtils::getFileModifiedTime(const std::string& path) { struct stat st{}; return ::stat(path.c_str(), &st) == 0 ? static_cast<int64_t>(st.st_mtime) : -1; }
int FileUtils::getFilePermissions(const std::string& path) { struct stat st{}; return ::stat(path.c_str(), &st) == 0 ? static_cast<int>(st.st_mode & 0777) : -1; }
bool FileUtils::setFilePermissions(const std::string& path, int mode) { return ::chmod(path.c_str(), static_cast<mode_t>(mode)) == 0; }
std::string FileUtils::getFileExtension(const std::string& path) { return std::filesystem::path(path).extension().string(); }
std::string FileUtils::changeFileExtension(const std::string& path, const std::string& ext) { auto p = std::filesystem::path(path); p.replace_extension(ext); return p.string(); }
std::string FileUtils::getFileNameWithoutExtension(const std::string& path) { return std::filesystem::path(path).stem().string(); }
std::string FileUtils::getFileName(const std::string& path) { return std::filesystem::path(path).filename().string(); }
std::string FileUtils::getParentDirectory(const std::string& path) { return std::filesystem::path(path).parent_path().string(); }
std::string FileUtils::getAbsolutePath(const std::string& path) { std::error_code ec; auto p = std::filesystem::absolute(path, ec); return ec ? path : p.string(); }
std::string FileUtils::joinPath(const std::string& a, const std::string& b) { return (std::filesystem::path(a) / b).string(); }
void FileUtils::normalizePath(std::string& s) { std::replace(s.begin(), s.end(), '\\', '/'); }
std::string_view FileUtils::getParentDir(std::string_view p) { auto i = p.find_last_of("/\\"); return i == std::string_view::npos ? std::string_view{} : p.substr(0, i); }
std::string FileUtils::normalizeFsPath(const std::string& path) { auto s = std::filesystem::path(path).lexically_normal().string(); normalizePath(s); return s; }
bool FileUtils::fileExists(const std::string& path) { std::error_code ec; return std::filesystem::exists(path, ec); }
bool FileUtils::isDirectory(const std::string& path) { std::error_code ec; return std::filesystem::is_directory(path, ec); }
bool FileUtils::isRegularFile(const std::string& path) { std::error_code ec; return std::filesystem::is_regular_file(path, ec); }
bool FileUtils::isSymbolicLink(const std::string& path) { std::error_code ec; return std::filesystem::is_symlink(path, ec); }
bool FileUtils::isReadable(const std::string& path) { return ::access(path.c_str(), R_OK) == 0; }
bool FileUtils::isWritable(const std::string& path) { return ::access(path.c_str(), W_OK) == 0; }
bool FileUtils::isExecutable(const std::string& path) { return ::access(path.c_str(), X_OK) == 0; }

int64_t FileUtils::getDirectorySize(const std::string& path) {
    int64_t total = 0;
    std::error_code ec;
    for (auto it = std::filesystem::recursive_directory_iterator(path, ec); !ec && it != std::filesystem::recursive_directory_iterator(); it.increment(ec)) {
        if (it->is_regular_file(ec)) total += static_cast<int64_t>(it->file_size(ec));
    }
    return total;
}

bool FileUtils::compareFiles(const std::string& f1, const std::string& f2) {
    if (getFileSize(f1) != getFileSize(f2)) return false;
    std::ifstream a(f1, std::ios::binary), b(f2, std::ios::binary);
    if (!a || !b) return false;
    std::array<char, 65536> x{}, y{};
    do {
        a.read(x.data(), x.size()); b.read(y.data(), y.size());
        if (a.gcount() != b.gcount() || std::memcmp(x.data(), y.data(), static_cast<size_t>(a.gcount())) != 0) return false;
    } while (a && b);
    return true;
}

bool FileUtils::createDirectory(const std::string& path) { if (path.empty()) return false; std::error_code ec; std::filesystem::create_directories(path, ec); return !ec; }
bool FileUtils::clearDirectory(const std::string& path) { std::error_code ec; for (const auto& e : std::filesystem::directory_iterator(path, ec)) std::filesystem::remove_all(e.path(), ec); return !ec; }
bool FileUtils::copyDirectory(const std::string& src, const std::string& dst) { std::error_code ec; std::filesystem::create_directories(dst, ec); std::filesystem::copy(src, dst, std::filesystem::copy_options::recursive | std::filesystem::copy_options::overwrite_existing, ec); return !ec; }

std::vector<std::string> FileUtils::listDirectory(const std::string& path, bool filesOnly) {
    std::vector<std::string> v;
    std::error_code ec;
    for (const auto& e : std::filesystem::directory_iterator(path, ec)) if (!filesOnly || e.is_regular_file(ec)) v.push_back(e.path().string());
    std::sort(v.begin(), v.end());
    return v;
}

std::vector<std::string> FileUtils::collectFiles(const std::string& path) {
    std::vector<std::string> v;
    std::error_code ec;
    if (std::filesystem::is_regular_file(path, ec)) return {path};
    for (auto it = std::filesystem::recursive_directory_iterator(path, ec); !ec && it != std::filesystem::recursive_directory_iterator(); it.increment(ec)) if (it->is_regular_file(ec)) v.push_back(it->path().string());
    std::sort(v.begin(), v.end());
    return v;
}

std::vector<std::string> FileUtils::collectFilesByExtension(const std::string& path, const std::vector<std::string>& exts) {
    auto v = collectFiles(path);
    v.erase(std::remove_if(v.begin(), v.end(), [&](const std::string& f) { return std::none_of(exts.begin(), exts.end(), [&](const std::string& e) { return endsWithIC(f, e); }); }), v.end());
    return v;
}

int FileUtils::countFiles(const std::string& path) { return static_cast<int>(collectFiles(path).size()); }
int FileUtils::batchDeleteFiles(const std::vector<std::string>& paths) { int n = 0; for (auto& p : paths) if (deleteDirectory(p) || deleteFile(p)) ++n; return n; }
int FileUtils::batchCopyFiles(const std::vector<std::string>& srcs, const std::vector<std::string>& dsts) { if (srcs.size() != dsts.size()) return 0; int n = 0; for (size_t i = 0; i < srcs.size(); ++i) if (copyFile(srcs[i], dsts[i])) ++n; return n; }
