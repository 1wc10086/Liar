#pragma once

#include <stddef.h>
#include <stdint.h>

#ifndef __stdcall
#if defined(_MSC_VER)
#define __stdcall __stdcall
#else
/* Liar : change - non-MSVC ABIs do not require __stdcall decoration. */
#define __stdcall
#endif
#endif

#ifdef __cplusplus
extern "C" {
#endif

typedef void *XMEMDECOMPRESSION_CONTEXT;
typedef void *XMEMCOMPRESSION_CONTEXT;
typedef uint32_t DWORD;
typedef int32_t HRESULT;
typedef size_t SIZE_T;

typedef enum _XMEMCODEC_TYPE {
    XMEMCODEC_DEFAULT = 0,
    XMEMCODEC_LZX = 1
} XMEMCODEC_TYPE;

typedef struct _XMEMCODEC_PARAMETERS_LZX {
    DWORD Flags;
    DWORD WindowSize;
    DWORD CompressionPartitionSize;
} XMEMCODEC_PARAMETERS_LZX;

HRESULT __stdcall XMemCreateDecompressionContext(
    XMEMCODEC_TYPE CodecType,
    const void *pCodecParams,
    DWORD Flags,
    XMEMDECOMPRESSION_CONTEXT *pContext);

HRESULT __stdcall XMemResetCompressionContext(
    XMEMCOMPRESSION_CONTEXT Context);

HRESULT __stdcall XMemBeginCompressionTD(
    XMEMCOMPRESSION_CONTEXT Context,
    SIZE_T SegmentPitch);

HRESULT __stdcall XMemEndCompressionTD(
    XMEMCOMPRESSION_CONTEXT Context,
    void *pHeaderData,
    SIZE_T *pHeaderSize,
    float Threshold);

HRESULT __stdcall XMemCompressSegmentTD(
    XMEMCOMPRESSION_CONTEXT Context,
    void *pDestination,
    SIZE_T *pDestSize,
    const void *pSource,
    SIZE_T *pSrcSize,
    float Threshold);

HRESULT __stdcall XMemCreateCompressionContext(
    XMEMCODEC_TYPE CodecType,
    const XMEMCODEC_PARAMETERS_LZX *pCodecParams,
    DWORD Flags,
    XMEMCOMPRESSION_CONTEXT *pContext);

void __stdcall XMemDestroyCompressionContext(
    XMEMCOMPRESSION_CONTEXT Context);

HRESULT __stdcall XMemDecompressSegmentTD(
    XMEMDECOMPRESSION_CONTEXT Context,
    void *pDestination,
    SIZE_T *pDestSize,
    const void *pSource,
    SIZE_T SrcSize,
    SIZE_T DestSize,
    SIZE_T Offset);

void __stdcall XMemDestroyDecompressionContext(
    XMEMDECOMPRESSION_CONTEXT Context);

int XMemDecompressLzxTdBuffer(
    const uint8_t *input,
    size_t input_size,
    uint8_t **output_data,
    size_t *output_size);

int XMemCompressLzxTdBufferEx(
    const uint8_t *input,
    size_t input_size,
    uint8_t **output_data,
    size_t *output_size,
    uint32_t window_size,
    uint32_t compressed_block_size,
    float threshold);

int XMemCompressLzxTdBuffer(
    const uint8_t *input,
    size_t input_size,
    uint8_t **output_data,
    size_t *output_size);

#ifdef __cplusplus
}
#endif
