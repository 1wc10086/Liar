#include "../../../api/precomp.hpp"

namespace XCOMPRESS
{
const unsigned __int8 enc_extra_bits[52] =
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

const unsigned int enc_slot_mask[52] =
{
    0x00000, 0x00000, 0x00000, 0x00000,
    0x00001, 0x00001, 0x00003, 0x00003,
    0x00007, 0x00007, 0x0000F, 0x0000F,
    0x0001F, 0x0001F, 0x0003F, 0x0003F,
    0x0007F, 0x0007F, 0x000FF, 0x000FF,
    0x001FF, 0x001FF, 0x003FF, 0x003FF,
    0x007FF, 0x007FF, 0x00FFF, 0x00FFF,
    0x01FFF, 0x01FFF, 0x03FFF, 0x03FFF,
    0x07FFF, 0x07FFF, 0x0FFFF, 0x0FFFF,
    0x1FFFF, 0x1FFFF, 0x1FFFF, 0x1FFFF,
    0x1FFFF, 0x1FFFF, 0x1FFFF, 0x1FFFF,
    0x1FFFF, 0x1FFFF, 0x1FFFF, 0x1FFFF,
    0x1FFFF, 0x1FFFF, 0x1FFFF, 0x1FFFF
};

void get_final_repeated_offset_states(t_encoder_context* context, unsigned int distances)
{
    int start_index = static_cast<int>(distances) - 1;
    unsigned char consecutive_new_offsets = 0;

    for (int i = start_index; i >= 0; --i)
    {
        if (context->enc_DistData[i] <= 2)
        {
            consecutive_new_offsets = 0;
        }
        else
        {
            ++consecutive_new_offsets;
            if (consecutive_new_offsets >= 3)
            {
                start_index = i;
                break;
            }
        }

        start_index = i - 1;
    }

    if (consecutive_new_offsets < 3)
    {
        start_index = 0;
    }

    for (unsigned int i = static_cast<unsigned int>(start_index); i < distances; ++i)
    {
        const unsigned int distance = context->enc_DistData[i];
        if (distance == 0)
        {
            continue;
        }

        if (distance <= 2)
        {
            const unsigned int value = context->enc_repeated_offset_at_literal_zero[distance];
            context->enc_repeated_offset_at_literal_zero[distance] =
                context->enc_repeated_offset_at_literal_zero[0];
            context->enc_repeated_offset_at_literal_zero[0] = value;
        }
        else
        {
            context->enc_repeated_offset_at_literal_zero[2] =
                context->enc_repeated_offset_at_literal_zero[1];
            context->enc_repeated_offset_at_literal_zero[1] =
                context->enc_repeated_offset_at_literal_zero[0];
            context->enc_repeated_offset_at_literal_zero[0] = distance - 2;
        }
    }
}

unsigned int estimate_compressed_block_size(t_encoder_context* context)
{
    unsigned int bits = 0x4B0;

    for (int i = 0; i < 0x100; ++i)
    {
        bits += context->enc_main_tree_freq[i] * context->enc_main_tree_len[i];
    }

    for (unsigned int slot = 0; slot < context->enc_num_position_slots; ++slot)
    {
        const unsigned int extra_bits = enc_extra_bits[slot];
        const int base = 0x100 + 8 * static_cast<int>(slot);

        for (int i = 0; i < 8; ++i)
        {
            bits += context->enc_main_tree_freq[base + i] * (context->enc_main_tree_len[base + i] + extra_bits);
        }
    }

    for (int i = 0; i < 0xF9; ++i)
    {
        bits += context->enc_secondary_tree_freq[i] * context->enc_secondary_tree_len[i];
    }

    return (bits + 7) >> 3;
}

void perform_flush_output_callback(t_encoder_context* context)
{
    if (context->enc_input_running_total > 0)
    {
        flush_output_bit_buffer(context);

        const int bytes = static_cast<int>(context->enc_output_buffer_curpos - context->enc_output_buffer_start);
        if (bytes > 0)
        {
            context->enc_output_callback_function(
                context->enc_fci_data,
                context->enc_output_buffer_start,
                bytes,
                static_cast<int>(context->enc_input_running_total));
        }
    }

    context->enc_input_running_total = 0;
    context->enc_bitbuf = 0;
    context->enc_output_buffer_curpos = context->enc_output_buffer_start;
    context->enc_bitcount = 32;
}

void encode_uncompressed_block(t_encoder_context* context, unsigned int bufpos, unsigned int block_size)
{
    output_bits(context, static_cast<signed char>(context->enc_bitcount) - 16, 0);

    for (int i = 0; i < 3; ++i)
    {
        unsigned int value = context->enc_repeated_offset_at_literal_zero[i];
        for (int byte_index = 0; byte_index < 4; ++byte_index)
        {
            if (context->enc_output_buffer_curpos >= context->enc_output_buffer_end)
            {
                context->enc_output_overflow = true;
                context->enc_output_buffer_curpos = context->enc_output_buffer_start;
            }

            *context->enc_output_buffer_curpos++ = static_cast<unsigned __int8>(value);
            value >>= 8;
        }
    }

    while (block_size != 0)
    {
        if (context->enc_output_buffer_curpos >= context->enc_output_buffer_end)
        {
            context->enc_output_overflow = true;
            context->enc_output_buffer_curpos = context->enc_output_buffer_start;
        }

        *context->enc_output_buffer_curpos++ = context->enc_MemWindow[bufpos++];
        --block_size;
        ++context->enc_input_running_total;

        if (context->enc_input_running_total == 0x8000)
        {
            perform_flush_output_callback(context);
            context->enc_num_block_splits = 0;
        }
    }

    context->enc_bitbuf = 0;
    context->enc_bitcount = 32;
}

void encode_verbatim_block(t_encoder_context* context, unsigned int literal_to_end_at)
{
    unsigned int literal_index = 0;
    unsigned int distance_index = 0;

    while (literal_index < literal_to_end_at)
    {
        const unsigned char bit = static_cast<unsigned char>(1u << (literal_index & 7));
        if ((context->enc_ItemType[literal_index >> 3] & bit) == 0)
        {
            const unsigned int symbol = context->enc_LitData[literal_index];
            output_bits(context, context->enc_main_tree_len[symbol], context->enc_main_tree_code[symbol]);
            ++literal_index;
            ++context->enc_input_running_total;
        }
        else
        {
            const unsigned int length_symbol = context->enc_LitData[literal_index];
            const unsigned int distance = context->enc_DistData[distance_index++];
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

            if (length_symbol < 7)
            {
                const unsigned int symbol = 0x100 + 8 * slot + length_symbol;
                output_bits(context, context->enc_main_tree_len[symbol], context->enc_main_tree_code[symbol]);
            }
            else
            {
                const unsigned int symbol = 0x107 + 8 * slot;
                output_bits(context, context->enc_main_tree_len[symbol], context->enc_main_tree_code[symbol]);
                output_bits(
                    context,
                    context->enc_secondary_tree_len[length_symbol - 7],
                    context->enc_secondary_tree_code[length_symbol - 7]);
            }

            if (enc_extra_bits[slot] != 0)
            {
                output_bits(context, enc_extra_bits[slot], enc_slot_mask[slot] & distance);
            }

            ++literal_index;
            context->enc_input_running_total += length_symbol + 2;
        }

        if (context->enc_input_running_total == 0x8000)
        {
            perform_flush_output_callback(context);
            context->enc_num_block_splits = 0;
        }
    }
}

void encode_aligned_block(t_encoder_context* context, unsigned int literal_to_end_at)
{
    unsigned int literal_index = 0;
    unsigned int distance_index = 0;

    while (literal_index < literal_to_end_at)
    {
        const unsigned char bit = static_cast<unsigned char>(1u << (literal_index & 7));
        if ((context->enc_ItemType[literal_index >> 3] & bit) == 0)
        {
            const unsigned int symbol = context->enc_LitData[literal_index];
            output_bits(context, context->enc_main_tree_len[symbol], context->enc_main_tree_code[symbol]);
            ++literal_index;
            ++context->enc_input_running_total;
        }
        else
        {
            const unsigned int length_symbol = context->enc_LitData[literal_index];
            const unsigned int distance = context->enc_DistData[distance_index++];
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

            if (length_symbol < 7)
            {
                const unsigned int symbol = 0x100 + 8 * slot + length_symbol;
                output_bits(context, context->enc_main_tree_len[symbol], context->enc_main_tree_code[symbol]);
            }
            else
            {
                const unsigned int symbol = 0x107 + 8 * slot;
                output_bits(context, context->enc_main_tree_len[symbol], context->enc_main_tree_code[symbol]);
                output_bits(
                    context,
                    context->enc_secondary_tree_len[length_symbol - 7],
                    context->enc_secondary_tree_code[length_symbol - 7]);
            }

            if (enc_extra_bits[slot] > 3)
            {
                const unsigned int bits = enc_extra_bits[slot] - 3;
                output_bits(context, bits, ((distance >> 3) & ((1u << bits) - 1)));
                output_bits(
                    context,
                    context->enc_aligned_tree_len[distance & 7],
                    context->enc_aligned_tree_code[distance & 7]);
            }
            else if (enc_extra_bits[slot] == 3)
            {
                output_bits(
                    context,
                    context->enc_aligned_tree_len[distance & 7],
                    context->enc_aligned_tree_code[distance & 7]);
            }
            else if (enc_extra_bits[slot] != 0)
            {
                output_bits(context, enc_extra_bits[slot], enc_slot_mask[slot] & distance);
            }

            ++literal_index;
            context->enc_input_running_total += length_symbol + 2;
        }

        if (context->enc_input_running_total == 0x8000)
        {
            perform_flush_output_callback(context);
            context->enc_num_block_splits = 0;
        }
    }
}

}
