#include <cstddef>
#include <cstdint>
#include <limits>
#include <memory>
#include <new>
#include <span>
#include <vector>
#include "google/output_string.h"
#include "google/vcdecoder.h"
#include "google/vcencoder.h"

namespace {

class Buffer final : public open_vcdiff::OutputStringInterface {
public:
    explicit Buffer(std::span<uint8_t> output) noexcept : output_(output) {}

    Buffer& append(const char* data, size_t size) override {
        if (!write(data, size)) failed_ = true;
        return *this;
    }

    void clear() override { size_ = 0; }

    void push_back(char value) override {
        if (!write(&value, 1)) failed_ = true;
    }

    void ReserveAdditionalBytes(size_t size) override {
        if (size > output_.size() - size_) failed_ = true;
    }

    [[nodiscard]] size_t size() const override { return size_; }
    [[nodiscard]] bool valid() const noexcept { return !failed_; }

private:
    [[nodiscard]] bool write(const char* data, size_t size) noexcept {
        if (size > output_.size() - size_) return false;
        auto destination = reinterpret_cast<char*>(output_.data()) + size_;
        __builtin_memcpy(destination, data, size);
        size_ += size;
        return true;
    }

    std::span<uint8_t> output_;
    size_t size_{};
    bool failed_{};
};

struct Dictionary {
    explicit Dictionary(const void* data, size_t size) : value(static_cast<const char*>(data), size), initialized(value.Init()) {}

    open_vcdiff::HashedDictionary value;
    bool initialized;
};

struct Encoder {
    Encoder(const void* dictionary, size_t dictionary_size, uint32_t flags, bool target_matching)
        : dictionary(dictionary, dictionary_size), value(&this->dictionary.value, static_cast<open_vcdiff::VCDiffFormatExtensionFlags>(flags), target_matching) {}

    Dictionary dictionary;
    open_vcdiff::VCDiffStreamingEncoder value;
};

struct Decoder {
    open_vcdiff::VCDiffStreamingDecoder value;
    bool started{};
};

[[nodiscard]] bool valid(const void* data, size_t size) noexcept {
    return data || size == 0;
}

}

