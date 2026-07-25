module;
#include <cstddef>
#include <optional>
#include <string_view>

export module utility.encryption.botan.botan_encryption;

import utility.encryption.botan.botan_core;

export namespace botan_ns {

class Encryption {
public:
    [[nodiscard]] static std::optional<buffer_type> hash(std::string_view algorithm, view_type input) {
        Hash context{algorithm};
        if (!context) return std::nullopt;
        buffer_type output(context.output_length());
        return context.update(input) && context.final(output) ? std::optional<buffer_type>{std::move(output)} : std::nullopt;
    }

    [[nodiscard]] static std::optional<buffer_type> cipher(std::string_view algorithm, Direction direction, view_type input, view_type key, view_type nonce) {
        buffer_type output(input.size() + 32);
        size_t written{};
        if (!cipher_crypt_to(algorithm, direction, key, nonce, input, output, written)) return std::nullopt;
        output.resize(written);
        return output;
    }

    [[nodiscard]] static std::optional<buffer_type> rsa_encrypt(view_type public_key, std::string_view padding, view_type input) {
        const auto size = rsa_encrypt_bound(public_key, padding, input.size());
        if (!size) return std::nullopt;
        buffer_type output(size);
        size_t written{};
        if (!rsa_encrypt_to(public_key, padding, input, output, written)) return std::nullopt;
        output.resize(written);
        return output;
    }

    [[nodiscard]] static std::optional<buffer_type> rsa_decrypt(view_type private_key, std::string_view password, std::string_view padding, view_type input) {
        buffer_type output(input.size());
        size_t written{};
        if (!rsa_decrypt_to(private_key, password, padding, input, output, written)) return std::nullopt;
        output.resize(written);
        return output;
    }
};

}
