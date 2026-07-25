module;
#include <cstddef>
#include <cstdint>
#include <span>
#include <string_view>
#include <utility>
#include <vector>

export module utility.compression.brotli.brotli_core;

export namespace brotli_ns {

using byte = uint8_t;
using buffer_type = std::vector<byte>;
using view_type = std::span<const byte>;

inline constexpr int32_t default_quality = 11;
inline constexpr int32_t default_window = 22;
inline constexpr int32_t default_mode = 0;
inline constexpr size_t error_code = static_cast<size_t>(-1);

enum class EncoderOperation : int32_t { process = 0, flush = 1, finish = 2, metadata = 3 };
enum class DecoderResult : size_t { error = 0, success = 1, needs_more_input = 2, needs_more_output = 3 };

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
    int32_t quality = default_quality;
    int32_t lgwin = default_window;
    int32_t mode = default_mode;
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
    [[nodiscard]] bool set_parameter(int32_t parameter, uint32_t value) noexcept;
    [[nodiscard]] bool set_options(const Options& options = {}) noexcept;
    [[nodiscard]] bool compress(BufferCursor& dst, ConstBufferCursor& src, EncoderOperation op = EncoderOperation::process, size_t* total_out = nullptr) noexcept;
    [[nodiscard]] bool finished() const noexcept;
    [[nodiscard]] bool has_more_output() const noexcept;
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
    [[nodiscard]] bool set_parameter(int32_t parameter, uint32_t value) noexcept;
    [[nodiscard]] DecoderResult decompress(BufferCursor& dst, ConstBufferCursor& src, size_t* total_out = nullptr) noexcept;
    [[nodiscard]] bool used() const noexcept;
    [[nodiscard]] bool finished() const noexcept;
    [[nodiscard]] int32_t error_code_detail() const noexcept;
    void reset() noexcept;

private:
    void* handle_{};
};

[[nodiscard]] uint32_t encoder_version() noexcept;
[[nodiscard]] uint32_t decoder_version() noexcept;
[[nodiscard]] bool loaded() noexcept;
[[nodiscard]] std::string_view library_path() noexcept;
[[nodiscard]] std::string_view load_error() noexcept;
[[nodiscard]] const char* decoder_error_string(int32_t code) noexcept;
[[nodiscard]] size_t compress_bound(size_t input_size) noexcept;
[[nodiscard]] bool compress_to(void* dst, size_t& dst_size, const void* src, size_t src_size, const Options& options = {}) noexcept;
[[nodiscard]] DecoderResult decompress_to(void* dst, size_t& dst_size, const void* src, size_t src_size) noexcept;

[[nodiscard]] inline bool compress_to(std::span<byte> dst, size_t& dst_size, view_type src, const Options& options = {}) noexcept { return compress_to(dst.data(), dst_size, src.data(), src.size(), options); }
[[nodiscard]] inline DecoderResult decompress_to(std::span<byte> dst, size_t& dst_size, view_type src) noexcept { return decompress_to(dst.data(), dst_size, src.data(), src.size()); }
[[nodiscard]] inline bool is_error(DecoderResult result) noexcept { return result == DecoderResult::error; }

}
