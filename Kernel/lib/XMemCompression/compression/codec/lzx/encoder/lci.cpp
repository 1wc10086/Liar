#include "../../../api/precomp.hpp"

#include <cstring>
#include <cstdint>
#include <array>
#include <algorithm>
#include <bit>
#include <new>

namespace XCOMPRESS
{
int ResetCompressionContextLzx(_LZXCOMPRESSION_CONTEXT_HEADER* pContext);
int XMCDShaInit(sha_state* state);
int XMCDShaUpdate(sha_state* state, const void* data, unsigned int size);
int XMCDShaFinal(sha_state* state, unsigned int* pHash);
void XMCDPRINT(char* Format, ...);
int LzxEncoderCallback(void* pParameter, unsigned __int8* pData, int CompressedSize, int UncompressedSize);
int LzxEncoderTDCallback(void* pParameter, unsigned __int8* pData, int CompressedSize, int UncompressedSize);

namespace
{
constexpr unsigned int LCI_CONTEXT_SIGNATURE = 0x4349434Cu;

struct LCI_CONTEXT
{
    unsigned int signature;
    std::uint32_t pad0;
    void* (__fastcall *pfnAlloc)(unsigned int);
    void (__fastcall *pfnFree)(void*);
    unsigned int cbDataBlockMax;
    unsigned int file_translation_size;
    t_encoder_context* encoder_context;
};

struct StreamEncoderBlockInfo
{
    std::uint8_t* pDestination;
    std::uint64_t DestSize;
    std::uint64_t CompressedSize;
    std::uint32_t ChunksEncoded;
    std::uint32_t ChunksFinal;
};

struct EncoderBlockInfo
{
    std::uint8_t* pDestination;
    std::uint64_t DestSize;
    std::uint64_t CompressedSize;
};

inline t_encoder_context* GetEncoderContext(_LZXCOMPRESSION_CONTEXT_HEADER* pContext)
{
    return reinterpret_cast<t_encoder_context*>(pContext->LzxData);
}

inline std::uint32_t& RawU32(void* base, std::size_t offset)
{
    return *reinterpret_cast<std::uint32_t*>(static_cast<std::uint8_t*>(base) + offset);
}

inline std::uint64_t& RawU64(void* base, std::size_t offset)
{
    return *reinterpret_cast<std::uint64_t*>(static_cast<std::uint8_t*>(base) + offset);
}
}

unsigned int XCTDGetSegmentCount(_XCOMPRESS_FILE_HEADER_LZXTDECODE* pHeader)
{
    return pHeader->SegmentCount;
}

unsigned int XCTDGetTranslationSize(_XCOMPRESS_FILE_HEADER_LZXTDECODE* pHeader)
{
    return (pHeader->DWord & 0x00C00000u) != 0 ? 0x20u : 0x14u;
}

unsigned int XCTDGetAccessTranslationSizeInDWords(_XCOMPRESS_FILE_HEADER_LZXTDECODE* pHeader)
{
    return (XCTDGetTranslationSize(pHeader) * XCTDGetSegmentCount(pHeader) + 0x1Fu) >> 5;
}

unsigned int XCTDGetAccessTranslationSize(_XCOMPRESS_FILE_HEADER_LZXTDECODE* pHeader)
{
    return XCTDGetAccessTranslationSizeInDWords(pHeader) << 2;
}

unsigned int XCTDGetHeaderSize(_XCOMPRESS_FILE_HEADER_LZXTDECODE* pHeader)
{
    return XCTDGetAccessTranslationSize(pHeader) + sizeof(_XCOMPRESS_FILE_HEADER_LZXTDECODE);
}

static unsigned int LocalCountLeadingZeros(int value)
{
    unsigned int count = 0;
    while ((value >= 0) && (count < 0x20))
    {
        ++count;
        value += value;
    }

    return count;
}

unsigned int Log2(unsigned int Value)
{
    return 31u - LocalCountLeadingZeros(static_cast<int>(Value));
}

static unsigned short LEndianSwap8In16Local(unsigned short Value)
{
    return static_cast<unsigned short>((Value << 8) | (Value >> 8));
}

unsigned int LEndianSwap8In32(unsigned int Value)
{
    return ((Value & 0x000000FFu) << 24) |
           ((Value & 0x0000FF00u) << 8) |
           ((Value & 0x00FF0000u) >> 8) |
           ((Value & 0xFF000000u) >> 24);
}

unsigned __int64 LEndianSwap8In64(unsigned __int64 Value)
{
    return ((Value & 0x00000000000000FFull) << 56) |
           ((Value & 0x000000000000FF00ull) << 40) |
           ((Value & 0x0000000000FF0000ull) << 24) |
           ((Value & 0x00000000FF000000ull) << 8) |
           ((Value & 0x000000FF00000000ull) >> 8) |
           ((Value & 0x0000FF0000000000ull) >> 24) |
           ((Value & 0x00FF000000000000ull) >> 40) |
           ((Value & 0xFF00000000000000ull) >> 56);
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

    if (pDstParams->CompressionPartitionSize == 0)
    {
        pDstParams->CompressionPartitionSize = 0x80000;
    }
}

int LCICreateCompression(
    unsigned int* pcbDataBlockMax,
    void* pvConfiguration,
    void* (__fastcall *pfnma)(unsigned int),
    void (__fastcall *pfnmf)(void*),
    unsigned int* pcbDstBufferMin,
    unsigned __int64* pmchHandle,
    int (__fastcall *pfnlzx_output_callback)(void*, unsigned __int8*, int, int),
    void* fci_data)
{
    *pmchHandle = 0;

    LCI_CONTEXT* const lci_context = static_cast<LCI_CONTEXT*>(pfnma(0x28));
    if (lci_context == nullptr)
    {
        return 1;
    }

    lci_context->file_translation_size = 0;
    lci_context->encoder_context = static_cast<t_encoder_context*>(pfnma(0x4408));
    if (lci_context->encoder_context == nullptr)
    {
        pfnmf(lci_context);
        return 1;
    }

    const unsigned int* const configuration = static_cast<const unsigned int*>(pvConfiguration);
    if (!LZX_EncodeInit(
            lci_context->encoder_context,
            static_cast<int>(configuration[0]),
            static_cast<int>(configuration[1]),
            pfnma,
            pfnmf,
            pfnlzx_output_callback,
            fci_data))
    {
        pfnmf(lci_context->encoder_context);
        pfnmf(lci_context);
        return 1;
    }

    lci_context->pfnAlloc = pfnma;
    lci_context->pfnFree = pfnmf;
    lci_context->cbDataBlockMax = *pcbDataBlockMax;
    lci_context->signature = LCI_CONTEXT_SIGNATURE;

    *pcbDstBufferMin = *pcbDataBlockMax + 0x1800;
    *pmchHandle = reinterpret_cast<unsigned __int64>(lci_context);
    return 0;
}

int LCICompress(
    unsigned __int64 hmc,
    void* pbSrc,
    unsigned int cbSrc,
    void* pbDst,
    unsigned int cbDst,
    unsigned int* pcbResult)
{
    LCI_CONTEXT* const context = reinterpret_cast<LCI_CONTEXT*>(hmc);
    if (context->signature != LCI_CONTEXT_SIGNATURE)
    {
        return 2;
    }

    if ((cbSrc > context->cbDataBlockMax) || (cbDst < (context->cbDataBlockMax + 0x1800)))
    {
        return 2;
    }

    int estimated_leftover_bytes = 0;
    if (LZX_Encode(
            context->encoder_context,
            static_cast<unsigned __int8*>(pbSrc),
            static_cast<int>(cbSrc),
            &estimated_leftover_bytes,
            static_cast<int>(context->file_translation_size)) == 0)
    {
        *pcbResult = static_cast<unsigned int>(estimated_leftover_bytes);
        return 0;
    }

    *pcbResult = 0;
    return 4;
}

int LCIFlushCompressorOutput(unsigned __int64 hmc)
{
    LCI_CONTEXT* const context = reinterpret_cast<LCI_CONTEXT*>(hmc);
    if (context->signature != LCI_CONTEXT_SIGNATURE)
    {
        return 2;
    }

    LZX_EncodeFlush(context->encoder_context);
    return 0;
}

int LCIResetCompression(unsigned __int64 hmc)
{
    LCI_CONTEXT* const context = reinterpret_cast<LCI_CONTEXT*>(hmc);
    if (context->signature != LCI_CONTEXT_SIGNATURE)
    {
        return 2;
    }

    LZX_EncodeNewGroup(context->encoder_context);
    return 0;
}

unsigned __int8* LCIGetInputData(unsigned __int64 hmc, unsigned int* input_position, unsigned int* bytes_available)
{
    LCI_CONTEXT* const context = reinterpret_cast<LCI_CONTEXT*>(hmc);
    if (context->signature != LCI_CONTEXT_SIGNATURE)
    {
        *bytes_available = 0;
        return nullptr;
    }

    return LZX_GetInputData(context->encoder_context, input_position, bytes_available);
}

int LCIResetState(unsigned __int64 hmc)
{
    LCI_CONTEXT* const context = reinterpret_cast<LCI_CONTEXT*>(hmc);
    if (context->signature != LCI_CONTEXT_SIGNATURE)
    {
        return 2;
    }

    LZX_EncodeResetState(context->encoder_context);
    return 0;
}

int LCISetTranslationSize(unsigned __int64 hmc, unsigned int size)
{
    LCI_CONTEXT* const context = reinterpret_cast<LCI_CONTEXT*>(hmc);
    if (context->signature != LCI_CONTEXT_SIGNATURE)
    {
        return 2;
    }

    context->file_translation_size = size;
    return 0;
}

int LCISetWindowData(unsigned __int64 hmc, const unsigned __int8* pb, unsigned int cb)
{
    LCI_CONTEXT* const context = reinterpret_cast<LCI_CONTEXT*>(hmc);
    if (context->signature != LCI_CONTEXT_SIGNATURE)
    {
        return 2;
    }

    LZX_EncodeInsertDictionary(context->encoder_context, pb, cb);
    return 0;
}

int LCIDestroyCompression(unsigned __int64 hmc)
{
    LCI_CONTEXT* const context = reinterpret_cast<LCI_CONTEXT*>(hmc);
    if (context->signature != LCI_CONTEXT_SIGNATURE)
    {
        return 2;
    }

    LZX_EncodeFree(context->encoder_context);
    context->pfnFree(context->encoder_context);
    context->signature = 0;
    context->pfnFree(context);
    return 0;
}

void DestroyCompressionContextLzx(_LZXCOMPRESSION_CONTEXT_HEADER* pContext)
{
    (void)pContext;
}

std::size_t GetCompressionContextSizeLzx(const _XMEMCODEC_PARAMETERS_LZX* pLzxParams, unsigned int Flags)
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

