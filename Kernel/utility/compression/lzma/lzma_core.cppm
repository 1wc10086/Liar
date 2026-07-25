module;
#include <cstdint>
#include <span>
#include <vector>
export module utility.lzma.lzma_core;

export {
namespace lzma_ns {
    using buffer_type = std::vector<uint8_t>;
    using view_type = std::span<const uint8_t>;
}
}
