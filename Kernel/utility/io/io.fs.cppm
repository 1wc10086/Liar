module;
#include <cstdint>
#include <functional>
#include <span>
#include <string>
#include <string_view>
#include <utility>
#include <vector>

export module utility.io.fs;

export class FileUtils {
public:
    [[nodiscard]] static std::vector<uint8_t> readFileBytes(const std::string& path);
    static bool writeFileBytes(const std::string& path, std::span<const uint8_t> data);
    static bool appendFileBytes(const std::string& path, std::span<const uint8_t> data);
    [[nodiscard]] static std::string readTextFile(const std::string& path);
    static bool writeTextFile(const std::string& path, std::string_view content);
    static bool appendTextFile(const std::string& path, std::string_view content);
    static void posixWrite(const std::string& path, const void* data, size_t size);
    static void posixWriteV(const std::string& path, const void* hdr, size_t hdrLen, const void* body, size_t bodyLen);
    [[nodiscard]] static bool posixReadInto(const std::string& path, uint8_t* buf, size_t size, int64_t fileOffset = 0) noexcept;
    [[nodiscard]] static int64_t posixFileSize(const std::string& path) noexcept;
    [[nodiscard]] static std::pair<const uint8_t*, size_t> mmapReadFile(const std::string& path);
    static void munmapFile(const uint8_t* addr, size_t length);
    static bool streamReadFile(const std::string& path, size_t chunkSize, std::function<bool(const uint8_t*, size_t)> cb);
    [[nodiscard]] static std::vector<std::vector<uint8_t>> batchReadFiles(const std::vector<std::string>& paths);
    static bool createFile(const std::string& path);
    static bool deleteFile(const std::string& path);
    static bool deleteDirectory(const std::string& path);
    static bool copyFile(const std::string& src, const std::string& dst);
    static bool moveFile(const std::string& src, const std::string& dst);
    static bool truncateFile(const std::string& path);
    [[nodiscard]] static int64_t getFileSize(const std::string& path);
    [[nodiscard]] static int64_t getFileModifiedTime(const std::string& path);
    [[nodiscard]] static int64_t getDirectorySize(const std::string& path);
    [[nodiscard]] static int getFilePermissions(const std::string& path);
    static bool setFilePermissions(const std::string& path, int mode);
    [[nodiscard]] static std::string getFileExtension(const std::string& path);
    [[nodiscard]] static std::string changeFileExtension(const std::string& path, const std::string& ext);
    [[nodiscard]] static std::string getFileNameWithoutExtension(const std::string& path);
    [[nodiscard]] static std::string getFileName(const std::string& path);
    [[nodiscard]] static std::string getParentDirectory(const std::string& path);
    [[nodiscard]] static std::string getAbsolutePath(const std::string& path);
    [[nodiscard]] static std::string joinPath(const std::string& a, const std::string& b);
    static void normalizePath(std::string& s);
    [[nodiscard]] static std::string_view getParentDir(std::string_view p);
    [[nodiscard]] static std::string normalizeFsPath(const std::string& path);
    [[nodiscard]] static bool fileExists(const std::string& path);
    [[nodiscard]] static bool isDirectory(const std::string& path);
    [[nodiscard]] static bool isRegularFile(const std::string& path);
    [[nodiscard]] static bool isSymbolicLink(const std::string& path);
    [[nodiscard]] static bool isReadable(const std::string& path);
    [[nodiscard]] static bool isWritable(const std::string& path);
    [[nodiscard]] static bool isExecutable(const std::string& path);
    [[nodiscard]] static bool compareFiles(const std::string& f1, const std::string& f2);
    static bool createDirectory(const std::string& path);
    static bool clearDirectory(const std::string& path);
    static bool copyDirectory(const std::string& src, const std::string& dst);
    [[nodiscard]] static std::vector<std::string> listDirectory(const std::string& path, bool filesOnly = false);
    [[nodiscard]] static std::vector<std::string> collectFiles(const std::string& path);
    [[nodiscard]] static std::vector<std::string> collectFilesByExtension(const std::string& path, const std::vector<std::string>& exts);
    [[nodiscard]] static int countFiles(const std::string& path);
    static int batchDeleteFiles(const std::vector<std::string>& paths);
    static int batchCopyFiles(const std::vector<std::string>& srcs, const std::vector<std::string>& dsts);
};


export namespace Rsb {
    using ::FileUtils;
}
