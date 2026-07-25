#include "../../../api/precomp.hpp"

namespace XCOMPRESS
{
namespace
{
constexpr HRESULT XMCD_E_BADPARAM = static_cast<HRESULT>(0x81DE2001u);
}

void DestroyCompressionContextLzx(_LZXCOMPRESSION_CONTEXT_HEADER* pContext);
std::size_t GetCompressionContextSizeLzx(const _XMEMCODEC_PARAMETERS_LZX* pLzxParams, unsigned int Flags);
void InitializeCompressionContextLzx(
    const _XMEMCODEC_PARAMETERS_LZX* pLzxParams,
    unsigned int Flags,
    void* pContextData,
    std::size_t ContextSize);
int ResetCompressionContextLzx(_LZXCOMPRESSION_CONTEXT_HEADER* pContext);
int BeginCompressionTDLzx(_LZXCOMPRESSION_CONTEXT_HEADER* pContext, unsigned __int64 SegmentPitch);
int EndCompressionTDLzx(
    _LZXCOMPRESSION_CONTEXT_HEADER* pContext,
    void* pHeaderData,
    unsigned __int64* pHeaderSize,
    float Threshold);
int CompressSegmentTDLzx(
    _LZXCOMPRESSION_CONTEXT_HEADER* pContext,
    void* pDestination,
    unsigned __int64* pDestSize,
    const void* pSource,
    unsigned __int64* pSrcSize,
    float Threshold);
int CompressLzx(
    _LZXCOMPRESSION_CONTEXT_HEADER* pContext,
    void* pDestination,
    unsigned __int64* pDestSize,
    const void* pSource,
    unsigned __int64 SrcSize);
int CompressStreamLzx(
    _LZXCOMPRESSION_CONTEXT_HEADER* pContext,
    void* pDestination,
    unsigned __int64* pDestSize,
    const void* pSource,
    unsigned __int64* pSrcSize);

HRESULT GetHResultFromCResult(int CResult)
{
    if (CResult == 0)
    {
        return 0;
    }

    if (CResult == 1)
    {
        return XMCD_E_OUTOFMEMORY;
    }

    if ((CResult == 6) || (CResult == 7))
    {
        return XMCD_E_BADPARAM;
    }

    return XMCD_E_FAIL;
}

void XMemDestroyCompressionContextLzx(void* const Context)
{
    DestroyCompressionContextLzx(reinterpret_cast<_LZXCOMPRESSION_CONTEXT_HEADER*>(Context));
}

std::size_t XMemGetCompressionContextSizeLzx(const _XMEMCODEC_PARAMETERS_LZX* pLzxParams, std::uint32_t Flags)
{
    return GetCompressionContextSizeLzx(pLzxParams, Flags);
}

void* XMemInitializeCompressionContextLzx(
    const _XMEMCODEC_PARAMETERS_LZX* pLzxParams,
    std::uint32_t Flags,
    void* pContextData,
    std::size_t ContextSize)
{
    _LZXCOMPRESSION_CONTEXT_HEADER* const context =
        reinterpret_cast<_LZXCOMPRESSION_CONTEXT_HEADER*>(pContextData);

    context->Common.Identifier = 0x76C3F250u;
    context->Common.CodecType = XMEMCODEC_LZX;
    context->Common.Flags = Flags;

    InitializeCompressionContextLzx(pLzxParams, Flags, pContextData, ContextSize);
    return pContextData;
}

HRESULT XMemResetCompressionContextLzx(void* Context)
{
    return GetHResultFromCResult(
        ResetCompressionContextLzx(reinterpret_cast<_LZXCOMPRESSION_CONTEXT_HEADER*>(Context)));
}

HRESULT XMemBeginCompressionTDLzx(void* Context, std::size_t SegmentPitch)
{
    return GetHResultFromCResult(
        BeginCompressionTDLzx(
            reinterpret_cast<_LZXCOMPRESSION_CONTEXT_HEADER*>(Context),
            static_cast<unsigned __int64>(SegmentPitch)));
}

HRESULT XMemEndCompressionTDLzx(
    void* Context,
    void* pHeaderData,
    std::size_t* pHeaderSize,
    float Threshold)
{
    return GetHResultFromCResult(
        EndCompressionTDLzx(
            reinterpret_cast<_LZXCOMPRESSION_CONTEXT_HEADER*>(Context),
            pHeaderData,
            reinterpret_cast<unsigned __int64*>(pHeaderSize),
            Threshold));
}

HRESULT XMemCompressSegmentTDLzx(
    void* Context,
    void* pDestination,
    std::size_t* pDestSize,
    const void* pSource,
    std::size_t* pSrcSize,
    float Threshold)
{
    if (*pSrcSize == 0)
    {
        *pDestSize = 0;
        return 0;
    }

    return GetHResultFromCResult(
        CompressSegmentTDLzx(
            reinterpret_cast<_LZXCOMPRESSION_CONTEXT_HEADER*>(Context),
            pDestination,
            reinterpret_cast<unsigned __int64*>(pDestSize),
            pSource,
            reinterpret_cast<unsigned __int64*>(pSrcSize),
            Threshold));
}

HRESULT XMemCompressLzx(
    void* Context,
    void* pDestination,
    std::size_t* pDestSize,
    const void* pSource,
    std::size_t SrcSize)
{
    if (SrcSize == 0)
    {
        *pDestSize = 0;
        return 0;
    }

    return GetHResultFromCResult(
        CompressLzx(
            reinterpret_cast<_LZXCOMPRESSION_CONTEXT_HEADER*>(Context),
            pDestination,
            reinterpret_cast<unsigned __int64*>(pDestSize),
            pSource,
            static_cast<unsigned __int64>(SrcSize)));
}

HRESULT XMemCompressStreamLzx(
    void* Context,
    void* pDestination,
    std::size_t* pDestSize,
    const void* pSource,
    std::size_t* pSrcSize)
{
    return GetHResultFromCResult(
        CompressStreamLzx(
            reinterpret_cast<_LZXCOMPRESSION_CONTEXT_HEADER*>(Context),
            pDestination,
            reinterpret_cast<unsigned __int64*>(pDestSize),
            pSource,
            reinterpret_cast<unsigned __int64*>(pSrcSize)));
}
}
