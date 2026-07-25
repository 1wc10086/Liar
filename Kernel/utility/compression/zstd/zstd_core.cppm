module;
#include <cstddef>
#include <cstdint>
#include <span>
#include <string_view>
#include <utility>
#include <vector>

export module utility.compression.zstd.zstd_core;

export namespace zstd_ns {

using byte = uint8_t;
using buffer_type = std::vector<byte>;
using view_type = std::span<const byte>;

inline constexpr uint64_t content_size_error = 0xffffffffffffffffull;
inline constexpr uint64_t content_size_unknown = 0xfffffffffffffffeull;

enum class ResetDirective : int32_t { session_only = 1, parameters = 2, session_and_parameters = 3 };
enum class EndDirective : int32_t { continue_ = 0, flush = 1, end = 2 };

struct Result {
    size_t code{};
    const char* error{};

    [[nodiscard]] explicit operator bool() const noexcept { return error == nullptr; }
};

struct AdvancedOptions {
    int32_t level = 3;
    bool checksum = false;
    bool content_size = true;
    int32_t workers = 0;
    int32_t window_log = 0;
    int32_t chain_log = 0;
    int32_t hash_log = 0;
    int32_t search_log = 0;
    int32_t min_match = 0;
    int32_t target_length = 0;
    int32_t strategy = 0;
};

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

class CDict {
public:
    CDict() = default;
    CDict(const void* dict, size_t dict_size, int32_t level = 3) noexcept;
    CDict(view_type dict, int32_t level = 3) noexcept : CDict(dict.data(), dict.size(), level) {}
    CDict(const CDict&) = delete;
    CDict& operator=(const CDict&) = delete;
    CDict(CDict&& other) noexcept;
    CDict& operator=(CDict&& other) noexcept;
    ~CDict();

    [[nodiscard]] explicit operator bool() const noexcept { return handle_ != nullptr; }
    [[nodiscard]] void* raw() const noexcept { return handle_; }
    void reset() noexcept;

private:
    void* handle_{};
};

class DDict {
public:
    DDict() = default;
    DDict(const void* dict, size_t dict_size) noexcept;
    explicit DDict(view_type dict) noexcept : DDict(dict.data(), dict.size()) {}
    DDict(const DDict&) = delete;
    DDict& operator=(const DDict&) = delete;
    DDict(DDict&& other) noexcept;
    DDict& operator=(DDict&& other) noexcept;
    ~DDict();

    [[nodiscard]] explicit operator bool() const noexcept { return handle_ != nullptr; }
    [[nodiscard]] void* raw() const noexcept { return handle_; }
    void reset() noexcept;

private:
    void* handle_{};
};

class CStream {
public:
    CStream() noexcept;
    CStream(const CStream&) = delete;
    CStream& operator=(const CStream&) = delete;
    CStream(CStream&& other) noexcept;
    CStream& operator=(CStream&& other) noexcept;
    ~CStream();

    [[nodiscard]] explicit operator bool() const noexcept { return handle_ != nullptr; }
    [[nodiscard]] void* raw() const noexcept { return handle_; }
    [[nodiscard]] size_t init(int32_t level = 3) noexcept;
    [[nodiscard]] size_t set_parameter(int32_t parameter, int32_t value) noexcept;
    [[nodiscard]] size_t load(const CDict& dict) noexcept;
    [[nodiscard]] size_t compress(BufferCursor& dst, ConstBufferCursor& src) noexcept;
    [[nodiscard]] size_t flush(BufferCursor& dst) noexcept;
    [[nodiscard]] size_t end(BufferCursor& dst) noexcept;
    void reset() noexcept;

private:
    void* handle_{};
};

class DStream {
public:
    DStream() noexcept;
    DStream(const DStream&) = delete;
    DStream& operator=(const DStream&) = delete;
    DStream(DStream&& other) noexcept;
    DStream& operator=(DStream&& other) noexcept;
    ~DStream();

