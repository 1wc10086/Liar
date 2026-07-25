#include "../../../api/precomp.hpp"

namespace XCOMPRESS
{
namespace
{
constexpr HRESULT XMCD_E_BADPARAM = static_cast<HRESULT>(0x81DE2001u);
}

void InitializeDecompressionContextLzx(
    const _XMEMCODEC_PARAMETERS_LZX* pLzxParams,
    unsigned long Flags,
    void* pContextData,
    std::size_t ContextSize);
void DestroyDecompressionContextLzx(_LZXDECOMPRESSION_CONTEXT_HEADER* pContext);
std::size_t GetDecompressionContextSizeLzx(const _XMEMCODEC_PARAMETERS_LZX* pLzxParams, unsigned long Flags);
int ResetDecompressionContextLzx(_LZXDECOMPRESSION_CONTEXT_HEADER* pContext);
int DecompressLzx(
    _LZXDECOMPRESSION_CONTEXT_HEADER* pContext,
    void* pDestination,
    unsigned __int64* pDestSize,
    const void* pSource,
    unsigned __int64 SrcSize);
int DecompressStreamLzx(
    _LZXDECOMPRESSION_CONTEXT_HEADER* pContext,
    void* pDestination,
    unsigned __int64* pDestSize,
    const void* pSource,
    unsigned __int64* pSrcSize);
void DecompressSegmentTDLzx(
    _LZXDECOMPRESSION_CONTEXT_HEADER* pContext,
    void* pDestination,
    unsigned __int64* pDestSize,
    const void* pSource,
    unsigned __int64 SrcSize,
    unsigned __int64 SegmentSize,
    unsigned __int64 SegmentOffset);

HRESULT GetHResultFromDResult(int DResult)
{
    if (DResult == 0)
    {
        return 0;
    }

    if (DResult == 1)
    {
        return XMCD_E_OUTOFMEMORY;
    }

    if (DResult == 6)
    {
        return XMCD_E_BADPARAM;
    }

    return XMCD_E_FAIL;
}

void* XMemInitializeDecompressionContextLzx(
    const _XMEMCODEC_PARAMETERS_LZX* pLzxParams,
    unsigned long Flags,
    void* pContextData,
    std::size_t ContextSize)
{
    _LZXDECOMPRESSION_CONTEXT_HEADER* const context =
        reinterpret_cast<_LZXDECOMPRESSION_CONTEXT_HEADER*>(pContextData);
    context->Common.Identifier = 0x76C3F251u;
    context->Common.CodecType = XMEMCODEC_LZX;
    context->Common.Flags = Flags;

    InitializeDecompressionContextLzx(pLzxParams, Flags, pContextData, ContextSize);
    return pContextData;
}

void XMemDestroyDecompressionContextLzx(void* const Context)
{
    DestroyDecompressionContextLzx(reinterpret_cast<_LZXDECOMPRESSION_CONTEXT_HEADER*>(Context));
}

std::size_t XMemGetDecompressionContextSizeLzx(
    const _XMEMCODEC_PARAMETERS_LZX* pLzxParams,
    unsigned long Flags)
{
    return GetDecompressionContextSizeLzx(pLzxParams, Flags);
}

HRESULT XMemResetDecompressionContextLzx(void* Context)
{
    return GetHResultFromDResult(
        ResetDecompressionContextLzx(reinterpret_cast<_LZXDECOMPRESSION_CONTEXT_HEADER*>(Context)));
}

HRESULT XMemDecompressLzx(
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

    return GetHResultFromDResult(
        DecompressLzx(
            reinterpret_cast<_LZXDECOMPRESSION_CONTEXT_HEADER*>(Context),
            pDestination,
            reinterpret_cast<unsigned __int64*>(pDestSize),
            pSource,
            static_cast<unsigned __int64>(SrcSize)));
}

HRESULT XMemDecompressStreamLzx(
    void* Context,
    void* pDestination,
    std::size_t* pDestSize,
    const void* pSource,
    std::size_t* pSrcSize)
{
    return GetHResultFromDResult(
        DecompressStreamLzx(
            reinterpret_cast<_LZXDECOMPRESSION_CONTEXT_HEADER*>(Context),
            pDestination,
            reinterpret_cast<unsigned __int64*>(pDestSize),
            pSource,
            reinterpret_cast<unsigned __int64*>(pSrcSize)));
}

HRESULT XMemDecompressSegmentTDLzx(
    void* Context,
    void* pDestination,
    std::size_t* pDestSize,
    const void* pSource,
    std::size_t SrcSize,
    std::size_t SegmentSize,
    std::size_t SegmentOffset)
{
    DecompressSegmentTDLzx(
        reinterpret_cast<_LZXDECOMPRESSION_CONTEXT_HEADER*>(Context),
        pDestination,
        reinterpret_cast<unsigned __int64*>(pDestSize),
        pSource,
        static_cast<unsigned __int64>(SrcSize),
        static_cast<unsigned __int64>(SegmentSize),
        static_cast<unsigned __int64>(SegmentOffset));
    return 0;
}
}
