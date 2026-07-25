module;
#include <cstddef>
#include <cstdint>
#include <span>
#include <functional>
#include <string_view>
#include <vector>

export module utility.requests.cpr.cpr_core;

export namespace cpr_ns {

using byte = uint8_t;
using bytes_view = std::span<const byte>;

struct Header {
    std::string_view name;
    std::string_view value;
};

struct Request {
    std::string_view url;
    std::string_view method{"GET"};
    std::span<const Header> headers;
    bytes_view body;
    int64_t timeout_ms{};
    int64_t connect_timeout_ms{};
    bool follow_redirects{true};
    bool verify_peer{true};
};

struct Response {
    int32_t code{};
    int64_t status{};
    double elapsed{};
    std::vector<byte> body;
    std::vector<byte> headers;

    [[nodiscard]] explicit operator bool() const noexcept { return code == 0; }
    [[nodiscard]] std::string_view error() const noexcept;
};

struct StreamCallbacks {
    std::function<bool(bytes_view)> write;
    std::function<bool(bytes_view)> header;
    std::function<size_t(std::span<byte>)> read;
    std::function<bool(int64_t, int64_t, int64_t, int64_t)> progress;
    int64_t upload_size{-1};
};

class Easy {
public:
    Easy() noexcept;
    Easy(const Easy&) = delete;
    Easy& operator=(const Easy&) = delete;
    Easy(Easy&&) noexcept;
    Easy& operator=(Easy&&) noexcept;
    ~Easy();

    [[nodiscard]] explicit operator bool() const noexcept;
    [[nodiscard]] bool set_long(int32_t option, int64_t value) noexcept;
    [[nodiscard]] bool set_off_t(int32_t option, int64_t value) noexcept;
    [[nodiscard]] int64_t get_long(int32_t info) noexcept;
    [[nodiscard]] double get_double(int32_t info) noexcept;
    [[nodiscard]] Response perform(const Request& request);
    [[nodiscard]] Response perform_stream(const Request& request, StreamCallbacks& callbacks);
    void reset() noexcept;

private:
    void* handle_{};
};

[[nodiscard]] bool loaded() noexcept;
[[nodiscard]] uint32_t abi_version() noexcept;
[[nodiscard]] std::string_view version() noexcept;
[[nodiscard]] std::string_view library_path() noexcept;
[[nodiscard]] std::string_view load_error() noexcept;
[[nodiscard]] std::string_view error(int32_t code) noexcept;
[[nodiscard]] Response request(const Request& request);
[[nodiscard]] Response request_stream(const Request& request, StreamCallbacks& callbacks);

}