    [[nodiscard]] explicit operator bool() const noexcept { return handle_ != nullptr; }
    [[nodiscard]] void* raw() const noexcept { return handle_; }
    [[nodiscard]] size_t init() noexcept;
    [[nodiscard]] size_t set_parameter(int32_t parameter, int32_t value) noexcept;
    [[nodiscard]] size_t load(const DDict& dict) noexcept;
    [[nodiscard]] size_t decompress(BufferCursor& dst, ConstBufferCursor& src) noexcept;
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
[[nodiscard]] bool is_error(size_t code) noexcept;
[[nodiscard]] const char* error_name(size_t code) noexcept;
[[nodiscard]] size_t compress_bound(size_t src_size) noexcept;
[[nodiscard]] uint64_t frame_content_size(const void* src, size_t src_size) noexcept;
[[nodiscard]] uint64_t find_decompressed_size(const void* src, size_t src_size) noexcept;
[[nodiscard]] bool frame_header_size(const void* src, size_t src_size, size_t& out_size) noexcept;
[[nodiscard]] uint32_t dict_id_from_dict(const void* dict, size_t dict_size) noexcept;
[[nodiscard]] uint32_t dict_id_from_frame(const void* src, size_t src_size) noexcept;
[[nodiscard]] size_t compress_to(void* dst, size_t dst_capacity, const void* src, size_t src_size, int32_t level = 3) noexcept;
[[nodiscard]] size_t decompress_to(void* dst, size_t dst_capacity, const void* src, size_t src_size) noexcept;
[[nodiscard]] size_t compress_using_dict_to(void* dst, size_t dst_capacity, const void* src, size_t src_size, const void* dict, size_t dict_size, int32_t level = 3) noexcept;
[[nodiscard]] size_t decompress_using_dict_to(void* dst, size_t dst_capacity, const void* src, size_t src_size, const void* dict, size_t dict_size) noexcept;
[[nodiscard]] size_t train_dictionary_to(void* dst, size_t dst_capacity, const void* samples, const size_t* sample_sizes, uint32_t sample_count) noexcept;
[[nodiscard]] size_t train_samples_bound(const size_t* sample_sizes, uint32_t sample_count) noexcept;
[[nodiscard]] size_t compress_advanced_to(void* dst, size_t dst_capacity, const void* src, size_t src_size, const AdvancedOptions& options = {}) noexcept;

[[nodiscard]] inline uint64_t frame_content_size(view_type input) noexcept { return frame_content_size(input.data(), input.size()); }
[[nodiscard]] inline uint64_t find_decompressed_size(view_type input) noexcept { return find_decompressed_size(input.data(), input.size()); }
[[nodiscard]] inline bool frame_header_size(view_type input, size_t& out_size) noexcept { return frame_header_size(input.data(), input.size(), out_size); }
[[nodiscard]] inline uint32_t dict_id_from_dict(view_type dict) noexcept { return dict_id_from_dict(dict.data(), dict.size()); }
[[nodiscard]] inline uint32_t dict_id_from_frame(view_type input) noexcept { return dict_id_from_frame(input.data(), input.size()); }
[[nodiscard]] inline size_t compress_to(std::span<byte> dst, view_type src, int32_t level = 3) noexcept { return compress_to(dst.data(), dst.size(), src.data(), src.size(), level); }
[[nodiscard]] inline size_t decompress_to(std::span<byte> dst, view_type src) noexcept { return decompress_to(dst.data(), dst.size(), src.data(), src.size()); }
[[nodiscard]] inline size_t compress_using_dict_to(std::span<byte> dst, view_type src, view_type dict, int32_t level = 3) noexcept { return compress_using_dict_to(dst.data(), dst.size(), src.data(), src.size(), dict.data(), dict.size(), level); }
[[nodiscard]] inline size_t decompress_using_dict_to(std::span<byte> dst, view_type src, view_type dict) noexcept { return decompress_using_dict_to(dst.data(), dst.size(), src.data(), src.size(), dict.data(), dict.size()); }
[[nodiscard]] inline size_t compress_advanced_to(std::span<byte> dst, view_type src, const AdvancedOptions& options = {}) noexcept { return compress_advanced_to(dst.data(), dst.size(), src.data(), src.size(), options); }

}
