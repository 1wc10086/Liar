module;
#include <algorithm>
#include <cctype>
#include <cstdint>
#include <expected>
#include <filesystem>
#include <format>
#include <iterator>
#include <ranges>
#include <string>
#include <string_view>
#include <system_error>
#include <fcntl.h>
#include <sys/mman.h>
#include <sys/stat.h>
#include <unistd.h>
export module utility.md5.utils;
import utility.md5.core;

export namespace MD5Utils {

[[nodiscard]] inline std::string digestToHex(const uint8_t digest[16]) {
    std::string r;
    r.reserve(32);
    for (int i = 0; i < 16; ++i)
        std::format_to(std::back_inserter(r), "{:02x}", static_cast<unsigned>(digest[i]));
    return r;
}

[[nodiscard]] inline std::string computeDataHash(const uint8_t* data, size_t len) {
    MD5Calculator c;
    c.update(data, len);
    uint8_t d[16];
    c.finalize(d);
    return digestToHex(d);
}

[[nodiscard]] inline std::string computeStringHash(std::string_view s) {
    return computeDataHash(reinterpret_cast<const uint8_t*>(s.data()), s.size());
}

[[nodiscard]] inline bool verifyStringHash(std::string_view s, std::string_view expected) {
    auto a = computeStringHash(s);
    auto toLower = [](unsigned char c) noexcept { return static_cast<char>(std::tolower(c)); };
    std::ranges::transform(a, a.begin(), toLower);

    std::string b(expected);
    std::ranges::transform(b, b.begin(), toLower);

    return a == b;
}

[[nodiscard]] inline std::expected<std::string, std::string>
computeFileHash(const std::filesystem::path& path) noexcept {
    const int fd = open(path.c_str(), O_RDONLY);
    if (fd == -1)
        return std::unexpected(path.string());

    struct stat sb{};
    if (fstat(fd, &sb) == -1) {
        close(fd);
        return std::unexpected(path.string());
    }

    MD5Calculator calc;
    const size_t sz = static_cast<size_t>(sb.st_size);

    if (sz > 0) {
        auto* addr = static_cast<const uint8_t*>(
            mmap(nullptr, sz, PROT_READ, MAP_PRIVATE, fd, 0));

        if (addr != MAP_FAILED) {
            madvise(const_cast<uint8_t*>(addr), sz, MADV_SEQUENTIAL);
            calc.update(addr, sz);
            munmap(const_cast<uint8_t*>(addr), sz);
        } else {
            constexpr size_t kBuf = 65536;
            uint8_t buf[kBuf];
            ssize_t n;
            while ((n = read(fd, buf, kBuf)) > 0)
                calc.update(buf, static_cast<size_t>(n));
        }
    }

    close(fd);

    uint8_t d[16];
    calc.finalize(d);
    return digestToHex(d);
}

[[nodiscard]] inline bool
verifyFileHash(const std::filesystem::path& path, std::string_view expected) noexcept {
    auto result = computeFileHash(path);
    if (!result) return false;
    return verifyStringHash(*result, expected);
}

}
