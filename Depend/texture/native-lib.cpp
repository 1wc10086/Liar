#include <cstddef>
#include <cstdint>
#include <cstring>
#include <limits>
#include <new>
#include "texture.h"

#define STB_IMAGE_RESIZE_IMPLEMENTATION
#include "lib/stb/stb_image_resize2.h"
extern "C" {
#include "lib/libjpeg-turbo/src/turbojpeg.h"
#include "lib/libwebp/src/webp/decode.h"
#include "lib/libwebp/src/webp/demux.h"
#include "lib/libwebp/src/webp/encode.h"
}

namespace {

struct Writer {
    uint8_t* data;
    size_t capacity;
    size_t size;
};

int write(const uint8_t* data, size_t size, const WebPPicture* picture) {
    auto& writer = *static_cast<Writer*>(picture->custom_ptr);
    if (size > writer.capacity - writer.size) return 0;
    std::memcpy(writer.data + writer.size, data, size);
    writer.size += size;
    return 1;
}

bool pixels(uint32_t width, uint32_t height, size_t bytes_per_pixel, size_t& out) {
    return width && height && width <= std::numeric_limits<size_t>::max() / height && static_cast<size_t>(width) * height <= std::numeric_limits<size_t>::max() / bytes_per_pixel && (out = static_cast<size_t>(width) * height * bytes_per_pixel, true);
}

struct Animation { WebPData data; WebPAnimDecoder* decoder; };

}

extern "C" __attribute__((visibility("default"))) uint32_t ktx_abi_version() { return 1; }

extern "C" __attribute__((visibility("default"))) int ktx_webp_info(const uint8_t* data, size_t size, uint32_t* width, uint32_t* height, uint32_t* flags) {
    WebPBitstreamFeatures features{};
    if (!data || VP8_STATUS_OK != WebPGetFeatures(data, size, &features)) return 0;
    if (width) *width = static_cast<uint32_t>(features.width);
    if (height) *height = static_cast<uint32_t>(features.height);
    if (flags) *flags = static_cast<uint32_t>((features.has_alpha != 0) | ((features.has_animation != 0) << 1) | ((features.format == 2) << 2));
    return 1;
}

extern "C" __attribute__((visibility("default"))) int ktx_webp_decode_rgba(const uint8_t* data, size_t size, uint8_t* dst, size_t dst_size, uint32_t width, uint32_t height, uint32_t stride) {
    size_t required{};
    return data && dst && stride >= static_cast<size_t>(width) * 4 && pixels(width, height, 4, required) && dst_size >= static_cast<size_t>(stride) * height && WebPDecodeRGBAInto(data, size, dst, dst_size, static_cast<int>(stride)) != nullptr;
}

extern "C" __attribute__((visibility("default"))) int ktx_webp_encode_rgba(const uint8_t* src, uint32_t width, uint32_t height, uint32_t stride, float quality, int lossless, int method, uint8_t* dst, size_t capacity, size_t* written) {
    if (!src || !dst || !written || !width || !height || stride < static_cast<size_t>(width) * 4) return 0;
    WebPConfig config{};
    WebPPicture picture{};
    Writer writer{dst, capacity, 0};
    if (!WebPConfigPreset(&config, WEBP_PRESET_DEFAULT, quality) || !WebPPictureInit(&picture)) return 0;
    config.lossless = lossless != 0;
    config.method = method;
    if (!WebPValidateConfig(&config)) return 0;
    picture.width = static_cast<int>(width);
    picture.height = static_cast<int>(height);
    picture.use_argb = 1;
    picture.writer = write;
    picture.custom_ptr = &writer;
    const int imported = WebPPictureImportRGBA(&picture, src, static_cast<int>(stride));
    const int encoded = imported && WebPEncode(&config, &picture);
    WebPPictureFree(&picture);
    if (!encoded) return 0;
    *written = writer.size;
    return 1;
}

extern "C" __attribute__((visibility("default"))) void* ktx_webp_animation_create(const uint8_t* data, size_t size) {
    if (!data || !size) return nullptr;
    auto* animation = new (std::nothrow) Animation{{data, size}, nullptr};
    if (!animation) return nullptr;
    WebPAnimDecoderOptions options{};
    if (!WebPAnimDecoderOptionsInit(&options) || !(animation->decoder = WebPAnimDecoderNew(&animation->data, &options))) { delete animation; return nullptr; }
    return animation;
}