    if (LzxParams.CompressionPartitionSize == 0)
    {
        LzxParams.CompressionPartitionSize = 0x80000;
    }

    unsigned __int64 ContextSize = 0;
    comp_init_compress_memory_context(
        nullptr,
        &ContextSize,
        static_cast<int>(LzxParams.WindowSize),
        static_cast<int>(LzxParams.CompressionPartitionSize),
        0x10,
        Flags);
    return static_cast<std::size_t>(ContextSize);
}

void InitializeCompressionContextLzx(
    const _XMEMCODEC_PARAMETERS_LZX* pLzxParams,
    unsigned int Flags,
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

    if (LzxParams.CompressionPartitionSize == 0)
    {
        LzxParams.CompressionPartitionSize = 0x80000;
    }

    unsigned __int64 RequiredContextSize = 0;
    comp_init_compress_memory_context(
        pContextData,
        &RequiredContextSize,
        static_cast<int>(LzxParams.WindowSize),
        static_cast<int>(LzxParams.CompressionPartitionSize),
        0x10,
        Flags);

    _LZXCOMPRESSION_CONTEXT_HEADER* const context =
        static_cast<_LZXCOMPRESSION_CONTEXT_HEADER*>(pContextData);
    context->LzxFlags = LzxParams.Flags;

    if ((Flags & 1) != 0)
    {
        ResetCompressionContextLzx(context);
    }
}

