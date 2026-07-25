module;
#include <cstddef>
#include <cstdint>
#include <span>
#include <string_view>
#include <vector>

export module utility.compression.tar.tar_core;

export namespace tar_ns {

using byte = uint8_t;
using buffer_type = std::vector<byte>;
using view_type = std::span<const byte>;

inline constexpr uint32_t regular_file = '0';
inline constexpr uint32_t directory = '5';

struct Entry {
    std::string_view name;
    uint32_t type{};
    uint64_t size{};
    view_type data;
};

struct InputEntry {
    std::string_view name;
    view_type data;
    bool directory{};
};

using EntryCallback = bool (*)(const Entry&, void*) noexcept;

[[nodiscard]] bool loaded() noexcept;
[[nodiscard]] std::string_view library_path() noexcept;
[[nodiscard]] std::string_view load_error() noexcept;
[[nodiscard]] bool extract(view_type input, EntryCallback callback, void* context = nullptr) noexcept;
[[nodiscard]] buffer_type create_file(view_type input, std::string_view name);
[[nodiscard]] buffer_type create(std::span<const InputEntry> entries);

}
