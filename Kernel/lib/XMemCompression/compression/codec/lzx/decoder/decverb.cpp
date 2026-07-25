#include "../../../api/precomp.hpp"

namespace XCOMPRESS
{
long special_decode_verbatim_block(t_decoder_context* context, long bufpos, int amount_to_decode)
{
    int bitcount = context->dec_bitcount;
    unsigned int bitbuf = context->dec_bitbuf;
    unsigned char* input_curpos = context->dec_input_curpos;
    unsigned char* const mem_window = context->dec_mem_window;
    int write_pos = static_cast<int>(bufpos);
    long window_pos = bufpos;
    const long end_pos = bufpos + amount_to_decode;

    while (window_pos < end_pos)
    {
        int symbol = context->dec_main_tree_table[bitbuf >> 22];
        if (symbol < 0)
        {
            unsigned int mask = 0x200000;
            do
            {
                const int index = -symbol;
                if ((bitbuf & mask) != 0)
                {
                    symbol = context->dec_main_tree_left_right[2 * index + 1];
                }
                else
                {
                    symbol = context->dec_main_tree_left_right[2 * index];
                }

                mask >>= 1;
            } while (symbol < 0);
        }

        if (input_curpos >= context->dec_end_input_pos)
        {
            return -1;
        }

        const unsigned int main_len = context->dec_main_tree_len[symbol];
        bitcount -= static_cast<int>(main_len);
        bitbuf <<= main_len;
        if (bitcount <= 0)
        {
            const unsigned int next = static_cast<unsigned int>(input_curpos[0]) |
                                      (static_cast<unsigned int>(input_curpos[1]) << 8);
            input_curpos += 2;
            bitbuf |= next << -bitcount;
            bitcount += 16;
        }

        symbol -= 0x100;
        if (symbol < 0)
        {
            mem_window[window_pos] = static_cast<unsigned char>(symbol);
            mem_window[write_pos + context->dec_window_size] = static_cast<unsigned char>(symbol);
            ++write_pos;
            ++window_pos;
            continue;
        }

        int match_length = symbol & 7;
        if (match_length == 7)
        {
            int length_symbol = context->dec_secondary_length_tree_table[bitbuf >> 24];
            if (length_symbol < 0)
            {
                unsigned int mask = 0x800000;
                do
                {
                    const int index = -length_symbol;
                    if ((bitbuf & mask) != 0)
                    {
                        length_symbol = context->dec_secondary_length_tree_left_right[2 * index + 1];
                    }
                    else
                    {
                        length_symbol = context->dec_secondary_length_tree_left_right[2 * index];
                    }

                    mask >>= 1;
                } while (length_symbol < 0);
            }

            const unsigned int secondary_len = context->dec_secondary_length_tree_len[length_symbol];
            bitcount -= static_cast<int>(secondary_len);
            bitbuf <<= secondary_len;
            if (bitcount <= 0)
            {
                const unsigned int next = static_cast<unsigned int>(input_curpos[0]) |
                                          (static_cast<unsigned int>(input_curpos[1]) << 8);
                input_curpos += 2;
                bitbuf |= next << -bitcount;
                bitcount += 16;
            }

            match_length = length_symbol + 7;
        }

        const int offset_slot = symbol >> 3;
        unsigned int match_offset;
        if (offset_slot <= 2)
        {
            match_offset = context->dec_last_matchpos_offset[offset_slot];
            if (offset_slot != 0)
            {
                context->dec_last_matchpos_offset[offset_slot] = context->dec_last_matchpos_offset[0];
            }
        }
        else
        {
            if (offset_slot <= 3)
            {
                match_offset = 1;
            }
            else
            {
                const unsigned int extra_bits =
                    context->dec_extra_bits_table[static_cast<unsigned char>(offset_slot)];
                bitcount -= static_cast<int>(extra_bits);
                const unsigned int extra = bitbuf >> (32 - extra_bits);
                bitbuf <<= extra_bits;
                if (bitcount <= 0)
                {
                    const unsigned int next = static_cast<unsigned int>(input_curpos[0]) |
                                              (static_cast<unsigned int>(input_curpos[1]) << 8);
                    input_curpos += 2;
                    bitbuf |= next << -bitcount;
                    bitcount += 16;
                    if (bitcount <= 0)
                    {
                        const unsigned int more = static_cast<unsigned int>(input_curpos[0]) |
                                                  (static_cast<unsigned int>(input_curpos[1]) << 8);
                        input_curpos += 2;
                        bitbuf |= more << -bitcount;
                        bitcount += 16;
                    }
                }

                match_offset = static_cast<unsigned int>(
                    context->MP_POS_minus2_table[static_cast<unsigned char>(offset_slot)]) + extra;
            }

            context->dec_last_matchpos_offset[2] = context->dec_last_matchpos_offset[1];
            context->dec_last_matchpos_offset[1] = context->dec_last_matchpos_offset[0];
        }

        context->dec_last_matchpos_offset[0] = match_offset;

        int remaining = match_length + 2;
        do
        {
            const unsigned char value =
                mem_window[context->dec_window_mask & (write_pos - static_cast<int>(match_offset))];
            mem_window[window_pos] = value;
            if (window_pos < 0x101)
            {
                mem_window[write_pos + context->dec_window_size] = value;
            }

            --remaining;
            ++write_pos;
            ++window_pos;
        } while (remaining > 0);
    }

    context->dec_bitcount = static_cast<char>(bitcount);
    context->dec_bitbuf = bitbuf;
    context->dec_input_curpos = input_curpos;
    return write_pos;
}

long fast_decode_verbatim_block(t_decoder_context* context, long bufpos, int amount_to_decode)
{
    int bitcount = context->dec_bitcount;
    unsigned int bitbuf = context->dec_bitbuf;
    unsigned char* input_curpos = context->dec_input_curpos;
    unsigned char* const mem_window = context->dec_mem_window;
    int write_pos = static_cast<int>(bufpos);
    const int end_pos = static_cast<int>(bufpos) + amount_to_decode;
    long window_pos = bufpos;

    while (window_pos < end_pos)
    {
        int symbol = context->dec_main_tree_table[bitbuf >> 22];
        if (symbol < 0)
        {
            unsigned int mask = 0x200000;
            do
            {
                const int index = -symbol;
                if ((bitbuf & mask) != 0)
                {
                    symbol = context->dec_main_tree_left_right[2 * index + 1];
                }
                else
                {
                    symbol = context->dec_main_tree_left_right[2 * index];
                }

                mask >>= 1;
            } while (symbol < 0);
        }

        if (input_curpos >= context->dec_end_input_pos)
        {
            return -1;
        }

        const unsigned int main_len = context->dec_main_tree_len[symbol];
        bitcount -= static_cast<int>(main_len);
        bitbuf <<= main_len;
        if (bitcount <= 0)
        {
            const unsigned int next = static_cast<unsigned int>(input_curpos[0]) |
                                      (static_cast<unsigned int>(input_curpos[1]) << 8);
            input_curpos += 2;
            bitbuf |= next << -bitcount;
            bitcount += 16;
        }

        symbol -= 0x100;
        if (symbol < 0)
        {
            mem_window[window_pos] = static_cast<unsigned char>(symbol);
            ++write_pos;
            ++window_pos;
            continue;
        }

        int match_length = symbol & 7;
        if (match_length == 7)
        {
            int length_symbol = context->dec_secondary_length_tree_table[bitbuf >> 24];
            if (length_symbol < 0)
            {
                unsigned int mask = 0x800000;
                do
                {
                    const int index = -length_symbol;
                    if ((bitbuf & mask) != 0)
                    {
                        length_symbol = context->dec_secondary_length_tree_left_right[2 * index + 1];
                    }
                    else
                    {
                        length_symbol = context->dec_secondary_length_tree_left_right[2 * index];
                    }

                    mask >>= 1;
                } while (length_symbol < 0);
            }

            const unsigned int secondary_len = context->dec_secondary_length_tree_len[length_symbol];
            bitcount -= static_cast<int>(secondary_len);
            bitbuf <<= secondary_len;
            if (bitcount <= 0)
            {
                const unsigned int next = static_cast<unsigned int>(input_curpos[0]) |
                                          (static_cast<unsigned int>(input_curpos[1]) << 8);
                input_curpos += 2;
                bitbuf |= next << -bitcount;
                bitcount += 16;
            }

            match_length = length_symbol + 7;
        }

        const int offset_slot = symbol >> 3;
        unsigned int match_offset;
        if (offset_slot <= 2)
        {
            match_offset = context->dec_last_matchpos_offset[offset_slot];
            if (offset_slot != 0)
            {
                context->dec_last_matchpos_offset[offset_slot] = context->dec_last_matchpos_offset[0];
            }
        }
        else
        {
            if (offset_slot <= 3)
            {
                match_offset = static_cast<unsigned int>(context->MP_POS_minus2_table[3]);
            }
            else
            {
                const unsigned int extra_bits =
                    context->dec_extra_bits_table[static_cast<unsigned char>(offset_slot)];
                bitcount -= static_cast<int>(extra_bits);
                const unsigned int extra = bitbuf >> (32 - extra_bits);
                bitbuf <<= extra_bits;
                if (bitcount <= 0)
                {
                    const unsigned int next = static_cast<unsigned int>(input_curpos[0]) |
                                              (static_cast<unsigned int>(input_curpos[1]) << 8);
                    input_curpos += 2;
                    bitbuf |= next << -bitcount;
                    bitcount += 16;
                    if (bitcount <= 0)
                    {
                        const unsigned int more = static_cast<unsigned int>(input_curpos[0]) |
                                                  (static_cast<unsigned int>(input_curpos[1]) << 8);
                        input_curpos += 2;
                        bitbuf |= more << -bitcount;
                        bitcount += 16;
                    }
                }

                match_offset = static_cast<unsigned int>(
                    context->MP_POS_minus2_table[static_cast<unsigned char>(offset_slot)]) + extra;
            }

            context->dec_last_matchpos_offset[2] = context->dec_last_matchpos_offset[1];
            context->dec_last_matchpos_offset[1] = context->dec_last_matchpos_offset[0];
        }

        context->dec_last_matchpos_offset[0] = match_offset;

        int remaining = match_length + 2;
        unsigned char* source =
            &mem_window[context->dec_window_mask & (write_pos - static_cast<int>(match_offset))];
        do
        {
            --remaining;
            ++write_pos;
            mem_window[window_pos++] = *source++;
        } while (remaining > 0);
    }

    context->dec_bitcount = static_cast<char>(bitcount);
    context->dec_bitbuf = bitbuf;
    context->dec_input_curpos = input_curpos;
    context->dec_bufpos = write_pos & static_cast<int>(context->dec_window_mask);
    return write_pos - end_pos;
}

int decode_verbatim_block(t_decoder_context* context, long bufpos, int amount_to_decode)
{
    int current_bufpos = static_cast<int>(bufpos);
    int remaining = amount_to_decode;

    if (current_bufpos < 0x101)
    {
        int special_amount = 0x101 - current_bufpos;
        if (special_amount > remaining)
        {
            special_amount = remaining;
        }

        const int new_bufpos = static_cast<int>(
            special_decode_verbatim_block(context, current_bufpos, special_amount));
        remaining += current_bufpos - new_bufpos;
        context->dec_bufpos = new_bufpos;
        current_bufpos = new_bufpos;

        if (remaining <= 0)
        {
            return remaining;
        }
    }

    return static_cast<int>(fast_decode_verbatim_block(context, current_bufpos, remaining));
}
}
