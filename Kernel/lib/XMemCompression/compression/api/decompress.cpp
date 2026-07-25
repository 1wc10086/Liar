#include "precomp.hpp"

namespace
{
void ValidateDecompressionContextParameters(
    XMEMCODEC_TYPE CodecType,
    const void* pCodecParams,
    std::uint32_t Flags)
{
    (void)CodecType;
    (void)pCodecParams;
    (void)Flags;
}

bool IsLzxCodec(XMEMCODEC_TYPE CodecType)
{
    return CodecType == XMEMCODEC_LZX;
}

bool IsSupportedCodec(XMEMCODEC_TYPE CodecType)
{
    return CodecType == XMEMCODEC_DEFAULT || CodecType == XMEMCODEC_LZX;
}

XCOMPRESS::_XMEMCODEC_CONTEXT_HEADER* AsDecompressionHeader(XMEMDECOMPRESSION_CONTEXT Context)
{
    return static_cast<XCOMPRESS::_XMEMCODEC_CONTEXT_HEADER*>(Context);
}
}

extern "C" std::size_t XMemGetDecompressionContextSize(
    XMEMCODEC_TYPE CodecType,
    const XMEMCODEC_PARAMETERS_LZX* pCodecParams,
    std::uint32_t Flags)
{
    ValidateDecompressionContextParameters(CodecType, pCodecParams, Flags);

    if (IsSupportedCodec(CodecType))
    {
        return XCOMPRESS::XMemGetDecompressionContextSizeLzx(pCodecParams, Flags);
    }

    return 0;
}

extern "C" HRESULT XMemResetDecompressionContext(XMEMDECOMPRESSION_CONTEXT Context)
{
    HRESULT result = XMCD_E_FAIL;

    if (IsLzxCodec(AsDecompressionHeader(Context)->CodecType))
    {
        result = XCOMPRESS::XMemResetDecompressionContextLzx(Context);
    }

    return result;
}

extern "C" HRESULT XMemDecompress(
    XMEMDECOMPRESSION_CONTEXT Context,
    void* pDestination,
    std::size_t* pDestSize,
    const void* pSource,
    std::size_t SrcSize)
{
    HRESULT result = XMCD_E_FAIL;

    if (IsLzxCodec(AsDecompressionHeader(Context)->CodecType))
    {
        result = XCOMPRESS::XMemDecompressLzx(Context, pDestination, pDestSize, pSource, SrcSize);
    }

    return result;
}

extern "C" HRESULT XMemDecompressStream(
    XMEMDECOMPRESSION_CONTEXT Context,
    void* pDestination,
    std::size_t* pDestSize,
    const void* pSource,
    std::size_t* pSrcSize)
{
    HRESULT result = XMCD_E_FAIL;

    if (IsLzxCodec(AsDecompressionHeader(Context)->CodecType))
    {
        result = XCOMPRESS::XMemDecompressStreamLzx(Context, pDestination, pDestSize, pSource, pSrcSize);
    }

    return result;
}

extern "C" HRESULT XMemDecompressSegmentTD(
    XMEMDECOMPRESSION_CONTEXT Context,
    void* pDestination,
    std::size_t* pDestSize,
    const void* pSource,
    std::size_t SrcSize,
    std::size_t SegmentSize,
    std::size_t SegmentOffset)
{
    return XCOMPRESS::XMemDecompressSegmentTDLzx(
        Context,
        pDestination,
        pDestSize,
        pSource,
        SrcSize,
        SegmentSize,
        SegmentOffset);
}

extern "C" void* XMemInitializeDecompressionContext(
    XMEMCODEC_TYPE CodecType,
    const XMEMCODEC_PARAMETERS_LZX* pCodecParams,
    std::uint32_t Flags,
    void* pContextData,
    std::size_t ContextSize)
{
    ValidateDecompressionContextParameters(CodecType, pCodecParams, Flags);

    if (IsSupportedCodec(CodecType))
    {
        return XCOMPRESS::XMemInitializeDecompressionContextLzx(pCodecParams, Flags, pContextData, ContextSize);
    }

    return nullptr;
}

extern "C" void XMemDestroyDecompressionContext(XMEMDECOMPRESSION_CONTEXT Context)
{
    XCOMPRESS::_XMEMCODEC_CONTEXT_HEADER* header = AsDecompressionHeader(Context);

    if (IsLzxCodec(header->CodecType))
    {
        XCOMPRESS::XMemDestroyDecompressionContextLzx(Context);
    }

    if ((header->Flags & XCOMPRESS::XMCD_CONTEXT_FLAG_OWNS_HEAP) != 0)
    {
        // Liar : change - context storage is allocated with std::malloc.
        std::free(Context);
    }
}

extern "C" HRESULT XMemCreateDecompressionContext(
    XMEMCODEC_TYPE CodecType,
    const XMEMCODEC_PARAMETERS_LZX* pCodecParams,
    std::uint32_t Flags,
    XMEMDECOMPRESSION_CONTEXT* pContext)
{
    ValidateDecompressionContextParameters(CodecType, pCodecParams, Flags);

    std::size_t contextSize = 0;
    if (IsSupportedCodec(CodecType))
    {
        contextSize = XCOMPRESS::XMemGetDecompressionContextSizeLzx(pCodecParams, Flags);
    }

    // Liar : change - replace HeapAlloc/GetProcessHeap for non-Windows targets.
    void* contextData = std::malloc(contextSize);
    if (contextData == nullptr)
    {
        return XMCD_E_OUTOFMEMORY;
    }

    void* context = XMemInitializeDecompressionContext(CodecType, pCodecParams, Flags, contextData, contextSize);
    AsDecompressionHeader(context)->Flags |= XCOMPRESS::XMCD_CONTEXT_FLAG_OWNS_HEAP;
    *pContext = context;
    return 0;
}
