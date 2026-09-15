namespace LiarUtil.Core.Texture;

internal enum PtxAlphaScheme
{
    None,
    A8,
    A8_A1,
    Palette4_A1,
    Palette5_A1,
}

internal static class PtxAlpha
{
    private static byte FourToEight(byte v) => (byte)((v << 4) | v);

    private static byte FiveToEight(byte v) => (byte)((v << 3) | (v >> 2));

    private static byte EightToFour(byte v) => (byte)(((uint)v * 15 + 127) / 255);

    private static byte EightToFive(byte v) => (byte)(((uint)v * 31 + 127) / 255);

    private static int PaletteBitWidth(int count)
    {
        if (count == 0)
        {
            return 1;
        }

        var bits = 0;
        var value = 1;
        while (value < count)
        {
            value <<= 1;
            bits++;
        }

        return bits;
    }

    public static void DecodeA8(ReadOnlySpan<byte> payload, Span<byte> pixels, int count)
    {
        if (payload.Length < count)
        {
            throw new InvalidDataException("PTX alpha A8 payload too small");
        }

        for (var i = 0; i < count; i++)
        {
            pixels[i * 4 + 3] = payload[i];
        }
    }

    public static byte[] EncodeA8(ReadOnlySpan<byte> pixels, int count)
    {
        var output = new byte[count];
        for (var i = 0; i < count; i++)
        {
            output[i] = pixels[i * 4 + 3];
        }

        return output;
    }

    private static uint ReadBitsMsb(ReadOnlySpan<byte> data, ref int position, int bits)
    {
        var value = 0u;
        for (var i = 0; i < bits; i++)
        {
            value <<= 1;
            if (position / 8 < data.Length)
            {
                value |= (uint)((data[position / 8] >> (7 - (position & 7))) & 1);
            }

            position++;
        }

        return value;
    }

    public static void DecodeA1Bitstream(ReadOnlySpan<byte> payload, Span<byte> pixels, int count)
    {
        var position = 0;
        for (var i = 0; i < count; i++)
        {
            pixels[i * 4 + 3] = ReadBitsMsb(payload, ref position, 1) != 0 ? (byte)255 : (byte)0;
        }
    }

    public static byte[] EncodeA1Bitstream(ReadOnlySpan<byte> pixels, int count)
    {
        var output = new byte[(count + 7) / 8];
        var position = 0;
        for (var i = 0; i < count; i++)
        {
            var bit = pixels[i * 4 + 3] >= 128 ? 1u : 0u;
            output[position / 8] |= (byte)(bit << (7 - (position & 7)));
            position++;
        }

        return output;
    }

    private static bool CanUsePureA1(ReadOnlySpan<byte> pixels, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var alpha = pixels[i * 4 + 3];
            if (alpha != 0 && alpha != 255)
            {
                return false;
            }
        }

