#include "../../../api/precomp.hpp"

namespace XCOMPRESS
{
void count_len(t_encoder_context* context, short i)
{
    if (i >= context->enc_tree_n)
    {
        ++context->enc_depth;
        const std::size_t index = static_cast<std::size_t>(static_cast<unsigned short>(i)) * 2;
        count_len(context, static_cast<short>(context->enc_tree_leftright[index]));
        count_len(context, static_cast<short>(context->enc_tree_leftright[index + 1]));
        --context->enc_depth;
        return;
    }

    int depth = context->enc_depth;
    if (depth > 16)
    {
        depth = 16;
    }

    ++context->enc_tree_len_cnt[depth];
}

void make_len(t_encoder_context* context, short root)
{
    for (int i = 0; i < 17; ++i)
    {
        context->enc_tree_len_cnt[i] = 0;
    }

    count_len(context, root);

    unsigned short total = 0;
    for (int i = 16; i != 0; --i)
    {
        total = static_cast<unsigned short>(
            total + (context->enc_tree_len_cnt[i] << (16 - i)));
    }

    while (total != 0)
    {
        --context->enc_tree_len_cnt[16];

        int i = 15;
        while ((i != 0) && (context->enc_tree_len_cnt[i] == 0))
        {
            --i;
        }

        if (i != 0)
        {
            context->enc_tree_len_cnt[i + 1] += 2;
            --context->enc_tree_len_cnt[i];
        }

        total = static_cast<unsigned short>(total - 1);
    }

    for (unsigned char len = 16; len != 0; --len)
    {
        short count = static_cast<short>(context->enc_tree_len_cnt[len]);
        while (count-- > 0)
        {
            const unsigned short symbol = *context->enc_tree_sortptr++;
            context->enc_len[symbol] = len;
        }
    }
}

void downheap(t_encoder_context* context, short i)
{
    short k = i;
    const unsigned short value = context->enc_tree_heap[static_cast<unsigned short>(i)];
    short j = static_cast<short>(static_cast<unsigned short>(i) * 2);

    while (j <= context->enc_tree_heapsize)
    {
        if ((j < context->enc_tree_heapsize) &&
            (context->enc_tree_freq[context->enc_tree_heap[static_cast<unsigned short>(j)]] >
             context->enc_tree_freq[context->enc_tree_heap[static_cast<unsigned short>(j + 1)]]))
        {
            ++j;
        }

        const unsigned short child = context->enc_tree_heap[static_cast<unsigned short>(j)];
        if (context->enc_tree_freq[value] <= context->enc_tree_freq[child])
        {
            break;
        }

        context->enc_tree_heap[static_cast<unsigned short>(k)] = child;
        k = j;
        j = static_cast<short>(j * 2);
    }

    context->enc_tree_heap[static_cast<unsigned short>(k)] = value;
}

void make_code(t_encoder_context* context, int n, char* len, unsigned short* code)
{
    unsigned short start[18]{};

    for (int i = 1; i <= 16; ++i)
    {
        start[i + 1] = static_cast<unsigned short>(
            2 * (start[i] + context->enc_tree_len_cnt[i]));
    }

    for (int i = 0; i < n; ++i)
    {
        const unsigned char symbol_len = static_cast<unsigned char>(len[i]);
        code[i] = start[symbol_len];
        ++start[symbol_len];
    }
}

void make_tree2(t_encoder_context* context, short avail, unsigned short* freqparm, unsigned short* codeparm)
{
    for (short i = static_cast<short>(context->enc_tree_heapsize >> 1); i >= 1; --i)
    {
        downheap(context, i);
    }

    context->enc_tree_sortptr = codeparm;
    short next = avail;

    do
    {
        const short first = static_cast<short>(context->enc_tree_heap[1]);
        if (first < context->enc_tree_n)
        {
            *context->enc_tree_sortptr++ = static_cast<unsigned short>(first);
        }

        const short last = context->enc_tree_heapsize;
        context->enc_tree_heap[1] = context->enc_tree_heap[static_cast<unsigned short>(last)];
        context->enc_tree_heapsize = static_cast<short>(last - 1);
        downheap(context, 1);

        const short second = static_cast<short>(context->enc_tree_heap[1]);
        if (second < context->enc_tree_n)
        {
            *context->enc_tree_sortptr++ = static_cast<unsigned short>(second);
        }

        const short parent = next++;
        freqparm[static_cast<unsigned short>(parent)] = static_cast<unsigned short>(
            freqparm[static_cast<unsigned short>(first)] +
            freqparm[static_cast<unsigned short>(second)]);

        context->enc_tree_heap[1] = static_cast<unsigned short>(parent);
        downheap(context, 1);

        const std::size_t child_index = static_cast<std::size_t>(static_cast<unsigned short>(parent)) * 2;
        context->enc_tree_leftright[child_index + 1] = static_cast<unsigned short>(second);
        context->enc_tree_leftright[child_index] = static_cast<unsigned short>(first);
    }
    while (context->enc_tree_heapsize > 1);

    context->enc_tree_sortptr = codeparm;
    for (int i = 0; i < 17; ++i)
    {
        context->enc_tree_len_cnt[i] = 0;
    }

    count_len(context, static_cast<short>(next - 1));

    unsigned short total = 0;
    for (int i = 16; i != 0; --i)
    {
        total = static_cast<unsigned short>(
            total + (context->enc_tree_len_cnt[i] << (16 - i)));
    }

    while (total != 0)
    {
        --context->enc_tree_len_cnt[16];

        int i = 15;
        while ((i != 0) && (context->enc_tree_len_cnt[i] == 0))
        {
            --i;
        }

        if (i != 0)
        {
            context->enc_tree_len_cnt[i + 1] += 2;
            --context->enc_tree_len_cnt[i];
        }

        total = static_cast<unsigned short>(total - 1);
    }

    for (unsigned char len = 16; len != 0; --len)
    {
        short count = static_cast<short>(context->enc_tree_len_cnt[len]);
        while (count-- > 0)
        {
            const unsigned short symbol = *context->enc_tree_sortptr++;
            context->enc_len[symbol] = len;
        }
    }
}

void make_tree(
    t_encoder_context* context,
    int nparm,
    unsigned short* freqparm,
    unsigned __int8* lenparm,
    unsigned short* codeparm,
    bool make_codes)
{
    const int n = nparm;

redo_tree:
    context->enc_tree_n = n;
    context->enc_tree_freq = freqparm;
    context->enc_len = lenparm;
    context->enc_depth = 0;
    context->enc_tree_heapsize = 0;
    context->enc_tree_heap[1] = 0;

    for (short i = 0; i < n; ++i)
    {
        context->enc_len[static_cast<unsigned short>(i)] = 0;
        if (freqparm[static_cast<unsigned short>(i)] != 0)
        {
            context->enc_tree_heap[++context->enc_tree_heapsize] = static_cast<unsigned short>(i);
        }
    }

    if (context->enc_tree_heapsize < 2)
    {
        if (context->enc_tree_heapsize == 0)
        {
            codeparm[context->enc_tree_heap[1]] = 0;
            return;
        }

        if (context->enc_tree_heap[1] != 0)
        {
            freqparm[0] = 1;
        }
        else
        {
            freqparm[1] = 1;
        }

        goto redo_tree;
    }

    make_tree2(context, static_cast<short>(n), freqparm, codeparm);

    if (!make_codes)
    {
        return;
    }

    unsigned short start[18]{};
    for (int i = 1; i <= 16; ++i)
    {
        start[i + 1] = static_cast<unsigned short>(
            2 * (start[i] + context->enc_tree_len_cnt[i]));
    }

    for (int i = 0; i < n; ++i)
    {
        const unsigned char len = lenparm[i];
        codeparm[i] = start[len];
        ++start[len];
    }
}
}
