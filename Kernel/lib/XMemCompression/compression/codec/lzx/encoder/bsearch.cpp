#include "../../../api/precomp.hpp"

#include <cstring>

namespace XCOMPRESS
{
namespace
{
constexpr int kMinMatch = 2;
constexpr int kMaxMatch = 0x101;
constexpr int kBreakLength = 50;
constexpr int kChunkSize = 0x8000;
constexpr unsigned int kNumRepeatedOffsets = 3;

unsigned short load_search_key(const t_encoder_context* context, unsigned long bufpos)
{
    unsigned short key = 0;
    std::memcpy(&key, context->enc_MemWindow + bufpos, sizeof(key));
    return key;
}
}

long binary_search_findmatch(t_encoder_context* context, unsigned long BufPos)
{
    const unsigned int bufpos = static_cast<unsigned int>(BufPos);
    const unsigned short key =
        static_cast<unsigned short>(context->enc_MemWindow[bufpos]) |
        static_cast<unsigned short>(
            static_cast<unsigned short>(context->enc_MemWindow[bufpos + 1]) << 8);
    unsigned int ptr = context->enc_tree_root[key];
    context->enc_tree_root[key] = bufpos;

    const unsigned int end_pos = bufpos - context->enc_window_size + 4;
    if (ptr <= end_pos)
    {
        context->enc_Right[bufpos] = 0;
        context->enc_Left[bufpos] = 0;
        return 0;
    }

    int clen = 2;
    int match_length = 2;
    int small_len = 2;
    int big_len = 2;
    context->enc_matchpos_table[2] = bufpos - ptr + (kNumRepeatedOffsets - 1);

    unsigned int* small_ptr = &context->enc_Left[bufpos];
    unsigned int* big_ptr = &context->enc_Right[bufpos];

    for (;;)
    {
        int same = clen;
        int val = static_cast<int>(context->enc_MemWindow[ptr + static_cast<unsigned int>(clen)]) -
                  static_cast<int>(context->enc_MemWindow[bufpos + static_cast<unsigned int>(clen)]);
        if (val == 0)
        {
            unsigned int scan = bufpos + static_cast<unsigned int>(clen);
            do
            {
                ++same;
                ++scan;
                if (same >= kMaxMatch)
                {
                    do
                    {
                        context->enc_matchpos_table[++match_length] = bufpos - ptr + (kNumRepeatedOffsets - 1);
                    }
                    while (match_length < same);

                    *small_ptr = context->enc_Left[ptr];
                    *big_ptr = context->enc_Right[ptr];
                    goto end_bsearch;
                }

                val = static_cast<int>(context->enc_MemWindow[ptr + static_cast<unsigned int>(same)]) -
                      static_cast<int>(context->enc_MemWindow[scan]);
            }
            while (val == 0);
        }

        if (val < 0)
        {
            if (same > big_len)
            {
                if (same > match_length)
                {
                    do
                    {
                        context->enc_matchpos_table[++match_length] =
                            bufpos - ptr + (kNumRepeatedOffsets - 1);
                    }
                    while (match_length < same);

                    if (same >= kBreakLength)
                    {
                        *small_ptr = context->enc_Left[ptr];
                        *big_ptr = context->enc_Right[ptr];
                        goto end_bsearch;
                    }
                }

                big_len = same;
                clen = (small_len < big_len) ? small_len : big_len;
            }

            *big_ptr = ptr;
            big_ptr = &context->enc_Left[ptr];
            ptr = *big_ptr;
        }
        else
        {
            if (same > small_len)
            {
                if (same > match_length)
                {
                    do
                    {
                        context->enc_matchpos_table[++match_length] =
                            bufpos - ptr + (kNumRepeatedOffsets - 1);
                    }
                    while (match_length < same);

                    if (same >= kBreakLength)
                    {
                        *small_ptr = context->enc_Left[ptr];
                        *big_ptr = context->enc_Right[ptr];
                        goto end_bsearch;
                    }
                }

                small_len = same;
                clen = (small_len < big_len) ? small_len : big_len;
            }

            *small_ptr = ptr;
            small_ptr = &context->enc_Right[ptr];
            ptr = *small_ptr;
        }

        if (ptr <= end_pos)
        {
            *small_ptr = 0;
            *big_ptr = 0;
            break;
        }
    }

end_bsearch:
    int best_repeated_offset = 0;
    int i = 0;

    for (i = 0; i < match_length; ++i)
    {
        if (context->enc_MemWindow[bufpos + i] !=
            context->enc_MemWindow[bufpos - context->enc_last_matchpos_offset[0] + i])
        {
            break;
        }
    }

    best_repeated_offset = i;
    if (i >= kMinMatch)
    {
        do
        {
            context->enc_matchpos_table[i] = 0;
        }
        while (--i >= kMinMatch);

        if (best_repeated_offset > kBreakLength)
        {
            goto quick_return;
        }
    }

    for (i = 0; i < match_length; ++i)
    {
        if (context->enc_MemWindow[bufpos + i] !=
            context->enc_MemWindow[bufpos - context->enc_last_matchpos_offset[1] + i])
        {
            break;
        }
    }

    if (i > best_repeated_offset)
    {
        do
        {
            context->enc_matchpos_table[++best_repeated_offset] = 1;
        }
        while (best_repeated_offset < i);
    }

    for (i = 0; i < match_length; ++i)
    {
        if (context->enc_MemWindow[bufpos + i] !=
            context->enc_MemWindow[bufpos - context->enc_last_matchpos_offset[2] + i])
        {
            break;
        }
    }

    if (i > best_repeated_offset)
    {
        do
        {
            context->enc_matchpos_table[++best_repeated_offset] = 2;
        }
        while (best_repeated_offset < i);
    }

quick_return:
    const int bytes_to_boundary = (kChunkSize - 1) - (static_cast<int>(bufpos) & (kChunkSize - 1));
    if (match_length > bytes_to_boundary)
    {
        match_length = bytes_to_boundary;
        if (match_length < kMinMatch)
        {
            match_length = 0;
        }
    }

    return match_length;
}

void quick_insert_bsearch_findmatch(t_encoder_context* context, unsigned long BufPos, unsigned long end_pos)
{
    const unsigned int bufpos = static_cast<unsigned int>(BufPos);
    const unsigned short key = load_search_key(context, bufpos);
    unsigned int ptr = context->enc_tree_root[key];
    context->enc_tree_root[key] = bufpos;

    if (ptr <= end_pos)
    {
        context->enc_Left[bufpos] = 0;
        context->enc_Right[bufpos] = 0;
        return;
    }

    int clen = 2;
    int small_len = 2;
    int big_len = 2;
    unsigned int* small_ptr = &context->enc_Left[bufpos];
    unsigned int* big_ptr = &context->enc_Right[bufpos];

    do
    {
        int same = clen;
        unsigned int a = ptr + static_cast<unsigned int>(clen);
        unsigned int b = bufpos + static_cast<unsigned int>(clen);

        int val;
        while ((val = static_cast<int>(context->enc_MemWindow[a++]) -
                      static_cast<int>(context->enc_MemWindow[b++])) == 0)
        {
            if (++same >= kBreakLength)
            {
                break;
            }
        }

        if (val < 0)
        {
            if (same > big_len)
            {
                if (same >= kBreakLength)
                {
                    *small_ptr = context->enc_Left[ptr];
                    *big_ptr = context->enc_Right[ptr];
                    return;
                }

                big_len = same;
                clen = (small_len < big_len) ? small_len : big_len;
            }

            *big_ptr = ptr;
            big_ptr = &context->enc_Left[ptr];
            ptr = *big_ptr;
        }
        else
        {
            if (same > small_len)
            {
                if (same >= kBreakLength)
                {
                    *small_ptr = context->enc_Left[ptr];
                    *big_ptr = context->enc_Right[ptr];
                    return;
                }

                small_len = same;
                clen = (small_len < big_len) ? small_len : big_len;
            }

            *small_ptr = ptr;
            small_ptr = &context->enc_Right[ptr];
            ptr = *small_ptr;
        }
    }
    while (ptr > end_pos);

    *small_ptr = 0;
    *big_ptr = 0;
}

void binary_search_remove_node(t_encoder_context* context, unsigned long BufPos, unsigned long end_pos)
{
    const unsigned int bufpos = static_cast<unsigned int>(BufPos);
    unsigned int* link = &context->enc_tree_root[load_search_key(context, bufpos)];
    if (*link != BufPos)
    {
        return;
    }

    if (*link <= end_pos)
    {
        *link = 0;
        context->enc_Left[bufpos] = 0;
        context->enc_Right[bufpos] = 0;
        return;
    }

    unsigned int ptr = bufpos;
    unsigned int left_node_pos = context->enc_Left[ptr];
    if (left_node_pos <= end_pos)
    {
        left_node_pos = context->enc_Left[ptr] = 0;
    }

    unsigned int right_node_pos = context->enc_Right[ptr];
    if (right_node_pos <= end_pos)
    {
        right_node_pos = context->enc_Right[ptr] = 0;
    }

    for (;;)
    {
        if (left_node_pos > right_node_pos)
        {
            if (left_node_pos <= end_pos)
            {
                left_node_pos = 0;
            }

            ptr = *link = left_node_pos;
            if (ptr == 0)
            {
                break;
            }

            left_node_pos = context->enc_Right[ptr];
            link = &context->enc_Right[ptr];
        }
        else
        {
            if (right_node_pos <= end_pos)
            {
                right_node_pos = 0;
            }

            ptr = *link = right_node_pos;
            if (ptr == 0)
            {
                break;
            }

            right_node_pos = context->enc_Left[ptr];
            link = &context->enc_Left[ptr];
        }
    }
}
}