int ResetCompressionContextLzx(_LZXCOMPRESSION_CONTEXT_HEADER* pContext)
{
    t_encoder_context* const encoder = GetEncoderContext(pContext);
    LZX_EncodeNewGroup(encoder);

    if ((pContext->Common.Flags & 1) != 0)
    {
        encoder->enc_dest_staging_offset = 0;
        RawU64(encoder, 0x43D4) = 0;
    }
    else if ((pContext->Common.Flags & 0x80000000u) != 0)
    {
        RawU32(encoder, 0x43B0) = 0;
        RawU64(encoder, 0x43B8) = 0;
        RawU64(encoder, 0x43C0) = 0;
        RawU64(encoder, 0x43D0) = 0;
        RawU64(encoder, 0x43D8) = 0;
        RawU32(encoder, 0x43E0) = 0;
        encoder->enc_td_encode_data_uncompressed = false;
    }

    return 0;
}

int BeginCompressionTDLzx(_LZXCOMPRESSION_CONTEXT_HEADER* pContext, unsigned __int64 SegmentPitch)
{
    t_encoder_context* const encoder = GetEncoderContext(pContext);
    void* const encoder_bytes = encoder;

    if (RawU32(encoder_bytes, 0x43B0) == 0)
    {
        RawU32(encoder_bytes, 0x43B0) = static_cast<std::uint32_t>(SegmentPitch);
    }

    comp_clear_compress_memory_context(encoder);

    const std::uint32_t saved_value_43D0 = RawU32(encoder_bytes, 0x43D0);
    RawU64(encoder_bytes, 0x43B8) = 0;
    RawU64(encoder_bytes, 0x43C0) = 0;
    RawU32(encoder_bytes, 0x43D0) = 0;
    RawU32(encoder_bytes, 0x43D4) = saved_value_43D0;

    const std::uint32_t saved_value_43DC = RawU32(encoder_bytes, 0x43DC);
    RawU32(encoder_bytes, 0x43DC) = 0;
    RawU32(encoder_bytes, 0x43E0) = saved_value_43DC;

    return XMCDShaInit(&encoder->enc_td_sha_state) != 0 ? 0 : 4;
}

