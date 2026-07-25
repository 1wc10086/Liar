module;
#include <array>
#include <span>
#include <string>
#include <string_view>
#include <vector>
#include "lib/rijndael/Rijndael.h"
export module utility.rijndael.decrypt;
export import utility.rijndael.core;
import utility.rijndael.utils;

export class RijndaelDecryptor {
public:
    [[nodiscard]] static std::vector<char> decrypt(std::span<const char> ciphertext,
                                                   RijndaelMode mode,
                                                   int blockSize,
                                                   int keySize,
                                                   std::string_view key,
                                                   std::string_view iv,
                                                   PaddingType padding) {
        if (!RijndaelUtils::isValidBlockSize(blockSize) || !RijndaelUtils::isValidBlockSize(keySize)) return {};
        if (ciphertext.size() % static_cast<size_t>(blockSize) != 0) return {};
        const std::string keyBuffer(key);
        const std::string ivBuffer(iv);
        static constexpr std::array<char, 32> emptyIv{};
        CRijndael rij;
        rij.MakeKey(keyBuffer.c_str(), mode == RijndaelMode::ECB ? emptyIv.data() : ivBuffer.c_str(), keySize, blockSize);
        std::vector<char> out(ciphertext.size());
        rij.Decrypt(ciphertext.data(), out.data(), static_cast<int>(ciphertext.size()), static_cast<int>(mode));
        return RijndaelUtils::unpadData(out, padding);
    }
};
