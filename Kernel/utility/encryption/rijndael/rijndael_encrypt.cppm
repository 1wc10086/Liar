module;
#include <array>
#include <span>
#include <string>
#include <string_view>
#include <vector>
#include "lib/rijndael/Rijndael.h"
export module utility.rijndael.encrypt;
export import utility.rijndael.core;
import utility.rijndael.utils;

export class RijndaelEncryptor {
public:
    [[nodiscard]] static std::vector<char> encrypt(std::span<const char> plaintext,
                                                   RijndaelMode mode,
                                                   int blockSize,
                                                   int keySize,
                                                   std::string_view key,
                                                   std::string_view iv,
                                                   PaddingType padding) {
        if (!RijndaelUtils::isValidBlockSize(blockSize) || !RijndaelUtils::isValidBlockSize(keySize)) return {};
        auto padded = RijndaelUtils::padData(plaintext, blockSize, padding);
        const std::string keyBuffer(key);
        const std::string ivBuffer(iv);
        static constexpr std::array<char, 32> emptyIv{};
        CRijndael rij;
        rij.MakeKey(keyBuffer.c_str(), mode == RijndaelMode::ECB ? emptyIv.data() : ivBuffer.c_str(), keySize, blockSize);
        std::vector<char> out(padded.size());
        rij.Encrypt(padded.data(), out.data(), static_cast<int>(padded.size()), static_cast<int>(mode));
        return out;
    }
};
