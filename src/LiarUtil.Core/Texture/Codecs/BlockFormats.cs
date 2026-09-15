using LiarUtil.Core.Core.Binary;

namespace LiarUtil.Core.Texture.Codecs;

internal static class BlockFormats
{
    public static void ReadRgb565Block(BufferReader stream, int width, int height, Span<byte> pixels)
    {
        for (var y = 0; y < height; y += 32)
        {
            for (var x = 0; x < width; x += 32)
            {
                for (var j = 0; j < 32; j++)
                {
                    for (var k = 0; k < 32; k++)
                    {
                        var value = stream.ReadUInt16();
                        if (y + j < height && x + k < width)
                        {
                            var o = ((y + j) * width + x + k) * 4;
                            pixels[o] = (byte)((value & 0xF800) >> 8);
                            pixels[o + 1] = (byte)((value & 0x7E0) >> 3);
                            pixels[o + 2] = (byte)((value & 0x1F) << 3);
                            pixels[o + 3] = 255;
                        }
                    }
                }
            }
        }
    }

    public static uint WriteRgb565Block(BufferWriter stream, int width, int height, Span<byte> pixels)
    {
        var newWidth = width;
        if ((newWidth & 31) != 0)
        {
            newWidth |= 31;
            newWidth++;
        }

        for (var y = 0; y < height; y += 32)
        {
            for (var x = 0; x < width; x += 32)
            {
                for (var j = 0; j < 32; j++)
                {
                    for (var k = 0; k < 32; k++)
                    {
                        if (y + j < height && x + k < width)
                        {
                            var o = ((y + j) * width + x + k) * 4;
                            stream.WriteUInt16((ushort)(((pixels[o + 2] & 0xF8) >> 3) | ((pixels[o + 1] & 0xFC) << 3) | ((pixels[o] & 0xF8) << 8)));
                        }
                        else
                        {
                            stream.WriteUInt16(0);
                        }
                    }
                }
            }
        }

        return (uint)(newWidth << 1);
    }

    public static void ReadRgba4444Block(BufferReader stream, int width, int height, Span<byte> pixels)
    {
        for (var y = 0; y < height; y += 32)
        {
            for (var x = 0; x < width; x += 32)
            {
                for (var j = 0; j < 32; j++)
                {
                    for (var k = 0; k < 32; k++)
                    {
                        var value = stream.ReadUInt16();
                        if (y + j < height && x + k < width)
                        {
                            var o = ((y + j) * width + x + k) * 4;
                            pixels[o] = (byte)(((value >> 12) << 4) | (value >> 12));
                            pixels[o + 1] = (byte)((((value & 0xF00) >> 8) << 4) | ((value & 0xF00) >> 8));
                            pixels[o + 2] = (byte)((((value & 0xF0) >> 4) << 4) | ((value & 0xF0) >> 4));
                            pixels[o + 3] = (byte)(((value & 0xF) << 4) | (value & 0xF));
                        }
                    }
                }
            }
        }
    }

    public static uint WriteRgba4444Block(BufferWriter stream, int width, int height, Span<byte> pixels)
    {
        var newWidth = width;
        if ((newWidth & 31) != 0)
        {
            newWidth |= 31;
            newWidth++;
        }

        for (var y = 0; y < height; y += 32)
        {
            for (var x = 0; x < width; x += 32)
            {
                for (var j = 0; j < 32; j++)
                {
                    for (var k = 0; k < 32; k++)
                    {
                        if (y + j < height && x + k < width)
                        {
                            var o = ((y + j) * width + x + k) * 4;
                            stream.WriteUInt16((ushort)((pixels[o + 3] >> 4) | (pixels[o + 2] & 0xF0) | ((pixels[o + 1] & 0xF0) << 4) | ((pixels[o] & 0xF0) << 8)));
                        }
                        else
                        {
                            stream.WriteUInt16(0);
                        }
                    }
                }
            }
        }

        return (uint)(newWidth << 1);
    }

    public static void ReadRgba5551Block(BufferReader stream, int width, int height, Span<byte> pixels)
    {
        for (var y = 0; y < height; y += 32)
        {
            for (var x = 0; x < width; x += 32)
            {
                for (var j = 0; j < 32; j++)
                {
                    for (var k = 0; k < 32; k++)
                    {
                        var value = stream.ReadUInt16();
                        if (y + j < height && x + k < width)
                        {
                            var o = ((y + j) * width + x + k) * 4;
                            pixels[o] = (byte)(((value >> 11) << 3) | ((value >> 11) >> 2));
                            pixels[o + 1] = (byte)((((value & 0x7C0) >> 6) << 3) | (((value & 0x7C0) >> 6) >> 2));
                            pixels[o + 2] = (byte)((((value & 0x3E) >> 1) << 3) | (((value & 0x3E) >> 1) >> 2));
                            pixels[o + 3] = (byte)((value & 1) != 0 ? 255 : 0);
                        }
                    }
                }
            }
        }
    }

