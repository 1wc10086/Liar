#pragma once

#include <stddef.h>
#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

uint32_t ktx_abi_version(void);
int ktx_webp_info(const uint8_t* data, size_t size, uint32_t* width, uint32_t* height, uint32_t* flags);
int ktx_webp_decode_rgba(const uint8_t* data, size_t size, uint8_t* dst, size_t dst_size, uint32_t width, uint32_t height, uint32_t stride);
int ktx_webp_encode_rgba(const uint8_t* src, uint32_t width, uint32_t height, uint32_t stride, float quality, int lossless, int method, uint8_t* dst, size_t capacity, size_t* written);
void* ktx_webp_animation_create(const uint8_t* data, size_t size);
void ktx_webp_animation_destroy(void* handle);
int ktx_webp_animation_info(void* handle, uint32_t* width, uint32_t* height, uint32_t* frames, uint32_t* loop_count, uint32_t* background);
int ktx_webp_animation_next_rgba(void* handle, uint8_t* dst, size_t dst_size, uint32_t* timestamp);
void ktx_webp_animation_reset(void* handle);
int ktx_jpeg_info(const uint8_t* data, size_t size, uint32_t* width, uint32_t* height, int* subsampling, int* colorspace);
int ktx_jpeg_decode(const uint8_t* data, size_t size, uint8_t* dst, size_t dst_size, uint32_t width, uint32_t height, uint32_t stride, int pixel_format, int flags);
int ktx_jpeg_encode(const uint8_t* src, uint32_t width, uint32_t height, uint32_t stride, int pixel_format, uint8_t* dst, size_t capacity, size_t* written, int subsampling, int quality, int flags);
int ktx_stb_resize_u8(const uint8_t* src, uint32_t src_width, uint32_t src_height, uint32_t src_stride, uint8_t* dst, uint32_t dst_width, uint32_t dst_height, uint32_t dst_stride, int layout, int srgb);
int ktx_stb_resize_f32(const float* src, uint32_t src_width, uint32_t src_height, uint32_t src_stride, float* dst, uint32_t dst_width, uint32_t dst_height, uint32_t dst_stride, int layout);

#ifdef __cplusplus
}
#endif
