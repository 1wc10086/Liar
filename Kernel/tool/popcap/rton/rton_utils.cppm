module;
#include <algorithm>
#include <cstdint>
#include <span>
#include <string>
#include <string_view>
#include <utility>
#include <vector>
export module tool.popcap.rton.utils;
export import tool.popcap.rton.core;
import utility.md5.utils;
import utility.rijndael.core;
import utility.rijndael.encrypt;
import utility.rijndael.decrypt;

export namespace RTONUtils {

[[nodiscard]] inline std::string latin1ToUtf8(std::string_view s) {
    std::string out;
    out.reserve(s.size() + 32);
    for (const unsigned char c : s) {
        if (c < 0x80u) {
            out += static_cast<char>(c);
        } else {
            out += static_cast<char>(0xC0u | (c >> 6u));
            out += static_cast<char>(0x80u | (c & 0x3Fu));
        }
    }
    return out;
}

[[nodiscard]] inline std::string utf8ToLatin1(std::string_view s) {
    std::string out;
    out.reserve(s.size());
    const auto* p   = reinterpret_cast<const uint8_t*>(s.data());
    const auto* end = p + s.size();
    while (p < end) {
        uint8_t b = *p;
        if (b < 0x80u) {
            out += static_cast<char>(b);
            ++p;
        } else if ((b & 0xE0u) == 0xC0u && p + 1 < end &&
                   (*(p+1) & 0xC0u) == 0x80u) {
            const uint32_t cp = ((b & 0x1Fu) << 6u) | (*(p+1) & 0x3Fu);
            out += (cp <= 0xFFu) ? static_cast<char>(static_cast<uint8_t>(cp))
                                 : '?';
            p += 2;
        } else if ((b & 0xF0u) == 0xE0u && p + 2 < end) {
            out += '?';
            p += 3;
        } else if ((b & 0xF8u) == 0xF0u && p + 3 < end) {
            out += '?';
            p += 4;
        } else {
            ++p;
        }
    }
    return out;
}

[[nodiscard]] inline bool isASCII(std::string_view s) noexcept {
    return std::all_of(s.begin(), s.end(),
                       [](char c){ return static_cast<unsigned char>(c) <= 127; });
}

[[nodiscard]] inline size_t latin1Len(std::string_view utf8) noexcept {
    size_t count = 0;
    for (unsigned char c : utf8) count += ((c & 0xC0u) != 0x80u);
    return count;
}

inline std::pair<std::string, std::string> generateKeyAndIV(std::string_view pwd) {
    auto hash = MD5Utils::computeStringHash(std::string(pwd));
    return { hash, std::string(hash.begin() + 4, hash.begin() + 28) };
}

inline std::vector<uint8_t> decryptBytes(std::span<const uint8_t> bytes,
                                          std::string_view pwd) {
    if (bytes.size() < 2) return {};
    std::vector<uint8_t> data(bytes.begin() + 2, bytes.end());
    auto [key, iv] = generateKeyAndIV(pwd);
    try {
        std::vector<char> c(data.begin(), data.end());
        auto r = RijndaelDecryptor::decrypt(c, RijndaelMode::CBC, 24, 32, key, iv,
                                            PaddingType::ZERO);
        return std::vector<uint8_t>(r.begin(), r.end());
    } catch (...) { return data; }
}

inline std::vector<uint8_t> encryptBytes(std::span<const uint8_t> bytes,
                                          std::string_view pwd) {
    auto [key, iv] = generateKeyAndIV(pwd);
    std::vector<char> c(bytes.begin(), bytes.end());
    auto enc = RijndaelEncryptor::encrypt(c, RijndaelMode::CBC, 24, 32, key, iv,
                                           PaddingType::ZERO);
    std::vector<uint8_t> r;
    r.reserve(enc.size() + 2);
    r.insert(r.end(), MAGIC, MAGIC + 2);
    r.insert(r.end(), enc.begin(), enc.end());
    return r;
}


}
