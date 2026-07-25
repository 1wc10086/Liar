module;
#include <array>
#include <cstddef>
#include <cstdint>
#include <cstdio>
#include <dlfcn.h>
#include <mutex>
#include <string_view>
#include <utility>

module utility.differentiation.vcdiff.vcdiff_core;

namespace vcdiff {
namespace {

struct Api {
    using abi_version_t = uint32_t (*)();
    using dictionary_create_t = void* (*)(const void*, size_t);
    using dictionary_free_t = void (*)(void*);
    using encoder_create_t = void* (*)(const void*, size_t, uint32_t, uint32_t);
    using encoder_free_t = void (*)(void*);
    using encoder_output_t = int32_t (*)(void*, void*, size_t, size_t*);
    using encoder_encode_t = int32_t (*)(void*, const void*, size_t, void*, size_t, size_t*);
    using encode_t = int32_t (*)(const void*, size_t, const void*, size_t, uint32_t, uint32_t, void*, size_t, size_t*);
    using decoder_create_t = void* (*)();
    using decoder_free_t = void (*)(void*);
    using decoder_limit_t = int32_t (*)(void*, size_t);
    using decoder_allow_target_t = void (*)(void*, uint32_t);
    using decoder_start_t = int32_t (*)(void*, const void*, size_t);
    using decoder_decode_t = int32_t (*)(void*, const void*, size_t, void*, size_t, size_t*);
    using decoder_finish_t = int32_t (*)(void*);
    using decode_t = int32_t (*)(const void*, size_t, const void*, size_t, void*, size_t, size_t*, size_t, size_t, uint32_t);

    void* library{};
    const char* path{};
    const char* error{};
    abi_version_t abi_version{};
    dictionary_create_t dictionary_create{};
    dictionary_free_t dictionary_free{};
    encoder_create_t encoder_create{};
    encoder_free_t encoder_free{};
    encoder_output_t encoder_start{};
    encoder_encode_t encoder_encode{};
    encoder_output_t encoder_finish{};
    encode_t encode{};
    decoder_create_t decoder_create{};
    decoder_free_t decoder_free{};
    decoder_limit_t decoder_set_max_target_file_size{};
    decoder_limit_t decoder_set_max_target_window_size{};
    decoder_allow_target_t decoder_set_allow_target{};
    decoder_start_t decoder_start{};
    decoder_decode_t decoder_decode{};
    decoder_finish_t decoder_finish{};
    decode_t decode{};

