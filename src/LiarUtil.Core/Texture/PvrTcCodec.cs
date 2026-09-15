using LiarUtil.Core.Texture.Codecs;

namespace LiarUtil.Core.Texture;

internal static class PvrtcCodec
{
    public static void DecodeRgba(ReadOnlySpan<byte> compressed, int width, int height, Span<byte> pixels)
    {
        PvrTc.DecodeRgba4Bpp(compressed, width, height, pixels);
    }

    public static void DecodeRgb(ReadOnlySpan<byte> compressed, int width, int height, Span<byte> pixels)
    {
        PvrTc.DecodeRgb4Bpp(compressed, width, height, pixels);
    }

    public static byte[] EncodeRgba(ReadOnlySpan<byte> pixels, int width, int height)
    {
        var output = new byte[(width * height) >> 1];
        PvrTc.EncodeRgba4Bpp(pixels, width, height, output);
        return output;
    }

    public static byte[] EncodeRgb(ReadOnlySpan<byte> pixels, int width, int height)
    {
        var output = new byte[(width * height) >> 1];
        PvrTc.EncodeRgb4Bpp(pixels, width, height, output);
        return output;
    }

    private static int NextPowerOfTwoMin8(int value)
    {
        var result = 1;
        while (result < value)
        {
            result <<= 1;
        }

        return Math.Max(8, result);
    }

    public static void DecodePaddedRgba(ReadOnlySpan<byte> compressed, int width, int height, Span<byte> pixels)
    {
        var nw = Math.Max(8, width);
        var nh = Math.Max(8, height);
        if ((nw & (nw - 1)) != 0)
        {
            nw = NextPowerOfTwoMin8(nw);
        }

        if ((nh & (nh - 1)) != 0)
        {
            nh = NextPowerOfTwoMin8(nh);
        }

        var decoded = new byte[nw * nh * 4];
        PvrTc.DecodeRgba4Bpp(compressed, nw, nh, decoded);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var s = (y * nw + x) * 4;
                var d = (y * width + x) * 4;
                pixels[d] = decoded[s];
                pixels[d + 1] = decoded[s + 1];
                pixels[d + 2] = decoded[s + 2];
                pixels[d + 3] = decoded[s + 3];
            }
        }
    }

    public static byte[] EncodePaddedRgba(ReadOnlySpan<byte> pixels, int width, int height)
    {
        var nw = Math.Max(8, width);
        var nh = Math.Max(8, height);
        if ((nw & (nw - 1)) != 0)
        {
            nw = NextPowerOfTwoMin8(nw);
        }

        if ((nh & (nh - 1)) != 0)
        {
            nh = NextPowerOfTwoMin8(nh);
        }

        var padded = new byte[nw * nh * 4];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var s = (y * width + x) * 4;
                var d = (y * nw + x) * 4;
                padded[d] = pixels[s];
                padded[d + 1] = pixels[s + 1];
                padded[d + 2] = pixels[s + 2];
                padded[d + 3] = pixels[s + 3];
            }
        }

        return EncodeRgba(padded, nw, nh);
    }

    public static void DecodePaddedRgb(ReadOnlySpan<byte> compressed, int width, int height, Span<byte> pixels)
    {
        var nw = Math.Max(8, width);
        var nh = Math.Max(8, height);
        if ((nw & (nw - 1)) != 0)
        {
            nw = NextPowerOfTwoMin8(nw);
        }

        if ((nh & (nh - 1)) != 0)
        {
            nh = NextPowerOfTwoMin8(nh);
        }

        var decoded = new byte[nw * nh * 4];
        PvrTc.DecodeRgb4Bpp(compressed, nw, nh, decoded);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var s = (y * nw + x) * 4;
                var d = (y * width + x) * 4;
                pixels[d] = decoded[s];
                pixels[d + 1] = decoded[s + 1];
                pixels[d + 2] = decoded[s + 2];
                pixels[d + 3] = 255;
            }
        }
    }

    public static byte[] EncodePaddedRgb(ReadOnlySpan<byte> pixels, int width, int height)
    {
        var nw = Math.Max(8, width);
        var nh = Math.Max(8, height);
        if ((nw & (nw - 1)) != 0)
        {
            nw = NextPowerOfTwoMin8(nw);
        }

        if ((nh & (nh - 1)) != 0)
        {
            nh = NextPowerOfTwoMin8(nh);
        }

        var padded = new byte[nw * nh * 3];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var s = (y * width + x) * 4;
                var d = (y * nw + x) * 3;
                padded[d] = pixels[s];
                padded[d + 1] = pixels[s + 1];
                padded[d + 2] = pixels[s + 2];
            }
        }

        return EncodeRgb(padded, nw, nh);
    }
}
