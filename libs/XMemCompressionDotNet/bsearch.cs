using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace XMemCompressionDotNet;

internal static partial class XCompress
{
    private const int kMinMatch = 2;
    private const int kMaxMatch = 0x101;
    private const int kBreakLength = 50;
    private const int kChunkSize = 0x8000;
    private const uint kNumRepeatedOffsets = 3;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ushort load_search_key(t_encoder_context context, uint bufpos)
    {
        byte[] window = context.enc_RealMemWindow;
        int offset = context.enc_MemWindowOffset + (int)bufpos;
        ref byte windowBase = ref MemoryMarshal.GetArrayDataReference(window);
        return (ushort)(Unsafe.Add(ref windowBase, offset) | (Unsafe.Add(ref windowBase, offset + 1) << 8));
    }

    internal static int binary_search_findmatch(t_encoder_context context, uint bufPos)
    {
        byte[] window = context.enc_RealMemWindow;
        uint[] treeRoot = context.enc_tree_root;
        uint[] left = context.enc_Left;
        uint[] right = context.enc_Right;
        uint[] matchpos = context.enc_matchpos_table;
        int windowOffset = context.enc_MemWindowOffset;
        ref byte windowBase = ref MemoryMarshal.GetArrayDataReference(window);
        ref uint treeRootBase = ref MemoryMarshal.GetArrayDataReference(treeRoot);
        ref uint leftBase = ref MemoryMarshal.GetArrayDataReference(left);
        ref uint rightBase = ref MemoryMarshal.GetArrayDataReference(right);
        ref uint matchposBase = ref MemoryMarshal.GetArrayDataReference(matchpos);
        uint bufpos = bufPos;
        int bufIndex = windowOffset + (int)bufpos;
        ushort key =
            (ushort)(Unsafe.Add(ref windowBase, bufIndex) |
                     (ushort)(Unsafe.Add(ref windowBase, bufIndex + 1) << 8));
        ref uint root = ref Unsafe.Add(ref treeRootBase, key);
        uint ptr = root;
        root = bufpos;

        uint end_pos = bufpos - context.enc_window_size + 4;
        if (ptr <= end_pos)
        {
            Unsafe.Add(ref rightBase, bufIndex) = 0;
            Unsafe.Add(ref leftBase, bufIndex) = 0;
            return 0;
        }

        int clen = 2;
        int match_length = 2;
        int small_len = 2;
        int big_len = 2;
        Unsafe.Add(ref matchposBase, 2) = bufpos - ptr + (kNumRepeatedOffsets - 1);

        ref uint small_ptr = ref Unsafe.Add(ref leftBase, bufIndex);
        ref uint big_ptr = ref Unsafe.Add(ref rightBase, bufIndex);

        while (true)
        {
            int same = clen;
            int val = Unsafe.Add(ref windowBase, windowOffset + (int)ptr + clen) -
                      Unsafe.Add(ref windowBase, bufIndex + clen);
            if (val == 0)
            {
                uint scan = bufpos + (uint)clen;
                do
                {
                    ++same;
                    ++scan;
                    if (same >= kMaxMatch)
                    {
                        do
                        {
                            Unsafe.Add(ref matchposBase, ++match_length) = bufpos - ptr + (kNumRepeatedOffsets - 1);
                        }
                        while (match_length < same);

                        int ptrIndex = windowOffset + (int)ptr;
                        small_ptr = Unsafe.Add(ref leftBase, ptrIndex);
                        big_ptr = Unsafe.Add(ref rightBase, ptrIndex);
                        goto end_bsearch;
                    }

                    val = Unsafe.Add(ref windowBase, windowOffset + (int)ptr + same) -
                          Unsafe.Add(ref windowBase, windowOffset + (int)scan);
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
                            Unsafe.Add(ref matchposBase, ++match_length) =
                                bufpos - ptr + (kNumRepeatedOffsets - 1);
                        }
                        while (match_length < same);

                        if (same >= kBreakLength)
                        {
                            int ptrIndex = windowOffset + (int)ptr;
                            small_ptr = Unsafe.Add(ref leftBase, ptrIndex);
                            big_ptr = Unsafe.Add(ref rightBase, ptrIndex);
                            goto end_bsearch;
                        }
                    }

                    big_len = same;
                    clen = small_len < big_len ? small_len : big_len;
                }

                big_ptr = ptr;
                big_ptr = ref Unsafe.Add(ref leftBase, windowOffset + (int)ptr);
                ptr = big_ptr;
            }
            else
            {
                if (same > small_len)
                {
                    if (same > match_length)
                    {
                        do
                        {
                            Unsafe.Add(ref matchposBase, ++match_length) =
                                bufpos - ptr + (kNumRepeatedOffsets - 1);
                        }
                        while (match_length < same);

                        if (same >= kBreakLength)
                        {
                            int ptrIndex = windowOffset + (int)ptr;
                            small_ptr = Unsafe.Add(ref leftBase, ptrIndex);
                            big_ptr = Unsafe.Add(ref rightBase, ptrIndex);
                            goto end_bsearch;
                        }
                    }

                    small_len = same;
                    clen = small_len < big_len ? small_len : big_len;
                }

                small_ptr = ptr;
                small_ptr = ref Unsafe.Add(ref rightBase, windowOffset + (int)ptr);
                ptr = small_ptr;
            }

