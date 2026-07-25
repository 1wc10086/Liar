module;
#include <cstddef>
#include <cstdint>
#include <span>
#include <string_view>
#include <utility>
#include <vector>

export module utility.compression.zip.zip_core;

export namespace zip_ns {

using byte = uint8_t;
using buffer_type = std::vector<byte>;
using view_type = std::span<const byte>;

struct InputEntry {
    std::string_view name;
    view_type data;
    bool directory{};
};

struct EntryInfo {
    std::string_view name;
    uint64_t size{};
    uint64_t compressed_size{};
    uint16_t method{};
    uint16_t encryption{};
    bool directory{};
};

class Archive;

class EntryStream {
public:
    EntryStream() = default;
    EntryStream(const EntryStream&) = delete;
    EntryStream& operator=(const EntryStream&) = delete;
    EntryStream(EntryStream&& other) noexcept;
    EntryStream& operator=(EntryStream&& other) noexcept;
    ~EntryStream();

    [[nodiscard]] explicit operator bool() const noexcept { return handle_ != nullptr; }
    [[nodiscard]] int64_t read(std::span<byte> output) noexcept;
    void reset() noexcept;

private:
    friend class Archive;
    explicit EntryStream(void* handle) noexcept : handle_(handle) {}
    void* handle_{};
};

class Archive {
public:
    Archive() = default;
    explicit Archive(view_type input, std::string_view password = {}) noexcept;
    Archive(const Archive&) = delete;
    Archive& operator=(const Archive&) = delete;
    Archive(Archive&& other) noexcept;
    Archive& operator=(Archive&& other) noexcept;
    ~Archive();

    [[nodiscard]] explicit operator bool() const noexcept { return handle_ != nullptr; }
    [[nodiscard]] uint64_t entry_count() const noexcept;
    [[nodiscard]] bool entry_info(uint64_t index, EntryInfo& output) const noexcept;
    [[nodiscard]] EntryStream open_entry(uint64_t index, std::string_view password = {}) const noexcept;
    void reset() noexcept;

private:
    void* handle_{};
};

[[nodiscard]] uint32_t abi_version() noexcept;
[[nodiscard]] std::string_view version_string() noexcept;
[[nodiscard]] bool loaded() noexcept;
[[nodiscard]] std::string_view library_path() noexcept;
[[nodiscard]] std::string_view load_error() noexcept;
[[nodiscard]] bool pkware_supported() noexcept;
void release(void* data) noexcept;
[[nodiscard]] int32_t compress_to(void** output, uint64_t& output_size, std::span<const InputEntry> entries, int32_t level = 6, std::string_view password = {}) noexcept;

}