int CompressSegmentTDLzx(
    _LZXCOMPRESSION_CONTEXT_HEADER* pContext,
    void* pDestination,
    unsigned __int64* pDestSize,
    const void* pSource,
    unsigned __int64* pSrcSize,
    float Threshold)
{
    t_encoder_context* const encoder = GetEncoderContext(pContext);
    const std::uint8_t* const source_begin = static_cast<const std::uint8_t*>(pSource);
    const unsigned int requested_source_size = static_cast<unsigned int>(*pSrcSize);
    const unsigned int segment_index = encoder->enc_td_segment_count;

    unsigned int header_size = 0;
    unsigned int translation_padding = 0;
    if (segment_index == 0)
    {
        header_size = 0x10;
        if (!encoder->enc_td_encode_data_uncompressed)
        {
            unsigned int translation_bits = 0x20;
            if (encoder->enc_td_translation_bits_expected <= 0x14)
            {
                translation_bits = 0x14;
            }

            header_size =
                0x10 + ((((translation_bits * encoder->enc_td_segment_count_expected) + 0x1Fu) >> 5) << 2);
        }

        translation_padding = encoder->enc_td_translation_padding;
    }

    const unsigned int segment_pitch = static_cast<unsigned int>(encoder->enc_td_segment_pitch);
    if (header_size >= (segment_pitch - 8))
    {
        XMCDPRINT(const_cast<char*>("The file header is too large to fit within the first compressed data segment.\n"));
        return 3;
    }

    const unsigned int payload_budget = segment_pitch - translation_padding - header_size;
    int result = 0;
    unsigned int source_consumed = 0;
    unsigned __int64 output_size = 0;

    if (encoder->enc_td_encode_data_uncompressed)
    {
        unsigned int copied_source_size = requested_source_size;
        const unsigned int max_copy = segment_pitch - header_size;
        if (copied_source_size > max_copy)
        {
            copied_source_size = max_copy;
        }

        std::uint8_t* const payload = static_cast<std::uint8_t*>(pDestination) + header_size;
        std::memcpy(payload, source_begin, copied_source_size);

        output_size = static_cast<unsigned __int64>(copied_source_size + header_size);
        source_consumed = copied_source_size;

        const unsigned __int64 available_destination = *pDestSize;
        *pDestSize = output_size;
        *pSrcSize = copied_source_size;
        if (available_destination < output_size)
        {
            result = 6;
        }

        encoder->enc_td_uncompressed_size += copied_source_size;
        encoder->enc_td_compressed_size += output_size;
        encoder->enc_tdat_uncompressed_size_list[segment_index] = copied_source_size;
    }
    else
    {
        EncoderBlockInfo block_info{};
        block_info.pDestination = static_cast<std::uint8_t*>(pDestination) + header_size;

        const unsigned __int64 available_destination = *pDestSize;
        const unsigned int consumed_prefix = translation_padding + header_size;
        if (available_destination > consumed_prefix)
        {
            block_info.DestSize = available_destination - consumed_prefix;
        }

        encoder->enc_output_callback_function = LzxEncoderTDCallback;
        encoder->enc_fci_data = &block_info;
        LZX_EncodeNewGroup(encoder);

        const std::uint8_t* source_ptr = source_begin;
        unsigned int source_remaining = requested_source_size;
        unsigned int lower_bound = 1;
        unsigned int compressed_payload_size = 0;
        unsigned int original_chunk_limit = 0x8000;
        if (source_remaining < original_chunk_limit)
        {
            original_chunk_limit = source_remaining;
        }

        unsigned int current_candidate = original_chunk_limit;
        unsigned int upper_bound = original_chunk_limit;
        unsigned __int64 accepted_compressed_size = block_info.CompressedSize;
        std::uint8_t* snapshot_destination = block_info.pDestination;
        unsigned __int64 snapshot_dest_size = block_info.DestSize;
        unsigned __int64 snapshot_compressed_size = block_info.CompressedSize;

        if (payload_budget != 0)
        {
            while ((current_candidate != 0) && (upper_bound >= lower_bound) && (source_remaining != 0))
            {
                const unsigned int available_payload = payload_budget - compressed_payload_size;
                if (available_payload < (current_candidate + 0x1800))
                {
                    snapshot_destination = block_info.pDestination;
                    snapshot_dest_size = block_info.DestSize;
                    snapshot_compressed_size = block_info.CompressedSize;
                    std::memcpy(
                        encoder->enc_context_data_snapshot,
                        encoder,
                        static_cast<std::size_t>(encoder->enc_context_data_size));
                }

                int estimated_leftover_bytes = 0;
                if (LZX_Encode(
                        encoder,
                        const_cast<std::uint8_t*>(source_ptr),
                        static_cast<int>(current_candidate),
                        &estimated_leftover_bytes,
                        0) != 0)
                {
                    result = 4;
                    break;
                }

                LZX_EncodeFlush(encoder);

                const unsigned int compressed_delta =
                    static_cast<unsigned int>(block_info.CompressedSize - accepted_compressed_size);
                if ((compressed_payload_size + compressed_delta) > payload_budget)
                {
                    upper_bound = current_candidate - 1;
                    if (upper_bound < lower_bound)
                    {
                        lower_bound = upper_bound;
                        current_candidate = upper_bound;
                    }
                    else
                    {
                        const unsigned int projected_candidate =
                            static_cast<unsigned int>(
                                (static_cast<unsigned __int64>(available_payload) * current_candidate) /
                                compressed_delta);
                        const unsigned int relaxed_candidate =
                            lower_bound + ((upper_bound - lower_bound) / 10);
                        unsigned int next_candidate =
                            projected_candidate > relaxed_candidate ? projected_candidate : relaxed_candidate;
                        if (next_candidate >= upper_bound)
                        {
                            next_candidate = upper_bound;
                        }

                        current_candidate = next_candidate;
                    }

                    block_info.pDestination = snapshot_destination;
                    block_info.DestSize = snapshot_dest_size;
                    block_info.CompressedSize = snapshot_compressed_size;
                    std::memcpy(
                        encoder,
                        encoder->enc_context_data_snapshot,
                        static_cast<std::size_t>(encoder->enc_context_data_size));

                    continue;
                }

                bool finalize_segment = false;
                if ((compressed_delta < available_payload) && (current_candidate != original_chunk_limit))
                {
                    lower_bound = current_candidate + 1;
                    if (lower_bound <= upper_bound)
                    {
                        const unsigned int projected_candidate =
                            static_cast<unsigned int>(
                                (static_cast<unsigned __int64>(available_payload) * current_candidate) /
                                compressed_delta);
                        const unsigned int relaxed_candidate =
                            upper_bound - ((upper_bound - lower_bound) / 10);
                        unsigned int next_candidate =
                            projected_candidate < relaxed_candidate ? projected_candidate : relaxed_candidate;
                        if (next_candidate <= lower_bound)
                        {
                            next_candidate = lower_bound;
                        }

                        current_candidate = next_candidate;
                        block_info.pDestination = snapshot_destination;
                        block_info.DestSize = snapshot_dest_size;
                        block_info.CompressedSize = snapshot_compressed_size;
                        std::memcpy(
                            encoder,
                            encoder->enc_context_data_snapshot,
                            static_cast<std::size_t>(encoder->enc_context_data_size));

                        continue;
                    }

                    // Legacy behavior stops the segment here once the best smaller
                    // chunk has been accepted and no larger candidate remains.
                    finalize_segment = true;
                }

                source_ptr += current_candidate;
                source_remaining -= current_candidate;
                compressed_payload_size += compressed_delta;

                if (finalize_segment)
                {
                    break;
                }

                accepted_compressed_size = block_info.CompressedSize;

                current_candidate = 0x8000;
                if (source_remaining < current_candidate)
                {
                    current_candidate = source_remaining;
                }

                original_chunk_limit = current_candidate;
                upper_bound = current_candidate;
                if (compressed_payload_size >= payload_budget)
                {
                    break;
                }
            }
        }

        unsigned int segment_body_size = static_cast<unsigned int>(block_info.CompressedSize);
        source_consumed = static_cast<unsigned int>(source_ptr - source_begin);

        if (result == 0)
        {
            unsigned int uncompressed_tail = payload_budget - compressed_payload_size;
            if (source_remaining < uncompressed_tail)
            {
                uncompressed_tail = source_remaining;
            }

            const float threshold_output =
                static_cast<float>(block_info.CompressedSize + uncompressed_tail);
            const float threshold_input = static_cast<float>(source_consumed + 2) * Threshold;
            if (!(threshold_output < threshold_input))
            {
                unsigned int raw_source_size = requested_source_size;
                const unsigned int max_raw_source_size = payload_budget - 2;
                if (raw_source_size > max_raw_source_size)
                {
                    raw_source_size = max_raw_source_size;
                }

                const unsigned int raw_segment_size = raw_source_size + 2;
                if (block_info.DestSize >= 2)
                {
                    std::uint8_t* const payload = static_cast<std::uint8_t*>(pDestination) + header_size;
                    *reinterpret_cast<unsigned short*>(payload) = 0;

                    unsigned int raw_copy_size = raw_source_size;
                    const unsigned __int64 max_copy_size = block_info.DestSize - 2;
                    if (raw_copy_size > max_copy_size)
                    {
                        raw_copy_size = static_cast<unsigned int>(max_copy_size);
                    }

                    std::memcpy(payload + 2, source_begin, raw_copy_size);
                }

                source_consumed = raw_source_size;
                source_remaining = requested_source_size - raw_source_size;
                block_info.pDestination = static_cast<std::uint8_t*>(pDestination) + header_size + raw_segment_size;
                segment_body_size = raw_segment_size;
            }
            else
            {
                unsigned int zero_fill = uncompressed_tail;
                if (zero_fill > 2)
                {
                    zero_fill = 2;
                }

                if (zero_fill != 0)
                {
                    std::memset(block_info.pDestination, 0, zero_fill);
                    block_info.pDestination += zero_fill;
                    segment_body_size += zero_fill;
                }
            }

            const unsigned int alignment = source_remaining != 0 ? segment_pitch : 1;
            const unsigned __int64 total_size_before_align =
                static_cast<unsigned __int64>(header_size + translation_padding) + segment_body_size;
            output_size = (total_size_before_align + alignment - 1) & ~static_cast<unsigned __int64>(alignment - 1);

            std::memset(
                block_info.pDestination,
                0,
                static_cast<std::size_t>(output_size - total_size_before_align));
            *pDestSize = output_size;

            if (output_size > (static_cast<unsigned __int64>(header_size + translation_padding) + block_info.DestSize))
            {
                result = 6;
            }
        }
        else
        {
            *pDestSize = 0;
        }

        *pSrcSize = source_consumed;
        encoder->enc_td_uncompressed_size += source_consumed;
        encoder->enc_td_compressed_size += *pDestSize;
        encoder->enc_tdat_uncompressed_size_list[segment_index] = source_consumed;
    }

    unsigned int translation_bits = 0;
    if (source_consumed != 0)
    {
        translation_bits = 32u - LocalCountLeadingZeros(static_cast<int>(source_consumed));
    }

    if (translation_bits > encoder->enc_td_translation_bits)
    {
        encoder->enc_td_translation_bits = translation_bits;
    }

    if ((segment_index == 0) && (result == 0))
    {
        if (encoder->enc_td_encode_data_uncompressed)
        {
            if (!XMCDShaUpdate(&encoder->enc_td_sha_state, source_begin, source_consumed))
            {
                result = 4;
            }
        }
        else if (!XMCDShaUpdate(
                      &encoder->enc_td_sha_state,
                      static_cast<std::uint8_t*>(pDestination) + header_size,
                      static_cast<unsigned int>(output_size - header_size)))
        {
            result = 4;
        }
    }

    ++encoder->enc_td_segment_count;
    return result;
}

