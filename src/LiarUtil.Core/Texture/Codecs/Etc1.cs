using System;

namespace LiarUtil.Core.Texture.Codecs;

public static class Etc1
{
    private static readonly int[] Table59T58H = { 3, 6, 11, 16, 23, 32, 41, 64 };

    private static readonly int[,] GTable =
    {
        { 2, 8, -2, -8 },
        { 5, 17, -5, -17 },
        { 9, 29, -9, -29 },
        { 13, 42, -13, -42 },
        { 18, 60, -18, -60 },
        { 24, 80, -24, -80 },
        { 33, 106, -33, -106 },
        { 47, 183, -47, -183 }
    };

    private static readonly long[,] GTable256 =
    {
        { 512, 2048, -512, -2048 },
        { 1280, 4352, -1280, -4352 },
        { 2304, 7424, -2304, -7424 },
        { 3328, 10752, -3328, -10752 },
        { 4608, 15360, -4608, -15360 },
        { 6144, 20480, -6144, -20480 },
        { 8448, 27136, -8448, -27136 },
        { 12032, 46848, -12032, -46848 }
    };

    private static readonly uint[,] GId =
    {
        { 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0 },
        { 3, 3, 2, 2, 3, 3, 2, 2, 3, 3, 2, 2, 3, 3, 2, 2 },
        { 5, 5, 5, 5, 5, 5, 5, 5, 4, 4, 4, 4, 4, 4, 4, 4 },
        { 7, 7, 6, 6, 7, 7, 6, 6, 7, 7, 6, 6, 7, 7, 6, 6 }
    };

    private static readonly uint[] GAvg2 =
    {
        0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77,
        0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF
    };

