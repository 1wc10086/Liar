module;
#include <array>
#include <cstddef>
#include <cstdint>
#include <cstdio>
#include <dlfcn.h>
#include <mutex>
#include <string_view>
#include <utility>

module utility.encryption.botan.botan_core;

namespace botan_ns {
namespace {

struct Api {
    using error_t = const char* (*)();
    using abi_version_t = uint32_t (*)();
    using hash_create_t = void* (*)(const char*, size_t);
    using hash_destroy_t = void (*)(void*);
    using hash_output_length_t = size_t (*)(const void*);
    using hash_update_t = int (*)(void*, const byte*, size_t);
    using hash_final_t = int (*)(void*, byte*, size_t);
    using cipher_create_t = void* (*)(const char*, size_t, uint32_t);
    using cipher_destroy_t = void (*)(void*);
    using cipher_size_t = size_t (*)(const void*);
    using cipher_output_length_t = size_t (*)(const void*, size_t);
    using cipher_set_key_t = int (*)(void*, const byte*, size_t);
    using cipher_start_t = int (*)(void*, const byte*, size_t);
    using cipher_process_t = int (*)(void*, byte*, size_t, size_t*);
    using cipher_finish_t = int (*)(void*, byte*, size_t, size_t, size_t*);
    using rsa_bound_t = size_t (*)(const byte*, size_t, const char*, size_t, size_t);
    using rsa_encrypt_t = int (*)(const byte*, size_t, const char*, size_t, const byte*, size_t, byte*, size_t, size_t*);
    using rsa_decrypt_t = int (*)(const byte*, size_t, const char*, size_t, const char*, size_t, const byte*, size_t, byte*, size_t, size_t*);
    using cipher_crypt_t = int (*)(const char*, size_t, uint32_t, const byte*, size_t, const byte*, size_t, const byte*, size_t, byte*, size_t, size_t*);

    void* lib{};
    const char* path{};
    const char* error{};
    error_t last_error{};
    abi_version_t abi_version{};
    hash_create_t hash_create{};
    hash_destroy_t hash_destroy{};
    hash_output_length_t hash_output_length{};
    hash_update_t hash_update{};
    hash_final_t hash_final{};
    cipher_create_t cipher_create{};
    cipher_destroy_t cipher_destroy{};
    cipher_size_t cipher_default_nonce_length{};
    cipher_output_length_t cipher_output_length{};
    cipher_set_key_t cipher_set_key{};
    cipher_start_t cipher_start{};
    cipher_process_t cipher_process{};
    cipher_finish_t cipher_finish{};
    rsa_bound_t rsa_encrypt_bound{};
    rsa_encrypt_t rsa_encrypt{};
    rsa_decrypt_t rsa_decrypt{};
    cipher_crypt_t cipher_crypt{};

