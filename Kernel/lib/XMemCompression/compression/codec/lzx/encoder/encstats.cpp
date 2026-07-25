#include "../../../api/precomp.hpp"

#include <cstring>

const unsigned int square_table[17] = {
    0, 1, 4, 9, 16, 25, 36, 49, 64, 81, 100, 121, 144, 169, 196, 225, 256,
};

const unsigned char log2_table[256] = {
    0, 1, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4,
    5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
    6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
    6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
    7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
    7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
    7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
    7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
    8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
    8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
    8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
    8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
    8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
    8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
    8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
    8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
};

namespace XCOMPRESS
{
void tally_aligned_bits(t_encoder_context* context, unsigned int dist_to_end_at)
{
    unsigned int* distance = context->enc_DistData;
    while (dist_to_end_at != 0)
    {
        const unsigned int value = *distance++;
        if (value >= 0x10)
        {
            ++context->enc_aligned_tree_freq[value & 7];
        }

        --dist_to_end_at;
    }
}

lzx_block_type get_aligned_stats(t_encoder_context* context, unsigned int dist_to_end_at)
{
    ::memset(context->enc_aligned_tree_freq, 0, 0x20u);

    unsigned int total = 0;
    unsigned int maximum = 0;

    unsigned int* distance = context->enc_DistData;
    for (unsigned int i = 0; i < dist_to_end_at; ++i)
    {
        const unsigned int value = *distance++;
        if (value >= 0x10)
        {
            context->enc_aligned_tree_freq[value & 7] += 1;
        }
    }

    for (int i = 0; i < 8; ++i)
    {
        const unsigned int value = context->enc_aligned_tree_freq[i];
        if (value > maximum)
        {
            maximum = value;
        }

        total += value;
    }

    if ((maximum > (total / 5)) && (dist_to_end_at >= 0x64))
    {
        return BLOCKTYPE_ALIGNED;
    }

    return BLOCKTYPE_VERBATIM;
}

unsigned int tally_frequency(
    t_encoder_context* context,
    unsigned int literal_to_start_at,
    unsigned int distance_to_start_at,
    unsigned int literal_to_end_at)
{
    unsigned int total = 0;
    unsigned int literal_index = literal_to_start_at;
    const unsigned char increment = 1;
    const unsigned char* item_type = context->enc_ItemType;
    const unsigned char* lit_data = context->enc_LitData;
    unsigned int distance_index = distance_to_start_at;

    while (literal_index < literal_to_end_at)
    {
        const unsigned char bit = static_cast<unsigned char>(increment << (literal_index & 7));
        if ((item_type[literal_index >> 3] & bit) == 0)
        {
            context->enc_main_tree_freq[lit_data[literal_index]] += increment;
            total += increment;
        }
        else
        {
            const unsigned int distance = context->enc_DistData[distance_index];
            if (lit_data[literal_index] < 7)
            {
                unsigned int slot;
                if (distance < 0x400)
                {
                    slot = context->enc_slot_table[distance];
                }
                else if (distance < 0x80000)
                {
                    slot = context->enc_slot_table[distance >> 9] + 0x12;
                }
                else
                {
                    slot = static_cast<unsigned char>(distance >> 17) + 0x22;
                }

                context->enc_main_tree_freq[lit_data[literal_index] + 8 * slot + 0x100] += increment;
            }
            else
            {
                unsigned int slot;
                if (distance < 0x400)
                {
                    slot = context->enc_slot_table[distance];
                }
                else if (distance < 0x80000)
                {
                    slot = context->enc_slot_table[distance >> 9] + 0x12;
                }
                else
                {
                    slot = static_cast<unsigned char>(distance >> 17) + 0x22;
                }

                context->enc_main_tree_freq[8 * slot + 0x107] += increment;
                context->enc_secondary_tree_freq[lit_data[literal_index] - 7] += increment;
            }

            ++distance_index;
            total += lit_data[literal_index] + 2;
        }

        ++literal_index;
    }

    return total;
}

unsigned int get_block_stats(
    t_encoder_context* context,
    unsigned int literal_to_start_at,
    unsigned int distance_to_start_at,
    unsigned int literal_to_end_at)
{
    ::memset(context->enc_main_tree_freq, 0, 0xAF0u);
    ::memset(context->enc_secondary_tree_freq, 0, 0x3E4u);
    return tally_frequency(context, literal_to_start_at, distance_to_start_at, literal_to_end_at);
}

unsigned int update_cumulative_block_stats(
    t_encoder_context* context,
    unsigned int literal_to_start_at,
    unsigned int distance_to_start_at,
    unsigned int literal_to_end_at)
{
    return tally_frequency(context, literal_to_start_at, distance_to_start_at, literal_to_end_at);
}

unsigned int return_difference(
    t_encoder_context* context,
    unsigned int item_start1,
    unsigned int item_start2,
    unsigned int dist_at_1,
    unsigned int dist_at_2,
    unsigned int size)
{
    const unsigned int num_position_slots = context->enc_num_position_slots;
    const unsigned int num_symbols = 8 * num_position_slots + 0x100;
    if (num_symbols >= 0x320)
    {
        return 0;
    }

    unsigned short freq1[0x320]{};
    unsigned short freq2[0x320]{};
    ::memset(freq1, 0, 2u * num_symbols);
    ::memset(freq2, 0, 2u * num_symbols);

    unsigned int difference = 0;
    if (size != 0)
    {
        const unsigned char* item_type = context->enc_ItemType;
        const unsigned char* lit_data = context->enc_LitData;
        unsigned int dist_index1 = dist_at_1;
        unsigned int dist_index2 = dist_at_2;

        while (size != 0)
        {
            unsigned int symbol1;
            const unsigned char bit1 = static_cast<unsigned char>(1u << (item_start1 & 7));
            if ((item_type[item_start1 >> 3] & bit1) == 0)
            {
                symbol1 = lit_data[item_start1];
            }
            else
            {
                const unsigned int distance = context->enc_DistData[dist_index1];
                if (lit_data[item_start1] < 7)
                {
                    unsigned int slot;
                    if (distance < 0x400)
                    {
                        slot = context->enc_slot_table[distance];
                    }
                    else if (distance < 0x80000)
                    {
                        slot = context->enc_slot_table[distance >> 9] + 0x12;
                    }
                    else
                    {
                        slot = static_cast<unsigned char>(distance >> 17) + 0x22;
                    }

                    symbol1 = lit_data[item_start1] + 8 * slot + 0x100;
                }
                else
                {
                    unsigned int slot;
                    if (distance < 0x400)
                    {
                        slot = context->enc_slot_table[distance];
                    }
                    else if (distance < 0x80000)
                    {
                        slot = context->enc_slot_table[distance >> 9] + 0x12;
                    }
                    else
                    {
                        slot = static_cast<unsigned char>(distance >> 17) + 0x22;
                    }

                    symbol1 = 8 * slot + 0x107;
                }

                ++dist_index1;
            }

            ++freq1[symbol1];
            ++item_start1;

            unsigned int symbol2;
            const unsigned char bit2 = static_cast<unsigned char>(1u << (item_start2 & 7));
            if ((item_type[item_start2 >> 3] & bit2) == 0)
            {
                symbol2 = lit_data[item_start2];
            }
            else
            {
                const unsigned int distance = context->enc_DistData[dist_index2];
                if (lit_data[item_start2] < 7)
                {
                    unsigned int slot;
                    if (distance < 0x400)
                    {
                        slot = context->enc_slot_table[distance];
                    }
                    else if (distance < 0x80000)
                    {
                        slot = context->enc_slot_table[distance >> 9] + 0x12;
                    }
                    else
                    {
                        slot = static_cast<unsigned char>(distance >> 17) + 0x22;
                    }

                    symbol2 = lit_data[item_start2] + 8 * slot + 0x100;
                }
                else
                {
                    unsigned int slot;
                    if (distance < 0x400)
                    {
                        slot = context->enc_slot_table[distance];
                    }
                    else if (distance < 0x80000)
                    {
                        slot = context->enc_slot_table[distance >> 9] + 0x12;
                    }
                    else
                    {
                        slot = static_cast<unsigned char>(distance >> 17) + 0x22;
                    }

                    symbol2 = 8 * slot + 0x107;
                }

                ++dist_index2;
            }

            ++item_start2;
            ++freq2[symbol2];
            --size;
        }
    }

    for (unsigned int i = 0; i < num_symbols; ++i)
    {
        unsigned int log1 = log2_table[freq1[i]];
        if (freq1[i] >= 0x100)
        {
            log1 = log2_table[freq1[i] >> 8] + 8;
        }

        unsigned int log2 = log2_table[freq2[i]];
        if (freq2[i] >= 0x100)
        {
            log2 = log2_table[freq2[i] >> 8] + 8;
        }

        const int delta = static_cast<int>(square_table[log1]) - static_cast<int>(square_table[log2]);
        difference += static_cast<unsigned int>(delta < 0 ? -delta : delta);
    }

    return difference;
}

bool split_block(
    t_encoder_context* context,
    unsigned int literal_to_start_at,
    unsigned int literal_to_end_at,
    unsigned int distance_to_end_at,
    unsigned int* split_at_literal,
    unsigned int* split_at_distance)
{
    unsigned short num_dist_at_item[1032]{};

    *split_at_literal = literal_to_end_at;
    if (split_at_distance != nullptr)
    {
        *split_at_distance = distance_to_end_at;
    }

    if ((literal_to_end_at - literal_to_start_at) < 0x1800)
    {
        return false;
    }

    const unsigned char num_block_splits = context->enc_num_block_splits;
    if (num_block_splits >= 4)
    {
        return false;
    }

    unsigned int distance_count = 0;
    unsigned int item_type_index = 0;
    if ((literal_to_end_at >> 3) != 0)
    {
        const unsigned char* item_type = context->enc_ItemType;
        unsigned short* distance_table = num_dist_at_item;
        do
        {
            if ((item_type_index & 7) == 0)
            {
                *distance_table++ = static_cast<unsigned short>(distance_count);
            }

            distance_count += context->enc_ones[*item_type++];
            ++item_type_index;
        } while (item_type_index < (literal_to_end_at >> 3));
    }

    const unsigned int first_candidate = (literal_to_start_at + 0x3Fu) & 0xFFFFFFC0u;
    const unsigned int last_center = literal_to_end_at - 0x1000u;
    unsigned int left = first_candidate + 0x800u;
    if (left >= last_center)
    {
        return false;
    }

    for (unsigned int right = left + 0x800u;; right += 0x400u)
    {
        if ((return_difference(
                 context,
                 left,
                 right - 0x400u,
                 num_dist_at_item[left >> 6],
                 num_dist_at_item[(right - 0x400u) >> 6],
                 0x400u) > 0x578u) &&
            (return_difference(
                 context,
                 left - 0x400u,
                 right,
                 num_dist_at_item[(left - 0x400u) >> 6],
                 num_dist_at_item[right >> 6],
                 0x400u) > 0x578u) &&
            (return_difference(
                 context,
                 left - 0x800u,
                 right + 0x400u,
                 num_dist_at_item[(left - 0x800u) >> 6],
                 num_dist_at_item[(right + 0x400u) >> 6],
                 0x400u) > 0x578u))
        {
            unsigned int best_literal = 0;
            unsigned int best_score = 0;
            unsigned int candidate = right - 0x600u;
            if (candidate < (right + 0x200u))
            {
                do
                {
                    const unsigned int score = return_difference(
                        context,
                        candidate - 0x400u,
                        candidate,
                        num_dist_at_item[(candidate - 0x400u) >> 6],
                        num_dist_at_item[candidate >> 6],
                        0x400u);

                    if (score > best_score)
                    {
                        best_score = score;
                        best_literal = candidate;
                    }

                    candidate += 0x40u;
                } while (candidate < (right + 0x200u));

                if ((best_score >= 0x6A4u) && ((best_literal - first_candidate) >= 0x1000u))
                {
                    context->enc_num_block_splits = num_block_splits + 1;
                    *split_at_literal = best_literal;
                    if (split_at_distance != nullptr)
                    {
                        *split_at_distance = num_dist_at_item[best_literal >> 6];
                    }

                    return true;
                }
            }
        }

        left += 0x400u;
        if (left >= last_center)
        {
            return false;
        }
    }
}

unsigned long get_block_stats(
    t_encoder_context* context,
    unsigned long literal_to_start_at,
    unsigned long distance_to_start_at,
    unsigned long literal_to_end_at)
{
    return static_cast<unsigned long>(
        get_block_stats(
            context,
            static_cast<unsigned int>(literal_to_start_at),
            static_cast<unsigned int>(distance_to_start_at),
            static_cast<unsigned int>(literal_to_end_at)));
}

unsigned long update_cumulative_block_stats(
    t_encoder_context* context,
    unsigned long literal_to_start_at,
    unsigned long distance_to_start_at,
    unsigned long literal_to_end_at)
{
    return static_cast<unsigned long>(
        update_cumulative_block_stats(
            context,
            static_cast<unsigned int>(literal_to_start_at),
            static_cast<unsigned int>(distance_to_start_at),
            static_cast<unsigned int>(literal_to_end_at)));
}

bool split_block(
    t_encoder_context* context,
    unsigned long literal_to_start_at,
    unsigned long literal_to_end_at,
    unsigned long distance_to_end_at,
    unsigned long* split_at_literal,
    unsigned long* split_at_distance)
{
    unsigned int split_literal32 = 0;
    unsigned int split_distance32 = 0;

    const bool result = split_block(
        context,
        static_cast<unsigned int>(literal_to_start_at),
        static_cast<unsigned int>(literal_to_end_at),
        static_cast<unsigned int>(distance_to_end_at),
        split_at_literal != nullptr ? &split_literal32 : nullptr,
        split_at_distance != nullptr ? &split_distance32 : nullptr);

    if (split_at_literal != nullptr)
    {
        *split_at_literal = split_literal32;
    }

    if (split_at_distance != nullptr)
    {
        *split_at_distance = split_distance32;
    }

    return result;
}
}