    public static void DecodePart(ulong block, Span<uint> dst)
    {
        block = ConvertByteOrder(block);

        uint br0, br1, bg0, bg1, bb0, bb1;

        if ((block & 0x2) != 0)
        {
            int dr, dg, db;

            uint r0 = (uint)((block & 0xF8000000UL) >> 27);
            uint g0 = (uint)((block & 0x00F80000UL) >> 19);
            uint b0 = (uint)((block & 0x0000F800UL) >> 11);

            dr = ((int)block << 5) >> 29;
            dg = ((int)block << 13) >> 29;
            db = ((int)block << 21) >> 29;

            int r1 = (int)r0 + dr;
            int g1 = (int)g0 + dg;
            int blue1 = (int)b0 + db;

            if (r1 < 0 || r1 > 31)
            {
                DecodeT(block, dst);
                return;
            }

            if (g1 < 0 || g1 > 31)
            {
                DecodeH(block, dst);
                return;
            }

            if (blue1 < 0 || blue1 > 31)
            {
                DecodePlanar(block, dst);
                return;
            }

            br0 = (r0 << 3) | (r0 >> 2);
            br1 = ((uint)r1 << 3) | ((uint)r1 >> 2);
            bg0 = (g0 << 3) | (g0 >> 2);
            bg1 = ((uint)g1 << 3) | ((uint)g1 >> 2);
            bb0 = (b0 << 3) | (b0 >> 2);
            bb1 = ((uint)blue1 << 3) | ((uint)blue1 >> 2);
        }
        else
        {
            br0 = (uint)(((block & 0xF0000000UL) >> 24) | ((block & 0xF0000000UL) >> 28));
            br1 = (uint)(((block & 0x0F000000UL) >> 20) | ((block & 0x0F000000UL) >> 24));
            bg0 = (uint)(((block & 0x00F00000UL) >> 16) | ((block & 0x00F00000UL) >> 20));
            bg1 = (uint)(((block & 0x000F0000UL) >> 12) | ((block & 0x000F0000UL) >> 16));
            bb0 = (uint)(((block & 0x0000F000UL) >> 8) | ((block & 0x0000F000UL) >> 12));
            bb1 = (uint)(((block & 0x00000F00UL) >> 4) | ((block & 0x00000F00UL) >> 8));
        }

        uint tcw0 = (uint)((block & 0xE0) >> 5);
        uint tcw1 = (uint)((block & 0x1C) >> 2);

        uint b1 = (uint)((block >> 32) & 0xFFFF);
        uint b2 = (uint)(block >> 48);

        b1 = (b1 | (b1 << 8)) & 0x00FF00FF;
        b1 = (b1 | (b1 << 4)) & 0x0F0F0F0F;
        b1 = (b1 | (b1 << 2)) & 0x33333333;
        b1 = (b1 | (b1 << 1)) & 0x55555555;

        b2 = (b2 | (b2 << 8)) & 0x00FF00FF;
        b2 = (b2 | (b2 << 4)) & 0x0F0F0F0F;
        b2 = (b2 | (b2 << 2)) & 0x33333333;
        b2 = (b2 | (b2 << 1)) & 0x55555555;

        uint idx = b1 | (b2 << 1);

        if ((block & 0x1) != 0)
        {
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    int mod = GTable[j < 2 ? (int)tcw0 : (int)tcw1, idx & 0x3];
                    long r = (j < 2 ? br0 : br1) + mod;
                    long g = (j < 2 ? bg0 : bg1) + mod;
                    long b = (j < 2 ? bb0 : bb1) + mod;
                    if ((((uint)r | (uint)g | (uint)b) & ~0xFFu) == 0)
                    {
                        dst[j * 4 + i] = (uint)r | ((uint)g << 8) | ((uint)b << 16) | 0xFF000000u;
                    }
                    else
                    {
                        dst[j * 4 + i] = ClampU8((int)r) | ((uint)ClampU8((int)g) << 8) | ((uint)ClampU8((int)b) << 16) | 0xFF000000u;
                    }
                    idx >>= 2;
                }
            }
        }
        else
        {
            for (int i = 0; i < 4; i++)
            {
                uint tbl = i < 2 ? tcw0 : tcw1;
                long cr = i < 2 ? br0 : br1;
                long cg = i < 2 ? bg0 : bg1;
                long cb = i < 2 ? bb0 : bb1;

                for (int j = 0; j < 4; j++)
                {
                    int mod = GTable[(int)tbl, idx & 0x3];
                    long r = cr + mod;
                    long g = cg + mod;
                    long b = cb + mod;
                    if ((((uint)r | (uint)g | (uint)b) & ~0xFFu) == 0)
                    {
                        dst[j * 4 + i] = (uint)r | ((uint)g << 8) | ((uint)b << 16) | 0xFF000000u;
                    }
                    else
                    {
                        dst[j * 4 + i] = ClampU8((int)r) | ((uint)ClampU8((int)g) << 8) | ((uint)ClampU8((int)b) << 16) | 0xFF000000u;
                    }
                    idx >>= 2;
                }
            }
        }
    }

    public static void CompressRgb(ReadOnlySpan<uint> src, Span<ulong> dst, uint blocks, int stride)
    {
        var buf = new uint[16];
        int w = 0;
        int si = 0;
        int di = 0;

        for (uint b = 0; b < blocks; b++)
        {
            int p = 0;
            for (int x = 0; x < 4; x++)
            {
                buf[p++] = src[si];
                si += stride;
                buf[p++] = src[si];
                si += stride;
                buf[p++] = src[si];
                si += stride;
                buf[p++] = src[si];
                si -= stride * 3 - 1;
            }

            if (++w == stride / 4)
            {
                si += stride * 3;
                w = 0;
            }

            dst[di++] = ProcessRgb(buf);
        }
    }

    private static uint ClampU8(int val)
    {
        if ((val & ~0xFF) == 0) return (uint)val;
        return (uint)((~val >> 31) & 0xFF);
    }

    private static uint ReverseBytes(uint v)
    {
        return ((v & 0xFF) << 24) | ((v & 0xFF00) << 8) | ((v >> 8) & 0xFF00) | ((v >> 24) & 0xFF);
    }

    private static ulong ConvertByteOrder(ulong d)
    {
        uint lo = (uint)d;
        uint hi = (uint)(d >> 32);
        lo = ReverseBytes(lo);
        hi = ReverseBytes(hi);
        return (ulong)lo | ((ulong)hi << 32);
    }

    private static int Expand6(uint value)
    {
        return (int)((value << 2) | (value >> 4));
    }

    private static int Expand7(uint value)
    {
        return (int)((value << 1) | (value >> 6));
    }

    private static void DecodeT(ulong block, Span<uint> dst)
    {
        uint r0 = (uint)((block >> 24) & 0x1B);
        uint rh0 = (r0 >> 3) & 0x3;
        uint rl0 = r0 & 0x3;
        uint g0 = (uint)((block >> 20) & 0xF);
        uint b0 = (uint)((block >> 16) & 0xF);

        uint r1 = (uint)((block >> 12) & 0xF);
        uint g1 = (uint)((block >> 8) & 0xF);
        uint b1 = (uint)((block >> 4) & 0xF);

        uint cr0 = (rh0 << 6) | (rl0 << 4) | (rh0 << 2) | rl0;
        uint cg0 = (g0 << 4) | g0;
        uint cb0 = (b0 << 4) | b0;

        uint cr1 = (r1 << 4) | r1;
        uint cg1 = (g1 << 4) | g1;
        uint cb1 = (b1 << 4) | b1;

        uint codeword = (uint)((((block >> 2) & 0x3) << 1) | (block & 0x1));

        uint c2r = ClampU8((int)cr1 + Table59T58H[(int)codeword]);
        uint c2g = ClampU8((int)cg1 + Table59T58H[(int)codeword]);
        uint c2b = ClampU8((int)cb1 + Table59T58H[(int)codeword]);

        uint c3r = ClampU8((int)cr1 - Table59T58H[(int)codeword]);
        uint c3g = ClampU8((int)cg1 - Table59T58H[(int)codeword]);
        uint c3b = ClampU8((int)cb1 - Table59T58H[(int)codeword]);

        Span<uint> colTab = stackalloc uint[4]
        {
            cr0 | (cg0 << 8) | (cb0 << 16) | 0xFF000000u,
            c2r | (c2g << 8) | (c2b << 16) | 0xFF000000u,
            cr1 | (cg1 << 8) | (cb1 << 16) | 0xFF000000u,
            c3r | (c3g << 8) | (c3b << 16) | 0xFF000000u
        };

        uint indexes = (uint)(block >> 32);
        for (int j = 0; j < 4; j++)
        {
            for (int i = 0; i < 4; i++)
            {
                uint index = (((indexes >> (j + i * 4 + 16)) & 0x1) << 1) | ((indexes >> (j + i * 4)) & 0x1);
                dst[j * 4 + i] = colTab[(int)index];
            }
        }
    }

    private static void DecodeH(ulong block, Span<uint> dst)
    {
        uint indexes = (uint)(block >> 32);

        uint r0444 = (uint)((block >> 27) & 0xF);
        uint g0444 = (uint)(((block >> 20) & 0x1) | (((block >> 24) & 0x7) << 1));
        uint b0444 = (uint)(((block >> 15) & 0x7) | (((block >> 19) & 0x1) << 3));

        uint r1444 = (uint)((block >> 11) & 0xF);
        uint g1444 = (uint)((block >> 7) & 0xF);
        uint b1444 = (uint)((block >> 3) & 0xF);

        uint r0 = (r0444 << 4) | r0444;
        uint g0 = (g0444 << 4) | g0444;
        uint b0 = (b0444 << 4) | b0444;

        uint r1 = (r1444 << 4) | r1444;
        uint g1 = (g1444 << 4) | g1444;
        uint b1 = (b1444 << 4) | b1444;

        uint codeword = (uint)(((block & 0x1) << 1) | (block & 0x4));
        uint c0 = (r0444 << 8) | (g0444 << 4) | b0444;
        uint c1 = (uint)((block >> 3) & 0xFFF);
        uint codewordLo = c0 >= c1 ? 1u : 0u;
        codeword |= codewordLo;

        Span<uint> colTab = stackalloc uint[4]
        {
            ClampU8((int)r0 + Table59T58H[(int)codeword]) | (ClampU8((int)g0 + Table59T58H[(int)codeword]) << 8) | (ClampU8((int)b0 + Table59T58H[(int)codeword]) << 16),
            ClampU8((int)r0 - Table59T58H[(int)codeword]) | (ClampU8((int)g0 - Table59T58H[(int)codeword]) << 8) | (ClampU8((int)b0 - Table59T58H[(int)codeword]) << 16),
            ClampU8((int)r1 + Table59T58H[(int)codeword]) | (ClampU8((int)g1 + Table59T58H[(int)codeword]) << 8) | (ClampU8((int)b1 + Table59T58H[(int)codeword]) << 16),
            ClampU8((int)r1 - Table59T58H[(int)codeword]) | (ClampU8((int)g1 - Table59T58H[(int)codeword]) << 8) | (ClampU8((int)b1 - Table59T58H[(int)codeword]) << 16)
        };

        for (int j = 0; j < 4; j++)
        {
            for (int i = 0; i < 4; i++)
            {
                uint index = (((indexes >> (j + i * 4 + 16)) & 0x1) << 1) | ((indexes >> (j + i * 4)) & 0x1);
                dst[j * 4 + i] = colTab[(int)index] | 0xFF000000u;
            }
        }
    }

    private static void DecodePlanar(ulong block, Span<uint> dst)
    {
        int bv = Expand6((uint)((block >> 32) & 0x3F));
        int gv = Expand7((uint)((block >> 38) & 0x7F));
        int rv = Expand6((uint)((block >> 45) & 0x3F));

        int bh = Expand6((uint)((block >> 51) & 0x3F));
        int gh = Expand7((uint)((block >> 57) & 0x7F));

        int rh0 = (int)(block & 0x01);
        int rh1 = (int)(((block >> 2) & 0x1F) << 1);
        int rh = Expand6((uint)(rh0 | rh1));

        int bo0 = (int)((block >> 7) & 0x07);
        int bo1 = (int)(((block >> 11) & 0x3) << 3);
        int bo2 = (int)(((block >> 16) & 0x1) << 5);
        int bo = Expand6((uint)(bo0 | bo1 | bo2));

        int go0 = (int)((block >> 17) & 0x3F);
        int go1 = (int)(((block >> 24) & 0x01) << 6);
        int go = Expand7((uint)(go0 | go1));

        int ro = Expand6((uint)((block >> 25) & 0x3F));

        for (int j = 0; j < 4; j++)
        {
            for (int i = 0; i < 4; i++)
            {
                uint r = (uint)((i * (rh - ro) + j * (rv - ro) + 4 * ro + 2) >> 2);
                uint g = (uint)((i * (gh - go) + j * (gv - go) + 4 * go + 2) >> 2);
                uint b = (uint)((i * (bh - bo) + j * (bv - bo) + 4 * bo + 2) >> 2);
                if (((r | g | b) & ~0xFFu) == 0)
                {
                    dst[j * 4 + i] = r | (g << 8) | (b << 16) | 0xFF000000u;
                }
                else
                {
                    uint rc = ClampU8((int)r);
                    uint gc = ClampU8((int)g);
                    uint bc = ClampU8((int)b);
                    dst[j * 4 + i] = rc | (gc << 8) | (bc << 16) | 0xFF000000u;
                }
            }
        }
    }

    private static int Mul8Bit(int a, int b)
    {
        int t = a * b + 128;
        return (t + (t >> 8)) >> 8;
    }

    private static int GetLeastError(ReadOnlySpan<uint> err, int num)
    {
        int idx = 0;
        for (int i = 1; i < num; i++)
        {
            if (err[i] < err[idx]) idx = i;
        }
        return idx;
    }

    private static int GetLeastError(ReadOnlySpan<ulong> err, int num)
    {
        int idx = 0;
        for (int i = 1; i < num; i++)
        {
            if (err[i] < err[idx]) idx = i;
        }
        return idx;
    }

    private static ulong FixByteOrder(ulong d)
    {
        return (d & 0x00000000FFFFFFFFUL) |
               ((d & 0xFF00000000000000UL) >> 24) |
               ((d & 0x000000FF00000000UL) << 24) |
               ((d & 0x00FF000000000000UL) >> 8) |
               ((d & 0x0000FF0000000000UL) << 8);
    }

    private static ulong EncodeSelectors(ulong d, ReadOnlySpan<ulong> terr, ReadOnlySpan<ushort> tsel, ReadOnlySpan<uint> id)
    {
        int tidx0 = GetLeastError(terr.Slice(0, 8), 8);
        int tidx1 = GetLeastError(terr.Slice(8, 8), 8);

        d |= (ulong)tidx0 << 26;
        d |= (ulong)tidx1 << 29;
        for (int i = 0; i < 16; i++)
        {
            ulong t = tsel[i * 8 + (id[i] % 2 == 0 ? tidx0 : tidx1)];
            d |= (t & 0x1) << (i + 32);
            d |= (t & 0x2) << (i + 47);
        }

        return d;
    }

    private static void Average(ReadOnlySpan<uint> src, Span<ushort> a)
    {
        uint[] r = new uint[4];
        uint[] g = new uint[4];
        uint[] b = new uint[4];

        for (int j = 0; j < 4; j++)
        {
            for (int i = 0; i < 4; i++)
            {
                int index = (j & 2) + (i >> 1);
                uint v = src[j * 4 + i];
                b[index] += v & 0xFF;
                g[index] += (v >> 8) & 0xFF;
                r[index] += (v >> 16) & 0xFF;
            }
        }

        a[0] = (ushort)((r[2] + r[3] + 4) / 8);
        a[1] = (ushort)((g[2] + g[3] + 4) / 8);
        a[2] = (ushort)((b[2] + b[3] + 4) / 8);
        a[3] = 0;
        a[4] = (ushort)((r[0] + r[1] + 4) / 8);
        a[5] = (ushort)((g[0] + g[1] + 4) / 8);
        a[6] = (ushort)((b[0] + b[1] + 4) / 8);
        a[7] = 0;
        a[8] = (ushort)((r[1] + r[3] + 4) / 8);
        a[9] = (ushort)((g[1] + g[3] + 4) / 8);
        a[10] = (ushort)((b[1] + b[3] + 4) / 8);
        a[11] = 0;
        a[12] = (ushort)((r[0] + r[2] + 4) / 8);
        a[13] = (ushort)((g[0] + g[2] + 4) / 8);
        a[14] = (ushort)((b[0] + b[2] + 4) / 8);
        a[15] = 0;
    }

    private static void ProcessAverages(Span<ushort> a)
    {
        for (int i = 0; i < 2; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                int c1 = Mul8Bit(a[(i * 2 + 1) * 4 + j], 31);
                int c2 = Mul8Bit(a[(i * 2) * 4 + j], 31);

                int diff = c2 - c1;
                if (diff > 3) diff = 3;
                else if (diff < -4) diff = -4;

                int co = c1 + diff;

                a[(5 + i * 2) * 4 + j] = (ushort)((c1 << 3) | (c1 >> 2));
                a[(4 + i * 2) * 4 + j] = (ushort)((co << 3) | (co >> 2));
            }
        }

        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                a[i * 4 + j] = (ushort)GAvg2[Mul8Bit(a[i * 4 + j], 15)];
            }
        }
    }

    private static uint CalcError(ReadOnlySpan<uint> block, ReadOnlySpan<ushort> av)
    {
        uint err = 0x3FFFFFFFu;
        err -= block[0] * 2u * av[2];
        err -= block[1] * 2u * av[1];
        err -= block[2] * 2u * av[0];
        err += 8u * ((uint)av[0] * av[0] + (uint)av[1] * av[1] + (uint)av[2] * av[2]);
        return err;
    }

    private static void CalcErrorBlock(ReadOnlySpan<uint> src, uint[,] err)
    {
        uint[,] terr = new uint[4, 4];

        for (int j = 0; j < 4; j++)
        {
            for (int i = 0; i < 4; i++)
            {
                int index = (j & 2) + (i >> 1);
                uint v = src[j * 4 + i];
                terr[index, 0] += v & 0xFF;
                terr[index, 1] += (v >> 8) & 0xFF;
                terr[index, 2] += (v >> 16) & 0xFF;
            }
        }

        for (int i = 0; i < 3; i++)
        {
            err[0, i] = terr[2, i] + terr[3, i];
            err[1, i] = terr[0, i] + terr[1, i];
            err[2, i] = terr[1, i] + terr[3, i];
            err[3, i] = terr[0, i] + terr[2, i];
        }
        for (int i = 0; i < 4; i++)
        {
            err[i, 3] = 0;
        }
    }

    private static void PrepareAverages(Span<ushort> a, ReadOnlySpan<uint> src, Span<uint> err)
    {
        Average(src, a);
        ProcessAverages(a);

        uint[,] errblock = new uint[4, 4];
        CalcErrorBlock(src, errblock);

        for (int i = 0; i < 4; i++)
        {
            Span<uint> blk = new uint[4];
            blk[0] = errblock[i, 0];
            blk[1] = errblock[i, 1];
            blk[2] = errblock[i, 2];
            blk[3] = errblock[i, 3];
            err[i / 2] += CalcError(blk, a.Slice(i * 4, 4));
            err[2 + i / 2] += CalcError(blk, a.Slice((i + 4) * 4, 4));
        }
    }

    private static void EncodeAverages(ref ulong d, ReadOnlySpan<ushort> a, int idx)
    {
        d |= (ulong)idx << 24;
        int baseIdx = idx << 1;

        if ((idx & 0x2) == 0)
        {
            for (int i = 0; i < 3; i++)
            {
                d |= (ulong)(a[baseIdx * 4 + i] >> 4) << (i * 8);
                d |= (ulong)(a[(baseIdx + 1) * 4 + i] >> 4) << (i * 8 + 4);
            }
        }
        else
        {
            for (int i = 0; i < 3; i++)
            {
                d |= (ulong)(a[(baseIdx + 1) * 4 + i] & 0xF8) << (i * 8);
                int c = ((a[baseIdx * 4 + i] & 0xF8) - (a[(baseIdx + 1) * 4 + i] & 0xF8)) >> 3;
                c &= 0x7;
                d |= (ulong)c << (i * 8);
            }
        }
    }

    private static ulong CheckSolid(ReadOnlySpan<uint> src)
    {
        uint refValue = src[0];
        for (int i = 1; i < 16; i++)
        {
            if (src[i] != refValue) return 0;
        }
        return 0x02000000UL |
               ((uint)(src[0] & 0xF8) << 16) |
               ((uint)((src[0] >> 8) & 0xF8) << 8) |
               ((uint)((src[0] >> 16) & 0xF8));
    }

    private static void FindBestFit(Span<ulong> terr, Span<ushort> tsel, ReadOnlySpan<ushort> a, ReadOnlySpan<uint> id, ReadOnlySpan<uint> src)
    {
        for (int i = 0; i < 16; i++)
        {
            uint v = src[i];
            int b = (int)(v & 0xFF);
            int g = (int)((v >> 8) & 0xFF);
            int r = (int)((v >> 16) & 0xFF);

            uint bid = id[i];
            Span<ulong> ter = terr.Slice((int)(bid % 2) * 8, 8);

            int dr = a[(int)bid * 4 + 0] - r;
            int dg = a[(int)bid * 4 + 1] - g;
            int db = a[(int)bid * 4 + 2] - b;

            int pix = dr * 77 + dg * 151 + db * 28;

            for (int t = 0; t < 8; t++)
            {
                long tab0 = GTable256[t, 0];
                long tab1 = GTable256[t, 1];
                long tab2 = GTable256[t, 2];
                long tab3 = GTable256[t, 3];

                uint selIdx = 0;
                long err = (tab0 + pix) * (tab0 + pix);
                long local = (tab1 + pix) * (tab1 + pix);
                if (local < err) { err = local; selIdx = 1; }
                local = (tab2 + pix) * (tab2 + pix);
                if (local < err) { err = local; selIdx = 2; }
                local = (tab3 + pix) * (tab3 + pix);
                if (local < err) { err = local; selIdx = 3; }

                tsel[i * 8 + t] = (ushort)selIdx;
                ter[t] += (ulong)err;
            }
        }
    }

    private static ulong ProcessRgb(ReadOnlySpan<uint> src)
    {
        ulong d = CheckSolid(src);
        if (d != 0) return d;

        Span<ushort> a = stackalloc ushort[32];
        Span<uint> err = stackalloc uint[4];
        PrepareAverages(a, src, err);

        int idx = GetLeastError(err, 4);
        EncodeAverages(ref d, a, idx);

        Span<ulong> terr = stackalloc ulong[16];
        Span<ushort> tsel = stackalloc ushort[128];
        Span<uint> id = stackalloc uint[16];
        for (int i = 0; i < 16; i++) id[i] = GId[idx, i];

        FindBestFit(terr, tsel, a, id, src);

        return FixByteOrder(EncodeSelectors(d, terr, tsel, id));
    }
}