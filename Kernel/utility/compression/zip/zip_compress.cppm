module;
#include <cstdint>
#include <cstring>
#include <limits>
#include <optional>
#include <span>
#include <string_view>
#include <vector>

export module utility.compression.zip.zip_compress;

import utility.compression.zip.zip_core;

export namespace zip_ns {

class Compressor {
public:
    [[nodiscard]] static std::optional<buffer_type> compress(std::span<const InputEntry> entries, int32_t level = 6, std::string_view password = {}) {
        void* raw{};
        uint64_t size{};
        if (compress_to(&raw, size, entries, level, password) != 0 || (!raw && size)) return std::nullopt;
        if (size > std::numeric_limits<size_t>::max()) { release(raw); return std::nullopt; }
        buffer_type output(size);
        if (size) std::memcpy(output.data(), raw, size);
        release(raw);
        return output;
    }

    [[nodiscard]] static std::optional<buffer_type> compress(view_type input, std::string_view name = "data", int32_t level = 6, std::string_view password = {}) {
        const InputEntry entry{name, input, false};
        return compress(std::span<const InputEntry>{&entry, 1}, level, password);
    }
};

}
