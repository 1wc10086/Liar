#include "../../../api/precomp.hpp"

#include <cstring>

namespace XCOMPRESS
{
unsigned int get_dec_mem_window_alloc_size(unsigned int dec_window_size)
{
    return dec_window_size + 0x105;
}

bool allocate_decompression_memory(t_decoder_context* context)
{
    unsigned int total = 4;
    context->dec_num_position_slots = 4;

    do
    {
        const unsigned char slot = context->dec_num_position_slots;
        const unsigned char extra_bits = context->dec_extra_bits_table[slot];
        context->dec_num_position_slots = static_cast<unsigned char>(slot + 1);
        total += 1u << extra_bits;
    }
    while (total < context->dec_window_size);

    context->dec_mem_window = static_cast<unsigned __int8*>(
        dec_malloc(context, get_dec_mem_window_alloc_size(context->dec_window_size)));
    return context->dec_mem_window != nullptr;
}

void free_decompression_memory(t_decoder_context* context)
{
    if (context->dec_mem_window != nullptr)
    {
        dec_free(context, context->dec_mem_window);
        context->dec_mem_window = nullptr;
    }
}

void init_decompression_memory_context(
    void* pContextData,
    unsigned __int64* pContextSize,
    int WindowSize,
    unsigned int HeaderSize,
    unsigned long Flags)
{
    const bool use_streaming_buffers = (Flags & 1) != 0;
    const bool use_td_staging = (Flags & 0x80000000u) != 0;

    unsigned int context_size = get_dec_mem_window_alloc_size(static_cast<unsigned int>(WindowSize));
    context_size += HeaderSize + 0x3040;
    if (use_streaming_buffers)
    {
        context_size += 0x980A;
    }
    else if (use_td_staging)
    {
        context_size += 0x8000;
    }
    context_size += 0x8000;

    *pContextSize = context_size;
    if (pContextData == nullptr)
    {
        return;
    }

    t_decoder_context* const context =
        reinterpret_cast<t_decoder_context*>(static_cast<unsigned __int8*>(pContextData) + HeaderSize);

    build_global_tables(context);

    context->dec_malloc = nullptr;
    context->dec_free = nullptr;
    context->dec_memory = nullptr;
    context->dec_window_size = static_cast<unsigned int>(WindowSize);
    context->dec_num_position_slots = 4;
    context->dec_window_mask = static_cast<unsigned int>(WindowSize - 1);

    unsigned int total = 4;
    do
    {
        const unsigned char slot = context->dec_num_position_slots;
        const unsigned char extra_bits = context->dec_extra_bits_table[slot];
        context->dec_num_position_slots = static_cast<unsigned char>(slot + 1);
        total += 1u << extra_bits;
    }
    while (total < context->dec_window_size);

    context->dec_td_last_source = nullptr;
    context->dec_td_last_source_size = 0;
    context->dec_td_last_segment_size = 0;
    context->dec_td_last_segment_offset = 0;
    context->dec_td_last_source_offset = 0;
    context->dec_td_last_decoded_size = 0;
    context->dec_td_last_stage_offset = 0;
    context->dec_td_last_stage_size = 0;

    unsigned __int8* cursor = reinterpret_cast<unsigned __int8*>(context) + 0x3040;
    context->dec_mem_window = cursor;
    cursor += get_dec_mem_window_alloc_size(static_cast<unsigned int>(WindowSize));

    context->dec_dest_staging_buffer = cursor;
    cursor += 0x8000;

    if (use_streaming_buffers)
    {
        context->dec_source_staging_buffer = cursor;
    }
    else if (use_td_staging)
    {
        context->dec_td_staging_buffer = cursor;
    }
}

void reset_decoder_trees(t_decoder_context* context)
{
    const std::size_t main_tree_size = 8 * context->dec_num_position_slots + 0x100;
    ::memset(context->dec_main_tree_len, 0, main_tree_size);
    ::memset(context->dec_main_tree_prev_len, 0, main_tree_size);
    ::memset(context->dec_secondary_length_tree_len, 0, 0xF9u);
    ::memset(context->dec_secondary_length_tree_prev_len, 0, 0xF9u);
}

void decoder_misc_init(t_decoder_context* context)
{
    context->dec_last_matchpos_offset[0] = 1;
    context->dec_last_matchpos_offset[1] = 1;
    context->dec_last_matchpos_offset[2] = 1;
    context->dec_position_at_start = 0;
    context->dec_decoder_state = DEC_STATE_START_NEW_BLOCK;
    context->dec_block_size = 0;
    context->dec_block_type = BLOCKTYPE_INVALID;
    context->dec_bufpos = 0;
    context->dec_current_file_size = 0;
    context->dec_first_time_this_group = true;
    context->dec_error_condition = false;
}
}
