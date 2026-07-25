#include "../../../api/precomp.hpp"

#include <cstring>

namespace XCOMPRESS
{
bool make_table(
    t_decoder_context*,
    int num_elements,
    const unsigned __int8* len,
    unsigned __int8 nbits,
    short* table,
    short* left_right)
{
    unsigned int count[17]{};
    unsigned int start[18]{};

    for (int i = 0; i < num_elements; ++i)
    {
        ++count[len[i]];
    }

    start[1] = 0;
    for (unsigned int i = 1; i <= 16; ++i)
    {
        start[i + 1] = start[i] + (count[i] << (16 - i));
    }

    if (start[17] != 0x10000)
    {
        if (start[17] != 0)
        {
            return false;
        }

        ::memset(table, 0, 2u * (1u << nbits));
        return true;
    }

    const unsigned int tablebits = nbits;
    const unsigned int shift = 16 - tablebits;

    for (unsigned int i = 1; i <= tablebits; ++i)
    {
        start[i] >>= shift;
        count[i] = 1u << (tablebits - i);
    }

    for (unsigned int i = tablebits + 1; i <= 16; ++i)
    {
        count[i] = 1u << (16 - i);
    }

    const unsigned int start_index = start[tablebits + 1] >> shift;
    if (start_index != 0x10000)
    {
        ::memset(table + start_index, 0, 2u * ((1u << tablebits) - start_index));
    }

    unsigned int next_node = static_cast<unsigned int>(num_elements);
    for (int symbol = 0; symbol < num_elements; ++symbol)
    {
        const unsigned int bitlen = len[symbol];
        if (bitlen == 0)
        {
            continue;
        }

        const unsigned int position = start[bitlen];
        const unsigned int next = position + count[bitlen];
        if (bitlen <= tablebits)
        {
            if (next > (1u << tablebits))
            {
                return false;
            }

            for (unsigned int index = position; index < next; ++index)
            {
                table[index] = static_cast<short>(symbol);
            }

            start[bitlen] = next;
            continue;
        }

        start[bitlen] = next;

        short* node = table + (position >> shift);
        unsigned int code = position << tablebits;
        unsigned int remaining = bitlen - tablebits;

        while (remaining != 0)
        {
            if (*node == 0)
            {
                left_right[2 * next_node] = 0;
                left_right[2 * next_node + 1] = 0;
                *node = static_cast<short>(-static_cast<short>(next_node));
                ++next_node;
            }

            if ((code & 0x8000u) == 0)
            {
                node = &left_right[-2 * (*node)];
            }
            else
            {
                node = &left_right[1 - 2 * (*node)];
            }

            code <<= 1;
            --remaining;
        }

        *node = static_cast<short>(symbol);
    }

    return true;
}

bool make_table_8bit(t_decoder_context*, unsigned __int8* len, unsigned __int8* table)
{
    unsigned short count[17]{};
    unsigned short start[18]{};

    for (int i = 0; i < 8; ++i)
    {
        ++count[len[i]];
    }

    start[1] = 0;
    for (unsigned short i = 1; i <= 16; ++i)
    {
        start[i + 1] = static_cast<unsigned short>(
            start[i] + static_cast<unsigned short>(count[i] << (16 - i)));
    }

    if (start[17] != 0)
    {
        return false;
    }

    for (unsigned short i = 1; i <= 7; ++i)
    {
        start[i] = static_cast<unsigned short>(start[i] >> 9);
        count[i] = static_cast<unsigned short>(1u << (7 - i));
    }

    for (unsigned short i = 8; i <= 16; ++i)
    {
        count[i] = static_cast<unsigned short>(1u << (16 - i));
    }

    ::memset(table, 0, 0x80u);

    for (unsigned int symbol = 0; symbol < 8; ++symbol)
    {
        const unsigned int bitlen = len[symbol];
        if (bitlen == 0)
        {
            continue;
        }

        const unsigned short position = start[bitlen];
        const unsigned short next = static_cast<unsigned short>(position + count[bitlen]);
        if (next > 0x80u)
        {
            return false;
        }

        if (position < next)
        {
            ::memset(table + position, static_cast<int>(symbol), count[bitlen]);
        }

        start[bitlen] = next;
    }

    return true;
}
}
