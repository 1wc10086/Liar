module;
#include <cstdint>
#include <span>
#include <vector>
export module utility.lz4.lz4_core;

export {
namespace lz4_ns {
    using buffer_type = std::vector<uint8_t>;
    using view_type = std::span<const uint8_t>;
}
}
