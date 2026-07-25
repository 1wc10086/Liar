#include <mbedtls/cipher.h>
#include <mbedtls/ctr_drbg.h>
#include <mbedtls/entropy.h>
#include <mbedtls/error.h>
#include <mbedtls/md.h>
#include <mbedtls/pk.h>
#include <mbedtls/rsa.h>
#include "blake3.h"
#include <algorithm>
#include <cstddef>
#include <cstdint>
#include <cstring>
#include <memory>
#include <new>
#include <string>
#include <string_view>
#include <vector>

namespace {

thread_local std::string error;

int fail(std::string_view message) noexcept { error.assign(message); return -1; }
int check(int result, std::string_view operation) noexcept {
    if (result == 0) return 0;
    char detail[128]{};
    mbedtls_strerror(result, detail, sizeof(detail));
    error.assign(operation).append(": ").append(detail);
    return -1;
}

int setup_cipher(mbedtls_cipher_context_t& context, const mbedtls_cipher_info_t* info, std::string_view algorithm) noexcept {
    if (check(mbedtls_cipher_setup(&context, info), "cipher setup")) return -1;
    if (algorithm.ends_with("-CBC")) return check(mbedtls_cipher_set_padding_mode(&context, MBEDTLS_PADDING_PKCS7), "cipher padding setup");
    return 0;
}

struct Hash final {
    const mbedtls_md_info_t* info{};
    mbedtls_md_context_t mbed{};
    blake3_hasher blake{};
    bool is_blake{};

    int status{};
    explicit Hash(std::string_view algorithm) : is_blake(algorithm == "BLAKE3") {
        mbedtls_md_init(&mbed);
        if (is_blake) blake3_hasher_init(&blake);
        else if ((info = mbedtls_md_info_from_string(std::string{algorithm}.c_str())) != nullptr) status = mbedtls_md_setup(&mbed, info, 0);
    }
    ~Hash() { mbedtls_md_free(&mbed); }
    [[nodiscard]] bool valid() const noexcept { return (is_blake || info != nullptr) && status == 0; }
};

struct Cipher final {
    const mbedtls_cipher_info_t* info{};
    mbedtls_cipher_context_t mbed{};
    uint32_t direction{};

    Cipher(std::string_view algorithm, uint32_t value) : direction(value) {
        mbedtls_cipher_init(&mbed);
        info = mbedtls_cipher_info_from_string(std::string{algorithm}.c_str());
    }
    ~Cipher() { mbedtls_cipher_free(&mbed); }
    [[nodiscard]] bool valid() const noexcept { return info != nullptr; }
};

bool configure_rsa_padding(mbedtls_pk_context& key, std::string_view padding) noexcept {
    if (!mbedtls_pk_can_do(&key, MBEDTLS_PK_RSA)) return false;
    auto* rsa = mbedtls_pk_rsa(key);
    if (padding == "PKCS1v15") return mbedtls_rsa_set_padding(rsa, MBEDTLS_RSA_PKCS_V15, MBEDTLS_MD_NONE) == 0;
    if (padding == "OAEP(SHA-256)") return mbedtls_rsa_set_padding(rsa, MBEDTLS_RSA_PKCS_V21, MBEDTLS_MD_SHA256) == 0;
    if (padding == "OAEP(SHA-384)") return mbedtls_rsa_set_padding(rsa, MBEDTLS_RSA_PKCS_V21, MBEDTLS_MD_SHA384) == 0;
    if (padding == "OAEP(SHA-512)") return mbedtls_rsa_set_padding(rsa, MBEDTLS_RSA_PKCS_V21, MBEDTLS_MD_SHA512) == 0;
    return false;
}

struct Rng final {
    mbedtls_entropy_context entropy;
    mbedtls_ctr_drbg_context drbg;
    Rng() {
        mbedtls_entropy_init(&entropy);
        mbedtls_ctr_drbg_init(&drbg);
    }
    ~Rng() { mbedtls_ctr_drbg_free(&drbg); mbedtls_entropy_free(&entropy); }
    [[nodiscard]] int seed() noexcept { return mbedtls_ctr_drbg_seed(&drbg, mbedtls_entropy_func, &entropy, nullptr, 0); }
};

int parse_public_key(mbedtls_pk_context& key, const uint8_t* data, size_t size) noexcept {
    if (size >= 11 && std::memcmp(data, "-----BEGIN ", 11) == 0) {
        std::vector<uint8_t> pem(data, data + size);
        pem.push_back(0);
        return mbedtls_pk_parse_public_key(&key, pem.data(), pem.size());
    }
    return mbedtls_pk_parse_public_key(&key, data, size);
}

int parse_private_key(mbedtls_pk_context& key, const uint8_t* data, size_t size, const uint8_t* password, size_t password_size, Rng& rng) noexcept {
    if (size >= 11 && std::memcmp(data, "-----BEGIN ", 11) == 0) {
        std::vector<uint8_t> pem(data, data + size);
        pem.push_back(0);
        return mbedtls_pk_parse_key(&key, pem.data(), pem.size(), password, password_size, mbedtls_ctr_drbg_random, &rng.drbg);
    }
    return mbedtls_pk_parse_key(&key, data, size, password, password_size, mbedtls_ctr_drbg_random, &rng.drbg);
}

}

