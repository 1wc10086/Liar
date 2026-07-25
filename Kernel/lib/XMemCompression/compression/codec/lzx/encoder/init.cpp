#include "../../../api/precomp.hpp"

#include <cstring>

namespace
{
constexpr unsigned __int8 kEncExtraBits[52] =
{
    0, 0, 0, 0,
    1, 1, 2, 2,
    3, 3, 4, 4,
    5, 5, 6, 6,
    7, 7, 8, 8,
    9, 9, 10, 10,
    11, 11, 12, 12,
    13, 13, 14, 14,
    15, 15, 16, 16,
    17, 17, 17, 17,
    17, 17, 17, 17,
    17, 17, 17, 17,
    17, 17, 17, 17
};

unsigned int DetermineNumPositionSlots(unsigned int window_size)
{
    unsigned int total = 4;
    unsigned int slot = 4;

    do
    {
        const unsigned int extra_bits = kEncExtraBits[slot];
        ++slot;
        total += 1u << extra_bits;
    }
    while (total < window_size);

    return slot;
}
}

namespace XCOMPRESS
{
void init_compression_memory(t_encoder_context* context)
{
    ::memset(context->enc_tree_root, 0, 0x40000u);

    const unsigned int window_size = context->enc_window_size;
    const unsigned int num_position_slots = context->enc_num_position_slots;
    context->enc_bitbuf = 0;
    context->enc_MemWindow = context->enc_RealMemWindow - window_size;
    context->enc_BufPos = window_size;
    context->enc_bufpos_last_output_block = window_size;
    context->enc_last_matchpos_offset[0] = 1;
    context->enc_Left = context->enc_RealLeft - window_size;
    context->enc_last_matchpos_offset[1] = 1;
    context->enc_Right = context->enc_RealRight - window_size;
    context->enc_last_matchpos_offset[2] = 1;
    context->enc_repeated_offset_at_literal_zero[0] = 1;
    context->enc_repeated_offset_at_literal_zero[1] = 1;
    context->enc_repeated_offset_at_literal_zero[2] = 1;
    context->enc_first_block = 1;
    context->enc_need_to_recalc_stats = true;
    context->enc_bitcount = 32;

    ::memset(context->enc_main_tree_prev_len, 0, 8 * num_position_slots + 0x100);
    ::memset(context->enc_secondary_tree_prev_len, 0, 0xF9u);
    ::memset(context->enc_main_tree_len, 8, 0x100u);
    ::memset(context->enc_main_tree_len + 0x100, 9, 8 * num_position_slots);
    ::memset(context->enc_secondary_tree_len, 6, 0xF9u);

    *reinterpret_cast<unsigned __int64*>(context->enc_aligned_tree_len) = 0x0303030303030303ull;

    prevent_far_matches(context);

    context->enc_input_running_total = 0;
    context->enc_bufpos_at_last_block = context->enc_BufPos;
    context->enc_earliest_window_data_remaining = context->enc_BufPos;
    context->enc_first_time_this_group = true;

    ::memset(context->enc_ItemType, 0, 0x2000u);

    context->enc_literals = 0;
    context->enc_distances = 0;
    context->enc_num_block_splits = 0;
    context->enc_repeated_offset_at_literal_zero[0] = 1;
    context->enc_repeated_offset_at_literal_zero[1] = 1;
    context->enc_repeated_offset_at_literal_zero[2] = 1;

    reset_translation(context);

    context->enc_num_cfdata_frames = 0;
    context->enc_inserted_dict_size = 0;

    ::memset(context->enc_main_tree_freq, 0, 0xAF0u);
    ::memset(context->enc_secondary_tree_freq, 0, 0x3E4u);
    ::memset(context->enc_aligned_tree_freq, 0, 0x20u);
    ::memset(context->enc_RealMemWindow, 0, context->enc_encoder_second_partition_size + window_size + 0x1101u);
}

void comp_free_compress_memory(t_encoder_context* context)
{
    if (context->enc_tree_root != nullptr)
    {
        context->enc_free(context->enc_tree_root);
        context->enc_tree_root = nullptr;
    }

    if (context->enc_RealLeft != nullptr)
    {
        context->enc_free(context->enc_RealLeft);
        context->enc_RealLeft = nullptr;
    }

    if (context->enc_RealRight != nullptr)
    {
        context->enc_free(context->enc_RealRight);
        context->enc_RealRight = nullptr;
    }

    if (context->enc_RealMemWindow != nullptr)
    {
        context->enc_free(context->enc_RealMemWindow);
        context->enc_RealMemWindow = nullptr;
        context->enc_MemWindow = nullptr;
    }

    if (context->enc_LitData != nullptr)
    {
        context->enc_free(context->enc_LitData);
        context->enc_LitData = nullptr;
    }

    if (context->enc_DistData != nullptr)
    {
        context->enc_free(context->enc_DistData);
        context->enc_DistData = nullptr;
    }

    if (context->enc_ItemType != nullptr)
    {
        context->enc_free(context->enc_ItemType);
        context->enc_ItemType = nullptr;
    }

    if (context->enc_decision_node != nullptr)
    {
        context->enc_free(context->enc_decision_node);
        context->enc_decision_node = nullptr;
    }

    free_compressed_output_buffer(context);
    context->enc_allocated_compression_memory = false;
}

void comp_init_compress_memory_context(
    void* pContextData,
    unsigned __int64* pContextSize,
    int WindowSize,
    int SecondPartitionSize,
    unsigned int HeaderSize,
    unsigned int Flags)
{
    const bool enable_streaming = (Flags & 1) != 0;
    const bool preserve_snapshot = (Flags & 0x80000000u) != 0;

    const unsigned int base_window_bytes = static_cast<unsigned int>(SecondPartitionSize + WindowSize + 0x1101);
    const unsigned int rounded_window_bytes = (base_window_bytes + 3) & ~3u;
    const unsigned int tree_bytes = base_window_bytes * 4;
    const unsigned int base_context_size = 2 * tree_bytes + rounded_window_bytes + 0x97D58u;
    const unsigned int streaming_extra = enable_streaming ? (0x26028u + 0x8000u) : 0u;
    const unsigned int snapshot_extra = preserve_snapshot ? (0x9D89Cu + base_context_size) : 0u;

    *pContextSize =
        static_cast<unsigned __int64>(HeaderSize) +
        base_context_size +
        streaming_extra +
        snapshot_extra;

    if (pContextData == nullptr)
    {
        return;
    }

    t_encoder_context* const context =
        reinterpret_cast<t_encoder_context*>(static_cast<unsigned __int8*>(pContextData) + HeaderSize);

    context->enc_window_size = static_cast<unsigned int>(WindowSize);
    context->enc_encoder_second_partition_size = static_cast<unsigned int>(SecondPartitionSize);
    context->enc_output_callback_function = nullptr;
    context->enc_malloc = nullptr;
    context->enc_free = nullptr;
    context->enc_fci_data = nullptr;
    context->enc_tree_root = nullptr;
    context->enc_RealLeft = nullptr;
    context->enc_RealRight = nullptr;
    context->enc_MemWindow = nullptr;
    context->enc_decision_node = nullptr;
    context->enc_LitData = nullptr;
    context->enc_DistData = nullptr;
    context->enc_ItemType = nullptr;
    context->enc_output_buffer_start = nullptr;
    context->enc_source_staging_buffer = nullptr;
    context->enc_context_data_snapshot = nullptr;

    context->enc_num_position_slots = DetermineNumPositionSlots(static_cast<unsigned int>(WindowSize));

    unsigned __int8* cursor = reinterpret_cast<unsigned __int8*>(context) + 0x4408;

    context->enc_RealLeft = reinterpret_cast<unsigned int*>(cursor);
    cursor += tree_bytes;
    context->enc_RealRight = reinterpret_cast<unsigned int*>(cursor);
    cursor += tree_bytes;
    context->enc_RealMemWindow = cursor;
    cursor += rounded_window_bytes;
    context->enc_LitData = cursor;
    cursor += 0x10000;
    context->enc_DistData = reinterpret_cast<unsigned int*>(cursor);
    cursor += 0x20000;
    context->enc_ItemType = cursor;
    cursor += 0x2000;

    create_slot_lookup_table(context);
    create_ones_table(context);

    context->enc_output_buffer_start = cursor;
    context->enc_output_buffer_curpos = context->enc_output_buffer_start;
    context->enc_output_buffer_end = context->enc_output_buffer_start + 0x97C0;
    cursor += 0x9800;

    context->enc_decision_node = reinterpret_cast<decision_node*>(cursor);
    cursor += 0x18150;
    context->enc_tree_root = reinterpret_cast<unsigned int*>(cursor);
    cursor += 0x40000;

    if (enable_streaming)
    {
        context->enc_dest_staging_buffer = cursor;
        context->enc_dest_staging_buffer_size = 0x26028u;
        context->enc_source_staging_buffer = cursor + context->enc_dest_staging_buffer_size;
        context->enc_dest_staging_size = 0;
        context->enc_source_staging_size = 0;
        context->enc_chunks_submitted = 0;
        context->enc_chunks_encoded = 0;
        context->enc_stream_flushed = true;
    }
    else if (preserve_snapshot)
    {
        context->enc_source_staging_buffer = cursor;
        context->enc_context_data_snapshot = cursor + 0x9D89Cu;
    }

    context->enc_context_data_size = base_context_size;
    context->enc_allocated_compression_memory = true;
}

void comp_clear_compress_memory_context(t_encoder_context* context)
{
    context->enc_num_position_slots = DetermineNumPositionSlots(context->enc_window_size);

    const unsigned int base_window_bytes =
        context->enc_encoder_second_partition_size + context->enc_window_size + 0x1101;
    const std::size_t tree_bytes = static_cast<std::size_t>(base_window_bytes) * 4;
    const std::size_t rounded_window_bytes = (base_window_bytes + 3u) & ~std::size_t(3);

    ::memset(context->enc_RealLeft, 0, tree_bytes);
    ::memset(context->enc_RealRight, 0, tree_bytes);
    ::memset(context->enc_RealMemWindow, 0, rounded_window_bytes);
    ::memset(context->enc_LitData, 0, 0x10000u);
    ::memset(context->enc_DistData, 0, 0x20000u);
    ::memset(context->enc_ItemType, 0, 0x2000u);
    ::memset(context->enc_output_buffer_start, 0, 0x9800u);
    ::memset(context->enc_decision_node, 0, 0x18150u);
    ::memset(context->enc_tree_root, 0, 0x40000u);

    context->enc_output_buffer_curpos = context->enc_output_buffer_start;
    context->enc_output_buffer_end = context->enc_output_buffer_start + 0x97C0;
    context->enc_MemWindow = context->enc_RealMemWindow;
}

bool comp_alloc_compress_memory(t_encoder_context* context)
{
    context->enc_tree_root = nullptr;
    context->enc_RealLeft = nullptr;
    context->enc_RealRight = nullptr;
    context->enc_MemWindow = nullptr;
    context->enc_decision_node = nullptr;
    context->enc_LitData = nullptr;
    context->enc_DistData = nullptr;
    context->enc_ItemType = nullptr;
    context->enc_output_buffer_start = nullptr;

    context->enc_num_position_slots = DetermineNumPositionSlots(context->enc_window_size);

    context->enc_tree_root = static_cast<unsigned int*>(context->enc_malloc(0x40000u));
    if (context->enc_tree_root == nullptr)
    {
        comp_free_compress_memory(context);
        return false;
    }

    const unsigned int tree_alloc = 4 * (context->enc_encoder_second_partition_size + context->enc_window_size + 0x1101);

    context->enc_RealLeft = static_cast<unsigned int*>(context->enc_malloc(tree_alloc));
    if (context->enc_RealLeft == nullptr)
    {
        comp_free_compress_memory(context);
        return false;
    }

    context->enc_RealRight = static_cast<unsigned int*>(context->enc_malloc(tree_alloc));
    if (context->enc_RealRight == nullptr)
    {
        comp_free_compress_memory(context);
        return false;
    }

    context->enc_RealMemWindow = static_cast<unsigned __int8*>(
        context->enc_malloc(context->enc_encoder_second_partition_size + context->enc_window_size + 0x1101u));
    if (context->enc_RealMemWindow == nullptr)
    {
        comp_free_compress_memory(context);
        return false;
    }

    context->enc_MemWindow = context->enc_RealMemWindow;

    context->enc_LitData = static_cast<unsigned __int8*>(context->enc_malloc(0x10000u));
    if (context->enc_LitData == nullptr)
    {
        comp_free_compress_memory(context);
        return false;
    }

    context->enc_DistData = static_cast<unsigned int*>(context->enc_malloc(0x20000u));
    if (context->enc_DistData == nullptr)
    {
        comp_free_compress_memory(context);
        return false;
    }

    context->enc_ItemType = static_cast<unsigned __int8*>(context->enc_malloc(0x2000u));
    if (context->enc_ItemType == nullptr)
    {
        comp_free_compress_memory(context);
        return false;
    }

    create_slot_lookup_table(context);
    create_ones_table(context);

    if (!init_compressed_output_buffer(context))
    {
        comp_free_compress_memory(context);
        return false;
    }

    context->enc_decision_node =
        static_cast<decision_node*>(context->enc_malloc(0x18150u));
    if (context->enc_decision_node == nullptr)
    {
        comp_free_compress_memory(context);
        return false;
    }

    context->enc_allocated_compression_memory = true;
    return true;
}
}
