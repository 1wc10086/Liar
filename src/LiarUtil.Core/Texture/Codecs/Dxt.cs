using LiarUtil.Core.Core.Binary;

namespace LiarUtil.Core.Texture.Codecs;

internal static class Dxt
{
    internal static ushort ColorTo565Public(byte r, byte g, byte b) =>
        (ushort)(((r >> 3) << 11) | ((g >> 2) << 5) | (b >> 3));

    private static ushort ColorTo555(byte r, byte g, byte b) =>
        (ushort)(((r >> 3) << 10) | ((g >> 3) << 5) | (b >> 3));

    internal static void GetMinMaxColorsPublic(ReadOnlySpan<byte> block, out byte[] min, out byte[] max)
    {
        min = [0, 0, 0, 0];
        max = [0, 0, 0, 0];
        var maxDistance = -1;
        for (var i = 0; i < 15; i++)
        {
            for (var j = i + 1; j < 16; j++)
            {
                var dr = block[i * 4] - block[j * 4];
                var dg = block[i * 4 + 1] - block[j * 4 + 1];
                var db = block[i * 4 + 2] - block[j * 4 + 2];
                var d = dr * dr + dg * dg + db * db;
                if (d > maxDistance)
                {
                    maxDistance = d;
                    min = block.Slice(i * 4, 4).ToArray();
                    max = block.Slice(j * 4, 4).ToArray();
                }
            }
        }

        if (ColorTo565Public(max[0], max[1], max[2]) < ColorTo565Public(min[0], min[1], min[2]))
        {
            (min, max) = (max, min);
        }
    }

    internal static int EmitColorIndicesPublic(ReadOnlySpan<byte> block, byte[] min, byte[] max)
    {
        Span<byte> c = stackalloc byte[15];
        c[0] = (byte)((max[0] & 0xF8) | (max[0] >> 5));
        c[1] = (byte)((max[1] & 0xFC) | (max[1] >> 6));
        c[2] = (byte)((max[2] & 0xF8) | (max[2] >> 5));
        c[4] = (byte)((min[0] & 0xF8) | (min[0] >> 5));
        c[5] = (byte)((min[1] & 0xFC) | (min[1] >> 6));
        c[6] = (byte)((min[2] & 0xF8) | (min[2] >> 5));
        c[8] = (byte)(((c[0] << 1) + c[4]) / 3);
        c[9] = (byte)(((c[1] << 1) + c[5]) / 3);
        c[10] = (byte)(((c[2] << 1) + c[6]) / 3);
        c[12] = (byte)((c[0] + (c[4] << 1)) / 3);
        c[13] = (byte)((c[1] + (c[5] << 1)) / 3);
        c[14] = (byte)((c[2] + (c[6] << 1)) / 3);

        var result = 0;
        for (var i = 15; i >= 0; i--)
        {
            var r = block[i * 4];
            var g = block[i * 4 + 1];
            var b = block[i * 4 + 2];
            var d0 = Math.Abs(c[0] - r) + Math.Abs(c[1] - g) + Math.Abs(c[2] - b);
            var d1 = Math.Abs(c[4] - r) + Math.Abs(c[5] - g) + Math.Abs(c[6] - b);
            var d2 = Math.Abs(c[8] - r) + Math.Abs(c[9] - g) + Math.Abs(c[10] - b);
            var d3 = Math.Abs(c[12] - r) + Math.Abs(c[13] - g) + Math.Abs(c[14] - b);
            var b0 = d0 > d3 ? 1 : 0;
            var b1 = d1 > d2 ? 1 : 0;
            var b2 = d0 > d2 ? 1 : 0;
            var b3 = d1 > d3 ? 1 : 0;
            var b4 = d2 > d3 ? 1 : 0;
            var code = ((b0 & b4) | (((b1 & b2) | (b0 & b3)) << 1)) << (i << 1);
            result |= code;
        }

        return result;
    }

