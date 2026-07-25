#pragma once

#include <cstddef>
#include <cstdint>

using HRESULT = std::int32_t;

constexpr HRESULT XMCD_E_FAIL = static_cast<HRESULT>(0x80004005u);
constexpr HRESULT XMCD_E_OUTOFMEMORY = static_cast<HRESULT>(0x8007000Eu);

enum XMEMCODEC_TYPE : int
{
    XMEMCODEC_DEFAULT = 0,
    XMEMCODEC_LZX = 1,
};

struct _XMEMCODEC_PARAMETERS_LZX
{
    std::uint32_t Flags;
    std::uint32_t WindowSize;
    std::uint32_t CompressionPartitionSize;
};

using XMEMCODEC_PARAMETERS_LZX = _XMEMCODEC_PARAMETERS_LZX;
using XMEMCOMPRESSION_CONTEXT = void*;
using XMEMDECOMPRESSION_CONTEXT = void*;

extern "C" {

std::size_t XMemGetCompressionContextSize(
    XMEMCODEC_TYPE CodecType,
    const XMEMCODEC_PARAMETERS_LZX* pCodecParams,
    std::uint32_t Flags);

HRESULT XMemResetCompressionContext(XMEMCOMPRESSION_CONTEXT Context);

HRESULT XMemBeginCompressionTD(
    XMEMCOMPRESSION_CONTEXT Context,
    std::size_t SegmentPitch);

HRESULT XMemEndCompressionTD(
    XMEMCOMPRESSION_CONTEXT Context,
    void* pHeaderData,
    std::size_t* pHeaderSize,
    float Threshold);

HRESULT XMemCompressSegmentTD(
    XMEMCOMPRESSION_CONTEXT Context,
    void* pDestination,
    std::size_t* pDestSize,
    const void* pSource,
    std::size_t* pSrcSize,
    float Threshold);

HRESULT XMemCompress(
    XMEMCOMPRESSION_CONTEXT Context,
    void* pDestination,
    std::size_t* pDestSize,
    const void* pSource,
    std::size_t SrcSize);

HRESULT XMemCompressStream(
    XMEMCOMPRESSION_CONTEXT Context,
    void* pDestination,
    std::size_t* pDestSize,
    const void* pSource,
    std::size_t* pSrcSize);

void* XMemInitializeCompressionContext(
    XMEMCODEC_TYPE CodecType,
    const XMEMCODEC_PARAMETERS_LZX* pCodecParams,
    std::uint32_t Flags,
    void* pContextData,
    std::size_t ContextSize);

void XMemDestroyCompressionContext(XMEMCOMPRESSION_CONTEXT Context);

HRESULT XMemCreateCompressionContext(
    XMEMCODEC_TYPE CodecType,
    const XMEMCODEC_PARAMETERS_LZX* pCodecParams,
    std::uint32_t Flags,
    XMEMCOMPRESSION_CONTEXT* pContext);

std::size_t XMemGetDecompressionContextSize(
    XMEMCODEC_TYPE CodecType,
    const XMEMCODEC_PARAMETERS_LZX* pCodecParams,
    std::uint32_t Flags);

HRESULT XMemResetDecompressionContext(XMEMDECOMPRESSION_CONTEXT Context);

HRESULT XMemDecompress(
    XMEMDECOMPRESSION_CONTEXT Context,
    void* pDestination,
    std::size_t* pDestSize,
    const void* pSource,
    std::size_t SrcSize);

HRESULT XMemDecompressStream(
    XMEMDECOMPRESSION_CONTEXT Context,
    void* pDestination,
    std::size_t* pDestSize,
    const void* pSource,
    std::size_t* pSrcSize);

HRESULT XMemDecompressSegmentTD(
    XMEMDECOMPRESSION_CONTEXT Context,
    void* pDestination,
    std::size_t* pDestSize,
    const void* pSource,
    std::size_t SrcSize,
    std::size_t SegmentSize,
    std::size_t SegmentOffset);

void* XMemInitializeDecompressionContext(
    XMEMCODEC_TYPE CodecType,
    const XMEMCODEC_PARAMETERS_LZX* pCodecParams,
    std::uint32_t Flags,
    void* pContextData,
    std::size_t ContextSize);

void XMemDestroyDecompressionContext(XMEMDECOMPRESSION_CONTEXT Context);

HRESULT XMemCreateDecompressionContext(
    XMEMCODEC_TYPE CodecType,
    const XMEMCODEC_PARAMETERS_LZX* pCodecParams,
    std::uint32_t Flags,
    XMEMDECOMPRESSION_CONTEXT* pContext);

}
