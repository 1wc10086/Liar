#include "../../../api/precomp.hpp"

#include <cstring>

namespace XCOMPRESS
{
void create_slot_lookup_table(t_encoder_context* context)
{
    *reinterpret_cast<unsigned int*>(context->enc_slot_table) = 0x03020100;

    int run_length = 2;
    unsigned char slot = 4;
    int offset = 4;

    while (offset < 0x400)
    {
        if (run_length > 0)
        {
            ::memset(context->enc_slot_table + offset, slot, static_cast<std::size_t>(run_length));
            offset += run_length;
        }

        ++slot;

        if (run_length > 0)
        {
            ::memset(context->enc_slot_table + offset, slot, static_cast<std::size_t>(run_length));
            offset += run_length;
        }

        ++slot;
        run_length *= 2;
    }
}

void create_ones_table(t_encoder_context* context)
{
    for (int value = 0; value < 0x100; ++value)
    {
        unsigned int v = static_cast<unsigned int>(value);
        unsigned char ones = 0;

        while (v != 0)
        {
            if ((v & 1) != 0)
            {
                ++ones;
            }

            v >>= 1;
        }

        context->enc_ones[value] = ones;
    }
}
}
