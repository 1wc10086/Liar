module;
#include <cstddef>
#include <span>
#include <vector>
export module utility.rijndael.utils;
export import utility.rijndael.core;

export namespace RijndaelUtils {

[[nodiscard]] inline bool isValidBlockSize(int size) noexcept {
    return size == 16 || size == 24 || size == 32;
}

[[nodiscard]] inline std::vector<char> padData(std::span<const char> data, int blockSize, PaddingType padding) {
    std::vector<char> out(data.begin(), data.end());
    const int rem = blockSize - static_cast<int>(data.size() % static_cast<size_t>(blockSize));
    if (rem == blockSize && padding == PaddingType::ZERO) return out;
    out.insert(out.end(), rem, padding == PaddingType::ZERO ? char{} : static_cast<char>(rem));
    return out;
}

[[nodiscard]] inline std::vector<char> unpadData(std::span<const char> data, PaddingType padding) {
    if (data.empty()) return {};
    if (padding == PaddingType::ZERO) {
        auto end = data.size();
        while (end && data[end - 1] == 0) --end;
        return {data.begin(), data.begin() + static_cast<std::ptrdiff_t>(end)};
    }
    const int pad = static_cast<unsigned char>(data.back());
    if (pad <= 0 || static_cast<size_t>(pad) > data.size()) return {data.begin(), data.end()};
    const auto start = data.size() - static_cast<size_t>(pad);
    for (auto i = start; i < data.size(); ++i) {
        if (data[i] != data.back()) return {data.begin(), data.end()};
    }
    return {data.begin(), data.end() - pad};
}

}
