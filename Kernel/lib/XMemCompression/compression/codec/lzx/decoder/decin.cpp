#include "../../../api/precomp.hpp"

#include <cstdint>

namespace XCOMPRESS
{
void initialise_decoder_bitbuf(t_decoder_context* context)
{
    if (context->dec_block_type == BLOCKTYPE_UNCOMPRESSED)
    {
        return;
    }

    unsigned __int8* const input = context->dec_input_curpos;
    unsigned __int8* const next = input + 4;
    if (next > context->dec_end_input_pos)
    {
        return;
    }

    context->dec_bitbuf =
        (static_cast<unsigned int>(input[1]) << 24) |
        (static_cast<unsigned int>(input[0]) << 16) |
        (static_cast<unsigned int>(input[3]) << 8) |
        static_cast<unsigned int>(input[2]);
    context->dec_bitcount = 16;
    context->dec_input_curpos = next;
}

void init_decoder_input(t_decoder_context* context)
{
    initialise_decoder_bitbuf(context);
}

void fillbuf(t_decoder_context* context, int n)
{
    int bitcount = static_cast<signed char>(context->dec_bitcount) - n;
    context->dec_bitcount = static_cast<char>(bitcount);
    context->dec_bitbuf <<= n;

    if (bitcount > 0)
    {
        return;
    }

    unsigned __int8* input = context->dec_input_curpos;
    unsigned __int8* const end = context->dec_end_input_pos;

    if (input >= end)
    {
        context->dec_error_condition = true;
        return;
    }

    do
    {
        unsigned int bits =
            static_cast<unsigned int>(input[0]) |
            (static_cast<unsigned int>(input[1]) << 8);
        const int shift = -bitcount;

        bitcount += 16;
        context->dec_bitcount = static_cast<char>(bitcount);
        input += 2;

        bits <<= shift;
        context->dec_input_curpos = input;
        context->dec_bitbuf |= bits;

        if (bitcount > 0)
        {
            return;
        }

        if (input >= end)
        {
            context->dec_error_condition = true;
            return;
        }
    }
    while (true);
}

unsigned long getbits(t_decoder_context* context, int n)
{
    const unsigned long result = context->dec_bitbuf >> (32 - n);
    fillbuf(context, n);
    return result;
}
}
