module;
#include <cstdint>
#include <span>
#include <vector>
export module utility.zlib.zlib_core;

export {
namespace zlib_ns {
    using buffer_type = std::vector<uint8_t>;
    using view_type = std::span<const uint8_t>;
}
}
