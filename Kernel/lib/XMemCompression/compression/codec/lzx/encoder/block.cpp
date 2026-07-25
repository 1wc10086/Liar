#include "../../../api/precomp.hpp"

namespace XCOMPRESS
{
unsigned int get_distances_from_literals(t_encoder_context* context, unsigned int literals)
{
    unsigned int distances = 0;

    const unsigned int whole_bytes = literals >> 3;
    for (unsigned int i = 0; i < whole_bytes; ++i)
    {
        distances += context->enc_ones[context->enc_ItemType[i]];
    }

    for (unsigned int literal_index = literals & 0xFFFFFFF8u; literal_index < literals; ++literal_index)
    {
        const unsigned char bit = static_cast<unsigned char>(1u << (literal_index & 7));
        if ((context->enc_ItemType[literal_index >> 3] & bit) != 0)
        {
            ++distances;
        }
    }

    return distances;
}

void do_block_output(t_encoder_context* context, long literal_to_end_at, long distance_to_end_at)
{
    const unsigned int block_size =
        get_block_stats(context, 0, 0, static_cast<unsigned int>(literal_to_end_at));
    int block_type = static_cast<int>(
        get_aligned_stats(context, static_cast<unsigned int>(distance_to_end_at)));

    create_trees(context, true);

    if ((estimate_compressed_block_size(context) >= block_size) &&
        (context->enc_bufpos_at_last_block >= context->enc_earliest_window_data_remaining))
    {
        block_type = 3;
    }

    output_bits(context, 3, static_cast<unsigned int>(block_type));
    output_bits(context, 8, static_cast<unsigned __int8>(block_size >> 16));
    output_bits(context, 8, static_cast<unsigned __int8>(block_size >> 8));
    output_bits(context, 8, static_cast<unsigned __int8>(block_size));

    if (block_type == 1)
    {
        encode_trees(context);
        encode_verbatim_block(context, static_cast<unsigned int>(literal_to_end_at));
        get_final_repeated_offset_states(context, static_cast<unsigned int>(distance_to_end_at));
    }
    else if (block_type == 2)
    {
        encode_aligned_tree(context);
        encode_trees(context);
        encode_aligned_block(context, static_cast<unsigned int>(literal_to_end_at));
        get_final_repeated_offset_states(context, static_cast<unsigned int>(distance_to_end_at));
    }
    else if (block_type == 3)
    {
        get_final_repeated_offset_states(context, static_cast<unsigned int>(distance_to_end_at));
        encode_uncompressed_block(context, context->enc_bufpos_at_last_block, block_size);
    }

    context->enc_bufpos_at_last_block += block_size;
}

void output_block(t_encoder_context* context)
{
    unsigned int where_to_split = context->enc_literals;
    unsigned int distances = context->enc_distances;

    context->enc_first_block = 0;
    split_block(
        context,
        0,
        context->enc_literals,
        context->enc_distances,
        &where_to_split,
        &distances);

    const unsigned int block_size = get_block_stats(context, 0, 0, where_to_split);
    int block_type = static_cast<int>(get_aligned_stats(context, distances));

    create_trees(context, true);
    if ((estimate_compressed_block_size(context) >= block_size) &&
        (context->enc_bufpos_at_last_block >= context->enc_earliest_window_data_remaining))
    {
        block_type = 3;
    }

    output_bits(context, 3, static_cast<unsigned int>(block_type));
    output_bits(context, 8, static_cast<unsigned __int8>(block_size >> 16));
    output_bits(context, 8, static_cast<unsigned __int8>(block_size >> 8));
    output_bits(context, 8, static_cast<unsigned __int8>(block_size));

    if (block_type == 1)
    {
        encode_trees(context);
        encode_verbatim_block(context, where_to_split);
        get_final_repeated_offset_states(context, distances);
    }
    else if (block_type == 2)
    {
        encode_aligned_tree(context);
        encode_trees(context);
        encode_aligned_block(context, where_to_split);
        get_final_repeated_offset_states(context, distances);
    }
    else if (block_type == 3)
    {
        get_final_repeated_offset_states(context, distances);
        encode_uncompressed_block(context, context->enc_bufpos_at_last_block, block_size);
    }

    const unsigned int original_literals = context->enc_literals;
    context->enc_bufpos_at_last_block += block_size;

    if (where_to_split == original_literals)
    {
        ::memset(context->enc_ItemType, 0, 0x2000u);
        context->enc_literals = 0;
        context->enc_distances = 0;
    }
    else
    {
        ::memmove(
            context->enc_ItemType,
            context->enc_ItemType + (where_to_split >> 3),
            ((original_literals >> 3) - (where_to_split >> 3) + 1));

        const unsigned int remaining_literals = original_literals - where_to_split;
        ::memset(
            context->enc_ItemType + (remaining_literals >> 3) + 1,
            0,
            0x1FFFu - (remaining_literals >> 3));

        ::memmove(
            context->enc_LitData,
            context->enc_LitData + where_to_split,
            original_literals - where_to_split);

        ::memmove(
            context->enc_DistData,
            context->enc_DistData + distances,
            4u * (context->enc_distances - distances));

        context->enc_literals -= where_to_split;
        context->enc_distances -= distances;
    }

    fix_tree_cost_estimates(context);
}

void flush_output_bit_buffer(t_encoder_context* context)
{
    const unsigned char bitcount = static_cast<unsigned char>(context->enc_bitcount);
    if (bitcount < 32)
    {
        output_bits(context, bitcount - 16, 0);
    }
}

long estimate_buffer_contents(t_encoder_context* context)
{
    create_trees(context, false);
    const unsigned int estimate = estimate_compressed_block_size(context);
    fix_tree_cost_estimates(context);
    return static_cast<long>(estimate);
}
}
