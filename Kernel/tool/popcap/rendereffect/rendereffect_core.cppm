module;
#include <array>
#include <bit>
#include <cstdint>
#include <limits>
#include <span>
#include <stdexcept>
#include <string>
#include <string_view>
#include <vector>
export module tool.popcap.rendereffect.core;

export namespace PopCap::RenderEffect {

struct Version {
    uint32_t number = 1;
    uint32_t variant = 3;
};

struct Block1 { uint32_t unknown_1{}, unknown_2{}, unknown_3{}, unknown_4{}, unknown_5{}, unknown_6{}; };
struct Block2 { uint32_t unknown_1{}, unknown_2{}, unknown_3{}; };
struct Block3 { uint32_t unknown_2{}; std::string string; };
struct Block4 { uint32_t unknown_1{}, unknown_2{}, unknown_3{}, unknown_4{}, unknown_5{}; };
struct Block5 { uint32_t unknown_1{}, unknown_2{}, unknown_3{}, unknown_4{}, unknown_5{}, unknown_6{}, unknown_7{}; };
struct Block6 { uint32_t unknown_1{}, unknown_2{}, unknown_3{}, unknown_4{}, unknown_5{}; };
struct Block7 { uint32_t unknown_1{}, unknown_2{}; };
struct Block8 { uint32_t unknown_1{}, unknown_2{}, unknown_3{}, unknown_4{}, unknown_5{}; };

struct Effect {
    std::vector<Block1> block_1;
    std::vector<Block2> block_2;
    std::vector<Block3> block_3;
    std::vector<Block4> block_4;
    std::vector<Block5> block_5;
    std::vector<Block6> block_6;
    std::vector<Block7> block_7;
    std::vector<Block8> block_8;
};

struct SectionInfo { uint32_t count{}, offset{}, size{}; };

inline constexpr uint32_t magic = 0x70636678;
inline constexpr uint32_t supported_version = 1;
inline constexpr uint32_t block1_size = 0x18;
inline constexpr uint32_t block3_size = 0x0C;
inline constexpr uint32_t block4_size = 0x14;
inline constexpr uint32_t block5_size = 0x1C;
inline constexpr uint32_t block6_size = 0x14;
inline constexpr uint32_t block7_size = 0x08;

[[nodiscard]] constexpr uint32_t block2_size(Version version) noexcept { return version.variant == 1 ? 0x0C : 0x08; }
[[nodiscard]] constexpr uint32_t block8_size(Version version) noexcept { return version.variant == 3 ? 0x14 : 0x0C; }
[[nodiscard]] constexpr uint32_t section_count(Version version) noexcept { return version.variant == 3 ? 8 : 7; }
[[nodiscard]] constexpr uint32_t block8_index(Version version) noexcept { return version.variant == 3 ? 7 : 6; }
[[nodiscard]] constexpr size_t header_size(Version version) noexcept { return 12 + static_cast<size_t>(section_count(version)) * 12; }

inline void validate_version(Version version) {
    if (version.number != supported_version || version.variant < 1 || version.variant > 3)
        throw std::runtime_error("Invalid popfx version");
}

[[nodiscard]] inline uint32_t narrow_u32(size_t value) {
    if (value > std::numeric_limits<uint32_t>::max()) throw std::runtime_error("popfx size overflow");
    return static_cast<uint32_t>(value);
}

[[nodiscard]] inline uint32_t read_u32(std::span<const uint8_t> data, size_t offset) {
    if (offset > data.size() || data.size() - offset < sizeof(uint32_t)) throw std::runtime_error("popfx read out of range");
    return static_cast<uint32_t>(data[offset]) |
           static_cast<uint32_t>(data[offset + 1]) << 8 |
           static_cast<uint32_t>(data[offset + 2]) << 16 |
           static_cast<uint32_t>(data[offset + 3]) << 24;
}

inline void write_u32(std::vector<uint8_t>& data, uint32_t value) {
    data.push_back(static_cast<uint8_t>(value));
    data.push_back(static_cast<uint8_t>(value >> 8));
    data.push_back(static_cast<uint8_t>(value >> 16));
    data.push_back(static_cast<uint8_t>(value >> 24));
}

[[nodiscard]] inline std::string read_c_string(std::span<const uint8_t> data, size_t offset) {
    if (offset >= data.size()) throw std::runtime_error("Invalid popfx string offset");
    const auto strings = data.subspan(offset);
    size_t length = 0;
    while (length < strings.size() && strings[length] != 0) ++length;
    if (length == strings.size()) throw std::runtime_error("Invalid popfx string terminator");
    return {reinterpret_cast<const char*>(strings.data()), length};
}

[[nodiscard]] inline size_t section_data_size(const Effect& effect, Version version) {
    validate_version(version);
    const auto multiply = [](size_t count, uint32_t size) {
        if (count > std::numeric_limits<size_t>::max() / size) throw std::runtime_error("popfx size overflow");
        return count * static_cast<size_t>(size);
    };
    size_t total = 0;
    const auto add = [&](size_t size) {
        if (total > std::numeric_limits<size_t>::max() - size) throw std::runtime_error("popfx size overflow");
        total += size;
    };
    add(multiply(effect.block_1.size(), block1_size));
    add(multiply(effect.block_2.size(), block2_size(version)));
    add(multiply(effect.block_3.size(), block3_size));
    add(multiply(effect.block_4.size(), block4_size));
    add(multiply(effect.block_5.size(), block5_size));
    add(multiply(effect.block_6.size(), block6_size));
    if (version.variant == 3) add(multiply(effect.block_7.size(), block7_size));
    add(multiply(effect.block_8.size(), block8_size(version)));
    return total;
}

[[nodiscard]] inline size_t string_data_size(const Effect& effect) {
    size_t total = 0;
    for (const auto& block : effect.block_3) {
        if (block.string.size() == std::numeric_limits<size_t>::max() || total > std::numeric_limits<size_t>::max() - block.string.size() - 1)
            throw std::runtime_error("popfx size overflow");
        total += block.string.size() + 1;
    }
    return total;
}

}
