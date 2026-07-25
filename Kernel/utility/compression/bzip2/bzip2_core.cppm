module;
#include <cstdint>
#include <span>
#include <vector>
export module utility.bzip2.bzip2_core;

export {
namespace bzip2_ns {
    using buffer_type = std::vector<uint8_t>;
    using view_type = std::span<const uint8_t>;
}
}