    internal static ulong EmitDxt5AlphaPublic(ReadOnlySpan<byte> block, out byte minA, out byte maxA)
    {
        minA = 255;
        maxA = 0;
        for (var i = 0; i < 16; i++)
        {
            var a = block[i * 4 + 3];
            minA = Math.Min(minA, a);
            maxA = Math.Max(maxA, a);
        }

        if (minA == maxA)
        {
            return 0;
        }

        var temp = (maxA - minA) >> 4;
        maxA = (byte)Math.Clamp(maxA - temp, 0, 255);
        minA = (byte)Math.Clamp(minA + temp, 0, 255);
        Span<byte> table = stackalloc byte[8];
        table[0] = maxA;
        table[1] = minA;
        table[2] = (byte)((6 * maxA + minA) / 7);
        table[3] = (byte)((5 * maxA + (minA << 1)) / 7);
        table[4] = (byte)(((maxA << 2) + 3 * minA) / 7);
        table[5] = (byte)((3 * maxA + (minA << 2)) / 7);
        table[6] = (byte)(((maxA << 1) + 5 * minA) / 7);
        table[7] = (byte)((maxA + 6 * minA) / 7);

        ulong alpha48 = 0;
        for (var i = 0; i < 16; i++)
        {
            var value = block[i * 4 + 3];
            var best = 0;
            var bestDistance = 999;
            for (var j = 0; j < 8; j++)
            {
                var d = Math.Abs(value - table[j]);
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = j;
                }
            }

            alpha48 |= (ulong)best << (i * 3);
        }

