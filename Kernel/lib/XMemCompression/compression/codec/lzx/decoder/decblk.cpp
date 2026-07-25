#include "../../../api/precomp.hpp"

#include <cstring>

namespace XCOMPRESS
{
int decode_block(t_decoder_context* context, lzx_block_type block_type, int bufpos, int amount_to_decode)
{
    if (block_type == BLOCKTYPE_ALIGNED)
    {
        return decode_aligned_offset_block(context, bufpos, amount_to_decode);
    }

    if (block_type == BLOCKTYPE_VERBATIM)
    {
        return decode_verbatim_block(context, bufpos, amount_to_decode);
    }

    if (block_type == BLOCKTYPE_UNCOMPRESSED)
    {
        return decode_uncompressed_block(context, bufpos, amount_to_decode);
    }

    return -1;
}

long decode_data(t_decoder_context* context, long bytes_to_decode)
{
    int decoded = 0;
    int remaining = static_cast<int>(bytes_to_decode);

    if (remaining > 0)
    {
        while (remaining > 0)
        {
            if (context->dec_decoder_state == DEC_STATE_START_NEW_BLOCK)
            {
                if (context->dec_first_time_this_group)
                {
                    context->dec_first_time_this_group = false;
                    if (getbits(context, 1) != 0)
                    {
                        const unsigned int high = getbits(context, 16);
                        const unsigned int low = getbits(context, 16);
                        context->dec_current_file_size = low | (high << 16);
                    }
                    else
                    {
                        context->dec_current_file_size = 0;
                    }
                }

                if (context->dec_block_type == BLOCKTYPE_UNCOMPRESSED)
                {
                    context->dec_block_type = BLOCKTYPE_INVALID;
                    initialise_decoder_bitbuf(context);
                }

                context->dec_block_type = static_cast<lzx_block_type>(getbits(context, 3));

                const unsigned int block_size =
                    (getbits(context, 8) << 16) |
                    (getbits(context, 8) << 8) |
                    getbits(context, 8);

                context->dec_original_block_size = static_cast<int>(block_size);
                context->dec_block_size = static_cast<int>(block_size);

                if (context->dec_block_type == BLOCKTYPE_ALIGNED)
                {
                    read_aligned_offset_tree(context);
                }

                if ((context->dec_block_type == BLOCKTYPE_VERBATIM) ||
                    (context->dec_block_type == BLOCKTYPE_ALIGNED))
                {
                    const std::size_t main_tree_size = 8 * context->dec_num_position_slots + 0x100;
                    ::memcpy(context->dec_main_tree_prev_len, context->dec_main_tree_len, main_tree_size);
                    ::memcpy(context->dec_secondary_length_tree_prev_len, context->dec_secondary_length_tree_len, 0xF9u);
                    read_main_and_secondary_trees(context);
                }
                else if (context->dec_block_type == BLOCKTYPE_UNCOMPRESSED)
                {
                    if (!handle_beginning_of_uncompressed_block(context))
                    {
                        return -1;
                    }
                }
                else
                {
                    return -1;
                }

                context->dec_decoder_state = DEC_STATE_DECODING_DATA;
            }

            while (context->dec_block_size > 0)
            {
                if (remaining <= 0)
                {
                    break;
                }

                int amount = remaining;
                if (context->dec_block_size < amount)
                {
                    amount = context->dec_block_size;
                }

                if (amount == 0)
                {
                    return -1;
                }

                if (decode_block(context, context->dec_block_type, context->dec_bufpos, amount) != 0)
                {
                    return -1;
                }

                context->dec_block_size -= amount;
                remaining -= amount;
                decoded += amount;
            }

            if (context->dec_block_size == 0)
            {
                context->dec_decoder_state = DEC_STATE_START_NEW_BLOCK;
            }

            if (remaining == 0)
            {
                initialise_decoder_bitbuf(context);
            }
        }
    }

    const unsigned __int8* source = nullptr;
    if (context->dec_bufpos != 0)
    {
        source = context->dec_mem_window + (context->dec_bufpos - decoded);
    }
    else
    {
        source = context->dec_mem_window + (context->dec_window_size - decoded);
    }

    copy_data_to_output(context, decoded, source);
    return decoded;
}
}