int EndCompressionTDLzx(
    _LZXCOMPRESSION_CONTEXT_HEADER* pContext,
    void* pHeaderData,
    unsigned __int64* pHeaderSize,
    float Threshold)
{
    t_encoder_context* const encoder = GetEncoderContext(pContext);

    unsigned int translation_bits = encoder->enc_td_translation_bits;
    unsigned int translation_entry_bits = 0x20;
    if (translation_bits <= 0x14)
    {
        translation_entry_bits = 0x14;
    }

    unsigned int expected_translation_entry_bits = 0x20;
    if (encoder->enc_td_translation_bits_expected <= 0x14)
    {
        expected_translation_entry_bits = 0x14;
    }

    unsigned int segment_count = 0;
    unsigned int access_translation_dwords = 0;
    unsigned int translation_mode = 0;
    unsigned int header_size = 0x10;

    if (!encoder->enc_td_encode_data_uncompressed && (encoder->enc_td_uncompressed_size != 0))
    {
        const unsigned int expected_segment_count = encoder->enc_td_segment_count_expected;
        segment_count = encoder->enc_td_segment_count;

        if ((expected_segment_count < segment_count) ||
            (translation_entry_bits > expected_translation_entry_bits))
        {
            *pHeaderSize = 0;
            return 7;
        }

        if ((expected_segment_count >= segment_count) &&
            (translation_entry_bits <= expected_translation_entry_bits))
        {
            const float segment_threshold = static_cast<float>(segment_count);
            const float payload_threshold =
                static_cast<float>(encoder->enc_td_uncompressed_size + 0x10) * Threshold;
            if (!(segment_threshold < payload_threshold))
            {
                encoder->enc_td_encode_data_uncompressed = true;
                *pHeaderSize = 0;
                return 7;
            }
        }

        if ((expected_segment_count > segment_count) ||
            (translation_entry_bits < expected_translation_entry_bits))
        {
            encoder->enc_td_translation_padding =
                ((((expected_segment_count * expected_translation_entry_bits) + 0x1Fu) >> 5) -
                 (((segment_count * translation_entry_bits) + 0x1Fu) >> 5))
                << 2;
            if (encoder->enc_td_translation_padding != 0)
            {
                *pHeaderSize = 0;
                return 7;
            }

            encoder->enc_td_segment_count_expected = segment_count;
            encoder->enc_td_translation_bits_expected = translation_bits;
        }

        access_translation_dwords = ((segment_count * translation_entry_bits) + 0x1Fu) >> 5;
        translation_mode = translation_entry_bits != 0x14 ? 1u : 0u;
        header_size = 0x10 + (access_translation_dwords << 2);
    }

    if (header_size >= (static_cast<unsigned int>(encoder->enc_td_segment_pitch) - 8))
    {
        XMCDPRINT(
            const_cast<char*>(
                "The file header is too large to fit within the first compressed data segment.  \n"
                "The compressed segment size must be increased or the uncompressed size of the file reduced.\n"));
        return 5;
    }

    const unsigned __int64 available_header_size = *pHeaderSize;
    *pHeaderSize = header_size;
    if (available_header_size < header_size)
    {
        return 6;
    }

    _XCOMPRESS_FILE_HEADER_LZXTDECODE* const header =
        static_cast<_XCOMPRESS_FILE_HEADER_LZXTDECODE*>(pHeaderData);
    header->Common.Identifier = 0xED12F50Fu;
    header->Common.Version = 1;
    header->Common.Reserved = 0;

    const unsigned int window_code =
        (Log2(encoder->enc_window_size) - Log2(0x8000u)) & 0xFu;
    const unsigned int segment_pitch_code =
        ((47u - Log2(static_cast<unsigned int>(encoder->enc_td_segment_pitch))) << 4) & 0x30u;
    const unsigned int preserved_header_bits = header->DWord & 0xFF000000u;
    const unsigned int packed_header =
        preserved_header_bits |
        window_code |
        segment_pitch_code |
        (segment_count << 6) |
        (translation_mode << 22);
    header->DWord = LEndianSwap8In32(packed_header);

    if (segment_count != 0)
    {
        unsigned int* const translation_list = encoder->enc_tdat_uncompressed_size_list;
        if (translation_mode == 0)
        {
            for (unsigned int group_base = 0; group_base < segment_count; group_base += 8)
            {
                unsigned int values[8]{};
                for (unsigned int i = 0; i < 8; ++i)
                {
                    const unsigned int index = group_base + i;
                    if (index < segment_count)
                    {
                        values[i] = translation_list[index];
                    }
                }

                const unsigned int packed_values[5] = {
                    (values[0] << 12) | (values[1] >> 8),
                    (values[1] << 24) | (values[2] << 4) | (values[3] >> 16),
                    (values[3] << 16) | (values[4] >> 4),
                    (values[4] << 28) | (values[5] << 8) | (values[6] >> 12),
                    (values[6] << 20) | values[7],
                };

                const unsigned int output_base = (group_base >> 3) * 5;
                for (unsigned int i = 0; i < 5; ++i)
                {
                    if ((output_base + i) >= access_translation_dwords)
                    {
                        break;
                    }

                    header->AccessTranslationList[output_base + i] = LEndianSwap8In32(packed_values[i]);
                }
            }
        }
        else
        {
            for (unsigned int i = 0; i < segment_count; ++i)
            {
                header->AccessTranslationList[i] = LEndianSwap8In32(translation_list[i]);
            }
        }
    }

    const unsigned __int64 uncompressed_size = LEndianSwap8In64(encoder->enc_td_uncompressed_size);
    const unsigned __int64 compressed_size = LEndianSwap8In64(encoder->enc_td_compressed_size);
    const bool hashFailed = !XMCDShaUpdate(&encoder->enc_td_sha_state, pHeaderData, 8) ||
        !XMCDShaUpdate(&encoder->enc_td_sha_state, static_cast<std::uint8_t*>(pHeaderData) + 0x0C, 4) ||
        !XMCDShaUpdate(&encoder->enc_td_sha_state, static_cast<std::uint8_t*>(pHeaderData) + 0x10, access_translation_dwords << 2) ||
        !XMCDShaUpdate(&encoder->enc_td_sha_state, &uncompressed_size, 8) ||
        !XMCDShaUpdate(&encoder->enc_td_sha_state, &compressed_size, 8);

    if ((XMCDShaFinal(&encoder->enc_td_sha_state, &header->Sha1Digest) != 0) && !hashFailed)
    {
        return 0;
    }

    return 4;
}

