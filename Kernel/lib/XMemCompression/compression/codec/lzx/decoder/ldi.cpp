#include "../../../api/precomp.hpp"

#include <cstring>
#include <cstdint>

namespace XCOMPRESS
{
namespace
{
constexpr unsigned int LDI_CONTEXT_SIGNATURE = 0x4349444Cu;

struct LDI_CONTEXT
{
    unsigned int signature;
    unsigned int cbDataBlockMax;
    unsigned int configuration_value;
    unsigned int pad0;
    t_decoder_context decoder_context;
};

inline t_decoder_context* GetDecoderContext(_LZXDECOMPRESSION_CONTEXT_HEADER* pContext)
{
    return reinterpret_cast<t_decoder_context*>(pContext->LzxData);
}
}

static unsigned short LEndianSwap8In16Local(unsigned short Value)
{
    return static_cast<unsigned short>((Value << 8) | (Value >> 8));
}

static void LzxReplaceDefaultParametersLocal(
    _XMEMCODEC_PARAMETERS_LZX* pDstParams,
    const _XMEMCODEC_PARAMETERS_LZX* pSrcParams)
{
    if (pSrcParams != nullptr)
    {
        pDstParams->Flags = pSrcParams->Flags;
        pDstParams->WindowSize = pSrcParams->WindowSize;
        pDstParams->CompressionPartitionSize = pSrcParams->CompressionPartitionSize;
    }
    else
    {
        pDstParams->Flags = 0;
        pDstParams->WindowSize = 0;
        pDstParams->CompressionPartitionSize = 0;
    }

    if (pDstParams->WindowSize == 0)
    {
        pDstParams->WindowSize = 0x20000;
    }
}

int LDICreateDecompression(
    unsigned int* pcbDataBlockMax,
    void* pvConfiguration,
    void* (__fastcall *pfnma)(unsigned int),
    void (__fastcall *pfnmf)(void*),
    void* pvmem,
    unsigned int* pcbSrcBufferMin,
    unsigned __int64* pmdhHandle)
{
    *pcbSrcBufferMin = *pcbDataBlockMax + 0x1800;
    if (pmdhHandle == nullptr)
    {
        return 0;
    }

    *pmdhHandle = 0;

    LDI_CONTEXT* context = nullptr;
    void* decoder_memory = nullptr;
    if (pfnma != nullptr)
    {
        context = static_cast<LDI_CONTEXT*>(pfnma(0x3050));
        decoder_memory = pvmem;
    }
    else
    {
        context = static_cast<LDI_CONTEXT*>(pvmem);
        decoder_memory = static_cast<std::uint8_t*>(pvmem) + 0x3050;
    }

    if (context == nullptr)
    {
        return 1;
    }

    const unsigned int* const configuration = static_cast<const unsigned int*>(pvConfiguration);
    context->decoder_context.dec_malloc = pfnma;
    context->decoder_context.dec_free = pfnmf;
    context->decoder_context.dec_memory = decoder_memory;
    context->cbDataBlockMax = *pcbDataBlockMax;
    context->configuration_value = configuration[1];
    context->signature = LDI_CONTEXT_SIGNATURE;

    if (!LZX_DecodeInit(&context->decoder_context, static_cast<int>(configuration[0])))
    {
        dec_free(&context->decoder_context, context);
        return 1;
    }

    *pmdhHandle = reinterpret_cast<unsigned __int64>(context);
    return 0;
}

int LDIDecompress(
    unsigned __int64 hmd,
    void* pbSrc,
    unsigned int cbSrc,
    void* pbDst,
    unsigned int* pcbResult)
{
    LDI_CONTEXT* const context = reinterpret_cast<LDI_CONTEXT*>(hmd);
    if (context->signature != LDI_CONTEXT_SIGNATURE)
    {
        return 2;
    }

    if (*pcbResult > context->cbDataBlockMax)
    {
        return 3;
    }

    long total_bytes_written = 0;
    const int decode_result = LZX_Decode(
        &context->decoder_context,
        static_cast<int>(*pcbResult),
        static_cast<unsigned __int8*>(pbSrc),
        static_cast<int>(cbSrc),
        static_cast<unsigned __int8*>(pbDst),
        static_cast<int>(*pcbResult),
        &total_bytes_written);

    *pcbResult = static_cast<unsigned int>(total_bytes_written);
    return decode_result == 0 ? 0 : 4;
}

int LDIResetDecompression(unsigned __int64 hmd)
{
    LDI_CONTEXT* const context = reinterpret_cast<LDI_CONTEXT*>(hmd);
    if (context->signature != LDI_CONTEXT_SIGNATURE)
    {
        return 2;
    }

    LZX_DecodeNewGroup(&context->decoder_context);
    return 0;
}

int LDIDestroyDecompression(unsigned __int64 hmd)
{
    LDI_CONTEXT* const context = reinterpret_cast<LDI_CONTEXT*>(hmd);
    if (context->signature != LDI_CONTEXT_SIGNATURE)
    {
        return 2;
    }

    LZX_DecodeFree(&context->decoder_context);
    context->signature = 0;
    dec_free(&context->decoder_context, context);
    return 0;
}

int LDIGetWindow(
    unsigned __int64 hmd,
    unsigned __int8** ppWindow,
    int* pFileOffset,
    int* pWindowOffset,
    int* pcbBytesAvail)
{
    LDI_CONTEXT* const context = reinterpret_cast<LDI_CONTEXT*>(hmd);
    t_decoder_context* const decoder = &context->decoder_context;

    *ppWindow = decoder->dec_mem_window;
    if (static_cast<unsigned int>(decoder->dec_position_at_start) < decoder->dec_window_size)
    {
        *pFileOffset = 0;
        *pWindowOffset = 0;
        *pcbBytesAvail = decoder->dec_position_at_start;
    }
    else
    {
        *pFileOffset = (decoder->dec_window_size - 1) & decoder->dec_position_at_start;
        *pcbBytesAvail = static_cast<int>(decoder->dec_window_size);
        *pWindowOffset = decoder->dec_position_at_start - static_cast<int>(decoder->dec_window_size);
    }

    return 0;
}

int LDISetWindowData(unsigned __int64 hmd, const unsigned __int8* pb, unsigned int cb)
{
    LDI_CONTEXT* const context = reinterpret_cast<LDI_CONTEXT*>(hmd);
    if (context->signature != LDI_CONTEXT_SIGNATURE)
    {
        return 2;
    }

    return LZX_DecodeInsertDictionary(&context->decoder_context, pb, cb) ? 0 : 4;
}

void DestroyDecompressionContextLzx(_LZXDECOMPRESSION_CONTEXT_HEADER* pContext)
{
    (void)pContext;
}

std::size_t GetDecompressionContextSizeLzx(const _XMEMCODEC_PARAMETERS_LZX* pLzxParams, unsigned long Flags)
{
    _XMEMCODEC_PARAMETERS_LZX LzxParams{};
    if (pLzxParams != nullptr)
    {
        LzxParams = *pLzxParams;
    }

    if (LzxParams.WindowSize == 0)
    {
        LzxParams.WindowSize = 0x20000;
    }

    unsigned __int64 ContextSize = 0;
    init_decompression_memory_context(nullptr, &ContextSize, static_cast<int>(LzxParams.WindowSize), 0x18, Flags);
    return static_cast<std::size_t>(ContextSize);
}

int ResetDecompressionContextLzx(_LZXDECOMPRESSION_CONTEXT_HEADER* pContext)
{
    t_decoder_context* const decoder = GetDecoderContext(pContext);
    LZX_DecodeNewGroup(decoder);

    if ((pContext->Common.Flags & 1) != 0)
    {
        decoder->dec_source_staging_size = 0;
        decoder->dec_end_of_stream = false;
        *reinterpret_cast<unsigned __int64*>(&decoder->dec_dest_staging_offset) = 0;
    }
    else if ((pContext->Common.Flags & 0x80000000u) != 0)
    {
        *reinterpret_cast<unsigned __int64*>(&decoder->dec_td_last_source) = 0;
        *reinterpret_cast<unsigned __int64*>(&decoder->dec_td_last_source_size) = 0;
        *reinterpret_cast<unsigned __int64*>(&decoder->dec_td_last_segment_offset) = 0;
        *reinterpret_cast<unsigned __int64*>(&decoder->dec_td_last_decoded_size) = 0;
        decoder->dec_td_last_stage_size = 0;
    }

    return 0;
}

int DecompressLzx(
    _LZXDECOMPRESSION_CONTEXT_HEADER* pContext,
    void* pDestination,
    unsigned __int64* pDestSize,
    const void* pSource,
    unsigned __int64 SrcSize)
{
    t_decoder_context* const decoder = GetDecoderContext(pContext);
    unsigned int remaining_source = 0;
    if (SrcSize > 5)
    {
        remaining_source = static_cast<unsigned int>(SrcSize - 5);
    }

    if ((pContext->Common.Flags & 1) == 0)
    {
        ResetDecompressionContextLzx(pContext);
    }

    const unsigned __int8* source_ptr = static_cast<const unsigned __int8*>(pSource);
    unsigned __int8* destination_ptr = static_cast<unsigned __int8*>(pDestination);
    unsigned int total_bytes_written = 0;

    while (remaining_source != 0)
    {
        unsigned int compressed_size = 0x8000;
        if (source_ptr[0] == 0xFF)
        {
            compressed_size = (static_cast<unsigned int>(source_ptr[3]) << 8) | source_ptr[4];
            source_ptr += 5;
            remaining_source -= 5;
        }
        else
        {
            compressed_size = (static_cast<unsigned int>(source_ptr[0]) << 8) | source_ptr[1];
            source_ptr += 2;
            remaining_source -= 2;
        }

        long block_bytes_written = 0;
        LZX_Decode(
            decoder,
            0x8000,
            const_cast<unsigned __int8*>(source_ptr),
            static_cast<int>(compressed_size),
            destination_ptr,
            0x8000,
            &block_bytes_written);

        total_bytes_written += block_bytes_written;
        destination_ptr += block_bytes_written;
        source_ptr += compressed_size;
        remaining_source -= compressed_size;
    }

    *pDestSize = total_bytes_written;
    return 0;
}

int DecompressStreamLzx(
    _LZXDECOMPRESSION_CONTEXT_HEADER* pContext,
    void* pDestination,
    unsigned __int64* pDestSize,
    const void* pSource,
    unsigned __int64* pSrcSize)
{
    t_decoder_context* const decoder = GetDecoderContext(pContext);
    const std::uint8_t* const source_bytes = static_cast<const std::uint8_t*>(pSource);
    std::uint8_t* const destination_bytes = static_cast<std::uint8_t*>(pDestination);

    const unsigned __int64 dest_capacity = *pDestSize;
    unsigned int source_available = 0;
    if (!decoder->dec_end_of_stream)
    {
        source_available = static_cast<unsigned int>(*pSrcSize);
    }

    std::uint64_t dest_written = 0;
    unsigned int source_consumed = 0;

    if (decoder->dec_dest_staging_size != 0)
    {
        unsigned int copy_size = decoder->dec_dest_staging_size;
        if (dest_capacity < copy_size)
        {
            copy_size = static_cast<unsigned int>(dest_capacity);
        }

        std::memcpy(
            destination_bytes,
            decoder->dec_dest_staging_buffer + decoder->dec_dest_staging_offset,
            copy_size);

        decoder->dec_dest_staging_size -= copy_size;
        decoder->dec_dest_staging_offset += copy_size;
        dest_written = copy_size;
    }

    if ((source_available == 0) && (decoder->dec_source_staging_size <= 1))
    {
        *pDestSize = dest_written;
        *pSrcSize = source_consumed;
        return 0;
    }

    unsigned int remaining_source = source_available;
    const std::uint8_t* block_source = source_bytes;

    if (decoder->dec_source_staging_size != 0)
    {
        block_source = decoder->dec_source_staging_buffer;

        const bool final_chunk = block_source[0] == 0xFF;
        const unsigned int header_size = final_chunk ? 5u : 2u;
        const unsigned int trailer_size = final_chunk ? 5u : 0u;
        const std::uint8_t* const size_ptr = final_chunk ? (block_source + 3) : block_source;

        if (decoder->dec_source_staging_size < header_size)
        {
            unsigned int copy_size = header_size - decoder->dec_source_staging_size;
            if (copy_size > remaining_source)
            {
                copy_size = remaining_source;
            }

            std::memcpy(
                decoder->dec_source_staging_buffer + decoder->dec_source_staging_size,
                source_bytes,
                copy_size);

            decoder->dec_source_staging_size += copy_size;
            source_consumed += copy_size;
            remaining_source -= copy_size;
        }

        if (remaining_source != 0)
        {
            unsigned int total_block_size =
                (static_cast<unsigned int>(size_ptr[0]) << 8) | size_ptr[1];
            total_block_size += trailer_size + header_size;

            unsigned int copy_size = total_block_size - decoder->dec_source_staging_size;
            if (copy_size > remaining_source)
            {
                copy_size = remaining_source;
            }

            std::memcpy(
                decoder->dec_source_staging_buffer + decoder->dec_source_staging_size,
                source_bytes + source_consumed,
                copy_size);

            source_consumed += copy_size;
            remaining_source -= copy_size;
            decoder->dec_source_staging_size += copy_size;

            if (decoder->dec_source_staging_size < total_block_size)
            {
                remaining_source = 0;
            }
        }
    }

    while (decoder->dec_dest_staging_size == 0)
    {
        if (remaining_source == 0)
        {
            break;
        }

        const unsigned int first_byte = block_source[0];
        unsigned int header_size = 0;
        unsigned int trailer_size = 0;
        unsigned int chunk_output_size = 0;
        unsigned int total_block_size = 0;

        if (first_byte == 0xFF)
        {
            header_size = 5;
            trailer_size = 5;
            if (remaining_source >= 5)
            {
                chunk_output_size = (static_cast<unsigned int>(block_source[1]) << 8) | block_source[2];
                total_block_size =
                    ((static_cast<unsigned int>(block_source[3]) << 8) | block_source[4]) + 10;
            }
            else
            {
                total_block_size = 5;
            }
        }
        else
        {
            header_size = 2;
            if (remaining_source >= 2)
            {
                chunk_output_size = 0x8000;
                total_block_size = ((static_cast<unsigned int>(block_source[0]) << 8) | block_source[1]) + 2;
            }
            else
            {
                total_block_size = 2;
            }
        }

        if (remaining_source < total_block_size)
        {
            std::memcpy(decoder->dec_source_staging_buffer, block_source, remaining_source);
            source_consumed += remaining_source;
            decoder->dec_source_staging_size = remaining_source;
            remaining_source = 0;
            break;
        }

        if ((remaining_source < (total_block_size + 5)) &&
            (block_source != decoder->dec_source_staging_buffer) &&
            (trailer_size == 0))
        {
            std::memcpy(decoder->dec_source_staging_buffer, block_source, total_block_size);
            source_consumed += total_block_size;
            block_source = decoder->dec_source_staging_buffer;
            decoder->dec_source_staging_size = total_block_size;
        }

        std::uint8_t* decode_destination = destination_bytes + dest_written;
        const unsigned __int64 remaining_dest = dest_capacity - dest_written;
        if (remaining_dest < chunk_output_size)
        {
            decode_destination = decoder->dec_dest_staging_buffer;
        }

        long dest_block_size = 0;
        const int decode_result = LZX_Decode(
            decoder,
            static_cast<int>(chunk_output_size),
            const_cast<unsigned __int8*>(block_source + header_size),
            static_cast<int>(total_block_size - trailer_size - header_size),
            decode_destination,
            static_cast<int>(chunk_output_size),
            &dest_block_size);

        if (decode_destination == decoder->dec_dest_staging_buffer)
        {
            unsigned int copy_size = dest_block_size;
            if (remaining_dest < copy_size)
            {
                copy_size = static_cast<unsigned int>(remaining_dest);
            }

            std::memcpy(destination_bytes + dest_written, decode_destination, copy_size);
            decoder->dec_dest_staging_offset = copy_size;
            decoder->dec_dest_staging_size = dest_block_size - copy_size;
            dest_written += copy_size;
        }
        else
        {
            dest_written += dest_block_size;
        }

        if (block_source == decoder->dec_source_staging_buffer)
        {
            block_source = source_bytes + source_consumed;
            decoder->dec_source_staging_size = 0;
        }
        else
        {
            source_consumed += total_block_size;
            block_source += total_block_size;
        }

        if (first_byte == 0xFF)
        {
            decoder->dec_end_of_stream = true;
            remaining_source = 0;
        }
        else
        {
            remaining_source = source_available - source_consumed;
        }
    }

    *pDestSize = dest_written;
    *pSrcSize = source_consumed;
    return 0;
}

void DecompressSegmentTDLzx(
    _LZXDECOMPRESSION_CONTEXT_HEADER* pContext,
    void* pDestination,
    unsigned __int64* pDestSize,
    const void* pSource,
    unsigned __int64 SrcSize,
    unsigned __int64 SegmentSize,
    unsigned __int64 SegmentOffset)
{
    t_decoder_context* const decoder = GetDecoderContext(pContext);
    const std::uint8_t* const source_bytes = static_cast<const std::uint8_t*>(pSource);
    const std::uint16_t* source_words = static_cast<const std::uint16_t*>(pSource);
    std::uint8_t* destination_bytes = static_cast<std::uint8_t*>(pDestination);
    const unsigned int source_size = static_cast<unsigned int>(SrcSize);
    const unsigned int segment_size = static_cast<unsigned int>(SegmentSize);
    const unsigned int segment_offset = static_cast<unsigned int>(SegmentOffset);
    const unsigned int destination_capacity = static_cast<unsigned int>(*pDestSize);

    unsigned int bytes_written = 0;
    unsigned int decoded_offset = 0;
    unsigned int source_offset = 0;
    unsigned int stage_offset = 0;
    unsigned int stage_size = 0;
    bool keep_stage_data = false;

    if (*source_words == 0)
    {
        unsigned int copy_size = destination_capacity;
        const unsigned int available_bytes = segment_size - segment_offset;
        if (copy_size > available_bytes)
        {
            copy_size = available_bytes;
        }

        std::memcpy(destination_bytes, source_bytes + segment_offset + 2, copy_size);
        decoder->dec_td_last_segment_offset = decoder->dec_td_last_segment_size;
        *pDestSize = copy_size;
        return;
    }

    const bool cache_hit =
        (decoder->dec_td_last_source == source_bytes) &&
        (decoder->dec_td_last_source_size == source_size) &&
        (decoder->dec_td_last_segment_size == segment_size) &&
        (segment_offset >= decoder->dec_td_last_segment_offset);

    const std::uint16_t* block_source = source_words;
    unsigned int remaining_source = source_size;
    if (cache_hit)
    {
        source_offset = decoder->dec_td_last_source_offset;
        block_source = reinterpret_cast<const std::uint16_t*>(source_bytes + source_offset);
        remaining_source = source_size - source_offset;
        decoded_offset = decoder->dec_td_last_decoded_size;

        if (decoder->dec_td_last_stage_size != 0)
        {
            const unsigned int delta = segment_offset - decoder->dec_td_last_segment_offset;
            if (delta < decoder->dec_td_last_stage_size)
            {
                unsigned int copy_size = decoder->dec_td_last_stage_size - delta;
                if (copy_size > destination_capacity)
                {
                    copy_size = destination_capacity;
                }

                std::memcpy(
                    destination_bytes,
                    decoder->dec_td_staging_buffer + decoder->dec_td_last_stage_offset + delta,
                    copy_size);

                bytes_written = copy_size;
                destination_bytes += copy_size;
                stage_offset = decoder->dec_td_last_stage_offset + delta + copy_size;
                stage_size = decoder->dec_td_last_stage_size - delta - copy_size;
                keep_stage_data = true;
            }
        }
    }
    else
    {
        LZX_DecodeNewGroup(decoder);
    }

    while ((remaining_source > 1) && (bytes_written < destination_capacity))
    {
        const unsigned int compressed_size =
            LEndianSwap8In16Local(*block_source);
        if (compressed_size == 0)
        {
            break;
        }

        block_source += 1;
        remaining_source -= 2;

        unsigned int chunk_output_size = 0x8000;
        const unsigned int remaining_segment_bytes = segment_size - decoded_offset;
        if (remaining_segment_bytes < chunk_output_size)
        {
            chunk_output_size = remaining_segment_bytes;
        }

        unsigned int prefix_skip = 0;
        if (segment_offset > decoded_offset)
        {
            prefix_skip = segment_offset - decoded_offset;
            if (prefix_skip > chunk_output_size)
            {
                prefix_skip = chunk_output_size;
            }
        }

        unsigned int bytes_from_chunk = chunk_output_size - prefix_skip;
        const unsigned int destination_remaining = destination_capacity - bytes_written;
        if (bytes_from_chunk > destination_remaining)
        {
            bytes_from_chunk = destination_remaining;
        }

        const bool stage_this_chunk = bytes_from_chunk < chunk_output_size;
        std::uint8_t* decode_destination = destination_bytes;
        if (stage_this_chunk)
        {
            decode_destination = decoder->dec_td_staging_buffer;
        }

        long total_bytes_written = 0;
        LZX_Decode(
            decoder,
            static_cast<int>(chunk_output_size),
            const_cast<unsigned __int8*>(reinterpret_cast<const unsigned __int8*>(block_source)),
            static_cast<int>(compressed_size),
            decode_destination,
            static_cast<int>(chunk_output_size),
            &total_bytes_written);

        if (stage_this_chunk && (bytes_from_chunk != 0))
        {
            std::memcpy(
                destination_bytes,
                decoder->dec_td_staging_buffer + prefix_skip,
                bytes_from_chunk);
        }

        bytes_written += bytes_from_chunk;
        destination_bytes += bytes_from_chunk;
        block_source =
            reinterpret_cast<const std::uint16_t*>(
                reinterpret_cast<const std::uint8_t*>(block_source) + compressed_size);
        remaining_source -= compressed_size;
        source_offset =
            static_cast<unsigned int>(reinterpret_cast<const std::uint8_t*>(block_source) - source_bytes);
        decoded_offset += chunk_output_size;

        keep_stage_data = stage_this_chunk;
        if (stage_this_chunk)
        {
            stage_offset = prefix_skip + bytes_from_chunk;
            stage_size = chunk_output_size - prefix_skip - bytes_from_chunk;
        }
        else
        {
            stage_offset = 0;
            stage_size = 0;
        }
    }

    decoder->dec_td_last_source = source_bytes;
    decoder->dec_td_last_source_size = source_size;
    decoder->dec_td_last_source_offset = source_offset;
    decoder->dec_td_last_segment_size = segment_size;
    decoder->dec_td_last_segment_offset = segment_offset + bytes_written;
    decoder->dec_td_last_decoded_size = decoded_offset;

    if (keep_stage_data)
    {
        decoder->dec_td_last_stage_offset = stage_offset;
        decoder->dec_td_last_stage_size = stage_size;
    }
    else
    {
        decoder->dec_td_last_stage_offset = 0;
        decoder->dec_td_last_stage_size = 0;
    }

    *pDestSize = bytes_written;
}

void InitializeDecompressionContextLzx(
    const _XMEMCODEC_PARAMETERS_LZX* pLzxParams,
    unsigned long Flags,
    void* pContextData,
    std::size_t ContextSize)
{
    (void)ContextSize;

    _XMEMCODEC_PARAMETERS_LZX LzxParams{};
    if (pLzxParams != nullptr)
    {
        LzxParams = *pLzxParams;
    }

    if (LzxParams.WindowSize == 0)
    {
        LzxParams.WindowSize = 0x20000;
    }

    unsigned __int64 RequiredContextSize = 0;
    init_decompression_memory_context(
        pContextData,
        &RequiredContextSize,
        static_cast<int>(LzxParams.WindowSize),
        0x18,
        Flags);

    _LZXDECOMPRESSION_CONTEXT_HEADER* const context =
        static_cast<_LZXDECOMPRESSION_CONTEXT_HEADER*>(pContextData);
    context->LzxFlags = LzxParams.Flags;

    if ((Flags & 1) != 0)
    {
        ResetDecompressionContextLzx(context);
    }
}
}
