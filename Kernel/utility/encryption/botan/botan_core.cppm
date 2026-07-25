module;
#include <cstddef>
#include <cstdint>
#include <span>
#include <string_view>
#include <vector>

export module utility.encryption.botan.botan_core;

export namespace botan_ns {

using byte = uint8_t;
using buffer_type = std::vector<byte>;
using view_type = std::span<const byte>;

enum class Direction : uint32_t { encrypt, decrypt };

[[nodiscard]] bool loaded() noexcept;
[[nodiscard]] uint32_t abi_version() noexcept;
[[nodiscard]] std::string_view library_path() noexcept;
[[nodiscard]] std::string_view load_error() noexcept;
[[nodiscard]] std::string_view last_error() noexcept;

class Hash {
public:
    explicit Hash(std::string_view algorithm) noexcept;
    Hash(const Hash&) = delete;
    Hash& operator=(const Hash&) = delete;
    Hash(Hash&& other) noexcept;
    Hash& operator=(Hash&& other) noexcept;
    ~Hash();

    [[nodiscard]] explicit operator bool() const noexcept { return handle_ != nullptr; }
    [[nodiscard]] size_t output_length() const noexcept;
    [[nodiscard]] bool update(view_type input) noexcept;
    [[nodiscard]] bool final(std::span<byte> output) noexcept;
    void reset() noexcept;

private:
    void* handle_{};
};

class Cipher {
public:
    Cipher(std::string_view algorithm, Direction direction) noexcept;
    Cipher(const Cipher&) = delete;
    Cipher& operator=(const Cipher&) = delete;
    Cipher(Cipher&& other) noexcept;
    Cipher& operator=(Cipher&& other) noexcept;
    ~Cipher();

    [[nodiscard]] explicit operator bool() const noexcept { return handle_ != nullptr; }
    [[nodiscard]] size_t default_nonce_length() const noexcept;
    [[nodiscard]] size_t output_length(size_t input_size) const noexcept;
    [[nodiscard]] bool set_key(view_type key) noexcept;
    [[nodiscard]] bool start(view_type nonce) noexcept;
    [[nodiscard]] bool process(std::span<byte> buffer, size_t& written) noexcept;
    [[nodiscard]] bool finish(std::span<byte> buffer, size_t input_size, size_t& written) noexcept;
    void reset() noexcept;

private:
    void* handle_{};
};

[[nodiscard]] size_t rsa_encrypt_bound(view_type public_key, std::string_view padding, size_t input_size) noexcept;
[[nodiscard]] bool rsa_encrypt_to(view_type public_key, std::string_view padding, view_type input, std::span<byte> output, size_t& written) noexcept;
[[nodiscard]] bool rsa_decrypt_to(view_type private_key, std::string_view password, std::string_view padding, view_type input, std::span<byte> output, size_t& written) noexcept;
[[nodiscard]] bool cipher_crypt_to(std::string_view algorithm, Direction direction, view_type key, view_type nonce, view_type input, std::span<byte> output, size_t& written) noexcept;

}