        return true;
    }

    public static void DecodeA8A1(ReadOnlySpan<byte> payload, Span<byte> pixels, int count)
    {
        if (payload.Length == 0)
        {
            throw new InvalidDataException("PTX alpha A8_A1 payload too small");
        }

        if (payload[0] == 0)
        {
            DecodeA1Bitstream(payload[1..], pixels, count);
        }
        else
        {
            DecodeA8(payload[1..], pixels, count);
        }
    }

    public static byte[] EncodeA8A1(ReadOnlySpan<byte> pixels, int count)
    {
        var pure = CanUsePureA1(pixels, count);
        var body = pure ? EncodeA1Bitstream(pixels, count) : EncodeA8(pixels, count);
        var output = new byte[body.Length + 1];
        output[0] = pure ? (byte)0 : (byte)1;
        body.CopyTo(output, 1);
        return output;
    }

    private static void DecodePalette(ReadOnlySpan<byte> payload, Span<byte> pixels, int count, bool four)
    {
        var maxCount = four ? 16 : 32;
        if (payload.Length == 0 || payload[0] > maxCount || payload.Length < 1 + payload[0])
        {
            throw new InvalidDataException("PTX alpha palette payload too small");
        }

        var paletteCount = payload[0];
        if (paletteCount == 0)
        {
            DecodeA1Bitstream(payload[1..], pixels, count);
            return;
        }

        Span<byte> palette = four ? stackalloc byte[16] : stackalloc byte[32];
        for (var i = 0; i < paletteCount; i++)
        {
            palette[i] = four ? FourToEight(payload[1 + i]) : FiveToEight(payload[1 + i]);
        }

        var bits = PaletteBitWidth(paletteCount);
        if (bits == 0)
        {
            for (var i = 0; i < count; i++)
            {
                pixels[i * 4 + 3] = palette[0];
            }

            return;
        }

        var position = 0;
        for (var i = 0; i < count; i++)
        {
            var index = (int)ReadBitsMsb(payload[(1 + paletteCount)..], ref position, bits);
            pixels[i * 4 + 3] = palette[Math.Min(index, paletteCount - 1)];
        }
    }

    private static byte[] EncodePalette(ReadOnlySpan<byte> pixels, int count, bool four)
    {
        var maxCount = four ? 16 : 32;
        Span<bool> used = four ? stackalloc bool[16] : stackalloc bool[32];
        var palette = new List<byte>();
        for (var i = 0; i < count; i++)
        {
            var quantized = four ? EightToFour(pixels[i * 4 + 3]) : EightToFive(pixels[i * 4 + 3]);
            if (!used[quantized])
            {
                used[quantized] = true;
                palette.Add(quantized);
            }
        }

        if ((palette.Count == 1 && (used[0] || used[maxCount - 1])) ||
            (palette.Count == 2 && used[0] && used[maxCount - 1]))
        {
            palette.Clear();
        }

        var bits = PaletteBitWidth(palette.Count);
        var output = new List<byte>(palette.Count + 1 + (count * bits + 7) / 8) { (byte)palette.Count };
        output.AddRange(palette);
        if (palette.Count == 0)
        {
            var alpha = EncodeA1Bitstream(pixels, count);
            output.AddRange(alpha);
            return [.. output];
        }

        if (bits == 0)
        {
            return [.. output];
        }

        Span<byte> lut = four ? stackalloc byte[16] : stackalloc byte[32];
        for (var i = 0; i < palette.Count; i++)
        {
            lut[palette[i]] = (byte)i;
        }

        var bitstream = new byte[(count * bits + 7) / 8];
        var position = 0;
        for (var i = 0; i < count; i++)
        {
            var quantized = four ? EightToFour(pixels[i * 4 + 3]) : EightToFive(pixels[i * 4 + 3]);
            var index = lut[quantized];
            for (var b = bits - 1; b >= 0; b--)
            {
                bitstream[position / 8] |= (byte)(((index >> b) & 1) << (7 - (position & 7)));
                position++;
            }
        }

        output.AddRange(bitstream);
        return [.. output];
    }

    public static void DecodeByScheme(PtxAlphaScheme scheme, ReadOnlySpan<byte> payload, Span<byte> pixels, int count)
    {
        switch (scheme)
        {
            case PtxAlphaScheme.A8:
                DecodeA8(payload, pixels, count);
                break;
            case PtxAlphaScheme.A8_A1:
                DecodeA8A1(payload, pixels, count);
                break;
            case PtxAlphaScheme.Palette4_A1:
                DecodePalette(payload, pixels, count, true);
                break;
            case PtxAlphaScheme.Palette5_A1:
                DecodePalette(payload, pixels, count, false);
                break;
        }
    }

    public static byte[] EncodeByScheme(PtxAlphaScheme scheme, ReadOnlySpan<byte> pixels, int count)
    {
        return scheme switch
        {
            PtxAlphaScheme.A8 => EncodeA8(pixels, count),
            PtxAlphaScheme.A8_A1 => EncodeA8A1(pixels, count),
            PtxAlphaScheme.Palette4_A1 => EncodePalette(pixels, count, true),
            PtxAlphaScheme.Palette5_A1 => EncodePalette(pixels, count, false),
            _ => [],
        };
    }

    public static bool IsValidA8Payload(int size, int count) => size == count;

    public static bool IsValidA8A1Payload(ReadOnlySpan<byte> payload, int count) =>
        payload.Length > 0 && payload.Length == 1 + (payload[0] == 0 ? (count + 7) >> 3 : count);

    public static bool IsValidPalettePayload(ReadOnlySpan<byte> payload, int count, bool four)
    {
        var maxCount = four ? 16 : 32;
        if (payload.Length == 0 || payload[0] > maxCount || payload.Length < 1 + payload[0])
        {
            return false;
        }

        for (var i = 0; i < payload[0]; i++)
        {
            if (payload[1 + i] >= maxCount)
            {
                return false;
            }
        }

        return payload.Length == 1 + payload[0] + ((count * PaletteBitWidth(payload[0]) + 7) >> 3);
    }

    public static (PtxAlphaScheme Scheme, bool Valid) AutoDetect(uint fileId, ReadOnlySpan<byte> payload, int count, uint alphaSize, uint alphaFormat)
    {
        if (payload.Length == 0)
        {
            return (PtxAlphaScheme.None, true);
        }

        var a8 = IsValidA8Payload(payload.Length, count);
        var a8a1 = IsValidA8A1Payload(payload, count);
        var p4 = IsValidPalettePayload(payload, count, true);
        var p5 = IsValidPalettePayload(payload, count, false);

        (PtxAlphaScheme Scheme, bool Valid) Palette(ReadOnlySpan<byte> p)
        {
            if (p4 && p5)
            {
                var high = false;
                for (var i = 0; i < p[0]; i++)
                {
                    high |= p[1 + i] > 15;
                }

                return high ? (PtxAlphaScheme.Palette5_A1, true) : (PtxAlphaScheme.Palette4_A1, true);
            }

            if (p4)
            {
                return (PtxAlphaScheme.Palette4_A1, true);
            }

            if (p5)
            {
                return (PtxAlphaScheme.Palette5_A1, true);
            }

            return (PtxAlphaScheme.None, false);
        }

        if (alphaSize != 0 && alphaSize == payload.Length)
        {
            var palette = Palette(payload);
            if (palette.Valid)
            {
                return palette;
            }

            if (a8a1)
            {
                return (PtxAlphaScheme.A8_A1, true);
            }
        }

        if (alphaFormat == 0x64)
        {
            var palette = Palette(payload);
            if (palette.Valid)
            {
                return palette;
            }
        }

        if (fileId == 150)
        {
            var palette = Palette(payload);
            if (palette.Valid && (alphaFormat == 0x64 || alphaSize != 0))
            {
                return palette;
            }

            if (a8)
            {
                return (PtxAlphaScheme.A8, true);
            }

            if (a8a1)
            {
                return (PtxAlphaScheme.A8_A1, true);
            }

            if (palette.Valid)
            {
                return palette;
            }
        }

        if (a8 && !p4 && !p5 && !a8a1)
        {
            return (PtxAlphaScheme.A8, true);
        }

        if (a8a1 && !a8 && !p4 && !p5)
        {
            return (PtxAlphaScheme.A8_A1, true);
        }

        var fallback = Palette(payload);
        if (fallback.Valid)
        {
            return fallback;
        }

        if (a8)
        {
            return (PtxAlphaScheme.A8, true);
        }

        if (a8a1)
        {
            return (PtxAlphaScheme.A8_A1, true);
        }

        return (PtxAlphaScheme.None, false);
    }
}
