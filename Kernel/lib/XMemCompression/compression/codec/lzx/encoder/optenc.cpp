#include "../../../api/precomp.hpp"

#include <cstring>

namespace XCOMPRESS
{
void opt_encode_top(t_encoder_context* context, long BytesRead);
long binary_search_findmatch(t_encoder_context* context, unsigned long BufPos);

void flush_all_pending_blocks(t_encoder_context* context)
{
    while (context->enc_literals != 0)
    {
        output_block(context);
    }

    perform_flush_output_callback(context);
}

void update_tree_estimates(t_encoder_context* context)
{
    if (context->enc_literals == 0)
    {
        return;
    }

    if (context->enc_need_to_recalc_stats)
    {
        get_block_stats(context, 0, 0, context->enc_literals);
        context->enc_need_to_recalc_stats = false;
    }
    else
    {
        update_cumulative_block_stats(
            context,
            context->enc_last_literals,
            context->enc_last_distances,
            context->enc_literals);
    }

    create_trees(context, false);
    fix_tree_cost_estimates(context);
    context->enc_last_literals = context->enc_literals;
    context->enc_last_distances = context->enc_distances;
}

void block_end(t_encoder_context* context, long BufPos)
{
    context->enc_first_block = 0;
    context->enc_need_to_recalc_stats = true;
    output_block(context);

    if (context->enc_literals < 0x1000)
    {
        context->enc_next_tree_create = 0x1000;
    }
    else
    {
        context->enc_next_tree_create = context->enc_literals + 0x1000;
    }

    context->enc_bufpos_last_output_block = static_cast<unsigned int>(BufPos);
}

bool redo_first_block(t_encoder_context* context, long* bufpos_ptr)
{
    const unsigned int old_bufpos = context->enc_bufpos_last_output_block;
    context->enc_first_block = 0;

    unsigned int available = static_cast<unsigned int>(*bufpos_ptr) - old_bufpos;
    const unsigned int back_distance = old_bufpos - context->enc_window_size;
    if (back_distance < context->enc_window_size)
    {
        available += back_distance;
    }
    else
    {
        available += context->enc_window_size;
    }

    const unsigned int max_available =
        static_cast<unsigned int>(context->enc_MemWindow - context->enc_RealMemWindow) +
        static_cast<unsigned int>(*bufpos_ptr);
    if (available > max_available)
    {
        return false;
    }

    unsigned int split_at_literal = 0;
    split_block(context, 0, context->enc_literals, context->enc_distances, &split_at_literal, nullptr);
    get_block_stats(context, 0, 0, split_at_literal);
    create_trees(context, false);
    fix_tree_cost_estimates(context);

    ::memset(context->enc_tree_root, 0, 0x40000u);
    ::memset(context->enc_ItemType, 0, 0x2000u);

    context->enc_input_running_total = 0;
    context->enc_literals = 0;
    context->enc_distances = 0;
    context->enc_last_matchpos_offset[0] = 1;
    context->enc_last_matchpos_offset[1] = 1;
    context->enc_last_matchpos_offset[2] = 1;
    context->enc_next_tree_create = split_at_literal;
    context->enc_repeated_offset_at_literal_zero[0] = 1;
    context->enc_repeated_offset_at_literal_zero[1] = 1;
    context->enc_repeated_offset_at_literal_zero[2] = 1;
    context->enc_need_to_recalc_stats = true;

    *bufpos_ptr = old_bufpos;
    return true;
}

void encoder_start(t_encoder_context* context)
{
    const unsigned int input_position = static_cast<unsigned int>(context->enc_MemWindow - context->enc_RealMemWindow) +
                                        context->enc_BufPos;
    const long bytes_read = comp_read_input(context, input_position, 0x8000);
    if (bytes_read > 0)
    {
        opt_encode_top(context, bytes_read);
    }
}

void opt_encode_top(t_encoder_context* context, long BytesRead)
{
    unsigned int BufPos = context->enc_BufPos;
    const unsigned int end_pos = BufPos + static_cast<unsigned int>(BytesRead);

    if (context->enc_first_time_this_group)
    {
        context->enc_first_time_this_group = false;
        context->enc_next_tree_create = 0x2710;

        if (context->enc_file_size_for_translation != 0)
        {
            output_bits(context, 1, 1);
            output_bits(context, 16, static_cast<unsigned int>(context->enc_file_size_for_translation >> 16));
            output_bits(context, 16, context->enc_file_size_for_translation & 0xFFFFu);
        }
        else
        {
            output_bits(context, 1, 0);
        }
    }
    else
    {
        for (unsigned int i = 0x32; i != 0; --i)
        {
            quick_insert_bsearch_findmatch(
                context,
                BufPos - i,
                BufPos - context->enc_window_size + 4);
        }
    }

    for (;;)
    {
        while (BufPos < end_pos)
        {
            unsigned int chunk_end = (BufPos + 0x8000u) & 0xFFFF8000u;
            if (chunk_end > end_pos)
            {
                chunk_end = end_pos;
            }

            int best_match_length = static_cast<int>(binary_search_findmatch(context, BufPos));
            if (best_match_length >= 2)
            {
                unsigned int clamped_end = BufPos + static_cast<unsigned int>(best_match_length);
                if (clamped_end > chunk_end)
                {
                    best_match_length = static_cast<int>(chunk_end - BufPos);
                }
            }

            if (best_match_length < 2)
            {
                context->enc_LitData[context->enc_literals] = context->enc_MemWindow[BufPos];
                ++context->enc_literals;
                ++BufPos;

                if (context->enc_literals >= 0xFFF8)
                {
                    block_end(context, static_cast<long>(BufPos));
                }

                continue;
            }

            if (best_match_length >= 0x32)
            {
                const unsigned int length = static_cast<unsigned int>(best_match_length);
                const unsigned int distance_code = context->enc_matchpos_table[length];

                if ((distance_code == 3) && (length > 0x10))
                {
                    quick_insert_bsearch_findmatch(
                        context,
                        BufPos + 1,
                        BufPos - context->enc_window_size + 5);
                }
                else
                {
                    for (unsigned int i = 1; i < length; ++i)
                    {
                        quick_insert_bsearch_findmatch(
                            context,
                            BufPos + i,
                            BufPos + i - context->enc_window_size + 4);
                    }
                }

                context->enc_ItemType[context->enc_literals >> 3] |=
                    static_cast<unsigned char>(1u << (context->enc_literals & 7));
                context->enc_LitData[context->enc_literals] = static_cast<unsigned char>(length - 2);
                context->enc_DistData[context->enc_distances] = distance_code;
                ++context->enc_literals;
                ++context->enc_distances;
                BufPos += length;

                if (distance_code >= 3)
                {
                    context->enc_last_matchpos_offset[2] = context->enc_last_matchpos_offset[1];
                    context->enc_last_matchpos_offset[1] = context->enc_last_matchpos_offset[0];
                    context->enc_last_matchpos_offset[0] = distance_code - 2;
                }
                else if (distance_code != 0)
                {
                    const unsigned int value = context->enc_last_matchpos_offset[distance_code];
                    context->enc_last_matchpos_offset[distance_code] = context->enc_last_matchpos_offset[0];
                    context->enc_last_matchpos_offset[0] = value;
                }

                if ((context->enc_literals >= 0xFFF8) || (context->enc_distances >= 0x7FF8))
                {
                    block_end(context, static_cast<long>(BufPos));
                }

                continue;
            }

            const unsigned int start_pos = BufPos;
            const unsigned int decision_limit = start_pos + 0xEFD;
            unsigned int reachable_end = start_pos + static_cast<unsigned int>(best_match_length);
            unsigned int remaining_in_chunk = chunk_end - start_pos;
            decision_node* const decisions = context->enc_decision_node;

            decisions[1].numbits = context->enc_main_tree_len[context->enc_MemWindow[start_pos]];
            decisions[1].path = start_pos;

            for (unsigned int length = 2; length <= static_cast<unsigned int>(best_match_length); ++length)
            {
                const unsigned int distance_code = context->enc_matchpos_table[length];
                unsigned int slot;
                if (distance_code < 0x400)
                {
                    slot = context->enc_slot_table[distance_code];
                }
                else if (distance_code < 0x80000)
                {
                    slot = context->enc_slot_table[distance_code >> 9] + 0x12;
                }
                else
                {
                    slot = static_cast<unsigned char>(distance_code >> 17) + 0x22;
                }

                unsigned int cost;
                if (length < 9)
                {
                    cost = context->enc_main_tree_len[0xFE + 8 * slot + length] + enc_extra_bits[slot];
                }
                else
                {
                    cost = context->enc_main_tree_len[0x107 + 8 * slot] +
                           enc_extra_bits[slot] +
                           context->enc_secondary_tree_len[length - 9];
                }

                decisions[length].link = distance_code;
                decisions[length].path = start_pos;
                decisions[length].numbits = cost;
            }

            decisions[0].numbits = 0;
            decisions[0].repeated_offset[0] = context->enc_last_matchpos_offset[0];
            decisions[0].repeated_offset[1] = context->enc_last_matchpos_offset[1];
            decisions[0].repeated_offset[2] = context->enc_last_matchpos_offset[2];

            unsigned int current_pos = start_pos;
            unsigned int output_end_pos = start_pos;
            for (;;)
            {
                ++current_pos;
                --remaining_in_chunk;

                decision_node& current_decision = decisions[current_pos - start_pos];
                if (current_decision.path != (current_pos - 1))
                {
                    const decision_node& previous = decisions[current_decision.path - start_pos];
                    if (current_decision.link >= 3)
                    {
                        context->enc_last_matchpos_offset[0] = current_decision.link - 2;
                        context->enc_last_matchpos_offset[1] = previous.repeated_offset[0];
                        context->enc_last_matchpos_offset[2] = previous.repeated_offset[1];
                    }
                    else if (current_decision.link == 0)
                    {
                        context->enc_last_matchpos_offset[0] = previous.repeated_offset[0];
                        context->enc_last_matchpos_offset[1] = previous.repeated_offset[1];
                        context->enc_last_matchpos_offset[2] = previous.repeated_offset[2];
                    }
                    else if (current_decision.link == 1)
                    {
                        context->enc_last_matchpos_offset[0] = previous.repeated_offset[1];
                        context->enc_last_matchpos_offset[1] = previous.repeated_offset[0];
                        context->enc_last_matchpos_offset[2] = previous.repeated_offset[2];
                    }
                    else
                    {
                        context->enc_last_matchpos_offset[0] = previous.repeated_offset[2];
                        context->enc_last_matchpos_offset[1] = previous.repeated_offset[1];
                        context->enc_last_matchpos_offset[2] = previous.repeated_offset[0];
                    }
                }

                current_decision.repeated_offset[0] = context->enc_last_matchpos_offset[0];
                current_decision.repeated_offset[1] = context->enc_last_matchpos_offset[1];
                current_decision.repeated_offset[2] = context->enc_last_matchpos_offset[2];

                if (current_pos == reachable_end)
                {
                    output_end_pos = current_pos;
                    break;
                }

                int current_match_length = static_cast<int>(binary_search_findmatch(context, current_pos));
                if ((current_pos + static_cast<unsigned int>(current_match_length)) > chunk_end)
                {
                    current_match_length = static_cast<int>(remaining_in_chunk);
                    if (remaining_in_chunk < 2)
                    {
                        current_match_length = 0;
                    }
                }

                if ((current_match_length <= 0x32) &&
                    ((current_pos + static_cast<unsigned int>(current_match_length)) < decision_limit))
                {
                    if ((current_match_length > 2) ||
                        ((current_match_length == 2) && (context->enc_matchpos_table[2] < 0x800)))
                    {
                        unsigned int candidate_end = current_pos + static_cast<unsigned int>(current_match_length);
                        if (candidate_end > reachable_end)
                        {
                            unsigned int new_limit = candidate_end - start_pos;
                            if (new_limit > 0xEFC)
                            {
                                new_limit = 0xEFC;
                            }

                            unsigned int invalid_index = reachable_end - start_pos + 1;
                            while (invalid_index <= new_limit)
                            {
                                decisions[invalid_index].numbits = 0xFFFFFFFFu;
                                ++invalid_index;
                            }

                            reachable_end = candidate_end;
                        }
                    }

                    const unsigned int literal_cost =
                        current_decision.numbits + context->enc_main_tree_len[context->enc_MemWindow[current_pos]];
                    decision_node& next_literal = decisions[current_pos + 1 - start_pos];
                    if (literal_cost < next_literal.numbits)
                    {
                        next_literal.numbits = literal_cost;
                        next_literal.path = current_pos;
                    }

                    for (unsigned int length = 2; length <= static_cast<unsigned int>(current_match_length); ++length)
                    {
                        const unsigned int distance_code = context->enc_matchpos_table[length];
                        unsigned int slot;
                        if (distance_code < 0x400)
                        {
                            slot = context->enc_slot_table[distance_code];
                        }
                        else if (distance_code < 0x80000)
                        {
                            slot = context->enc_slot_table[distance_code >> 9] + 0x12;
                        }
                        else
                        {
                            slot = static_cast<unsigned char>(distance_code >> 17) + 0x22;
                        }

                        unsigned int cost;
                        if (length < 9)
                        {
                            cost = context->enc_main_tree_len[0xFE + 8 * slot + length] + enc_extra_bits[slot];
                        }
                        else
                        {
                            cost = context->enc_main_tree_len[0x107 + 8 * slot] +
                                   enc_extra_bits[slot] +
                                   context->enc_secondary_tree_len[length - 9];
                        }

                        cost += current_decision.numbits;

                        decision_node& target = decisions[current_pos + length - start_pos];
                        if (cost < target.numbits)
                        {
                            target.numbits = cost;
                            target.path = current_pos;
                            target.link = distance_code;
                        }
                    }

                    continue;
                }

                const unsigned int length = static_cast<unsigned int>(current_match_length);
                const unsigned int next_pos = current_pos + length;
                const unsigned int distance_code = context->enc_matchpos_table[length];
                decision_node& direct_target = decisions[next_pos - start_pos];
                direct_target.link = distance_code;
                direct_target.path = current_pos;

                if ((distance_code == 3) && (length > 0x10))
                {
                    quick_insert_bsearch_findmatch(
                        context,
                        current_pos + 1,
                        current_pos - context->enc_window_size + 5);
                }
                else
                {
                    for (unsigned int i = 1; i < length; ++i)
                    {
                        quick_insert_bsearch_findmatch(
                            context,
                            current_pos + i,
                            current_pos + i - context->enc_window_size + 4);
                    }
                }

                BufPos = next_pos;
                if (distance_code >= 3)
                {
                    context->enc_last_matchpos_offset[2] = context->enc_last_matchpos_offset[1];
                    context->enc_last_matchpos_offset[1] = context->enc_last_matchpos_offset[0];
                    context->enc_last_matchpos_offset[0] = distance_code - 2;
                }
                else if (distance_code != 0)
                {
                    const unsigned int value = context->enc_last_matchpos_offset[distance_code];
                    context->enc_last_matchpos_offset[distance_code] = context->enc_last_matchpos_offset[0];
                    context->enc_last_matchpos_offset[0] = value;
                }

                output_end_pos = next_pos;
                break;
            }

            unsigned int output_pos = output_end_pos;
            unsigned int output_count = 0;
            unsigned int predecessor = decisions[output_pos - start_pos].path;
            do
            {
                const unsigned int previous_pos = predecessor;
                ++output_count;
                predecessor = decisions[previous_pos - start_pos].path;
                decisions[previous_pos - start_pos].path = output_pos;
                output_pos = previous_pos;
            }
            while (output_pos != start_pos);

            while ((context->enc_literals + output_count >= 0xFFF8) ||
                   (context->enc_distances + output_count >= 0x7FF8))
            {
                block_end(context, static_cast<long>(start_pos));
            }

            output_pos = start_pos;
            while (output_count-- != 0)
            {
                const unsigned int next_pos = decisions[output_pos - start_pos].path;
                if (next_pos > (output_pos + 1))
                {
                    context->enc_ItemType[context->enc_literals >> 3] |=
                        static_cast<unsigned char>(1u << (context->enc_literals & 7));
                    context->enc_LitData[context->enc_literals] =
                        static_cast<unsigned char>(next_pos - output_pos - 2);
                    context->enc_DistData[context->enc_distances] =
                        decisions[next_pos - start_pos].link;
                    ++context->enc_distances;
                }
                else
                {
                    context->enc_LitData[context->enc_literals] = context->enc_MemWindow[output_pos];
                }

                ++context->enc_literals;
                output_pos = next_pos;
            }

            BufPos = output_pos;

            if (context->enc_literals >= context->enc_next_tree_create)
            {
                if (context->enc_literals != 0)
                {
                    if (context->enc_need_to_recalc_stats)
                    {
                        get_block_stats(context, 0, 0, context->enc_literals);
                        context->enc_need_to_recalc_stats = false;
                    }
                    else
                    {
                        update_cumulative_block_stats(
                            context,
                            context->enc_last_literals,
                            context->enc_last_distances,
                            context->enc_literals);
                    }

                    create_trees(context, false);
                    fix_tree_cost_estimates(context);
                    context->enc_last_literals = context->enc_literals;
                    context->enc_last_distances = context->enc_distances;
                }

                context->enc_next_tree_create += 0x1000;
            }

            if (context->enc_first_block != 0)
            {
                if ((context->enc_literals >= 0xFE00) || (context->enc_distances >= 0x7E00))
                {
                    if (!redo_first_block(context, reinterpret_cast<long*>(&BufPos)))
                    {
                        block_end(context, static_cast<long>(BufPos));
                    }
                }
            }
        }

        context->enc_earliest_window_data_remaining = BufPos - context->enc_window_size;
        if (BytesRead < 0x8000)
        {
            if ((context->enc_first_block != 0) &&
                redo_first_block(context, reinterpret_cast<long*>(&BufPos)))
            {
                continue;
            }

            break;
        }

        const unsigned int remove_path = context->enc_earliest_window_data_remaining + 0x36;
        for (unsigned int i = 1; i <= 0x32; ++i)
        {
            binary_search_remove_node(context, BufPos - i, remove_path);
        }

        unsigned int window_usage = static_cast<unsigned int>(context->enc_MemWindow - context->enc_RealMemWindow);
        window_usage += BufPos;
        if (window_usage >= (context->enc_encoder_second_partition_size + context->enc_window_size))
        {
            if ((context->enc_first_block != 0) &&
                redo_first_block(context, reinterpret_cast<long*>(&BufPos)))
            {
                continue;
            }

            ::memmove(
                context->enc_RealMemWindow,
                context->enc_RealMemWindow + context->enc_encoder_second_partition_size,
                context->enc_window_size);
            ::memmove(
                context->enc_RealLeft,
                context->enc_RealLeft + context->enc_encoder_second_partition_size,
                4u * context->enc_window_size);
            ::memmove(
                context->enc_RealRight,
                context->enc_RealRight + context->enc_encoder_second_partition_size,
                4u * context->enc_window_size);

            context->enc_MemWindow -= context->enc_encoder_second_partition_size;
            context->enc_Left -= context->enc_encoder_second_partition_size;
            context->enc_Right -= context->enc_encoder_second_partition_size;
            context->enc_earliest_window_data_remaining = BufPos - context->enc_window_size;
        }

        break;
    }

    context->enc_BufPos = BufPos;
}
}
