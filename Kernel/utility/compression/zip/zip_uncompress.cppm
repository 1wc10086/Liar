module;
#include <cstddef>
#include <cstdint>
#include <optional>
#include <span>
#include <string_view>
#include <vector>

export module utility.compression.zip.zip_uncompress;

import utility.compression.zip.zip_core;

export namespace zip_ns {

class Decompressor {
public:
    [[nodiscard]] static std::optional<buffer_type> extract(view_type input, uint64_t index = 0, std::string_view password = {}) {
        Archive archive{input, password};
        if (!archive) return std::nullopt;
        EntryInfo info;
        if (!archive.entry_info(index, info) || info.directory || info.size > SIZE_MAX) return std::nullopt;
        auto stream = archive.open_entry(index, password);
        if (!stream) return std::nullopt;
        buffer_type output(static_cast<size_t>(info.size));
        size_t position{};
        while (position < output.size()) {
            const auto read = stream.read(std::span<byte>{output}.subspan(position));
            if (read <= 0 || static_cast<uint64_t>(read) > output.size() - position) return std::nullopt;
            position += static_cast<size_t>(read);
        }
        return output;
    }
};

}
