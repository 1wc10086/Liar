#include "../../../api/precomp.hpp"

#include <cstdint>
#include <cstring>

namespace XCOMPRESS
{
void LZX_EncodeFree(t_encoder_context* context)
{
    comp_free_compress_memory(context);
}

void LZX_EncodeNewGroup(t_encoder_context* context)
{
    init_compression_memory(context);
}

long LZX_Encode(
    t_encoder_context* context,
    unsigned __int8* input_data,
    int input_size,
    int* estimated_bytes_compressed,
    int file_size_for_translation)
{
    context->enc_input_ptr = input_data;
    context->enc_input_left = input_size;
    context->enc_file_size_for_translation = static_cast<unsigned int>(file_size_for_translation);

    encoder_start(context);

    if (context->enc_output_overflow)
    {
        *estimated_bytes_compressed = 0;
        return 2;
    }

    *estimated_bytes_compressed = static_cast<int>(estimate_buffer_contents(context));
    return 0;
}

bool LZX_EncodeFlush(t_encoder_context* context)
{
    flush_all_pending_blocks(context);
    return !context->enc_output_overflow;
}

void LZX_EncodeResetState(t_encoder_context* context)
{
    ::memset(context->enc_ItemType, 0, 0x2000u);

    const unsigned int num_position_slots = context->enc_num_position_slots;

    context->enc_literals = 0;
    context->enc_distances = 0;
    context->enc_input_running_total = 0;

    context->enc_last_matchpos_offset[0] = 1;
    context->enc_last_matchpos_offset[1] = 1;
    context->enc_last_matchpos_offset[2] = 1;

    context->enc_repeated_offset_at_literal_zero[0] = 1;
    context->enc_repeated_offset_at_literal_zero[1] = 1;
    context->enc_repeated_offset_at_literal_zero[2] = 1;

    ::memset(context->enc_main_tree_prev_len, 0, 8 * num_position_slots + 0x100);
    ::memset(context->enc_secondary_tree_prev_len, 0, 0xF9u);

    context->enc_bitbuf = 0;
    context->enc_bitcount = 32;
    context->enc_output_overflow = false;
    context->enc_bufpos_last_output_block = context->enc_BufPos;
    context->enc_next_tree_create = 0x1000;
    context->enc_first_block = 0;
    context->enc_need_to_recalc_stats = true;

    reset_translation(context);

    context->enc_num_cfdata_frames = 0;
    context->enc_first_time_this_group = true;
    context->enc_num_block_splits = 0;
}

unsigned __int8* LZX_GetInputData(
    t_encoder_context* context,
    unsigned int* input_position,
    unsigned int* bytes_available)
{
    unsigned int position = context->enc_BufPos - context->enc_window_size;
    unsigned int window_offset = context->enc_window_size;

    if (position >= context->enc_window_size)
    {
        position -= context->enc_window_size;
        *input_position = position;
        *bytes_available = context->enc_window_size;
        window_offset = context->enc_BufPos - context->enc_window_size;
    }
    else
    {
        *input_position = 0;
        *bytes_available = position;
    }

    return context->enc_MemWindow + window_offset;
}

void LZX_EncodeInsertDictionary(
    t_encoder_context* context,
    const unsigned __int8* input_data,
    unsigned int input_size)
{
    context->enc_file_size_for_translation = 0;
    context->enc_input_ptr = const_cast<unsigned __int8*>(input_data);
    context->enc_input_left = static_cast<int>(input_size);
    context->enc_first_time_this_group = false;

    unsigned long input_position = static_cast<unsigned long>(reinterpret_cast<std::uintptr_t>(context->enc_MemWindow));
    input_position -= static_cast<unsigned long>(reinterpret_cast<std::uintptr_t>(context->enc_RealMemWindow));
    input_position += context->enc_BufPos;

    const unsigned int bytes_read =
        static_cast<unsigned int>(comp_read_input(context, input_position, static_cast<long>(input_size)));

    unsigned int bufpos = context->enc_BufPos;
    context->enc_inserted_dict_size += bytes_read;

    const unsigned int end_bufpos = bufpos + bytes_read;
    while (bufpos < end_bufpos)
    {
        quick_insert_bsearch_findmatch(context, bufpos, bufpos - context->enc_window_size + 4);
        ++bufpos;
    }

    context->enc_earliest_window_data_remaining = bufpos - context->enc_window_size;

    const unsigned long path = context->enc_earliest_window_data_remaining + 0x36;
    for (unsigned int i = 1; i <= 0x32; ++i)
    {
        binary_search_remove_node(context, bufpos - i, path);
    }

    context->enc_BufPos = bufpos;
    context->enc_bufpos_at_last_block = bufpos;
}

bool LZX_EncodeInit(
    t_encoder_context* context,
    int compression_window_size,
    int second_partition_size,
    void* (__fastcall *pfnma)(unsigned int),
    void (__fastcall *pfnmf)(void*),
    int (__fastcall *pfnlzx_output_callback)(void*, unsigned __int8*, int, int),
    void* fci_data)
{
    context->enc_window_size = static_cast<unsigned int>(compression_window_size);
    context->enc_fci_data = fci_data;

    if ((second_partition_size & 0x7FFF) != 0)
    {
        second_partition_size &= static_cast<int>(0xFFFF8000);
    }

    if (second_partition_size < 0x8000)
    {
        second_partition_size = 0x8000;
    }

    if (compression_window_size < 0x8000)
    {
        return false;
    }

    context->enc_encoder_second_partition_size = static_cast<unsigned int>(second_partition_size);
    context->enc_malloc = pfnma;
    context->enc_output_callback_function = pfnlzx_output_callback;
    context->enc_free = pfnmf;

    if (!comp_alloc_compress_memory(context))
    {
        return false;
    }

    init_compression_memory(context);
    return true;
}
}
