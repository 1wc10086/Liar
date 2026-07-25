#pragma once

#include "xcompress.h"

#include <cstddef>
#include <cstdint>

struct _XCOMPRESS_FILE_HEADER
{
    std::uint32_t Identifier;
    std::uint16_t Version;
    std::uint16_t Reserved;
};

struct _XCOMPRESS_FILE_HEADER_LZXTDECODE
{
    _XCOMPRESS_FILE_HEADER Common;
    std::uint32_t Sha1Digest;
    union
    {
        std::uint32_t DWord;
        struct
        {
            std::uint32_t WindowSize : 4;
            std::uint32_t SegmentPitch : 2;
            std::uint32_t SegmentCount : 16;
            std::uint32_t TranslationSize : 2;
        };
    };
    std::uint32_t AccessTranslationList[0];
};

struct _XCOMPRESS_FILE_HEADER_LZXNATIVE
{
    _XCOMPRESS_FILE_HEADER Common;
    std::uint32_t ContextFlags;
    _XMEMCODEC_PARAMETERS_LZX CodecParams;
    std::uint32_t UncompressedSizeHigh;
    std::uint32_t UncompressedSizeLow;
    std::uint32_t CompressedSizeHigh;
    std::uint32_t CompressedSizeLow;
    std::uint32_t UncompressedBlockSize;
    std::uint32_t CompressedBlockSizeMax;
};

