#include "precomp.hpp"

namespace
{
void ValidateCompressionContextParameters(
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

XCOMPRESS::_XMEMCODEC_CONTEXT_HEADER* AsCompressionHeader(XMEMCOMPRESSION_CONTEXT Context)
{
    return static_cast<XCOMPRESS::_XMEMCODEC_CONTEXT_HEADER*>(Context);
}
}

extern "C" std::size_t XMemGetCompressionContextSize(
    XMEMCODEC_TYPE CodecType,
    const XMEMCODEC_PARAMETERS_LZX* pCodecParams,
    std::uint32_t Flags)
{
    ValidateCompressionContextParameters(CodecType, pCodecParams, Flags);

    if (IsSupportedCodec(CodecType))
    {
        return XCOMPRESS::XMemGetCompressionContextSizeLzx(pCodecParams, Flags);
    }

    return 0;
}

extern "C" HRESULT XMemResetCompressionContext(XMEMCOMPRESSION_CONTEXT Context)
{
    HRESULT result = XMCD_E_FAIL;

    if (IsLzxCodec(AsCompressionHeader(Context)->CodecType))
    {
        result = XCOMPRESS::XMemResetCompressionContextLzx(Context);
    }

    return result;
}

extern "C" HRESULT XMemBeginCompressionTD(
    XMEMCOMPRESSION_CONTEXT Context,
    std::size_t SegmentPitch)
{
    HRESULT result = XMCD_E_FAIL;

    if (IsLzxCodec(AsCompressionHeader(Context)->CodecType))
    {
        result = XCOMPRESS::XMemBeginCompressionTDLzx(Context, SegmentPitch);
    }

    return result;
}

extern "C" HRESULT XMemEndCompressionTD(
    XMEMCOMPRESSION_CONTEXT Context,
    void* pHeaderData,
    std::size_t* pHeaderSize,
    float Threshold)
{
    HRESULT result = XMCD_E_FAIL;

    if (IsLzxCodec(AsCompressionHeader(Context)->CodecType))
    {
        result = XCOMPRESS::XMemEndCompressionTDLzx(Context, pHeaderData, pHeaderSize, Threshold);
    }

    return result;
}

extern "C" HRESULT XMemCompressSegmentTD(
    XMEMCOMPRESSION_CONTEXT Context,
    void* pDestination,
    std::size_t* pDestSize,
    const void* pSource,
    std::size_t* pSrcSize,
    float Threshold)
{
    HRESULT result = XMCD_E_FAIL;

    if (IsLzxCodec(AsCompressionHeader(Context)->CodecType))
    {
        result = XCOMPRESS::XMemCompressSegmentTDLzx(
            Context,
            pDestination,
            pDestSize,
            pSource,
            pSrcSize,
            Threshold);
    }

    return result;
}

extern "C" HRESULT XMemCompress(
    XMEMCOMPRESSION_CONTEXT Context,
    void* pDestination,
    std::size_t* pDestSize,
    const void* pSource,
    std::size_t SrcSize)
{
    HRESULT result = XMCD_E_FAIL;

    if (IsLzxCodec(AsCompressionHeader(Context)->CodecType))
    {
        result = XCOMPRESS::XMemCompressLzx(Context, pDestination, pDestSize, pSource, SrcSize);
    }

    return result;
}

extern "C" HRESULT XMemCompressStream(
    XMEMCOMPRESSION_CONTEXT Context,
    void* pDestination,
    std::size_t* pDestSize,
    const void* pSource,
    std::size_t* pSrcSize)
{
    HRESULT result = XMCD_E_FAIL;

    if (IsLzxCodec(AsCompressionHeader(Context)->CodecType))
    {
        result = XCOMPRESS::XMemCompressStreamLzx(Context, pDestination, pDestSize, pSource, pSrcSize);
    }

    return result;
}

extern "C" void* XMemInitializeCompressionContext(
    XMEMCODEC_TYPE CodecType,
    const XMEMCODEC_PARAMETERS_LZX* pCodecParams,
    std::uint32_t Flags,
    void* pContextData,
    std::size_t ContextSize)
{
    ValidateCompressionContextParameters(CodecType, pCodecParams, Flags);

    if (IsSupportedCodec(CodecType))
    {
        return XCOMPRESS::XMemInitializeCompressionContextLzx(pCodecParams, Flags, pContextData, ContextSize);
    }

    return nullptr;
}

extern "C" void XMemDestroyCompressionContext(XMEMCOMPRESSION_CONTEXT Context)
{
    XCOMPRESS::_XMEMCODEC_CONTEXT_HEADER* header = AsCompressionHeader(Context);

    if (IsLzxCodec(header->CodecType))
    {
        XCOMPRESS::XMemDestroyCompressionContextLzx(Context);
    }

    if ((header->Flags & XCOMPRESS::XMCD_CONTEXT_FLAG_OWNS_HEAP) != 0)
    {
        // Liar : change - context storage is allocated with std::malloc.
        std::free(Context);
    }
}

extern "C" HRESULT XMemCreateCompressionContext(
    XMEMCODEC_TYPE CodecType,
    const XMEMCODEC_PARAMETERS_LZX* pCodecParams,
    std::uint32_t Flags,
    XMEMCOMPRESSION_CONTEXT* pContext)
{
    ValidateCompressionContextParameters(CodecType, pCodecParams, Flags);

    std::size_t contextSize = 0;
    if (IsSupportedCodec(CodecType))
    {
        contextSize = XCOMPRESS::XMemGetCompressionContextSizeLzx(pCodecParams, Flags);
    }

    // Liar : change - replace HeapAlloc/GetProcessHeap for non-Windows targets.
    void* contextData = std::malloc(contextSize);
    if (contextData == nullptr)
    {
        return XMCD_E_OUTOFMEMORY;
    }

    void* context = XMemInitializeCompressionContext(CodecType, pCodecParams, Flags, contextData, contextSize);
    AsCompressionHeader(context)->Flags |= XCOMPRESS::XMCD_CONTEXT_FLAG_OWNS_HEAP;
    *pContext = context;
    return 0;
}
