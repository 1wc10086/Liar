using LiarUtil.Core.Core.Binary;

namespace LiarUtil.Core.Texture.Codecs;

internal static class PixelFormats
{
    private static int ClampLuma(int r, int g, int b) =>
        Math.Clamp((int)(r * 0.299 + g * 0.587 + b * 0.114), 0, 255);

    public static void ReadAbgr8888(BufferReader stream, Span<byte> pixels, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var t = stream.ReadUInt32();
            var o = i * 4;
            pixels[o] = (byte)(t & 0xFF);
            pixels[o + 1] = (byte)((t >> 8) & 0xFF);
            pixels[o + 2] = (byte)((t >> 16) & 0xFF);
            pixels[o + 3] = (byte)(t >> 24);
        }
    }

    public static void WriteAbgr8888(BufferWriter stream, Span<byte> pixels, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var o = i * 4;
            stream.WriteUInt32(pixels[o] | (uint)(pixels[o + 1] << 8) | (uint)(pixels[o + 2] << 16) | (uint)(pixels[o + 3] << 24));
        }
    }

    public static void ReadArgb8888(BufferReader stream, Span<byte> pixels, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var t = stream.ReadUInt32();
            var o = i * 4;
            pixels[o] = (byte)((t >> 16) & 0xFF);
            pixels[o + 1] = (byte)((t >> 8) & 0xFF);
            pixels[o + 2] = (byte)(t & 0xFF);
            pixels[o + 3] = (byte)(t >> 24);
        }
    }

    public static void WriteArgb8888(BufferWriter stream, Span<byte> pixels, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var o = i * 4;
            stream.WriteUInt32((uint)((pixels[o + 3] << 24) | (pixels[o] << 16) | (pixels[o + 1] << 8) | pixels[o + 2]));
        }
    }

    public static void ReadXrgb8888(BufferReader stream, Span<byte> pixels, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var t = stream.ReadUInt32();
            var o = i * 4;
            pixels[o] = (byte)((t >> 16) & 0xFF);
            pixels[o + 1] = (byte)((t >> 8) & 0xFF);
            pixels[o + 2] = (byte)(t & 0xFF);
            pixels[o + 3] = 255;
        }
    }

    public static void WriteXrgb8888(BufferWriter stream, Span<byte> pixels, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var o = i * 4;
            stream.WriteUInt32(0xFF000000u | (uint)(pixels[o] << 16) | (uint)(pixels[o + 1] << 8) | pixels[o + 2]);
        }
    }

    public static void ReadArgb1555(BufferReader stream, Span<byte> pixels, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var t = stream.ReadUInt16();
            var o = i * 4;
            var r = (t >> 10) & 31;
            var g = (t >> 5) & 31;
            var b = t & 31;
            pixels[o] = (byte)((r << 3) | (r >> 2));
            pixels[o + 1] = (byte)((g << 3) | (g >> 2));
            pixels[o + 2] = (byte)((b << 3) | (b >> 2));
            pixels[o + 3] = (byte)((t >> 15) != 0 ? 255 : 0);
        }
    }

    public static void WriteArgb1555(BufferWriter stream, Span<byte> pixels, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var o = i * 4;
            stream.WriteUInt16((ushort)(((pixels[o + 3] & 0x80) << 8) | ((pixels[o] & 0xF8) << 7) | ((pixels[o + 1] & 0xF8) << 2) | ((pixels[o + 2] & 0xF8) >> 3)));
        }
    }

    public static void ReadArgb4444(BufferReader stream, Span<byte> pixels, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var t = stream.ReadUInt16();
            var o = i * 4;
            var a = t >> 12;
            var r = (t >> 8) & 15;
            var g = (t >> 4) & 15;
            var b = t & 15;
            pixels[o] = (byte)((r << 4) | r);
            pixels[o + 1] = (byte)((g << 4) | g);
            pixels[o + 2] = (byte)((b << 4) | b);
            pixels[o + 3] = (byte)((a << 4) | a);
        }
    }

    public static void WriteArgb4444(BufferWriter stream, Span<byte> pixels, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var o = i * 4;
            stream.WriteUInt16((ushort)(((pixels[o + 3] & 0xF0) << 8) | ((pixels[o] & 0xF0) << 4) | (pixels[o + 1] & 0xF0) | ((pixels[o + 2] & 0xF0) >> 4)));
        }
    }

    public static void ReadRgba4444(BufferReader stream, Span<byte> pixels, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var t = stream.ReadUInt16();
            var o = i * 4;
            var r = (t >> 12) & 15;
            var g = (t >> 8) & 15;
            var b = (t >> 4) & 15;
            var a = t & 15;
            pixels[o] = (byte)((r << 4) | r);
            pixels[o + 1] = (byte)((g << 4) | g);
            pixels[o + 2] = (byte)((b << 4) | b);
            pixels[o + 3] = (byte)((a << 4) | a);
        }
    }

    public static void WriteRgba4444(BufferWriter stream, Span<byte> pixels, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var o = i * 4;
            stream.WriteUInt16((ushort)(((pixels[o] & 0xF0) << 8) | ((pixels[o + 1] & 0xF0) << 4) | (pixels[o + 2] & 0xF0) | ((pixels[o + 3] & 0xF0) >> 4)));
        }
    }

    public static void ReadRgba5551(BufferReader stream, Span<byte> pixels, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var t = stream.ReadUInt16();
            var o = i * 4;
            var r = (t >> 11) & 31;
            var g = (t >> 6) & 31;
            var b = (t >> 1) & 31;
            pixels[o] = (byte)((r << 3) | (r >> 2));
            pixels[o + 1] = (byte)((g << 3) | (g >> 2));
            pixels[o + 2] = (byte)((b << 3) | (b >> 2));
            pixels[o + 3] = (byte)((t & 1) != 0 ? 255 : 0);
        }
    }

    public static void WriteRgba5551(BufferWriter stream, Span<byte> pixels, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var o = i * 4;
            stream.WriteUInt16((ushort)(((pixels[o] & 0xF8) << 8) | ((pixels[o + 1] & 0xF8) << 3) | ((pixels[o + 2] & 0xF8) >> 2) | ((pixels[o + 3] & 0x80) >> 7)));
        }
    }

    public static void ReadRgb565(BufferReader stream, Span<byte> pixels, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var t = stream.ReadUInt16();
            var o = i * 4;
            var r = (t >> 11) & 31;
            var g = (t >> 5) & 63;
            var b = t & 31;
            pixels[o] = (byte)((r << 3) | (r >> 2));
            pixels[o + 1] = (byte)((g << 2) | (g >> 4));
            pixels[o + 2] = (byte)((b << 3) | (b >> 2));
            pixels[o + 3] = 255;
        }
    }

    public static void WriteRgb565(BufferWriter stream, Span<byte> pixels, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var o = i * 4;
            stream.WriteUInt16((ushort)(((pixels[o] & 0xF8) << 8) | ((pixels[o + 1] & 0xFC) << 3) | ((pixels[o + 2] & 0xF8) >> 3)));
        }
    }

    public static void ReadL8(BufferReader stream, Span<byte> pixels, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var l = stream.ReadUInt8();
            var o = i * 4;
            pixels[o] = l;
            pixels[o + 1] = l;
            pixels[o + 2] = l;
            pixels[o + 3] = 255;
        }
    }

    public static void WriteL8(BufferWriter stream, Span<byte> pixels, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var o = i * 4;
            stream.WriteUInt8((byte)ClampLuma(pixels[o], pixels[o + 1], pixels[o + 2]));
        }
    }

    public static void ReadLa88(BufferReader stream, Span<byte> pixels, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var t = stream.ReadUInt16();
            var o = i * 4;
            var l = (byte)((t >> 8) & 0xFF);
            pixels[o] = l;
            pixels[o + 1] = l;
            pixels[o + 2] = l;
            pixels[o + 3] = (byte)(t & 0xFF);
        }
    }

    public static void WriteLa88(BufferWriter stream, Span<byte> pixels, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var o = i * 4;
            stream.WriteUInt16((ushort)((ClampLuma(pixels[o], pixels[o + 1], pixels[o + 2]) << 8) | pixels[o + 3]));
        }
    }
}