    [[nodiscard]] bool ready() const noexcept { return library && abi_version && dictionary_create && dictionary_free && encoder_create && encoder_free && encoder_start && encoder_encode && encoder_finish && encode && decoder_create && decoder_free && decoder_set_max_target_file_size && decoder_set_max_target_window_size && decoder_set_allow_target && decoder_start && decoder_decode && decoder_finish && decode; }
};

Api api;
std::once_flag api_once;
std::array<char, 4096> fallback_path{};

template <class T>
void bind(T& output, const char* name) noexcept { output = reinterpret_cast<T>(dlsym(api.library, name)); }

void init_api() noexcept {
    constexpr const char* names[] = {"libMyNativeTexture.so", "MyNativeTexture.so"};
    for (auto name : names) {
        api.library = dlopen(name, RTLD_NOW | RTLD_LOCAL);
        if (api.library) { api.path = name; break; }
    }
    if (!api.library) {
        Dl_info info{};
        if (dladdr(reinterpret_cast<const void*>(&init_api), &info) && info.dli_fname) {
            std::string_view path{info.dli_fname};
            if (const auto slash = path.rfind('/'); slash != std::string_view::npos && slash + 1 < fallback_path.size()) {
                const auto dir = path.substr(0, slash);
                if (dir.size() + 23 < fallback_path.size()) {
                    std::snprintf(fallback_path.data(), fallback_path.size(), "%.*s/libMyNativeTexture.so", static_cast<int>(dir.size()), dir.data());
                    api.library = dlopen(fallback_path.data(), RTLD_NOW | RTLD_LOCAL);
                    if (api.library) api.path = fallback_path.data();
                }
            }
        }
    }
    if (!api.library) { api.error = dlerror(); return; }
    bind(api.abi_version, "kvcdiff_abi_version"); bind(api.dictionary_create, "kvcdiff_dictionary_create"); bind(api.dictionary_free, "kvcdiff_dictionary_free"); bind(api.encoder_create, "kvcdiff_encoder_create"); bind(api.encoder_free, "kvcdiff_encoder_free"); bind(api.encoder_start, "kvcdiff_encoder_start"); bind(api.encoder_encode, "kvcdiff_encoder_encode"); bind(api.encoder_finish, "kvcdiff_encoder_finish"); bind(api.encode, "kvcdiff_encode"); bind(api.decoder_create, "kvcdiff_decoder_create"); bind(api.decoder_free, "kvcdiff_decoder_free"); bind(api.decoder_set_max_target_file_size, "kvcdiff_decoder_set_max_target_file_size"); bind(api.decoder_set_max_target_window_size, "kvcdiff_decoder_set_max_target_window_size"); bind(api.decoder_set_allow_target, "kvcdiff_decoder_set_allow_target"); bind(api.decoder_start, "kvcdiff_decoder_start"); bind(api.decoder_decode, "kvcdiff_decoder_decode"); bind(api.decoder_finish, "kvcdiff_decoder_finish"); bind(api.decode, "kvcdiff_decode");
    if (!api.ready()) api.error = "missing required kvcdiff symbol";
}

Api& vcdiff_api() noexcept { std::call_once(api_once, init_api); return api; }

}

uint32_t abi_version() noexcept { auto& a = vcdiff_api(); return a.abi_version ? a.abi_version() : 0; }
bool loaded() noexcept { return vcdiff_api().ready(); }
std::string_view library_path() noexcept { auto& a = vcdiff_api(); return a.path ? a.path : ""; }
std::string_view load_error() noexcept { auto& a = vcdiff_api(); return a.error ? a.error : ""; }

Dictionary::Dictionary(view_type input) noexcept { auto& a = vcdiff_api(); if (a.dictionary_create) handle_ = a.dictionary_create(input.data(), input.size()); }
Dictionary::Dictionary(Dictionary&& other) noexcept : handle_(std::exchange(other.handle_, nullptr)) {}
Dictionary& Dictionary::operator=(Dictionary&& other) noexcept { if (this != &other) { reset(); handle_ = std::exchange(other.handle_, nullptr); } return *this; }
Dictionary::~Dictionary() { reset(); }
void Dictionary::reset() noexcept { auto& a = vcdiff_api(); if (handle_ && a.dictionary_free) a.dictionary_free(std::exchange(handle_, nullptr)); else handle_ = nullptr; }

Encoder::Encoder(view_type dictionary, Format format, bool target_matching) noexcept { auto& a = vcdiff_api(); if (a.encoder_create) handle_ = a.encoder_create(dictionary.data(), dictionary.size(), static_cast<uint32_t>(format), target_matching); }
Encoder::Encoder(Encoder&& other) noexcept : handle_(std::exchange(other.handle_, nullptr)) {}
Encoder& Encoder::operator=(Encoder&& other) noexcept { if (this != &other) { reset(); handle_ = std::exchange(other.handle_, nullptr); } return *this; }
Encoder::~Encoder() { reset(); }
bool Encoder::start(mutable_view_type output, size_t& written) noexcept { auto& a = vcdiff_api(); return handle_ && a.encoder_start && a.encoder_start(handle_, output.data(), output.size(), &written); }
bool Encoder::encode(view_type input, mutable_view_type output, size_t& written) noexcept { auto& a = vcdiff_api(); return handle_ && a.encoder_encode && a.encoder_encode(handle_, input.data(), input.size(), output.data(), output.size(), &written); }
bool Encoder::finish(mutable_view_type output, size_t& written) noexcept { auto& a = vcdiff_api(); return handle_ && a.encoder_finish && a.encoder_finish(handle_, output.data(), output.size(), &written); }
void Encoder::reset() noexcept { auto& a = vcdiff_api(); if (handle_ && a.encoder_free) a.encoder_free(std::exchange(handle_, nullptr)); else handle_ = nullptr; }

Decoder::Decoder() noexcept { auto& a = vcdiff_api(); if (a.decoder_create) handle_ = a.decoder_create(); }
Decoder::Decoder(Decoder&& other) noexcept : handle_(std::exchange(other.handle_, nullptr)) {}
Decoder& Decoder::operator=(Decoder&& other) noexcept { if (this != &other) { reset(); handle_ = std::exchange(other.handle_, nullptr); } return *this; }
Decoder::~Decoder() { reset(); }
bool Decoder::max_target_file_size(size_t size) noexcept { auto& a = vcdiff_api(); return handle_ && a.decoder_set_max_target_file_size && a.decoder_set_max_target_file_size(handle_, size); }
bool Decoder::max_target_window_size(size_t size) noexcept { auto& a = vcdiff_api(); return handle_ && a.decoder_set_max_target_window_size && a.decoder_set_max_target_window_size(handle_, size); }
void Decoder::allow_target(bool allow) noexcept { auto& a = vcdiff_api(); if (handle_ && a.decoder_set_allow_target) a.decoder_set_allow_target(handle_, allow); }
bool Decoder::start(view_type dictionary) noexcept { auto& a = vcdiff_api(); return handle_ && a.decoder_start && a.decoder_start(handle_, dictionary.data(), dictionary.size()); }
bool Decoder::decode(view_type input, mutable_view_type output, size_t& written) noexcept { auto& a = vcdiff_api(); return handle_ && a.decoder_decode && a.decoder_decode(handle_, input.data(), input.size(), output.data(), output.size(), &written); }
bool Decoder::finish() noexcept { auto& a = vcdiff_api(); return handle_ && a.decoder_finish && a.decoder_finish(handle_); }
void Decoder::reset() noexcept { auto& a = vcdiff_api(); if (handle_ && a.decoder_free) a.decoder_free(std::exchange(handle_, nullptr)); else handle_ = nullptr; }

bool encode_to(view_type dictionary, view_type input, mutable_view_type output, size_t& written, Format format, bool target_matching) noexcept { auto& a = vcdiff_api(); return a.encode && a.encode(dictionary.data(), dictionary.size(), input.data(), input.size(), static_cast<uint32_t>(format), target_matching, output.data(), output.size(), &written); }
bool decode_to(view_type dictionary, view_type input, mutable_view_type output, size_t& written, size_t max_target_file_size, size_t max_target_window_size, bool allow_target) noexcept { auto& a = vcdiff_api(); return a.decode && a.decode(dictionary.data(), dictionary.size(), input.data(), input.size(), output.data(), output.size(), &written, max_target_file_size, max_target_window_size, allow_target); }

}