extern "C" {

const char* kbotan_error() noexcept { return error.c_str(); }
uint32_t kbotan_abi_version() noexcept { return 2; }

Hash* kbotan_hash_create(const char* algorithm, size_t size) noexcept {
    if (!algorithm) { fail("invalid hash algorithm"); return nullptr; }
    try {
        auto value = std::make_unique<Hash>(std::string_view{algorithm, size});
        if (!value->valid()) { fail("unsupported hash algorithm"); return nullptr; }
        if (!value->is_blake && check(mbedtls_md_starts(&value->mbed), "hash initialization")) return nullptr;
        return value.release();
    } catch (...) { fail("hash creation failed"); return nullptr; }
}
void kbotan_hash_destroy(Hash* hash) noexcept { delete hash; }
size_t kbotan_hash_output_length(const Hash* hash) noexcept { return !hash ? 0 : hash->is_blake ? BLAKE3_OUT_LEN : mbedtls_md_get_size(hash->info); }
int kbotan_hash_update(Hash* hash, const uint8_t* input, size_t size) noexcept {
    if (!hash || (!input && size)) return fail("invalid hash input");
    if (hash->is_blake) { blake3_hasher_update(&hash->blake, input, size); return 0; }
    return check(mbedtls_md_update(&hash->mbed, input, size), "hash update");
}
int kbotan_hash_final(Hash* hash, uint8_t* output, size_t size) noexcept {
    if (!hash || !output || size < kbotan_hash_output_length(hash)) return fail("invalid hash output");
    if (hash->is_blake) { blake3_hasher_finalize(&hash->blake, output, BLAKE3_OUT_LEN); return 0; }
    return check(mbedtls_md_finish(&hash->mbed, output), "hash finalization");
}

Cipher* kbotan_cipher_create(const char* algorithm, size_t size, uint32_t direction) noexcept {
    if (!algorithm || direction > 1) { fail("invalid cipher algorithm"); return nullptr; }
    try {
        auto value = std::make_unique<Cipher>(std::string_view{algorithm, size}, direction);
        if (!value->valid()) { fail("unsupported cipher algorithm"); return nullptr; }
        if (setup_cipher(value->mbed, value->info, std::string_view{algorithm, size})) return nullptr;
        return value.release();
    } catch (...) { fail("cipher creation failed"); return nullptr; }
}
void kbotan_cipher_destroy(Cipher* cipher) noexcept { delete cipher; }
size_t kbotan_cipher_default_nonce_length(const Cipher* cipher) noexcept { return cipher ? mbedtls_cipher_get_iv_size(&cipher->mbed) : 0; }
size_t kbotan_cipher_output_length(const Cipher* cipher, size_t input_size) noexcept { return cipher ? input_size + mbedtls_cipher_get_block_size(&cipher->mbed) : 0; }
int kbotan_cipher_set_key(Cipher* cipher, const uint8_t* key, size_t size) noexcept { return cipher && key ? check(mbedtls_cipher_setkey(&cipher->mbed, key, static_cast<int>(size * 8), cipher->direction == 0 ? MBEDTLS_ENCRYPT : MBEDTLS_DECRYPT), "cipher key setup") : fail("invalid cipher key"); }
int kbotan_cipher_start(Cipher* cipher, const uint8_t* nonce, size_t size) noexcept { return cipher && (nonce || !size) ? check(mbedtls_cipher_set_iv(&cipher->mbed, nonce, size), "cipher nonce setup") : fail("invalid cipher nonce"); }
int kbotan_cipher_process(Cipher*, uint8_t*, size_t, size_t*) noexcept { return fail("use one-shot cipher API"); }
int kbotan_cipher_finish(Cipher*, uint8_t*, size_t, size_t, size_t*) noexcept { return fail("use one-shot cipher API"); }

int kbotan_cipher_crypt(const char* algorithm, size_t algorithm_size, uint32_t direction, const uint8_t* key, size_t key_size, const uint8_t* nonce, size_t nonce_size, const uint8_t* input, size_t input_size, uint8_t* output, size_t output_size, size_t* written) noexcept {
    if (!algorithm || !key || (!nonce && nonce_size) || (!input && input_size) || !output || !written || direction > 1) return fail("invalid cipher input");
    const std::string_view algorithm_name{algorithm, algorithm_size};
    Cipher cipher{algorithm_name, direction};
    if (!cipher.valid()) return fail("unsupported one-shot cipher algorithm");
    if (setup_cipher(cipher.mbed, cipher.info, algorithm_name) || check(mbedtls_cipher_setkey(&cipher.mbed, key, static_cast<int>(key_size * 8), direction == 0 ? MBEDTLS_ENCRYPT : MBEDTLS_DECRYPT), "cipher key setup") || check(mbedtls_cipher_set_iv(&cipher.mbed, nonce, nonce_size), "cipher nonce setup") || check(mbedtls_cipher_reset(&cipher.mbed), "cipher reset")) return -1;
    if (output_size < input_size + mbedtls_cipher_get_block_size(&cipher.mbed)) return fail("cipher output buffer too small");
    size_t update{};
    size_t final{};
    if (check(mbedtls_cipher_update(&cipher.mbed, input, input_size, output, &update), "cipher update") || check(mbedtls_cipher_finish(&cipher.mbed, output + update, &final), "cipher finalization")) return -1;
    *written = update + final;
    return 0;
}

size_t kbotan_rsa_encrypt_bound(const uint8_t* key_data, size_t key_size, const char*, size_t, size_t) noexcept {
    mbedtls_pk_context key;
    mbedtls_pk_init(&key);
    const auto result = parse_public_key(key, key_data, key_size);
    const auto size = result == 0 ? mbedtls_pk_get_len(&key) : 0;
    mbedtls_pk_free(&key);
    return size;
}
int kbotan_rsa_encrypt(const uint8_t* key_data, size_t key_size, const char* padding, size_t padding_size, const uint8_t* input, size_t input_size, uint8_t* output, size_t output_size, size_t* written) noexcept {
    if (!key_data || !padding || (!input && input_size) || !output || !written) return fail("invalid RSA encryption input");
    mbedtls_pk_context key; mbedtls_pk_init(&key);
    Rng rng;
    auto result = check(rng.seed(), "RSA random initialization");
    if (!result) result = check(parse_public_key(key, key_data, key_size), "RSA public key parse");
    if (!result && !configure_rsa_padding(key, {padding, padding_size})) result = fail("unsupported RSA padding");
    if (!result && output_size < mbedtls_pk_get_len(&key)) result = fail("RSA output buffer too small");
    if (!result) result = check(mbedtls_pk_encrypt(&key, input, input_size, output, written, output_size, mbedtls_ctr_drbg_random, &rng.drbg), "RSA encryption");
    mbedtls_pk_free(&key);
    return result;
}
int kbotan_rsa_decrypt(const uint8_t* key_data, size_t key_size, const char* password, size_t password_size, const char* padding, size_t padding_size, const uint8_t* input, size_t input_size, uint8_t* output, size_t output_size, size_t* written) noexcept {
    if (!key_data || !password || !padding || (!input && input_size) || !output || !written) return fail("invalid RSA decryption input");
    mbedtls_pk_context key; mbedtls_pk_init(&key);
    Rng rng;
    auto result = check(rng.seed(), "RSA random initialization");
    if (!result) result = check(parse_private_key(key, key_data, key_size, reinterpret_cast<const uint8_t*>(password), password_size, rng), "RSA private key parse");
    if (!result && !configure_rsa_padding(key, {padding, padding_size})) result = fail("unsupported RSA padding");
    if (!result) result = check(mbedtls_pk_decrypt(&key, input, input_size, output, written, output_size, mbedtls_ctr_drbg_random, &rng.drbg), "RSA decryption");
    mbedtls_pk_free(&key);
    return result;
}

}