    public static uint WriteRgba5551Block(BufferWriter stream, int width, int height, Span<byte> pixels)
    {
        var newWidth = width;
        if ((newWidth & 31) != 0)
        {
            newWidth |= 31;
            newWidth++;
        }

        for (var y = 0; y < height; y += 32)
        {
            for (var x = 0; x < width; x += 32)
            {
                for (var j = 0; j < 32; j++)
                {
                    for (var k = 0; k < 32; k++)
                    {
                        if (y + j < height && x + k < width)
                        {
                            var o = ((y + j) * width + x + k) * 4;
                            stream.WriteUInt16((ushort)(((pixels[o + 3] & 0x80) >> 7) | ((pixels[o + 2] & 0xF8) >> 2) | ((pixels[o + 1] & 0xF8) << 3) | ((pixels[o] & 0xF8) << 8)));
                        }
                        else
                        {
                            stream.WriteUInt16(0);
                        }
                    }
                }
            }
        }

        return (uint)(newWidth << 1);
    }

    public static void ReadArgb8888Padding(BufferReader stream, int width, int height, int blockSize, Span<byte> pixels)
    {
        var offset = stream.Position;
        var index = 0;
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var t = stream.ReadUInt32();
                var o = index * 4;
                pixels[o] = (byte)((t >> 16) & 0xFF);
                pixels[o + 1] = (byte)((t >> 8) & 0xFF);
                pixels[o + 2] = (byte)(t & 0xFF);
                pixels[o + 3] = (byte)(t >> 24);
                index++;
            }

            stream.Position = offset + (y + 1) * blockSize;
        }
    }

    public static uint WriteArgb8888Padding(BufferWriter stream, int width, int height, int blockSize, Span<byte> pixels)
    {
        var padding = blockSize - (width << 2);
        var index = 0;
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var o = index * 4;
                stream.WriteUInt32((uint)((pixels[o + 3] << 24) | (pixels[o] << 16) | (pixels[o + 1] << 8) | pixels[o + 2]));
                index++;
            }

            for (var p = 0; p < padding; p++)
            {
                stream.WriteUInt8(0);
            }
        }

        return (uint)blockSize;
    }

    public static void ReadXrgb8888A8(BufferReader stream, int width, int height, Span<byte> pixels)
    {
        PixelFormats.ReadXrgb8888(stream, pixels, width * height);
        for (var i = 0; i < width * height; i++)
        {
            pixels[i * 4 + 3] = stream.ReadUInt8();
        }
    }

    public static uint WriteXrgb8888A8(BufferWriter stream, int width, int height, Span<byte> pixels)
    {
        PixelFormats.WriteXrgb8888(stream, pixels, width * height);
        for (var i = 0; i < width * height; i++)
        {
            stream.WriteUInt8(pixels[i * 4 + 3]);
        }

        return (uint)(width << 3);
    }

    public static void ReadXbgr8888A8(BufferReader stream, int width, int height, Span<byte> pixels)
    {
        for (var i = 0; i < width * height; i++)
        {
            var t = stream.ReadUInt32();
            var o = i * 4;
            pixels[o] = (byte)(t & 0xFF);
            pixels[o + 1] = (byte)((t >> 8) & 0xFF);
            pixels[o + 2] = (byte)((t >> 16) & 0xFF);
            pixels[o + 3] = 255;
        }

        for (var i = 0; i < width * height; i++)
        {
            pixels[i * 4 + 3] = stream.ReadUInt8();
        }
    }

    public static uint WriteXbgr8888A8(BufferWriter stream, int width, int height, Span<byte> pixels)
    {
        for (var i = 0; i < width * height; i++)
        {
            var o = i * 4;
            stream.WriteUInt32(0xFF000000u | (uint)(pixels[o + 2] << 16) | (uint)(pixels[o + 1] << 8) | pixels[o]);
        }

        for (var i = 0; i < width * height; i++)
        {
            stream.WriteUInt8(pixels[i * 4 + 3]);
        }

        return (uint)(width << 3);
    }
}