    [[nodiscard]] bool ready() const noexcept {
        return lib && last_error && abi_version && hash_create && hash_destroy && hash_output_length && hash_update && hash_final && cipher_create && cipher_destroy && cipher_default_nonce_length && cipher_output_length && cipher_set_key && cipher_start && cipher_process && cipher_finish && rsa_encrypt_bound && rsa_encrypt && rsa_decrypt && cipher_crypt;
    }
};

Api api;
std::once_flag api_once;
std::array<char, 4096> fallback_path{};

template <class T>
void bind(T& out, const char* name) noexcept { out = reinterpret_cast<T>(dlsym(api.lib, name)); }

void init_api() noexcept {
    constexpr const char* names[] = {"libbotan.so", "libbotan.so.3"};
    for (const auto* name : names) {
        api.lib = dlopen(name, RTLD_NOW | RTLD_LOCAL);
        if (api.lib) { api.path = name; break; }
    }
    if (!api.lib) {
        Dl_info info{};
        if (dladdr(reinterpret_cast<const void*>(&init_api), &info) && info.dli_fname) {
            const std::string_view path{info.dli_fname};
            const auto slash = path.rfind('/');
            if (slash != std::string_view::npos && slash + 13 < fallback_path.size()) {
                const auto dir = path.substr(0, slash);
                std::snprintf(fallback_path.data(), fallback_path.size(), "%.*s/libbotan.so", static_cast<int>(dir.size()), dir.data());
                api.lib = dlopen(fallback_path.data(), RTLD_NOW | RTLD_LOCAL);
                if (api.lib) api.path = fallback_path.data();
            }
        }
    }
    if (!api.lib) { api.error = dlerror(); return; }
    bind(api.last_error, "kbotan_error"); bind(api.abi_version, "kbotan_abi_version");
    bind(api.hash_create, "kbotan_hash_create"); bind(api.hash_destroy, "kbotan_hash_destroy"); bind(api.hash_output_length, "kbotan_hash_output_length"); bind(api.hash_update, "kbotan_hash_update"); bind(api.hash_final, "kbotan_hash_final");
    bind(api.cipher_create, "kbotan_cipher_create"); bind(api.cipher_destroy, "kbotan_cipher_destroy"); bind(api.cipher_default_nonce_length, "kbotan_cipher_default_nonce_length"); bind(api.cipher_output_length, "kbotan_cipher_output_length"); bind(api.cipher_set_key, "kbotan_cipher_set_key"); bind(api.cipher_start, "kbotan_cipher_start"); bind(api.cipher_process, "kbotan_cipher_process"); bind(api.cipher_finish, "kbotan_cipher_finish");
    bind(api.rsa_encrypt_bound, "kbotan_rsa_encrypt_bound"); bind(api.rsa_encrypt, "kbotan_rsa_encrypt"); bind(api.rsa_decrypt, "kbotan_rsa_decrypt");
    bind(api.cipher_crypt, "kbotan_cipher_crypt");
    if (!api.ready()) api.error = "missing required kbotan symbol";
}

Api& botan() noexcept { std::call_once(api_once, init_api); return api; }

}

bool loaded() noexcept { return botan().ready(); }
uint32_t abi_version() noexcept { auto& a = botan(); return a.abi_version ? a.abi_version() : 0; }
std::string_view library_path() noexcept { auto& a = botan(); return a.path ? a.path : ""; }
std::string_view load_error() noexcept { auto& a = botan(); return a.error ? a.error : ""; }
std::string_view last_error() noexcept { auto& a = botan(); return a.last_error ? a.last_error() : load_error(); }

Hash::Hash(std::string_view algorithm) noexcept { auto& a = botan(); if (a.hash_create) handle_ = a.hash_create(algorithm.data(), algorithm.size()); }
Hash::Hash(Hash&& other) noexcept : handle_(std::exchange(other.handle_, nullptr)) {}
Hash& Hash::operator=(Hash&& other) noexcept { if (this != &other) { reset(); handle_ = std::exchange(other.handle_, nullptr); } return *this; }
Hash::~Hash() { reset(); }
size_t Hash::output_length() const noexcept { auto& a = botan(); return handle_ && a.hash_output_length ? a.hash_output_length(handle_) : 0; }
bool Hash::update(view_type input) noexcept { auto& a = botan(); return handle_ && a.hash_update && a.hash_update(handle_, input.data(), input.size()) == 0; }
bool Hash::final(std::span<byte> output) noexcept { auto& a = botan(); return handle_ && a.hash_final && a.hash_final(handle_, output.data(), output.size()) == 0; }
void Hash::reset() noexcept { auto& a = botan(); if (handle_ && a.hash_destroy) a.hash_destroy(std::exchange(handle_, nullptr)); else handle_ = nullptr; }

Cipher::Cipher(std::string_view algorithm, Direction direction) noexcept { auto& a = botan(); if (a.cipher_create) handle_ = a.cipher_create(algorithm.data(), algorithm.size(), static_cast<uint32_t>(direction)); }
Cipher::Cipher(Cipher&& other) noexcept : handle_(std::exchange(other.handle_, nullptr)) {}
Cipher& Cipher::operator=(Cipher&& other) noexcept { if (this != &other) { reset(); handle_ = std::exchange(other.handle_, nullptr); } return *this; }
Cipher::~Cipher() { reset(); }
size_t Cipher::default_nonce_length() const noexcept { auto& a = botan(); return handle_ && a.cipher_default_nonce_length ? a.cipher_default_nonce_length(handle_) : 0; }
size_t Cipher::output_length(size_t input_size) const noexcept { auto& a = botan(); return handle_ && a.cipher_output_length ? a.cipher_output_length(handle_, input_size) : 0; }
bool Cipher::set_key(view_type key) noexcept { auto& a = botan(); return handle_ && a.cipher_set_key && a.cipher_set_key(handle_, key.data(), key.size()) == 0; }
bool Cipher::start(view_type nonce) noexcept { auto& a = botan(); return handle_ && a.cipher_start && a.cipher_start(handle_, nonce.data(), nonce.size()) == 0; }
bool Cipher::process(std::span<byte> buffer, size_t& written) noexcept { auto& a = botan(); return handle_ && a.cipher_process && a.cipher_process(handle_, buffer.data(), buffer.size(), &written) == 0; }
bool Cipher::finish(std::span<byte> buffer, size_t input_size, size_t& written) noexcept { auto& a = botan(); return handle_ && input_size <= buffer.size() && a.cipher_finish && a.cipher_finish(handle_, buffer.data(), input_size, buffer.size(), &written) == 0; }
void Cipher::reset() noexcept { auto& a = botan(); if (handle_ && a.cipher_destroy) a.cipher_destroy(std::exchange(handle_, nullptr)); else handle_ = nullptr; }

size_t rsa_encrypt_bound(view_type public_key, std::string_view padding, size_t input_size) noexcept { auto& a = botan(); return a.rsa_encrypt_bound ? a.rsa_encrypt_bound(public_key.data(), public_key.size(), padding.data(), padding.size(), input_size) : 0; }
bool rsa_encrypt_to(view_type public_key, std::string_view padding, view_type input, std::span<byte> output, size_t& written) noexcept { auto& a = botan(); return a.rsa_encrypt && a.rsa_encrypt(public_key.data(), public_key.size(), padding.data(), padding.size(), input.data(), input.size(), output.data(), output.size(), &written) == 0; }
bool rsa_decrypt_to(view_type private_key, std::string_view password, std::string_view padding, view_type input, std::span<byte> output, size_t& written) noexcept { auto& a = botan(); return a.rsa_decrypt && a.rsa_decrypt(private_key.data(), private_key.size(), password.data(), password.size(), padding.data(), padding.size(), input.data(), input.size(), output.data(), output.size(), &written) == 0; }
bool cipher_crypt_to(std::string_view algorithm, Direction direction, view_type key, view_type nonce, view_type input, std::span<byte> output, size_t& written) noexcept { auto& a = botan(); return a.cipher_crypt && a.cipher_crypt(algorithm.data(), algorithm.size(), static_cast<uint32_t>(direction), key.data(), key.size(), nonce.data(), nonce.size(), input.data(), input.size(), output.data(), output.size(), &written) == 0; }

}
