using LiarUtil.Core.Core.Binary;

namespace LiarUtil.Core.Texture.Codecs;

internal static class DxtMorton
{
    private static readonly int[] Order = [0, 2, 8, 10, 1, 3, 9, 11, 4, 6, 12, 14, 5, 7, 13, 15];

    private static readonly int[] MortonX =
    [
        0, 4, 0, 4, 8, 12, 8, 12, 0, 4, 0, 4, 8, 12, 8, 12,
        16, 20, 16, 20, 24, 28, 24, 28, 16, 20, 16, 20, 24, 28, 24, 28,
        0, 4, 0, 4, 8, 12, 8, 12, 0, 4, 0, 4, 8, 12, 8, 12,
        16, 20, 16, 20, 24, 28, 24, 28, 16, 20, 16, 20, 24, 28, 24, 28,
    ];

    private static readonly int[] MortonY =
    [
        0, 0, 4, 4, 0, 0, 4, 4, 8, 8, 12, 12, 8, 8, 12, 12,
        0, 0, 4, 4, 0, 0, 4, 4, 8, 8, 12, 12, 8, 8, 12, 12,
        16, 16, 20, 20, 16, 16, 20, 20, 24, 24, 28, 28, 24, 24, 28, 28,
        16, 16, 20, 20, 16, 16, 20, 20, 24, 24, 28, 28, 24, 24, 28, 28,
    ];

    private static int MortonIndex(int i, int minwh, int mink, bool bigw, int w)
    {
        var mxBits = 0;
        var myBits = 0;
        for (var j = 0; j < 16; j++)
        {
            mxBits |= (i & (1 << (j << 1))) >> j;
            myBits |= (i & ((1 << (j << 1)) << 1)) >> j;
        }

        myBits >>= 1;
        var j2 = (i >> (2 * mink)) << (2 * mink);
        var v = j2 | ((mxBits & (minwh - 1)) << mink) | (myBits & (minwh - 1));
        var x = bigw ? v / minwh : v % minwh;
        var y = bigw ? v % minwh : v / minwh;
        return y * w + x;
    }

    private static (int Width, int Height) NextPowerOfTwo(int width, int height)
    {
        var nw = width;
        var nh = height;
        if ((nw & (nw - 1)) != 0)
        {
            nw = 2 << (int)Math.Floor(Math.Log2(nw));
        }

        if ((nh & (nh - 1)) != 0)
        {
            nh = 2 << (int)Math.Floor(Math.Log2(nh));
        }

        return (nw, nh);
    }

