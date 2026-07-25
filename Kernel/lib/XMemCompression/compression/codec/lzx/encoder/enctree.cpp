#include "../../../api/precomp.hpp"

#include <cstring>

namespace XCOMPRESS
{
namespace
{
const unsigned char Modulo17Lookup[35] = {
    0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16,
    0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16,
    0,
};
}

void WriteRepTree(t_encoder_context* context, unsigned __int8* pLen, unsigned __int8* pLastLen, int Num)
{
    unsigned short SmallFreq[48];
    unsigned char MiniLen[24];
    unsigned short MiniCode[24];

    ::memset(SmallFreq, 0, sizeof(SmallFreq));

    const int count = Num;
    const unsigned char saved_length = pLen[count];
    pLen[count] = 123;

    for (int i = 0; i < count; ++i)
    {
        int repeat_count = 0;
        const unsigned char current = pLen[i];
        if (pLen[i + 1] == current)
        {
            const unsigned char* run = pLen + i + 1;
            do
            {
                ++run;
                ++repeat_count;
            }
            while (*run == current);

            if (repeat_count >= 4)
            {
                if (current == 0)
                {
                    if (repeat_count > 0x33)
                    {
                        repeat_count = 0x33;
                    }

                    if (repeat_count > 0x13)
                    {
                        ++SmallFreq[18];
                    }
                    else
                    {
                        ++SmallFreq[17];
                    }
                }
                else
                {
                    if (repeat_count > 5)
                    {
                        repeat_count = 5;
                    }

                    ++SmallFreq[Modulo17Lookup[pLastLen[i] - current + 17]];
                    ++SmallFreq[19];
                }

                i += repeat_count - 1;
                continue;
            }
        }

        ++SmallFreq[Modulo17Lookup[pLastLen[i] - current + 17]];
    }

    make_tree(context, 20, SmallFreq, MiniLen, MiniCode, true);
    for (int i = 0; i < 20; ++i)
    {
        output_bits(context, 4, MiniLen[i]);
    }

    for (int i = 0; i < count; ++i)
    {
        int repeat_count = 0;
        const unsigned char current = pLen[i];
        unsigned char symbol;

        if (pLen[i + 1] == current)
        {
            const unsigned char* run = pLen + i + 1;
            do
            {
                ++run;
                ++repeat_count;
            }
            while (*run == current);

            if (repeat_count >= 4)
            {
                if (current == 0)
                {
                    if (repeat_count > 0x33)
                    {
                        repeat_count = 0x33;
                    }

                    symbol = static_cast<unsigned char>((repeat_count > 0x13) + 17);
                }
                else
                {
                    if (repeat_count > 5)
                    {
                        repeat_count = 5;
                    }

                    symbol = 19;
                }
            }
            else
            {
                symbol = Modulo17Lookup[pLastLen[i] - current + 17];
            }
        }
        else
        {
            symbol = Modulo17Lookup[pLastLen[i] - current + 17];
        }

        output_bits(context, MiniLen[symbol], MiniCode[symbol]);

        if (symbol == 17)
        {
            output_bits(context, 4, static_cast<unsigned int>(repeat_count - 4));
            i += repeat_count - 1;
        }
        else if (symbol == 18)
        {
            output_bits(context, 5, static_cast<unsigned int>(repeat_count - 20));
            i += repeat_count - 1;
        }
        else if (symbol == 19)
        {
            output_bits(context, 1, static_cast<unsigned int>(repeat_count - 4));

            const unsigned char delta_symbol = Modulo17Lookup[pLastLen[i] - current + 17];
            output_bits(context, MiniLen[delta_symbol], MiniCode[delta_symbol]);

            i += repeat_count - 1;
        }
    }

    pLen[count] = saved_length;
    ::memcpy(pLastLen, pLen, static_cast<std::size_t>(count));
}

void create_trees(t_encoder_context* context, bool generate_codes)
{
    make_tree(
        context,
        static_cast<int>(8 * context->enc_num_position_slots + 0x100),
        context->enc_main_tree_freq,
        context->enc_main_tree_len,
        context->enc_main_tree_code,
        generate_codes);

    make_tree(
        context,
        0xF9,
        context->enc_secondary_tree_freq,
        context->enc_secondary_tree_len,
        context->enc_secondary_tree_code,
        generate_codes);

    make_tree(
        context,
        8,
        context->enc_aligned_tree_freq,
        context->enc_aligned_tree_len,
        context->enc_aligned_tree_code,
        true);
}

void prevent_far_matches(t_encoder_context* context)
{
    unsigned int slot = static_cast<unsigned int>(context->enc_slot_table[0x100]) + 0x12;
    if (slot < context->enc_num_position_slots)
    {
        unsigned char* tree_len = &context->enc_main_tree_len[0x100 + 8 * slot];
        do
        {
            *tree_len = 100;
            ++slot;
            tree_len += 8;
        }
        while (slot < context->enc_num_position_slots);
    }
}

void encode_trees(t_encoder_context* context)
{
    WriteRepTree(context, context->enc_main_tree_len, context->enc_main_tree_prev_len, 0x100);
    WriteRepTree(
        context,
        context->enc_main_tree_len + 0x100,
        context->enc_main_tree_prev_len + 0x100,
        static_cast<int>(context->enc_num_position_slots << 3));
    WriteRepTree(context, context->enc_secondary_tree_len, context->enc_secondary_tree_prev_len, 0xF9);
}

void encode_aligned_tree(t_encoder_context* context)
{
    make_tree(
        context,
        8,
        context->enc_aligned_tree_freq,
        context->enc_aligned_tree_len,
        context->enc_aligned_tree_code,
        true);

    for (unsigned int i = 0; i < 8; ++i)
    {
        output_bits(context, 3, context->enc_aligned_tree_len[i]);
    }
}

void fix_tree_cost_estimates(t_encoder_context* context)
{
    for (unsigned int i = 0; i < 0x100; ++i)
    {
        if (context->enc_main_tree_len[i] == 0)
        {
            context->enc_main_tree_len[i] = 11;
        }
    }

    const unsigned int main_tree_count = 8 * context->enc_num_position_slots + 0x100;
    for (unsigned int i = 0x100; i < main_tree_count; ++i)
    {
        if (context->enc_main_tree_len[i] == 0)
        {
            context->enc_main_tree_len[i] = 12;
        }
    }

    for (unsigned int i = 0; i < 0xF9; ++i)
    {
        if (context->enc_secondary_tree_len[i] == 0)
        {
            context->enc_secondary_tree_len[i] = 8;
        }
    }

    prevent_far_matches(context);
}
}
