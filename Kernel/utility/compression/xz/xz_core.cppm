module;
#include <cstddef>
#include <cstdint>
#include <span>
#include <string_view>
#include <utility>
#include <vector>

export module utility.compression.xz.xz_core;

export namespace xz_ns {

using byte = uint8_t;
using buffer_type = std::vector<byte>;
using view_type = std::span<const byte>;

inline constexpr uint32_t preset_default = 6;
inline constexpr uint32_t preset_extreme = 1u << 31;
inline constexpr uint32_t check_none = 0;
inline constexpr uint32_t check_crc32 = 1;
inline constexpr uint32_t check_crc64 = 4;
inline constexpr uint32_t check_sha256 = 10;
inline constexpr uint32_t decoder_concatenated = 0x08;
inline constexpr uint32_t decoder_ignore_check = 0x10;
inline constexpr uint64_t unlimited_memory = 0;

enum class Code : uint32_t { ok = 0, stream_end = 1, no_check = 2, unsupported_check = 3, get_check = 4, mem_error = 5, memlimit_error = 6, format_error = 7, options_error = 8, data_error = 9, buf_error = 10, prog_error = 11 };
enum class Action : uint32_t { run = 0, sync_flush = 1, full_flush = 2, finish = 3, full_barrier = 4 };

struct BufferCursor {
    void* data{};
    size_t size{};
    size_t pos{};
};

struct ConstBufferCursor {
    const void* data{};
    size_t size{};
    size_t pos{};
};

struct Options {
    uint32_t preset = preset_default;
    uint32_t check = check_crc64;
};

class CStream {
public:
    explicit CStream(const Options& options = {}) noexcept;
    CStream(const CStream&) = delete;
    CStream& operator=(const CStream&) = delete;
    CStream(CStream&& other) noexcept;
    CStream& operator=(CStream&& other) noexcept;
    ~CStream();

    [[nodiscard]] explicit operator bool() const noexcept { return handle_ != nullptr; }
    [[nodiscard]] void* raw() const noexcept { return handle_; }
    [[nodiscard]] Code code(BufferCursor& dst, ConstBufferCursor& src, Action action = Action::run) noexcept;
    [[nodiscard]] uint64_t total_in() const noexcept;
    [[nodiscard]] uint64_t total_out() const noexcept;
    void reset() noexcept;

private:
    void* handle_{};
};

class DStream {
public:
    explicit DStream(uint64_t memlimit = unlimited_memory, uint32_t flags = decoder_concatenated) noexcept;
    DStream(const DStream&) = delete;
    DStream& operator=(const DStream&) = delete;
    DStream(DStream&& other) noexcept;
    DStream& operator=(DStream&& other) noexcept;
    ~DStream();

    [[nodiscard]] explicit operator bool() const noexcept { return handle_ != nullptr; }
    [[nodiscard]] void* raw() const noexcept { return handle_; }
    [[nodiscard]] Code code(BufferCursor& dst, ConstBufferCursor& src, Action action = Action::run) noexcept;
    [[nodiscard]] uint64_t total_in() const noexcept;
    [[nodiscard]] uint64_t total_out() const noexcept;
    void reset() noexcept;

private:
    void* handle_{};
};

[[nodiscard]] uint32_t abi_version() noexcept;
[[nodiscard]] uint32_t version_number() noexcept;
[[nodiscard]] std::string_view version_string() noexcept;
[[nodiscard]] bool loaded() noexcept;
[[nodiscard]] std::string_view library_path() noexcept;
[[nodiscard]] std::string_view load_error() noexcept;
[[nodiscard]] const char* error_name(Code code) noexcept;
[[nodiscard]] uint64_t memusage_decoder(view_type input, uint64_t memlimit = unlimited_memory, uint32_t flags = decoder_concatenated) noexcept;
[[nodiscard]] uint64_t easy_encoder_memusage(uint32_t preset = preset_default) noexcept;
[[nodiscard]] uint64_t easy_decoder_memusage(uint32_t preset = preset_default) noexcept;
[[nodiscard]] size_t compress_bound(size_t src_size) noexcept;
[[nodiscard]] Code compress_to(void* dst, size_t dst_capacity, size_t& dst_pos, const void* src, size_t src_size, const Options& options = {}) noexcept;
[[nodiscard]] Code decompress_to(void* dst, size_t dst_capacity, size_t& dst_pos, const void* src, size_t src_size, size_t& src_pos, uint64_t memlimit = unlimited_memory, uint32_t flags = decoder_concatenated) noexcept;

[[nodiscard]] inline bool success(Code code) noexcept { return code == Code::ok || code == Code::stream_end; }
[[nodiscard]] inline bool is_error(Code code) noexcept { return !success(code); }
[[nodiscard]] inline size_t compress_bound(view_type input) noexcept { return compress_bound(input.size()); }
[[nodiscard]] inline Code compress_to(std::span<byte> dst, size_t& dst_pos, view_type src, const Options& options = {}) noexcept { return compress_to(dst.data(), dst.size(), dst_pos, src.data(), src.size(), options); }
[[nodiscard]] inline Code decompress_to(std::span<byte> dst, size_t& dst_pos, view_type src, size_t& src_pos, uint64_t memlimit = unlimited_memory, uint32_t flags = decoder_concatenated) noexcept { return decompress_to(dst.data(), dst.size(), dst_pos, src.data(), src.size(), src_pos, memlimit, flags); }

}