int CompressLzx(
    _LZXCOMPRESSION_CONTEXT_HEADER* pContext,
    void* pDestination,
    unsigned __int64* pDestSize,
    const void* pSource,
    unsigned __int64 SrcSize)
{
    t_encoder_context* const encoder = GetEncoderContext(pContext);
    if ((pContext->Common.Flags & 1) == 0)
    {
        ResetCompressionContextLzx(pContext);
    }

    StreamEncoderBlockInfo block_info{};
    block_info.pDestination = static_cast<std::uint8_t*>(pDestination);
    block_info.DestSize = *pDestSize;

    encoder->enc_output_callback_function = LzxEncoderCallback;
    encoder->enc_fci_data = &block_info;

    const unsigned __int8* source = static_cast<const unsigned __int8*>(pSource);
    unsigned __int64 source_remaining = SrcSize;
    unsigned int chunk_index = 0;
    unsigned int final_chunk_index = 0;
    int estimated_leftover_bytes = 0;

    while (source_remaining != 0)
    {
        unsigned int chunk_size = 0x8000;
        if (source_remaining < chunk_size)
        {
            chunk_size = static_cast<unsigned int>(source_remaining);
        }

        ++chunk_index;
        if (source_remaining <= 0x8000)
        {
            final_chunk_index = chunk_index;
        }

        block_info.ChunksFinal = final_chunk_index;
        LZX_Encode(
            encoder,
            const_cast<unsigned __int8*>(source),
            static_cast<int>(chunk_size),
            &estimated_leftover_bytes,
            0);

        source += chunk_size;
        source_remaining -= chunk_size;
    }

    LZX_EncodeFlush(encoder);
    *pDestSize = block_info.CompressedSize;
    return (block_info.CompressedSize > block_info.DestSize) ? 6 : 0;
}

