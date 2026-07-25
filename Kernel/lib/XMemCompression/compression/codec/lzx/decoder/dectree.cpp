#include "../../../api/precomp.hpp"

#include <cstring>

namespace XCOMPRESS
{
bool ReadRepTree(t_decoder_context* context, int num_elements, unsigned __int8* lastlen, unsigned __int8* len)
{
    unsigned __int8 small_bitlen[32];
    short leftright_s[96];
    short small_table[256];

    for (int i = 0; i < 20; ++i)
    {
        small_bitlen[i] = static_cast<unsigned __int8>(getbits(context, 4));
    }

    if (context->dec_error_condition)
    {
        return false;
    }

    make_table(context, 20, small_bitlen, 8, small_table, leftright_s);

    unsigned __int8* input_curpos = context->dec_input_curpos;
    unsigned __int8* const end_input_pos = context->dec_end_input_pos;
    unsigned int bitbuf = context->dec_bitbuf;
    int bitcount = context->dec_bitcount;
    bool error_condition = context->dec_error_condition;

    int element = 0;
    while ((element < num_elements) && !error_condition)
    {
        int symbol = small_table[bitbuf >> 24];
        if (symbol < 0)
        {
            unsigned int mask = 0x800000;
            do
            {
                const int index = -symbol;
                if ((bitbuf & mask) != 0)
                {
                    symbol = leftright_s[2 * index + 1];
                }
                else
                {
                    symbol = leftright_s[2 * index];
                }

                mask >>= 1;
            } while (symbol < 0);
        }

        const unsigned int symbol_len = small_bitlen[symbol];
        bitcount -= static_cast<int>(symbol_len);
        bitbuf <<= symbol_len;
        if (bitcount <= 0)
        {
            if (input_curpos >= end_input_pos)
            {
                error_condition = true;
            }
            else
            {
                const unsigned int next = static_cast<unsigned int>(input_curpos[0]) |
                                          (static_cast<unsigned int>(input_curpos[1]) << 8);
                input_curpos += 2;
                bitbuf |= next << -bitcount;
                bitcount += 16;
                if ((bitcount <= 0) && (input_curpos < end_input_pos))
                {
                    const unsigned int more = static_cast<unsigned int>(input_curpos[0]) |
                                              (static_cast<unsigned int>(input_curpos[1]) << 8);
                    input_curpos += 2;
                    bitbuf |= more << -bitcount;
                    bitcount += 16;
                }
                else if (bitcount <= 0)
                {
                    error_condition = true;
                }
            }
        }

        if (error_condition)
        {
            break;
        }

        if (symbol == 17)
        {
            unsigned int run_length = (bitbuf >> 28) + 4;
            bitcount -= 4;
            bitbuf <<= 4;
            if (bitcount <= 0)
            {
                if (input_curpos >= end_input_pos)
                {
                    error_condition = true;
                }
                else
                {
                    const unsigned int next = static_cast<unsigned int>(input_curpos[0]) |
                                              (static_cast<unsigned int>(input_curpos[1]) << 8);
                    input_curpos += 2;
                    bitbuf |= next << -bitcount;
                    bitcount += 16;
                    if ((bitcount <= 0) && (input_curpos < end_input_pos))
                    {
                        const unsigned int more = static_cast<unsigned int>(input_curpos[0]) |
                                                  (static_cast<unsigned int>(input_curpos[1]) << 8);
                        input_curpos += 2;
                        bitbuf |= more << -bitcount;
                        bitcount += 16;
                    }
                    else if (bitcount <= 0)
                    {
                        error_condition = true;
                    }
                }
            }

            if ((element + static_cast<int>(run_length)) > num_elements)
            {
                run_length = static_cast<unsigned int>(num_elements - element);
            }

            if (run_length != 0)
            {
                ::memset(len + element, 0, run_length);
                element += static_cast<int>(run_length);
            }

            --element;
        }
        else if (symbol == 18)
        {
            unsigned int run_length = (bitbuf >> 27) + 20;
            bitcount -= 5;
            bitbuf <<= 5;
            if (bitcount <= 0)
            {
                if (input_curpos >= end_input_pos)
                {
                    error_condition = true;
                }
                else
                {
                    const unsigned int next = static_cast<unsigned int>(input_curpos[0]) |
                                              (static_cast<unsigned int>(input_curpos[1]) << 8);
                    input_curpos += 2;
                    bitbuf |= next << -bitcount;
                    bitcount += 16;
                    if ((bitcount <= 0) && (input_curpos < end_input_pos))
                    {
                        const unsigned int more = static_cast<unsigned int>(input_curpos[0]) |
                                                  (static_cast<unsigned int>(input_curpos[1]) << 8);
                        input_curpos += 2;
                        bitbuf |= more << -bitcount;
                        bitcount += 16;
                    }
                    else if (bitcount <= 0)
                    {
                        error_condition = true;
                    }
                }
            }

            if ((element + static_cast<int>(run_length)) > num_elements)
            {
                run_length = static_cast<unsigned int>(num_elements - element);
            }

            if (run_length != 0)
            {
                ::memset(len + element, 0, run_length);
                element += static_cast<int>(run_length);
            }

            --element;
        }
        else if (symbol == 19)
        {
            unsigned int run_length = (bitbuf >> 31) + 4;
            bitcount -= 1;
            bitbuf <<= 1;
            if (bitcount <= 0)
            {
                if (input_curpos >= end_input_pos)
                {
                    error_condition = true;
                }
                else
                {
                    const unsigned int next = static_cast<unsigned int>(input_curpos[0]) |
                                              (static_cast<unsigned int>(input_curpos[1]) << 8);
                    input_curpos += 2;
                    bitbuf |= next << -bitcount;
                    bitcount += 16;
                    if ((bitcount <= 0) && (input_curpos < end_input_pos))
                    {
                        const unsigned int more = static_cast<unsigned int>(input_curpos[0]) |
                                                  (static_cast<unsigned int>(input_curpos[1]) << 8);
                        input_curpos += 2;
                        bitbuf |= more << -bitcount;
                        bitcount += 16;
                    }
                    else if (bitcount <= 0)
                    {
                        error_condition = true;
                    }
                }
            }

            int rep_symbol = small_table[bitbuf >> 24];
            if (rep_symbol < 0)
            {
                unsigned int mask = 0x800000;
                do
                {
                    const int index = -rep_symbol;
                    if ((bitbuf & mask) != 0)
                    {
                        rep_symbol = leftright_s[2 * index + 1];
                    }
                    else
                    {
                        rep_symbol = leftright_s[2 * index];
                    }

                    mask >>= 1;
                } while (rep_symbol < 0);
            }

            const unsigned int rep_len = small_bitlen[rep_symbol];
            bitcount -= static_cast<int>(rep_len);
            bitbuf <<= rep_len;
            if (bitcount <= 0)
            {
                if (input_curpos >= end_input_pos)
                {
                    error_condition = true;
                }
                else
                {
                    const unsigned int next = static_cast<unsigned int>(input_curpos[0]) |
                                              (static_cast<unsigned int>(input_curpos[1]) << 8);
                    input_curpos += 2;
                    bitbuf |= next << -bitcount;
                    bitcount += 16;
                    if ((bitcount <= 0) && (input_curpos < end_input_pos))
                    {
                        const unsigned int more = static_cast<unsigned int>(input_curpos[0]) |
                                                  (static_cast<unsigned int>(input_curpos[1]) << 8);
                        input_curpos += 2;
                        bitbuf |= more << -bitcount;
                        bitcount += 16;
                    }
                    else if (bitcount <= 0)
                    {
                        error_condition = true;
                    }
                }
            }

            if ((element + static_cast<int>(run_length)) > num_elements)
            {
                run_length = static_cast<unsigned int>(num_elements - element);
            }

            int value = static_cast<int>(lastlen[element]) - rep_symbol + 17;
            if (value >= 17)
            {
                value -= 17;
            }

            if (run_length != 0)
            {
                ::memset(len + element, value, run_length);
                element += static_cast<int>(run_length);
            }

            --element;
        }
        else
        {
            int value = static_cast<int>(lastlen[element]) - symbol + 17;
            if (value >= 17)
            {
                value -= 17;
            }

            len[element] = static_cast<unsigned __int8>(value);
        }

        ++element;
    }

    context->dec_input_curpos = input_curpos;
    context->dec_bitbuf = bitbuf;
    context->dec_bitcount = static_cast<char>(bitcount);
    context->dec_error_condition = error_condition;
    return !error_condition;
}

bool read_main_and_secondary_trees(t_decoder_context* context)
{
    if (!ReadRepTree(context, 0x100, context->dec_main_tree_prev_len, context->dec_main_tree_len))
    {
        return false;
    }

    if (!ReadRepTree(
            context,
            8 * context->dec_num_position_slots,
            context->dec_main_tree_prev_len + 0x100,
            context->dec_main_tree_len + 0x100))
    {
        return false;
    }

    if (!make_table(
            context,
            8 * context->dec_num_position_slots + 0x100,
            context->dec_main_tree_len,
            10,
            context->dec_main_tree_table,
            context->dec_main_tree_left_right))
    {
        return false;
    }

    if (!ReadRepTree(
            context,
            0xF9,
            context->dec_secondary_length_tree_prev_len,
            context->dec_secondary_length_tree_len))
    {
        return false;
    }

    return make_table(
        context,
        0xF9,
        context->dec_secondary_length_tree_len,
        8,
        context->dec_secondary_length_tree_table,
        context->dec_secondary_length_tree_left_right);
}

bool read_aligned_offset_tree(t_decoder_context* context)
{
    for (int i = 0; i < 8; ++i)
    {
        context->dec_aligned_len[i] = static_cast<unsigned __int8>(getbits(context, 3));
    }

    if (context->dec_error_condition)
    {
        return false;
    }

    return make_table_8bit(
        context,
        reinterpret_cast<unsigned __int8*>(context->dec_aligned_len),
        reinterpret_cast<unsigned __int8*>(context->dec_aligned_table));
}
}