namespace XCOMPRESS
{
constexpr std::uint32_t XMCD_CONTEXT_FLAG_OWNS_HEAP = 0x40000000u;

struct _XMEMCODEC_CONTEXT_HEADER
{
    std::uint32_t Identifier;
    XMEMCODEC_TYPE CodecType;
    std::uint32_t Flags;
};

using XMEMCODEC_CONTEXT_HEADER = _XMEMCODEC_CONTEXT_HEADER;

struct _LZXCOMPRESSION_CONTEXT_HEADER
{
    XMEMCODEC_CONTEXT_HEADER Common;
    std::uint32_t LzxFlags;
    std::uint8_t LzxData[0];
};

struct _LZXDECOMPRESSION_CONTEXT_HEADER
{
    XMEMCODEC_CONTEXT_HEADER Common;
    std::uint32_t LzxFlags;
    unsigned __int64 WindowSize;
    std::uint8_t LzxData[0];
};

enum lzx_block_type : int
{
    BLOCKTYPE_INVALID = 0,
    BLOCKTYPE_VERBATIM = 1,
    BLOCKTYPE_ALIGNED = 2,
    BLOCKTYPE_UNCOMPRESSED = 3,
};

enum decoder_state : int
{
    DEC_STATE_UNKNOWN = 0,
    DEC_STATE_START_NEW_BLOCK = 1,
    DEC_STATE_DECODING_DATA = 2,
};

struct decision_node
{
    unsigned int link;
    unsigned int path;
    unsigned int repeated_offset[3];
    unsigned int numbits;
};

struct sha_state
{
    unsigned __int64 prov;
    unsigned __int64 hash;
};

struct t_decoder_context
{
    std::uint8_t* dec_mem_window;
    unsigned int dec_window_size;
    unsigned int dec_window_mask;
    unsigned int dec_last_matchpos_offset[3];
    std::int16_t dec_main_tree_table[1024];
    std::int16_t dec_secondary_length_tree_table[256];
    std::uint8_t dec_main_tree_len[672];
    std::uint8_t dec_secondary_length_tree_len[249];
    std::uint8_t pad1[3];
    char dec_aligned_table[128];
    std::uint8_t dec_aligned_len[8];
    std::int16_t dec_main_tree_left_right[2688];
    std::int16_t dec_secondary_length_tree_left_right[996];
    std::uint8_t* dec_input_curpos;
    std::uint8_t* dec_end_input_pos;
    std::uint8_t* dec_output_buffer;
    int dec_position_at_start;
    std::uint8_t dec_main_tree_prev_len[672];
    std::uint8_t dec_secondary_length_tree_prev_len[249];
    unsigned int dec_bitbuf;
    char dec_bitcount;
    std::uint8_t dec_num_position_slots;
    bool dec_first_time_this_group;
    bool dec_error_condition;
    int dec_bufpos;
    unsigned int dec_current_file_size;
    unsigned int dec_instr_pos;
    unsigned int dec_num_cfdata_frames;
    int dec_original_block_size;
    int dec_block_size;
    lzx_block_type dec_block_type;
    decoder_state dec_decoder_state;
    void* (__fastcall *dec_malloc)(unsigned int);
    void (__fastcall *dec_free)(void*);
    void* dec_memory;
    std::uint8_t dec_extra_bits_table[52];
    int MP_POS_minus2_table[51];
    bool dec_noncached_dest;
    std::uint8_t pad2[7];
    std::uint8_t* dec_dest_staging_buffer;
    union
    {
        struct
        {
            unsigned int dec_dest_staging_offset;
            unsigned int dec_dest_staging_size;
            std::uint8_t* dec_source_staging_buffer;
            unsigned int dec_source_staging_size;
            bool dec_end_of_stream;
            std::uint8_t pad3[3];
        };
        struct
        {
            const std::uint8_t* dec_td_last_source;
            unsigned int dec_td_last_source_size;
            unsigned int dec_td_last_segment_size;
            unsigned int dec_td_last_segment_offset;
            unsigned int dec_td_last_source_offset;
            unsigned int dec_td_last_decoded_size;
            unsigned int dec_td_last_stage_offset;
            unsigned int dec_td_last_stage_size;
            std::uint8_t* dec_td_staging_buffer;
        };
    };
};

struct t_encoder_context
{
    std::uint8_t* enc_MemWindow;
    unsigned int enc_window_size;
    std::uint8_t pad3[4];
    unsigned int* enc_tree_root;
    unsigned int* enc_Left;
    unsigned int* enc_Right;
    unsigned int enc_bitbuf;
    char enc_bitcount;
    bool enc_output_overflow;
    char pad1[2];
    unsigned int enc_literals;
    unsigned int enc_distances;
    unsigned int* enc_DistData;
    std::uint8_t* enc_LitData;
    std::uint8_t* enc_ItemType;
    unsigned int enc_repeated_offset_at_literal_zero[3];
    unsigned int enc_last_matchpos_offset[3];
    unsigned int enc_matchpos_table[258];
    unsigned int enc_BufPos;
    std::uint8_t enc_slot_table[1024];
    std::uint8_t* enc_output_buffer_start;
    std::uint8_t* enc_output_buffer_curpos;
    std::uint8_t* enc_output_buffer_end;
    unsigned int enc_input_running_total;
    unsigned int enc_bufpos_last_output_block;
    unsigned int enc_num_position_slots;
    unsigned int enc_file_size_for_translation;
    bool enc_allocated_compression_memory;
    std::uint8_t enc_num_block_splits;
    std::uint8_t enc_ones[256];
    std::uint8_t enc_first_block;
    bool enc_need_to_recalc_stats;
    bool enc_first_time_this_group;
    std::uint8_t pad4[3];
    unsigned int enc_encoder_second_partition_size;
    unsigned int enc_earliest_window_data_remaining;
    unsigned int enc_bufpos_at_last_block;
    std::uint8_t pad5[4];
    std::uint8_t* enc_input_ptr;
    int enc_input_left;
    unsigned int enc_instr_pos;
    unsigned short* enc_tree_freq;
    unsigned short* enc_tree_sortptr;
    std::uint8_t* enc_len;
    std::int16_t enc_tree_heap[702];
    unsigned short enc_tree_leftright[2798];
    unsigned short enc_tree_len_cnt[17];
    int enc_tree_n;
    std::int16_t enc_tree_heapsize;
    char enc_depth;
    std::uint8_t pad6;
    unsigned int enc_next_tree_create;
    unsigned int enc_last_literals;
    unsigned int enc_last_distances;
    decision_node* enc_decision_node;
    std::uint8_t enc_main_tree_len[701];
    std::uint8_t enc_secondary_tree_len[250];
    std::uint8_t pad8[1];
    unsigned short enc_main_tree_freq[1400];
    unsigned short enc_main_tree_code[700];
    std::uint8_t enc_main_tree_prev_len[701];
    std::uint8_t pad9[1];
    unsigned short enc_secondary_tree_freq[498];
    unsigned short enc_secondary_tree_code[249];
    std::uint8_t enc_secondary_tree_prev_len[250];
    unsigned short enc_aligned_tree_freq[16];
    unsigned short enc_aligned_tree_code[8];
    std::uint8_t enc_aligned_tree_len[8];
    std::uint8_t enc_aligned_tree_prev_len[8];
    std::uint8_t pad10[2];
    std::uint8_t* enc_RealMemWindow;
    unsigned int* enc_RealLeft;
    unsigned int* enc_RealRight;
    unsigned int enc_num_cfdata_frames;
    std::uint8_t pad11[4];
    void* enc_fci_data;
    void* (__fastcall *enc_malloc)(unsigned int);
    void (__fastcall *enc_free)(void*);
    unsigned int enc_inserted_dict_size;
    std::uint8_t pad12[4];
    int (__fastcall *enc_output_callback_function)(void*, unsigned __int8*, int, int);
    union
    {
        struct
        {
            std::uint8_t* enc_dest_staging_buffer;
            unsigned int enc_dest_staging_buffer_size;
            unsigned int enc_dest_staging_offset;
            unsigned int enc_dest_staging_size;
            std::uint8_t pad13[4];
            std::uint8_t* enc_source_staging_buffer;
            unsigned int enc_source_staging_size;
            unsigned int enc_chunks_submitted;
            unsigned int enc_chunks_encoded;
            bool enc_stream_flushed;
            std::uint8_t pad14[3];
        };
        struct
        {
            unsigned __int64 enc_td_segment_pitch;
            unsigned __int64 enc_td_uncompressed_size;
            unsigned __int64 enc_td_compressed_size;
            unsigned int* enc_tdat_uncompressed_size_list;
            unsigned int enc_td_segment_count;
            unsigned int enc_td_segment_count_expected;
            unsigned int enc_td_translation_padding;
            unsigned int enc_td_translation_bits;
            unsigned int enc_td_translation_bits_expected;
            bool enc_td_encode_data_uncompressed;
            std::uint8_t pad15[3];
        };
    };
    sha_state enc_td_sha_state;
    unsigned int enc_context_data_size;
    std::uint8_t pad16[4];
    std::uint8_t* enc_context_data_snapshot;
};

static_assert(sizeof(t_decoder_context) == 12352, "t_decoder_context size");
static_assert(sizeof(t_encoder_context) == 17416, "t_encoder_context size");
static_assert(offsetof(t_decoder_context, dec_window_size) == 0x8, "dec_window_size offset");
static_assert(offsetof(t_decoder_context, dec_num_cfdata_frames) == 0x2ED4, "dec_num_cfdata_frames offset");
static_assert(offsetof(t_decoder_context, dec_malloc) == 0x2EE8, "dec_malloc offset");
static_assert(offsetof(t_decoder_context, dec_extra_bits_table) == 0x2F00, "dec_extra_bits_table offset");
static_assert(offsetof(t_decoder_context, dec_dest_staging_buffer) == 0x3008, "dec_dest_staging_buffer offset");
static_assert(offsetof(t_encoder_context, enc_output_overflow) == 0x2D, "enc_output_overflow offset");
static_assert(offsetof(t_encoder_context, enc_BufPos) == 0x470, "enc_BufPos offset");
static_assert(offsetof(t_encoder_context, enc_output_buffer_start) == 0x878, "enc_output_buffer_start offset");
static_assert(offsetof(t_encoder_context, enc_first_block) == 0x9A2, "enc_first_block offset");
static_assert(offsetof(t_encoder_context, enc_secondary_tree_len) == 0x2835, "enc_secondary_tree_len offset");
static_assert(offsetof(t_encoder_context, enc_secondary_tree_prev_len) == 0x422C, "enc_secondary_tree_prev_len offset");
static_assert(offsetof(t_encoder_context, enc_fci_data) == 0x4388, "enc_fci_data offset");
static_assert(offsetof(t_encoder_context, enc_td_sha_state) == 0x43E8, "enc_td_sha_state offset");

void LZX_DecodeFree(t_decoder_context* context);
void LZX_DecodeNewGroup(t_decoder_context* context);
int LZX_Decode(
    t_decoder_context* context,
    long bytes_to_decode,
    unsigned __int8* compressed_input_buffer,
    long compressed_input_size,
    unsigned __int8* uncompressed_output_buffer,
    long uncompressed_output_size,
    long* bytes_decoded);
bool LZX_DecodeInsertDictionary(
    t_decoder_context* context,
    const unsigned __int8* data,
    unsigned long data_size);
void build_global_tables(t_decoder_context* context);
void* dec_malloc(t_decoder_context* context, unsigned long cb);
void dec_free(t_decoder_context* context, void* pv);
bool LZX_DecodeInit(t_decoder_context* context, long compression_window_size);
void initialise_decoder_bitbuf(t_decoder_context* context);
void fillbuf(t_decoder_context* context, int n);
unsigned long getbits(t_decoder_context* context, int n);
unsigned int get_dec_mem_window_alloc_size(unsigned int dec_window_size);
void init_decompression_memory_context(
    void* pContextData,
    unsigned __int64* pContextSize,
    int WindowSize,
    unsigned int HeaderSize,
    unsigned long Flags);
int decode_block(t_decoder_context* context, lzx_block_type block_type, int bufpos, int amount_to_decode);

void LZX_EncodeFree(t_encoder_context* context);
void LZX_EncodeNewGroup(t_encoder_context* context);
long LZX_Encode(
    t_encoder_context* context,
    unsigned __int8* input_data,
    int input_size,
    int* estimated_bytes_compressed,
    int file_size_for_translation);
bool LZX_EncodeFlush(t_encoder_context* context);
void LZX_EncodeResetState(t_encoder_context* context);
unsigned __int8* LZX_GetInputData(
    t_encoder_context* context,
    unsigned int* input_position,
    unsigned int* bytes_available);
void LZX_EncodeInsertDictionary(
    t_encoder_context* context,
    const unsigned __int8* input_data,
    unsigned int input_size);
bool LZX_EncodeInit(
    t_encoder_context* context,
    int compression_window_size,
    int second_partition_size,
    void* (__fastcall *pfnma)(unsigned int),
    void (__fastcall *pfnmf)(void*),
    int (__fastcall *pfnlzx_output_callback)(void*, unsigned __int8*, int, int),
    void* fci_data);
void output_bits(t_encoder_context* context, int n, unsigned int x);
bool init_compressed_output_buffer(t_encoder_context* context);
void free_compressed_output_buffer(t_encoder_context* context);
int read_input_data(t_encoder_context* context, unsigned __int8* mem, int amount);
void encoder_translate_e8(t_encoder_context* context, unsigned __int8* mem, int bytes);
void flush_output_bit_buffer(t_encoder_context* context);
void comp_init_compress_memory_context(
    void* pContextData,
    unsigned __int64* pContextSize,
    int WindowSize,
    int SecondPartitionSize,
    unsigned int HeaderSize,
    unsigned int Flags);
void comp_clear_compress_memory_context(t_encoder_context* context);

std::size_t XMemGetCompressionContextSizeLzx(const _XMEMCODEC_PARAMETERS_LZX* pCodecParams, std::uint32_t Flags);
HRESULT XMemResetCompressionContextLzx(void* Context);
HRESULT XMemBeginCompressionTDLzx(void* Context, std::size_t SegmentPitch);
HRESULT XMemEndCompressionTDLzx(void* Context, void* pHeaderData, std::size_t* pHeaderSize, float Threshold);
HRESULT XMemCompressSegmentTDLzx(
    void* Context,
    void* pDestination,
    std::size_t* pDestSize,
    const void* pSource,
    std::size_t* pSrcSize,
    float Threshold);
HRESULT XMemCompressLzx(
    void* Context,
    void* pDestination,
    std::size_t* pDestSize,
    const void* pSource,
    std::size_t SrcSize);
HRESULT XMemCompressStreamLzx(
    void* Context,
    void* pDestination,
    std::size_t* pDestSize,
    const void* pSource,
    std::size_t* pSrcSize);
void* XMemInitializeCompressionContextLzx(
    const _XMEMCODEC_PARAMETERS_LZX* pCodecParams,
    std::uint32_t Flags,
    void* pContextData,
    std::size_t ContextSize);
void XMemDestroyCompressionContextLzx(void* const Context);

std::size_t XMemGetDecompressionContextSizeLzx(const _XMEMCODEC_PARAMETERS_LZX* pCodecParams, unsigned long Flags);
HRESULT XMemResetDecompressionContextLzx(void* Context);
HRESULT XMemDecompressLzx(
    void* Context,
    void* pDestination,
    std::size_t* pDestSize,
    const void* pSource,
    std::size_t SrcSize);
HRESULT XMemDecompressStreamLzx(
    void* Context,
    void* pDestination,
    std::size_t* pDestSize,
    const void* pSource,
    std::size_t* pSrcSize);
HRESULT XMemDecompressSegmentTDLzx(
    void* Context,
    void* pDestination,
    std::size_t* pDestSize,
    const void* pSource,
    std::size_t SrcSize,
    std::size_t SegmentSize,
    std::size_t SegmentOffset);
void* XMemInitializeDecompressionContextLzx(
    const _XMEMCODEC_PARAMETERS_LZX* pCodecParams,
    unsigned long Flags,
    void* pContextData,
    std::size_t ContextSize);
void XMemDestroyDecompressionContextLzx(void* const Context);

void free_decompression_memory(t_decoder_context* context);
void reset_decoder_trees(t_decoder_context* context);
void decoder_misc_init(t_decoder_context* context);
void init_decoder_translation(t_decoder_context* context);
long decode_data(t_decoder_context* context, long bytes_to_decode);
void init_decoder_input(t_decoder_context* context);
bool allocate_decompression_memory(t_decoder_context* context);
int decode_uncompressed_block(t_decoder_context* context, long bufpos, int amount_to_decode);
int decode_verbatim_block(t_decoder_context* context, long bufpos, int amount_to_decode);
int decode_aligned_offset_block(t_decoder_context* context, long bufpos, int amount_to_decode);
void copy_data_to_output(t_decoder_context* context, long bytes, const unsigned __int8* source);
bool read_main_and_secondary_trees(t_decoder_context* context);
bool handle_beginning_of_uncompressed_block(t_decoder_context* context);
bool read_aligned_offset_tree(t_decoder_context* context);
bool ReadRepTree(t_decoder_context* context, int num_elements, unsigned __int8* lastlen, unsigned __int8* len);
bool make_table(
    t_decoder_context* context,
    int num_elements,
    const unsigned __int8* len,
    unsigned __int8 nbits,
    short* table,
    short* left_right);
bool make_table_8bit(t_decoder_context* context, unsigned __int8* len, unsigned __int8* table);
void decoder_translate_e8(t_decoder_context* context, unsigned __int8* mem, int bytes);
long special_decode_aligned_block(t_decoder_context* context, long bufpos, int amount_to_decode);
long fast_decode_aligned_offset_block(t_decoder_context* context, long bufpos, int amount_to_decode);
long special_decode_verbatim_block(t_decoder_context* context, long bufpos, int amount_to_decode);
long fast_decode_verbatim_block(t_decoder_context* context, long bufpos, int amount_to_decode);

void comp_free_compress_memory(t_encoder_context* context);
long estimate_buffer_contents(t_encoder_context* context);
unsigned int get_distances_from_literals(t_encoder_context* context, unsigned int literals);
void do_block_output(t_encoder_context* context, long literal_to_end_at, long distance_to_end_at);
void output_block(t_encoder_context* context);
void encoder_start(t_encoder_context* context);
void flush_all_pending_blocks(t_encoder_context* context);
void reset_translation(t_encoder_context* context);
void init_compression_memory(t_encoder_context* context);
void prevent_far_matches(t_encoder_context* context);
void create_ones_table(t_encoder_context* context);
void create_slot_lookup_table(t_encoder_context* context);
void make_tree(
    t_encoder_context* context,
    int n,
    unsigned short* freq,
    unsigned __int8* len,
    unsigned short* code,
    bool generate_codes);
void create_trees(t_encoder_context* context, bool force_rebuild);
void encode_trees(t_encoder_context* context);
void encode_aligned_tree(t_encoder_context* context);
void get_final_repeated_offset_states(t_encoder_context* context, unsigned int distances);
extern const unsigned __int8 enc_extra_bits[52];
extern const unsigned int enc_slot_mask[52];
unsigned int estimate_compressed_block_size(t_encoder_context* context);
void fix_tree_cost_estimates(t_encoder_context* context);
void perform_flush_output_callback(t_encoder_context* context);
void encode_uncompressed_block(t_encoder_context* context, unsigned int bufpos, unsigned int block_size);
void encode_verbatim_block(t_encoder_context* context, unsigned int literal_to_end_at);
void encode_aligned_block(t_encoder_context* context, unsigned int literal_to_end_at);
void tally_aligned_bits(t_encoder_context* context, unsigned int dist_to_end_at);
lzx_block_type get_aligned_stats(t_encoder_context* context, unsigned int dist_to_end_at);
unsigned int tally_frequency(
    t_encoder_context* context,
    unsigned int literal_to_start_at,
    unsigned int distance_to_start_at,
    unsigned int literal_to_end_at);
unsigned int update_cumulative_block_stats(
    t_encoder_context* context,
    unsigned int literal_to_start_at,
    unsigned int distance_to_start_at,
    unsigned int literal_to_end_at);
unsigned int get_block_stats(
    t_encoder_context* context,
    unsigned int literal_to_start_at,
    unsigned int distance_to_start_at,
    unsigned int literal_to_end_at);
unsigned int return_difference(
    t_encoder_context* context,
    unsigned int item_start1,
    unsigned int item_start2,
    unsigned int dist_at_1,
    unsigned int dist_at_2,
    unsigned int size);
bool split_block(
    t_encoder_context* context,
    unsigned int literal_to_start_at,
    unsigned int literal_to_end_at,
    unsigned int distance_to_end_at,
    unsigned int* split_at_literal,
    unsigned int* split_at_distance);
void binary_search_remove_node(t_encoder_context* context, unsigned long position, unsigned long path);
void quick_insert_bsearch_findmatch(t_encoder_context* context, unsigned long position, unsigned long path);
long comp_read_input(t_encoder_context* context, unsigned long input_position, long input_size);
bool comp_alloc_compress_memory(t_encoder_context* context);
}