int CompressStreamLzx(
    _LZXCOMPRESSION_CONTEXT_HEADER* pContext,
    void* pDestination,
    unsigned __int64* pDestSize,
    const void* pSource,
    unsigned __int64* pSrcSize)
{
    t_encoder_context* const encoder = GetEncoderContext(pContext);
    encoder->enc_output_callback_function = LzxEncoderCallback;

    StreamEncoderBlockInfo block_info{};
    encoder->enc_fci_data = &block_info;

    unsigned int source_available = static_cast<unsigned int>(*pSrcSize);
    std::uint64_t dest_capacity = *pDestSize;
    std::uint64_t dest_written = 0;
    unsigned int source_consumed = 0;
    const unsigned __int8* source_bytes = static_cast<const unsigned __int8*>(pSource);

    if (encoder->enc_dest_staging_size != 0)
    {
        unsigned int copy_size = encoder->enc_dest_staging_size;
        if (dest_capacity < copy_size)
        {
            copy_size = static_cast<unsigned int>(dest_capacity);
        }

        std::memcpy(
            pDestination,
            encoder->enc_dest_staging_buffer + encoder->enc_dest_staging_offset,
            copy_size);

        dest_written = copy_size;
        encoder->enc_dest_staging_size -= copy_size;
        encoder->enc_dest_staging_offset += copy_size;
    }

    unsigned int target_chunk_size;
    if (source_available != 0)
    {
        target_chunk_size = 0x8000;
        encoder->enc_stream_flushed = false;
    }
    else
    {
        target_chunk_size = encoder->enc_source_staging_size;
    }

    const unsigned __int8* encode_source = source_bytes;
    unsigned int available_to_encode;
    if (encoder->enc_source_staging_size != 0)
    {
        const unsigned int staged_bytes = encoder->enc_source_staging_size;
        unsigned int copy_size = target_chunk_size - staged_bytes;
        if (source_available < copy_size)
        {
            copy_size = source_available;
        }

        std::memcpy(
            encoder->enc_source_staging_buffer + staged_bytes,
            source_bytes,
            copy_size);

        encoder->enc_source_staging_size += copy_size;
        source_consumed = copy_size;
        encode_source = encoder->enc_source_staging_buffer;
        available_to_encode = staged_bytes + source_available;
    }
    else
    {
        available_to_encode = source_available;
    }

    while (encoder->enc_dest_staging_size == 0)
    {
        if (available_to_encode < target_chunk_size)
        {
            break;
        }

        if ((available_to_encode == target_chunk_size) &&
            ((source_available != 0) || encoder->enc_stream_flushed))
        {
            break;
        }

        const std::uint64_t remaining_dest = dest_capacity - dest_written;
        if (remaining_dest < encoder->enc_dest_staging_buffer_size)
        {
            block_info.pDestination = encoder->enc_dest_staging_buffer;
            block_info.DestSize = encoder->enc_dest_staging_buffer_size;
        }
        else
        {
            block_info.pDestination = static_cast<unsigned __int8*>(pDestination) + dest_written;
            block_info.DestSize = remaining_dest;
        }

        block_info.CompressedSize = 0;
        block_info.ChunksEncoded = encoder->enc_chunks_encoded;

        if (target_chunk_size != 0)
        {
            ++encoder->enc_chunks_submitted;

            int estimated_leftover_bytes = 0;
            block_info.ChunksFinal = 0;
            if (source_available == 0)
            {
                block_info.ChunksFinal = encoder->enc_chunks_submitted;
            }

            LZX_Encode(
                encoder,
                const_cast<unsigned __int8*>(encode_source),
                static_cast<int>(target_chunk_size),
                &estimated_leftover_bytes,
                0);
        }
        else
        {
            block_info.ChunksFinal = encoder->enc_chunks_submitted;
            LZX_EncodeFlush(encoder);
            encoder->enc_stream_flushed = true;
        }

        encoder->enc_chunks_encoded = block_info.ChunksEncoded;

        if (block_info.pDestination == encoder->enc_dest_staging_buffer)
        {
            unsigned int copy_size = static_cast<unsigned int>(block_info.CompressedSize);
            if (remaining_dest < copy_size)
            {
                copy_size = static_cast<unsigned int>(remaining_dest);
            }

            std::memcpy(
                static_cast<unsigned __int8*>(pDestination) + dest_written,
                block_info.pDestination,
                copy_size);

            encoder->enc_dest_staging_offset = copy_size;
            encoder->enc_dest_staging_size = static_cast<unsigned int>(block_info.CompressedSize) - copy_size;
            dest_written += copy_size;
        }
        else
        {
            dest_written += block_info.CompressedSize;
        }

        if (encode_source == encoder->enc_source_staging_buffer)
        {
            encode_source = source_bytes + source_consumed;
            encoder->enc_source_staging_size = 0;
        }
        else
        {
            encode_source += target_chunk_size;
            source_consumed += target_chunk_size;
        }

        if (source_available != 0)
        {
            available_to_encode = source_available - source_consumed;
        }
        else
        {
            target_chunk_size = 0;
            available_to_encode = 0;
        }
    }

    unsigned int bytes_to_stage = target_chunk_size - encoder->enc_source_staging_size;
    unsigned int source_remaining = source_available - source_consumed;
    if (source_remaining < bytes_to_stage)
    {
        bytes_to_stage = source_remaining;
    }

    std::memcpy(
        encoder->enc_source_staging_buffer + encoder->enc_source_staging_size,
        source_bytes + source_consumed,
        bytes_to_stage);

    encoder->enc_source_staging_size += bytes_to_stage;
    *pDestSize = dest_written;
    *pSrcSize = static_cast<unsigned __int64>(source_consumed + bytes_to_stage);
    return 0;
}

// Liar : change - portable SHA-1 state replaces the original Windows CryptoAPI flow.
namespace {
struct Sha1 {
    std::array<uint32_t, 5> state{0x67452301u, 0xEFCDAB89u, 0x98BADCFEu, 0x10325476u, 0xC3D2E1F0u};
    std::array<uint8_t, 64> block{};
    uint64_t bits = 0;
    size_t used = 0;