        return alpha48;
    }

    internal static void DecodeDxtColorPublic(ushort c0, ushort c1, uint bits, Span<byte> dst, ReadOnlySpan<byte> alpha, bool useG3, bool isDxt1)
    {
        Span<byte> tc = stackalloc byte[16];
        DecodeRgb565(c0, useG3, tc);
        DecodeRgb565(c1, useG3, tc[4..]);
        tc[7] = 255;
        if (!isDxt1 || c0 > c1)
        {
            tc[8] = (byte)(((tc[0] << 1) + tc[4] + 1) / 3);
            tc[9] = (byte)(((tc[1] << 1) + tc[5] + 1) / 3);
            tc[10] = (byte)(((tc[2] << 1) + tc[6] + 1) / 3);
            tc[11] = 255;
            tc[12] = (byte)((tc[0] + (tc[4] << 1) + 1) / 3);
            tc[13] = (byte)((tc[1] + (tc[5] << 1) + 1) / 3);
            tc[14] = (byte)((tc[2] + (tc[6] << 1) + 1) / 3);
            tc[15] = 255;
        }
        else
        {
            tc[8] = (byte)((tc[0] + tc[4]) >> 1);
            tc[9] = (byte)((tc[1] + tc[5]) >> 1);
            tc[10] = (byte)((tc[2] + tc[6]) >> 1);
            tc[11] = 255;
            tc[12] = 0;
            tc[13] = 0;
            tc[14] = 0;
            tc[15] = 0;
        }

        for (var i = 0; i < 16; i++)
        {
            var index = (int)((bits >> (i * 2)) & 3);
            var o = i * 4;
            dst[o] = tc[index * 4];
            dst[o + 1] = tc[index * 4 + 1];
            dst[o + 2] = tc[index * 4 + 2];
            dst[o + 3] = alpha.Length > 0 ? alpha[i] : tc[index * 4 + 3];
        }
    }

    private static void DecodeRgb565(ushort value, bool useG3, Span<byte> dst)
    {
        var b = value & 0x1F;
        var g = (value & 0x7E0) >> 5;
        var r = (value & 0xF800) >> 11;
        var shift = useG3 ? 3 : 4;
        dst[0] = (byte)((r << 3) | (r >> 2));
        dst[1] = (byte)((g << 2) | (g >> shift));
        dst[2] = (byte)((b << 3) | (b >> 2));
        dst[3] = 255;
    }

    private static void DecodeDxt3Alpha(ulong alpha64, Span<byte> alpha)
    {
        for (var i = 0; i < 16; i++)
        {
            var t = (int)((alpha64 >> (i * 4)) & 0xF);
            alpha[i] = (byte)((t << 4) | t);
        }
    }

    internal static void DecodeDxt5AlphaPublic(byte a0, byte a1, ulong alpha48, Span<byte> alpha)
    {
        Span<byte> table = stackalloc byte[8];
        if (a0 > a1)
        {
            table[0] = a0;
            table[1] = a1;
            table[2] = (byte)((6 * a0 + a1) / 7);
            table[3] = (byte)((5 * a0 + (a1 << 1)) / 7);
            table[4] = (byte)(((a0 << 2) + 3 * a1) / 7);
            table[5] = (byte)((3 * a0 + (a1 << 2)) / 7);
            table[6] = (byte)(((a0 << 1) + 5 * a1) / 7);
            table[7] = (byte)((a0 + 6 * a1) / 7);
        }
        else
        {
            table[0] = a0;
            table[1] = a1;
            table[2] = (byte)(((a0 << 2) + a1) / 5);
            table[3] = (byte)((3 * a0 + (a1 << 1)) / 5);
            table[4] = (byte)(((a0 << 1) + 3 * a1) / 5);
            table[5] = (byte)((a0 + (a1 << 2)) / 5);
            table[6] = 0;
            table[7] = 255;
        }

        for (var i = 0; i < 16; i++)
        {
            alpha[i] = table[(int)(alpha48 & 7)];
            alpha48 >>= 3;
        }
    }

    public static void DecodeDxt1(BufferReader stream, int width, int height, Span<byte> pixels)
    {
        Span<byte> color = stackalloc byte[64];
        var loop = new DxtReadLoop(isDxt1: true, useG3: false, dxt3Alpha: false, dxt5Alpha: false);
        loop.Run(stream, width, height, pixels, color);
    }

    public static uint EncodeDxt1(BufferWriter stream, int width, int height, Span<byte> pixels)
    {
        var loop = new DxtWriteLoop(dxt3Alpha: false, dxt5Alpha: false);
        loop.Run(stream, width, height, pixels);
        return (uint)(width >> 1);
    }

    public static void DecodeDxt3(BufferReader stream, int width, int height, Span<byte> pixels)
    {
        Span<byte> color = stackalloc byte[64];
        var loop = new DxtReadLoop(isDxt1: false, useG3: false, dxt3Alpha: true, dxt5Alpha: false);
        loop.Run(stream, width, height, pixels, color);
    }

    public static uint EncodeDxt3(BufferWriter stream, int width, int height, Span<byte> pixels)
    {
        var loop = new DxtWriteLoop(dxt3Alpha: true, dxt5Alpha: false);
        loop.Run(stream, width, height, pixels);
        return (uint)width;
    }

    public static void DecodeDxt5(BufferReader stream, int width, int height, Span<byte> pixels)
    {
        Span<byte> color = stackalloc byte[64];
        var loop = new DxtReadLoop(isDxt1: false, useG3: false, dxt3Alpha: false, dxt5Alpha: true);
        loop.Run(stream, width, height, pixels, color);
    }

    public static uint EncodeDxt5(BufferWriter stream, int width, int height, Span<byte> pixels)
    {
        var loop = new DxtWriteLoop(dxt3Alpha: false, dxt5Alpha: true);
        loop.Run(stream, width, height, pixels);
        return (uint)width;
    }

    private readonly struct DxtReadLoop
    {
        private readonly bool _isDxt1;
        private readonly bool _useG3;
        private readonly bool _dxt3Alpha;
        private readonly bool _dxt5Alpha;

        public DxtReadLoop(bool isDxt1, bool useG3, bool dxt3Alpha, bool dxt5Alpha)
        {
            _isDxt1 = isDxt1;
            _useG3 = useG3;
            _dxt3Alpha = dxt3Alpha;
            _dxt5Alpha = dxt5Alpha;
        }

        public void Run(BufferReader stream, int width, int height, Span<byte> pixels, Span<byte> color)
        {
            Span<byte> alpha = stackalloc byte[16];
            for (var y = 0; y < height; y += 4)
            {
                for (var x = 0; x < width; x += 4)
                {
                    if (_dxt3Alpha)
                    {
                        ulong alpha64 = stream.ReadUInt16() | ((ulong)stream.ReadUInt16() << 16) | ((ulong)stream.ReadUInt16() << 32) | ((ulong)stream.ReadUInt16() << 48);
                        DecodeDxt3Alpha(alpha64, alpha);
                    }
                    else if (_dxt5Alpha)
                    {
                        var t = stream.ReadUInt16();
                        ulong alpha48 = stream.ReadUInt16() | ((ulong)stream.ReadUInt16() << 16) | ((ulong)stream.ReadUInt16() << 32);
                        DecodeDxt5AlphaPublic((byte)(t & 0xFF), (byte)(t >> 8), alpha48, alpha);
                    }

                    var c0 = stream.ReadUInt16();
                    var c1 = stream.ReadUInt16();
                    uint bits = stream.ReadUInt16() | ((uint)stream.ReadUInt16() << 16);
                    DecodeDxtColorPublic(c0, c1, bits, color, _dxt3Alpha || _dxt5Alpha ? alpha : default, _useG3, _isDxt1);
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
            }
        }
    }

    private sealed class DxtWriteLoop
    {
        private readonly bool _dxt3Alpha;
        private readonly bool _dxt5Alpha;

        public DxtWriteLoop(bool dxt3Alpha, bool dxt5Alpha)
        {
            _dxt3Alpha = dxt3Alpha;
            _dxt5Alpha = dxt5Alpha;
        }

        public void Run(BufferWriter stream, int width, int height, Span<byte> pixels)
        {
            Span<byte> color = stackalloc byte[64];
            for (var y = 0; y < height; y += 4)
            {
                for (var x = 0; x < width; x += 4)
                {
                    for (var j = 0; j < 4; j++)
                    {
                        for (var k = 0; k < 4; k++)
                        {
                            var o = ((y + j) < height && (x + k) < width ? (y + j) * width + x + k : 0) * 4;
                            var s = (j << 2 | k) * 4;
                            if ((y + j) < height && (x + k) < width)
                            {
                                color[s] = pixels[o];
                                color[s + 1] = pixels[o + 1];
                                color[s + 2] = pixels[o + 2];
                                color[s + 3] = pixels[o + 3];
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

                    if (_dxt3Alpha)
                    {
                        Span<ushort> t = stackalloc ushort[4];
                        for (var j = 0; j < 4; j++)
                        {
                            for (var k = 0; k < 4; k++)
                            {
                                t[j] |= (ushort)((color[(j << 2 | k) * 4 + 3] >> 4) << (k << 2));
                            }
                        }

                        for (var j = 0; j < 4; j++)
                        {
                            stream.WriteUInt16(t[j]);
                        }
                    }
                    else if (_dxt5Alpha)
                    {
                        var alpha48 = EmitDxt5AlphaPublic(color, out var minA, out var maxA);
                        stream.WriteUInt16((ushort)((minA << 8) | maxA));
                        stream.WriteUInt16((ushort)(alpha48 & 0xFFFF));
                        stream.WriteUInt16((ushort)((alpha48 >> 16) & 0xFFFF));
                        stream.WriteUInt16((ushort)(alpha48 >> 32));
                    }

                    GetMinMaxColorsPublic(color, out var min, out var max);
                    var indices = EmitColorIndicesPublic(color, min, max);
                    stream.WriteUInt16(ColorTo565Public(max[0], max[1], max[2]));
                    stream.WriteUInt16(ColorTo565Public(min[0], min[1], min[2]));
                    stream.WriteUInt16((ushort)(indices & 0xFFFF));
                    stream.WriteUInt16((ushort)(indices >> 16));
                }
            }
        }
    }
}