    public static void DecodeDxt5Morton(BufferReader stream, int width, int height, Span<byte> pixels)
    {
        var (nw, nh) = NextPowerOfTwo(width, height);
        var resized = nw != width || nh != height;
        Span<byte> full = resized ? new byte[nw * nh * 4] : pixels;
        var minwh = Math.Min(nw, nh);
        var mink = (int)Math.Log2(minwh);
        var bigw = nw > nh;
        Span<byte> color = stackalloc byte[64];
        Span<byte> alpha = stackalloc byte[16];
        var pO = 0;
        for (var y = 0; y < nh; y += 4)
        {
            for (var x = 0; x < nw; x += 4)
            {
                var t = stream.ReadUInt16();
                ulong alpha48 = stream.ReadUInt16() | ((ulong)stream.ReadUInt16() << 16) | ((ulong)stream.ReadUInt16() << 32);
                Dxt.DecodeDxt5AlphaPublic((byte)(t & 0xFF), (byte)(t >> 8), alpha48, alpha);
                var c0 = stream.ReadUInt16();
                var c1 = stream.ReadUInt16();
                uint bits = stream.ReadUInt16() | ((uint)stream.ReadUInt16() << 16);
                Dxt.DecodeDxtColorPublic(c0, c1, bits, color, alpha, true, false);
                for (var i = 0; i < 16; i++)
                {
                    var dst = MortonIndex(pO + Order[i], minwh, mink, bigw, nw) * 4;
                    var s = i * 4;
                    full[dst] = color[s];
                    full[dst + 1] = color[s + 1];
                    full[dst + 2] = color[s + 2];
                    full[dst + 3] = color[s + 3];
                }

                pO += 16;
            }
        }

        if (resized)
        {
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var s = (y * nw + x) * 4;
                    var d = (y * width + x) * 4;
                    pixels[d] = full[s];
                    pixels[d + 1] = full[s + 1];
                    pixels[d + 2] = full[s + 2];
                    pixels[d + 3] = full[s + 3];
                }
            }
        }
    }

    public static uint EncodeDxt5Morton(BufferWriter stream, int width, int height, Span<byte> pixels)
    {
        var (nw, nh) = NextPowerOfTwo(width, height);
        var resized = nw != width || nh != height;
        var full = pixels;
        if (resized)
        {
            full = new byte[nw * nh * 4];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var s = (y * width + x) * 4;
                    var d = (y * nw + x) * 4;
                    full[d] = pixels[s];
                    full[d + 1] = pixels[s + 1];
                    full[d + 2] = pixels[s + 2];
                    full[d + 3] = pixels[s + 3];
                }
            }
        }

        var minwh = Math.Min(nw, nh);
        var mink = (int)Math.Log2(minwh);
        var bigw = nw > nh;
        Span<byte> color = stackalloc byte[64];
        var pO = 0;
        for (var y = 0; y < nh; y += 4)
        {
            for (var x = 0; x < nw; x += 4)
            {
                for (var n = 0; n < 16; n++)
                {
                    var s = MortonIndex(pO + Order[n], minwh, mink, bigw, nw) * 4;
                    var d = n * 4;
                    color[d] = full[s];
                    color[d + 1] = full[s + 1];
                    color[d + 2] = full[s + 2];
                    color[d + 3] = full[s + 3];
                }

                pO += 16;
                var alpha48 = Dxt.EmitDxt5AlphaPublic(color, out var minA, out var maxA);
                stream.WriteUInt16((ushort)((minA << 8) | maxA));
                stream.WriteUInt16((ushort)(alpha48 & 0xFFFF));
                stream.WriteUInt16((ushort)((alpha48 >> 16) & 0xFFFF));
                stream.WriteUInt16((ushort)(alpha48 >> 32));
                Dxt.GetMinMaxColorsPublic(color, out var min, out var max);
                var indices = Dxt.EmitColorIndicesPublic(color, min, max);
                stream.WriteUInt16(Dxt.ColorTo565Public(max[0], max[1], max[2]));
                stream.WriteUInt16(Dxt.ColorTo565Public(min[0], min[1], min[2]));
                stream.WriteUInt16((ushort)(indices & 0xFFFF));
                stream.WriteUInt16((ushort)(indices >> 16));
            }
        }

        return (uint)nw;
    }

    public static void DecodeDxt5MortonBlock(BufferReader stream, int width, int height, Span<byte> pixels)
    {
        var maxD = Math.Max(width, height);
        var nw = width;
        var nh = height;
        if (maxD < 32)
        {
            var (w2, h2) = NextPowerOfTwo(nw, nh);
            nw = w2;
            nh = h2;
            if (nw != nh)
            {
                nw = nh = Math.Max(nw, nh);
            }
        }
        else
        {
            if ((nw & 31) != 0)
            {
                nw |= 31;
                nw++;
            }

            if ((nh & 31) != 0)
            {
                nh |= 31;
                nh++;
            }
        }

        Span<byte> color = stackalloc byte[64];
        Span<byte> alpha = stackalloc byte[16];
        static void ReadBlock(BufferReader stream, Span<byte> pixels, Span<byte> color, Span<byte> alpha, int width, int height, int bx, int by)
        {
            var t = stream.ReadUInt16();
            ulong alpha48 = stream.ReadUInt16() | ((ulong)stream.ReadUInt16() << 16) | ((ulong)stream.ReadUInt16() << 32);
            Dxt.DecodeDxt5AlphaPublic((byte)(t & 0xFF), (byte)(t >> 8), alpha48, alpha);
            var c0 = stream.ReadUInt16();
            var c1 = stream.ReadUInt16();
            uint bits = stream.ReadUInt16() | ((uint)stream.ReadUInt16() << 16);
            Dxt.DecodeDxtColorPublic(c0, c1, bits, color, alpha, false, false);
            for (var i = 0; i < 4; i++)
            {
                for (var j = 0; j < 4; j++)
                {
                    if (bx + j < width && by + i < height)
                    {
                        var o = ((by + i) * width + bx + j) * 4;
                        var s = (i << 2 | j) * 4;
                        pixels[o] = color[s];
                        pixels[o + 1] = color[s + 1];
                        pixels[o + 2] = color[s + 2];
                        pixels[o + 3] = color[s + 3];
                    }
                }
            }
        }

        if (nw < 32)
        {
            var maxDi = (nw * nw) >> 4;
            for (var di = 0; di < maxDi; di++)
            {
                ReadBlock(stream, pixels, color, alpha, width, height, MortonX[di], MortonY[di]);
            }
        }
        else
        {
            for (var y = 0; y < nh; y += 32)
            {
                for (var x = 0; x < nw; x += 32)
                {
                    for (var di = 0; di < 64; di++)
                    {
                        ReadBlock(stream, pixels, color, alpha, width, height, x + MortonX[di], y + MortonY[di]);
                    }
                }
            }
        }
    }

    public static uint EncodeDxt5MortonBlock(BufferWriter stream, int width, int height, Span<byte> pixels)
    {
        var maxD = Math.Max(width, height);
        var nw = width;
        var nh = height;
        if (maxD < 32)
        {
            var (w2, h2) = NextPowerOfTwo(nw, nh);
            nw = w2;
            nh = h2;
            if (nw != nh)
            {
                nw = nh = Math.Max(nw, nh);
            }
        }
        else
        {
            if ((nw & 31) != 0)
            {
                nw |= 31;
                nw++;
            }

            if ((nh & 31) != 0)
            {
                nh |= 31;
                nh++;
            }
        }

        Span<byte> color = stackalloc byte[64];
        static void WriteBlock(BufferWriter stream, Span<byte> pixels, Span<byte> color, int width, int height, int bx, int by)
        {
            for (var j = 0; j < 4; j++)
            {
                for (var k = 0; k < 4; k++)
                {
                    var s = (j << 2 | k) * 4;
                    if (by + j < height && bx + k < width)
                    {
                        var p = ((by + j) * width + bx + k) * 4;
                        color[s] = pixels[p];
                        color[s + 1] = pixels[p + 1];
                        color[s + 2] = pixels[p + 2];
                        color[s + 3] = pixels[p + 3];
                    }
                    else
                    {
                        color[s] = 0;
                        color[s + 1] = 0;
                        color[s + 2] = 0;
                        color[s + 3] = 0;
                    }
                }
            }

            var alpha48 = Dxt.EmitDxt5AlphaPublic(color, out var minA, out var maxA);
            stream.WriteUInt16((ushort)((minA << 8) | maxA));
            stream.WriteUInt16((ushort)(alpha48 & 0xFFFF));
            stream.WriteUInt16((ushort)((alpha48 >> 16) & 0xFFFF));
            stream.WriteUInt16((ushort)(alpha48 >> 32));
            Dxt.GetMinMaxColorsPublic(color, out var min, out var max);
            var indices = Dxt.EmitColorIndicesPublic(color, min, max);
            stream.WriteUInt16(Dxt.ColorTo565Public(max[0], max[1], max[2]));
            stream.WriteUInt16(Dxt.ColorTo565Public(min[0], min[1], min[2]));
            stream.WriteUInt16((ushort)(indices & 0xFFFF));
            stream.WriteUInt16((ushort)(indices >> 16));
        }

        if (nw < 32)
        {
            var maxDi = (nw * nw) >> 4;
            for (var di = 0; di < maxDi; di++)
            {
                WriteBlock(stream, pixels, color, width, height, MortonX[di], MortonY[di]);
            }
        }
        else
        {
            for (var y = 0; y < nh; y += 32)
            {
                for (var x = 0; x < nw; x += 32)
                {
                    for (var di = 0; di < 64; di++)
                    {
                        WriteBlock(stream, pixels, color, width, height, x + MortonX[di], y + MortonY[di]);
                    }
                }
            }
        }

        return (uint)nw;
    }

    public static void DecodeDxt5Padding(BufferReader stream, int width, int height, int blockSize, Span<byte> pixels)
    {
        Span<byte> color = stackalloc byte[64];
        Span<byte> alpha = stackalloc byte[16];
        var offset = stream.Position;
        var times = 0;
        for (var y = 0; y < height; y += 4)
        {
            for (var x = 0; x < width; x += 4)
            {
                var t = stream.ReadUInt16();
                ulong alpha48 = stream.ReadUInt16() | ((ulong)stream.ReadUInt16() << 16) | ((ulong)stream.ReadUInt16() << 32);
                Dxt.DecodeDxt5AlphaPublic((byte)(t & 0xFF), (byte)(t >> 8), alpha48, alpha);
                var c0 = stream.ReadUInt16();
                var c1 = stream.ReadUInt16();
                uint bits = stream.ReadUInt16() | ((uint)stream.ReadUInt16() << 16);
                Dxt.DecodeDxtColorPublic(c0, c1, bits, color, alpha, true, false);
                for (var i = 0; i < 4; i++)
                {
                    for (var j = 0; j < 4; j++)
                    {
                        if (x + j < width && y + i < height)
                        {
                            var o = ((y + i) * width + x + j) * 4;
                            var s = (i << 2 | j) * 4;
                            pixels[o] = color[s];
                            pixels[o + 1] = color[s + 1];
                            pixels[o + 2] = color[s + 2];
                            pixels[o + 3] = color[s + 3];
                        }
                    }
                }
            }

            times++;
            stream.Position = offset + times * blockSize;
        }

        stream.Position = offset + times * blockSize;
    }

    public static uint EncodeDxt5Padding(BufferWriter stream, int width, int height, int blockSize, Span<byte> pixels)
    {
        var newWidth = width;
        if ((newWidth & 3) != 0)
        {
            newWidth |= 3;
            newWidth++;
        }

        var cdSize = blockSize - (newWidth << 2);
        Span<byte> color = stackalloc byte[64];
        for (var y = 0; y < height; y += 4)
        {
            for (var x = 0; x < width; x += 4)
            {
                for (var j = 0; j < 4; j++)
                {
                    for (var k = 0; k < 4; k++)
                    {
                        var s = (j << 2 | k) * 4;
                        if (y + j < height && x + k < width)
                        {
                            var p = ((y + j) * width + x + k) * 4;
                            color[s] = pixels[p];
                            color[s + 1] = pixels[p + 1];
                            color[s + 2] = pixels[p + 2];
                            color[s + 3] = pixels[p + 3];
                        }
                        else
                        {
                            color[s] = 0;
                            color[s + 1] = 0;
                            color[s + 2] = 0;
                            color[s + 3] = 0;
                        }
                    }
                }

                var alpha48 = Dxt.EmitDxt5AlphaPublic(color, out var minA, out var maxA);
                stream.WriteUInt16((ushort)((minA << 8) | maxA));
                stream.WriteUInt16((ushort)(alpha48 & 0xFFFF));
                stream.WriteUInt16((ushort)((alpha48 >> 16) & 0xFFFF));
                stream.WriteUInt16((ushort)(alpha48 >> 32));
                Dxt.GetMinMaxColorsPublic(color, out var min, out var max);
                var indices = Dxt.EmitColorIndicesPublic(color, min, max);
                stream.WriteUInt16(Dxt.ColorTo565Public(max[0], max[1], max[2]));
                stream.WriteUInt16(Dxt.ColorTo565Public(min[0], min[1], min[2]));
                stream.WriteUInt16((ushort)(indices & 0xFFFF));
                stream.WriteUInt16((ushort)(indices >> 16));
            }

            for (var j = 0; j < cdSize; j++)
            {
                stream.WriteUInt8(0xCD);
            }
        }

        for (var j = 0; j < blockSize; j++)
        {
            stream.WriteUInt8(0xCD);
        }

        return (uint)(blockSize >> 2);
    }
}