extern "C" __attribute__((visibility("default"))) void ktx_webp_animation_destroy(void* handle) { auto* animation = static_cast<Animation*>(handle); if (animation) { WebPAnimDecoderDelete(animation->decoder); delete animation; } }
extern "C" __attribute__((visibility("default"))) int ktx_webp_animation_info(void* handle, uint32_t* width, uint32_t* height, uint32_t* frames, uint32_t* loop_count, uint32_t* background) {
    auto* animation = static_cast<Animation*>(handle); WebPAnimInfo info{};
    if (!animation || !WebPAnimDecoderGetInfo(animation->decoder, &info)) return 0;
    if (width) *width = info.canvas_width; if (height) *height = info.canvas_height; if (frames) *frames = info.frame_count; if (loop_count) *loop_count = info.loop_count; if (background) *background = info.bgcolor;
    return 1;
}
extern "C" __attribute__((visibility("default"))) int ktx_webp_animation_next_rgba(void* handle, uint8_t* dst, size_t dst_size, uint32_t* timestamp) {
    auto* animation = static_cast<Animation*>(handle); uint8_t* frame{}; int time{}; WebPAnimInfo info{};
    size_t frame_size{};
    if (!animation || !dst || !WebPAnimDecoderGetInfo(animation->decoder, &info) || !pixels(info.canvas_width, info.canvas_height, 4, frame_size) || dst_size < frame_size || !WebPAnimDecoderGetNext(animation->decoder, &frame, &time)) return 0;
    std::memcpy(dst, frame, frame_size); if (timestamp) *timestamp = static_cast<uint32_t>(time); return 1;
}
extern "C" __attribute__((visibility("default"))) void ktx_webp_animation_reset(void* handle) { if (auto* animation = static_cast<Animation*>(handle)) WebPAnimDecoderReset(animation->decoder); }

extern "C" __attribute__((visibility("default"))) int ktx_jpeg_info(const uint8_t* data, size_t size, uint32_t* width, uint32_t* height, int* subsampling, int* colorspace) {
    tjhandle handle = tjInitDecompress(); int w{}, h{}, subsamp{}, jpeg_colorspace{};
    const int ok = handle && !tjDecompressHeader3(handle, data, static_cast<unsigned long>(size), &w, &h, &subsamp, &jpeg_colorspace);
    if (handle) tjDestroy(handle); if (!ok) return 0;
    if (width) *width = static_cast<uint32_t>(w); if (height) *height = static_cast<uint32_t>(h); if (subsampling) *subsampling = subsamp; if (colorspace) *colorspace = jpeg_colorspace; return 1;
}

extern "C" __attribute__((visibility("default"))) int ktx_jpeg_decode(const uint8_t* data, size_t size, uint8_t* dst, size_t dst_size, uint32_t width, uint32_t height, uint32_t stride, int pixel_format, int flags) {
    if (!data || !dst || pixel_format < 0 || pixel_format >= TJ_NUMPF) return 0;
    const int pixel_size = tjPixelSize[pixel_format];
    if (pixel_size <= 0 || stride < static_cast<size_t>(width) * pixel_size || dst_size < static_cast<size_t>(stride) * height) return 0;
    tjhandle handle = tjInitDecompress(); const int ok = handle && !tjDecompress2(handle, data, static_cast<unsigned long>(size), dst, static_cast<int>(width), static_cast<int>(stride), static_cast<int>(height), pixel_format, flags);
    if (handle) tjDestroy(handle); return ok;
}

extern "C" __attribute__((visibility("default"))) int ktx_jpeg_encode(const uint8_t* src, uint32_t width, uint32_t height, uint32_t stride, int pixel_format, uint8_t* dst, size_t capacity, size_t* written, int subsampling, int quality, int flags) {
    if (!src || !dst || !written || pixel_format < 0 || pixel_format >= TJ_NUMPF || tjPixelSize[pixel_format] <= 0 || stride < static_cast<size_t>(width) * tjPixelSize[pixel_format] || capacity > std::numeric_limits<unsigned long>::max()) return 0;
    tjhandle handle = tjInitCompress(); unsigned char* output = dst; unsigned long output_size = static_cast<unsigned long>(capacity);
    const int ok = handle && !tjCompress2(handle, src, static_cast<int>(width), static_cast<int>(stride), static_cast<int>(height), pixel_format, &output, &output_size, subsampling, quality, flags | TJFLAG_NOREALLOC) && output == dst;
    if (handle) tjDestroy(handle); if (!ok) return 0; *written = output_size; return 1;
}

extern "C" __attribute__((visibility("default"))) int ktx_stb_resize_u8(const uint8_t* src, uint32_t src_width, uint32_t src_height, uint32_t src_stride, uint8_t* dst, uint32_t dst_width, uint32_t dst_height, uint32_t dst_stride, int layout, int srgb) {
    if (!src || !dst || !src_width || !src_height || !dst_width || !dst_height) return 0;
    return (srgb ? stbir_resize_uint8_srgb(src, src_width, src_height, src_stride, dst, dst_width, dst_height, dst_stride, static_cast<stbir_pixel_layout>(layout)) : stbir_resize_uint8_linear(src, src_width, src_height, src_stride, dst, dst_width, dst_height, dst_stride, static_cast<stbir_pixel_layout>(layout))) != nullptr;
}

extern "C" __attribute__((visibility("default"))) int ktx_stb_resize_f32(const float* src, uint32_t src_width, uint32_t src_height, uint32_t src_stride, float* dst, uint32_t dst_width, uint32_t dst_height, uint32_t dst_stride, int layout) {
    if (!src || !dst || !src_width || !src_height || !dst_width || !dst_height) return 0;
    return stbir_resize_float_linear(src, src_width, src_height, src_stride, dst, dst_width, dst_height, dst_stride, static_cast<stbir_pixel_layout>(layout)) != nullptr;
}
