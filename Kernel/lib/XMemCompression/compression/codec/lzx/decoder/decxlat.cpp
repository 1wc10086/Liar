#include "../../../api/precomp.hpp"

#include <cstring>

namespace XCOMPRESS
{
void init_decoder_translation(t_decoder_context* context)
{
    context->dec_instr_pos = 0;
}

void decoder_translate_e8(t_decoder_context* context, unsigned __int8* mem, int bytes)
{
    if (bytes <= 6)
    {
        context->dec_instr_pos += static_cast<unsigned int>(bytes);
        return;
    }

    unsigned __int8* const base = mem;
    unsigned __int8 tail[6];
    ::memcpy(tail, base + bytes - 6, sizeof(tail));
    ::memset(base + bytes - 6, 0xE8, sizeof(tail));

    const unsigned int limit = context->dec_instr_pos + static_cast<unsigned int>(bytes) - 10;

    unsigned int skipped = 0;
    while (true)
    {
        while (*mem != 0xE8)
        {
            ++mem;
            ++skipped;
        }

        context->dec_instr_pos += skipped;

        const unsigned int instr_pos = context->dec_instr_pos;
        if (instr_pos >= limit)
        {
            break;
        }

        int relative = *reinterpret_cast<int*>(mem + 1);
        const int translation_size = static_cast<int>(context->dec_current_file_size);

        if (relative < translation_size)
        {
            relative -= static_cast<int>(instr_pos);
            *reinterpret_cast<int*>(mem + 1) = relative;
        }
        else
        {
            const int neg_relative = -relative;
            if (static_cast<unsigned int>(neg_relative) <= instr_pos)
            {
                *reinterpret_cast<int*>(mem + 1) = translation_size + relative;
            }
        }

        mem += 5;
        context->dec_instr_pos = instr_pos + 5;
        skipped = 0;
    }

    context->dec_instr_pos = limit + 10;
    ::memcpy(base + bytes - 6, tail, sizeof(tail));
}
}
