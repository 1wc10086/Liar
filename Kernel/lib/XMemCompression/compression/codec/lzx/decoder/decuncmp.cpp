#include "../../../api/precomp.hpp"

#include <cstring>

namespace XCOMPRESS
{
int decode_uncompressed_block(t_decoder_context* context, long bufpos, int amount_to_decode)
{
    unsigned __int8* input = context->dec_input_curpos;
    const int available = static_cast<int>(context->dec_end_input_pos - input);
    const int end_bufpos = bufpos + amount_to_decode;

    unsigned __int8* const window = context->dec_mem_window;
    unsigned __int8* const destination = window + bufpos;
    unsigned __int8* const destination_end = window + end_bufpos;

    const int bytes_to_copy = (amount_to_decode < available) ? amount_to_decode : available;
    unsigned __int8* const copy_end = destination + bytes_to_copy;

    int current_bufpos = bufpos;
    if (destination < copy_end)
    {
        ::memcpy(destination, input, static_cast<std::size_t>(copy_end - destination));
        input += copy_end - destination;
        current_bufpos += static_cast<int>(copy_end - destination);
    }

    if (copy_end != destination_end)
    {
        return -1;
    }

    context->dec_input_curpos = input;

    const int wrap_copy = ((end_bufpos > 0x101) ? 0x101 : end_bufpos) - bufpos;
    unsigned __int8* const wrap_destination = window + context->dec_window_size + bufpos;
    if (wrap_copy > 0)
    {
        ::memcpy(
            wrap_destination,
            wrap_destination - context->dec_window_size,
            static_cast<std::size_t>(wrap_copy));
    }

    context->dec_bufpos = current_bufpos & context->dec_window_mask;
    return 0;
}

bool handle_beginning_of_uncompressed_block(t_decoder_context* context)
{
    context->dec_input_curpos -= 2;

    unsigned __int8* input = context->dec_input_curpos;
    if ((input + 4) >= context->dec_end_input_pos)
    {
        return false;
    }

    for (int i = 0; i < 3; ++i)
    {
        const unsigned int value =
            (static_cast<unsigned int>(input[0]) << 0) |
            (static_cast<unsigned int>(input[1]) << 8) |
            (static_cast<unsigned int>(input[2]) << 16) |
            (static_cast<unsigned int>(input[3]) << 24);
        context->dec_last_matchpos_offset[i] = value;
        input += 4;
    }

    context->dec_input_curpos = input;
    return true;
}
}