            if (ptr <= end_pos)
            {
                small_ptr = 0;
                big_ptr = 0;
                break;
            }
        }

    end_bsearch:
        int best_repeated_offset = 0;
        int i = 0;

        for (i = 0; i < match_length; ++i)
        {
            if (Unsafe.Add(ref windowBase, bufIndex + i) !=
                Unsafe.Add(ref windowBase, windowOffset + (int)(bufpos - context.enc_last_matchpos_offset[0]) + i))
            {
                break;
            }
        }

        best_repeated_offset = i;
        if (i >= kMinMatch)
        {
            do
            {
                Unsafe.Add(ref matchposBase, i) = 0;
            }
            while (--i >= kMinMatch);

            if (best_repeated_offset > kBreakLength)
            {
                goto quick_return;
            }
        }

        for (i = 0; i < match_length; ++i)
        {
            if (Unsafe.Add(ref windowBase, bufIndex + i) !=
                Unsafe.Add(ref windowBase, windowOffset + (int)(bufpos - context.enc_last_matchpos_offset[1]) + i))
            {
                break;
            }
        }

        if (i > best_repeated_offset)
        {
            do
            {
                Unsafe.Add(ref matchposBase, ++best_repeated_offset) = 1;
            }
            while (best_repeated_offset < i);
        }

        for (i = 0; i < match_length; ++i)
        {
            if (Unsafe.Add(ref windowBase, bufIndex + i) !=
                Unsafe.Add(ref windowBase, windowOffset + (int)(bufpos - context.enc_last_matchpos_offset[2]) + i))
            {
                break;
            }
        }

        if (i > best_repeated_offset)
        {
            do
            {
                Unsafe.Add(ref matchposBase, ++best_repeated_offset) = 2;
            }
            while (best_repeated_offset < i);
        }

    quick_return:
        int bytes_to_boundary = (kChunkSize - 1) - ((int)bufpos & (kChunkSize - 1));
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

    internal static void quick_insert_bsearch_findmatch(t_encoder_context context, uint bufPos, uint end_pos)
    {
        byte[] window = context.enc_RealMemWindow;
        uint[] treeRoot = context.enc_tree_root;
        uint[] left = context.enc_Left;
        uint[] right = context.enc_Right;
        int windowOffset = context.enc_MemWindowOffset;
        ref byte windowBase = ref MemoryMarshal.GetArrayDataReference(window);
        ref uint treeRootBase = ref MemoryMarshal.GetArrayDataReference(treeRoot);
        ref uint leftBase = ref MemoryMarshal.GetArrayDataReference(left);
        ref uint rightBase = ref MemoryMarshal.GetArrayDataReference(right);
        uint bufpos = bufPos;
        int bufIndex = windowOffset + (int)bufpos;
        ushort key = (ushort)(Unsafe.Add(ref windowBase, bufIndex) | (Unsafe.Add(ref windowBase, bufIndex + 1) << 8));
        ref uint root = ref Unsafe.Add(ref treeRootBase, key);
        uint ptr = root;
        root = bufpos;

        if (ptr <= end_pos)
        {
            Unsafe.Add(ref leftBase, bufIndex) = 0;
            Unsafe.Add(ref rightBase, bufIndex) = 0;
            return;
        }

        int clen = 2;
        int small_len = 2;
        int big_len = 2;
        ref uint small_ptr = ref Unsafe.Add(ref leftBase, bufIndex);
        ref uint big_ptr = ref Unsafe.Add(ref rightBase, bufIndex);

        do
        {
            int same = clen;
            uint a = ptr + (uint)clen;
            uint b = bufpos + (uint)clen;

            int val;
            while ((val = Unsafe.Add(ref windowBase, windowOffset + (int)a++) -
                          Unsafe.Add(ref windowBase, windowOffset + (int)b++)) == 0)
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
                        int ptrIndex = windowOffset + (int)ptr;
                        small_ptr = Unsafe.Add(ref leftBase, ptrIndex);
                        big_ptr = Unsafe.Add(ref rightBase, ptrIndex);
                        return;
                    }

                    big_len = same;
                    clen = small_len < big_len ? small_len : big_len;
                }

                big_ptr = ptr;
                big_ptr = ref Unsafe.Add(ref leftBase, windowOffset + (int)ptr);
                ptr = big_ptr;
            }
            else
            {
                if (same > small_len)
                {
                    if (same >= kBreakLength)
                    {
                        int ptrIndex = windowOffset + (int)ptr;
                        small_ptr = Unsafe.Add(ref leftBase, ptrIndex);
                        big_ptr = Unsafe.Add(ref rightBase, ptrIndex);
                        return;
                    }

                    small_len = same;
                    clen = small_len < big_len ? small_len : big_len;
                }

                small_ptr = ptr;
                small_ptr = ref Unsafe.Add(ref rightBase, windowOffset + (int)ptr);
                ptr = small_ptr;
            }
        }
        while (ptr > end_pos);

        small_ptr = 0;
        big_ptr = 0;
    }

    internal static void binary_search_remove_node(t_encoder_context context, uint bufPos, uint end_pos)
    {
        uint[] treeRoot = context.enc_tree_root;
        uint[] left = context.enc_Left;
        uint[] right = context.enc_Right;
        int windowOffset = context.enc_MemWindowOffset;
        ref uint treeRootBase = ref MemoryMarshal.GetArrayDataReference(treeRoot);
        ref uint leftBase = ref MemoryMarshal.GetArrayDataReference(left);
        ref uint rightBase = ref MemoryMarshal.GetArrayDataReference(right);
        uint bufpos = bufPos;
        ref uint link = ref Unsafe.Add(ref treeRootBase, load_search_key(context, bufpos));
        if (link != bufPos)
        {
            return;
        }

        if (link <= end_pos)
        {
            link = 0;
            int bufIndex = windowOffset + (int)bufpos;
            Unsafe.Add(ref leftBase, bufIndex) = 0;
            Unsafe.Add(ref rightBase, bufIndex) = 0;
            return;
        }

        uint ptr = bufpos;
        uint left_node_pos = Unsafe.Add(ref leftBase, windowOffset + (int)ptr);
        if (left_node_pos <= end_pos)
        {
            left_node_pos = 0;
            Unsafe.Add(ref leftBase, windowOffset + (int)ptr) = 0;
        }

        uint right_node_pos = Unsafe.Add(ref rightBase, windowOffset + (int)ptr);
        if (right_node_pos <= end_pos)
        {
            right_node_pos = 0;
            Unsafe.Add(ref rightBase, windowOffset + (int)ptr) = 0;
        }

        while (true)
        {
            if (left_node_pos > right_node_pos)
            {
                if (left_node_pos <= end_pos)
                {
                    left_node_pos = 0;
                }

                ptr = link = left_node_pos;
                if (ptr == 0)
                {
                    break;
                }

                left_node_pos = Unsafe.Add(ref rightBase, windowOffset + (int)ptr);
                link = ref Unsafe.Add(ref rightBase, windowOffset + (int)ptr);
            }
            else
            {
                if (right_node_pos <= end_pos)
                {
                    right_node_pos = 0;
                }

                ptr = link = right_node_pos;
                if (ptr == 0)
                {
                    break;
                }

                right_node_pos = Unsafe.Add(ref leftBase, windowOffset + (int)ptr);
                link = ref Unsafe.Add(ref leftBase, windowOffset + (int)ptr);
            }
        }
    }
}
