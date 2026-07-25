module;
#include <cstdint>
export module tool.popcap.bbone.core;

export namespace BBone {

inline constexpr uint16_t MAGIC = 0x5678;

struct PluginMapEntry {
    uint8_t id{};
    uint32_t offset{};
    uint32_t length{};
};

}