extern "C" {

uint32_t kvcdiff_abi_version() noexcept { return 1; }

void* kvcdiff_dictionary_create(const void* data, size_t size) noexcept {
    if (!valid(data, size)) return nullptr;
    try {
        auto dictionary = std::make_unique<Dictionary>(data, size);
        return dictionary->initialized ? dictionary.release() : nullptr;
    } catch (...) {
        return nullptr;
    }
}

void kvcdiff_dictionary_free(void* handle) noexcept { delete static_cast<Dictionary*>(handle); }

void* kvcdiff_encoder_create(const void* dictionary, size_t dictionary_size, uint32_t flags, uint32_t target_matching) noexcept {
    if (!valid(dictionary, dictionary_size)) return nullptr;
    try {
        auto encoder = std::make_unique<Encoder>(dictionary, dictionary_size, flags, target_matching != 0);
        return encoder->dictionary.initialized ? encoder.release() : nullptr;
    } catch (...) {
        return nullptr;
    }
}

void kvcdiff_encoder_free(void* handle) noexcept { delete static_cast<Encoder*>(handle); }

int32_t kvcdiff_encoder_start(void* handle, void* output, size_t output_capacity, size_t* output_size) noexcept {
    if (!handle || !output_size || !valid(output, output_capacity)) return 0;
    Buffer destination{std::span{static_cast<uint8_t*>(output), output_capacity}};
    const auto result = static_cast<Encoder*>(handle)->value.StartEncodingToInterface(&destination);
    *output_size = destination.size();
    return result && destination.valid();
}

int32_t kvcdiff_encoder_encode(void* handle, const void* input, size_t input_size, void* output, size_t output_capacity, size_t* output_size) noexcept {
    if (!handle || !output_size || !valid(input, input_size) || !valid(output, output_capacity)) return 0;
    Buffer destination{std::span{static_cast<uint8_t*>(output), output_capacity}};
    const auto result = static_cast<Encoder*>(handle)->value.EncodeChunkToInterface(static_cast<const char*>(input), input_size, &destination);
    *output_size = destination.size();
    return result && destination.valid();
}

int32_t kvcdiff_encoder_finish(void* handle, void* output, size_t output_capacity, size_t* output_size) noexcept {
    if (!handle || !output_size || !valid(output, output_capacity)) return 0;
    Buffer destination{std::span{static_cast<uint8_t*>(output), output_capacity}};
    const auto result = static_cast<Encoder*>(handle)->value.FinishEncodingToInterface(&destination);
    *output_size = destination.size();
    return result && destination.valid();
}

int32_t kvcdiff_encode(const void* dictionary, size_t dictionary_size, const void* input, size_t input_size, uint32_t flags, uint32_t target_matching, void* output, size_t output_capacity, size_t* output_size) noexcept {
    if (!output_size || !valid(dictionary, dictionary_size) || !valid(input, input_size) || !valid(output, output_capacity)) return 0;
    Encoder encoder{dictionary, dictionary_size, flags, target_matching != 0};
    if (!encoder.dictionary.initialized) return 0;
    Buffer destination{std::span{static_cast<uint8_t*>(output), output_capacity}};
    const auto result = encoder.value.StartEncodingToInterface(&destination) && encoder.value.EncodeChunkToInterface(static_cast<const char*>(input), input_size, &destination) && encoder.value.FinishEncodingToInterface(&destination);
    *output_size = destination.size();
    return result && destination.valid();
}

void* kvcdiff_decoder_create() noexcept {
    try {
        return new Decoder;
    } catch (...) {
        return nullptr;
    }
}

void kvcdiff_decoder_free(void* handle) noexcept { delete static_cast<Decoder*>(handle); }

int32_t kvcdiff_decoder_set_max_target_file_size(void* handle, size_t size) noexcept {
    return handle && static_cast<Decoder*>(handle)->value.SetMaximumTargetFileSize(size);
}

int32_t kvcdiff_decoder_set_max_target_window_size(void* handle, size_t size) noexcept {
    return handle && static_cast<Decoder*>(handle)->value.SetMaximumTargetWindowSize(size);
}

void kvcdiff_decoder_set_allow_target(void* handle, uint32_t allow) noexcept {
    if (handle) static_cast<Decoder*>(handle)->value.SetAllowVcdTarget(allow != 0);
}

int32_t kvcdiff_decoder_start(void* handle, const void* dictionary, size_t dictionary_size) noexcept {
    if (!handle || !valid(dictionary, dictionary_size)) return 0;
    auto& decoder = *static_cast<Decoder*>(handle);
    decoder.value.StartDecoding(static_cast<const char*>(dictionary), dictionary_size);
    decoder.started = true;
    return 1;
}

int32_t kvcdiff_decoder_decode(void* handle, const void* input, size_t input_size, void* output, size_t output_capacity, size_t* output_size) noexcept {
    if (!handle || !output_size || !valid(input, input_size) || !valid(output, output_capacity)) return 0;
    auto& decoder = *static_cast<Decoder*>(handle);
    if (!decoder.started) return 0;
    Buffer destination{std::span{static_cast<uint8_t*>(output), output_capacity}};
    const auto result = decoder.value.DecodeChunkToInterface(static_cast<const char*>(input), input_size, &destination);
    *output_size = destination.size();
    return result && destination.valid();
}

int32_t kvcdiff_decoder_finish(void* handle) noexcept {
    if (!handle) return 0;
    auto& decoder = *static_cast<Decoder*>(handle);
    const auto result = decoder.started && decoder.value.FinishDecoding();
    decoder.started = false;
    return result;
}

int32_t kvcdiff_decode(const void* dictionary, size_t dictionary_size, const void* input, size_t input_size, void* output, size_t output_capacity, size_t* output_size, size_t max_target_file_size, size_t max_target_window_size, uint32_t allow_target) noexcept {
    if (!output_size || !valid(dictionary, dictionary_size) || !valid(input, input_size) || !valid(output, output_capacity)) return 0;
    Decoder decoder;
    if ((max_target_file_size && !decoder.value.SetMaximumTargetFileSize(max_target_file_size)) || (max_target_window_size && !decoder.value.SetMaximumTargetWindowSize(max_target_window_size))) return 0;
    decoder.value.SetAllowVcdTarget(allow_target != 0);
    decoder.value.StartDecoding(static_cast<const char*>(dictionary), dictionary_size);
    Buffer destination{std::span{static_cast<uint8_t*>(output), output_capacity}};
    const auto result = decoder.value.DecodeChunkToInterface(static_cast<const char*>(input), input_size, &destination) && decoder.value.FinishDecoding();
    *output_size = destination.size();
    return result && destination.valid();
}

}