    void transform(const uint8_t* data) {
        std::array<uint32_t, 80> words{};
        for (size_t i = 0; i < 16; ++i)
            words[i] = (static_cast<uint32_t>(data[i * 4]) << 24) | (static_cast<uint32_t>(data[i * 4 + 1]) << 16) |
                (static_cast<uint32_t>(data[i * 4 + 2]) << 8) | data[i * 4 + 3];
        for (size_t i = 16; i < words.size(); ++i) words[i] = std::rotl(words[i - 3] ^ words[i - 8] ^ words[i - 14] ^ words[i - 16], 1);
        auto [a, b, c, d, e] = state;
        for (size_t i = 0; i < words.size(); ++i) {
            const auto [f, k] = i < 20 ? std::pair{(b & c) | (~b & d), 0x5A827999u} : i < 40 ? std::pair{b ^ c ^ d, 0x6ED9EBA1u} : i < 60 ? std::pair{(b & c) | (b & d) | (c & d), 0x8F1BBCDCu} : std::pair{b ^ c ^ d, 0xCA62C1D6u};
            const auto next = std::rotl(a, 5) + f + e + k + words[i];
            e = d; d = c; c = std::rotl(b, 30); b = a; a = next;
        }
        state[0] += a; state[1] += b; state[2] += c; state[3] += d; state[4] += e;
    }

    void update(const void* input, size_t size) {
        const auto* data = static_cast<const uint8_t*>(input);
        bits += static_cast<uint64_t>(size) * 8;
        while (size) {
            const auto count = std::min(size, block.size() - used);
            std::memcpy(block.data() + used, data, count);
            used += count; data += count; size -= count;
            if (used == block.size()) { transform(block.data()); used = 0; }
        }
    }

    [[nodiscard]] uint32_t finish() {
        block[used++] = 0x80;
        if (used > 56) { std::fill(block.begin() + used, block.end(), 0); transform(block.data()); used = 0; }
        std::fill(block.begin() + used, block.begin() + 56, 0);
        for (size_t i = 0; i < 8; ++i) block[63 - i] = static_cast<uint8_t>(bits >> (i * 8));
        transform(block.data());
        return std::byteswap(state[0]);
    }
};
}

int XMCDShaInit(sha_state* state) {
    state->prov = 0;
    state->hash = reinterpret_cast<unsigned __int64>(new (std::nothrow) Sha1);
    return state->hash != 0;
}

int XMCDShaUpdate(sha_state* state, const void* data, unsigned int size) {
    if (!state->hash) return 0;
    reinterpret_cast<Sha1*>(state->hash)->update(data, size);
    return 1;
}

int XMCDShaFinal(sha_state* state, unsigned int* hash) {
    if (!state->hash) return 0;
    auto* sha = reinterpret_cast<Sha1*>(state->hash);
    *hash = sha->finish();
    delete sha;
    state->hash = 0;
    return 1;
}

int LzxEncoderCallback(void* pParameter, unsigned __int8* pData, int CompressedSize, int UncompressedSize)
{
    StreamEncoderBlockInfo* const block = static_cast<StreamEncoderBlockInfo*>(pParameter);
    ++block->ChunksEncoded;

    unsigned int trailer_size = 0;
    if (block->ChunksEncoded != block->ChunksFinal)
    {
        if ((block->CompressedSize + 2) <= block->DestSize)
        {
            block->pDestination[0] = static_cast<std::uint8_t>(CompressedSize >> 8);
            block->pDestination[1] = static_cast<std::uint8_t>(CompressedSize);
            block->pDestination += 2;
        }

        block->CompressedSize += 2;
    }
    else
    {
        trailer_size = 5;
        if ((block->CompressedSize + trailer_size) <= block->DestSize)
        {
            block->pDestination[0] = 0xFF;
            block->pDestination[1] = static_cast<std::uint8_t>(UncompressedSize >> 8);
            block->pDestination[2] = static_cast<std::uint8_t>(UncompressedSize);
            block->pDestination[3] = static_cast<std::uint8_t>(CompressedSize >> 8);
            block->pDestination[4] = static_cast<std::uint8_t>(CompressedSize);
            block->pDestination += trailer_size;
        }

        block->CompressedSize += trailer_size;
    }

    std::uint64_t bytes_available = 0;
    if (block->DestSize >= block->CompressedSize)
    {
        bytes_available = block->DestSize - block->CompressedSize;
    }

    std::uint64_t bytes_to_copy = static_cast<unsigned int>(CompressedSize);
    if (bytes_to_copy > bytes_available)
    {
        bytes_to_copy = bytes_available;
    }

    std::memcpy(block->pDestination, pData, static_cast<std::size_t>(bytes_to_copy));
    block->pDestination += bytes_to_copy;
    block->CompressedSize += static_cast<unsigned int>(CompressedSize);

    if (trailer_size != 0)
    {
        bytes_available = 0;
        if (block->DestSize >= block->CompressedSize)
        {
            bytes_available = block->DestSize - block->CompressedSize;
        }

        std::uint64_t bytes_to_zero = trailer_size;
        if (bytes_to_zero > bytes_available)
        {
            bytes_to_zero = bytes_available;
        }

        std::memset(block->pDestination, 0, static_cast<std::size_t>(bytes_to_zero));
        block->pDestination += bytes_to_zero;
        block->CompressedSize += trailer_size;
    }

    return 0;
}

int LzxEncoderTDCallback(void* pParameter, unsigned __int8* pData, int CompressedSize, int UncompressedSize)
{
    (void)UncompressedSize;

    EncoderBlockInfo* const block = static_cast<EncoderBlockInfo*>(pParameter);
    if ((block->CompressedSize + 2) <= block->DestSize)
    {
        block->pDestination[0] = static_cast<std::uint8_t>(CompressedSize >> 8);
        block->pDestination[1] = static_cast<std::uint8_t>(CompressedSize);
        block->pDestination += 2;
    }

    block->CompressedSize += 2;

    std::uint64_t bytes_available = 0;
    if (block->DestSize >= block->CompressedSize)
    {
        bytes_available = block->DestSize - block->CompressedSize;
    }

    std::uint64_t bytes_to_copy = static_cast<unsigned int>(CompressedSize);
    if (bytes_to_copy > bytes_available)
    {
        bytes_to_copy = bytes_available;
    }

    std::memcpy(block->pDestination, pData, static_cast<std::size_t>(bytes_to_copy));
    block->CompressedSize += static_cast<unsigned int>(CompressedSize);
    block->pDestination += bytes_to_copy;
    return 0;
}
}
