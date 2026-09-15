using AstcSharp;
using AstcSharp.Core;

namespace LiarUtil.Core.Texture.Codecs;

internal static class AstcCodec
{
    public static void Decode(ReadOnlySpan<byte> blocks, int width, int height, int blockWidth, int blockHeight, Span<byte> pixels)
    {
        var footprint = Footprint.FromFootprintType(FootprintFromSize(blockWidth, blockHeight));
        using var source = new MemoryStream(blocks.ToArray(), writable: false);
        using var destination = new MemoryStream(width * height * 4);
        AstcDecoder.DecompressImage(source, destination, width, height, footprint);
        destination.Position = 0;
        destination.ReadExactly(pixels);
    }

    public static byte[] Encode(ReadOnlyMemory<byte> pixels, int width, int height, int blockWidth, int blockHeight)
    {
        var footprint = Footprint.FromFootprintType(FootprintFromSize(blockWidth, blockHeight));
        return AstcEncoder.CompressImage(pixels, width, height, footprint);
    }

    private static FootprintType FootprintFromSize(int width, int height) => (width, height) switch
    {
        (4, 4) => FootprintType.Footprint4x4,
        (5, 5) => FootprintType.Footprint5x5,
        (6, 6) => FootprintType.Footprint6x6,
        (8, 8) => FootprintType.Footprint8x8,
        _ => throw new ArgumentException($"Unsupported PTX ASTC footprint: {width}x{height}"),
    };
}

internal static class Etc1Codec
{
    public static void Decode(ReadOnlySpan<byte> blocks, int width, int height, Span<byte> pixels)
    {
        Span<uint> blockPixels = stackalloc uint[16];
        var position = 0;
        for (var by = 0; by < (height + 3) / 4; by++)
        {
            for (var bx = 0; bx < (width + 3) / 4; bx++)
            {
                var block = System.Buffers.Binary.BinaryPrimitives.ReadUInt64LittleEndian(blocks[position..]);
                position += 8;
                Etc1.DecodePart(block, blockPixels);
                for (var sy = 0; sy < 4; sy++)
                {
                    for (var sx = 0; sx < 4; sx++)
                    {
                        var y = by * 4 + sy;
                        var x = bx * 4 + sx;
                        if (y < height && x < width)
                        {
                            var p = blockPixels[sy * 4 + sx];
                            var o = (y * width + x) * 4;
                            pixels[o] = (byte)(p & 0xFF);
                            pixels[o + 1] = (byte)((p >> 8) & 0xFF);
                            pixels[o + 2] = (byte)((p >> 16) & 0xFF);
                            pixels[o + 3] = 255;
                        }
                    }
                }
            }
        }
    }

    public static byte[] Encode(ReadOnlySpan<byte> pixels, int width, int height)
    {
        var alignedWidth = (width + 3) & ~3;
        var alignedHeight = (height + 3) & ~3;
        var source = new uint[alignedWidth * alignedHeight];
        for (var i = 0; i < source.Length; i++)
        {
            source[i] = 0xFF000000u;
        }

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var o = (y * width + x) * 4;
                source[y * alignedWidth + x] = 0xFF000000u | (uint)(pixels[o] << 16) | (uint)(pixels[o + 1] << 8) | pixels[o + 2];
            }
        }

        var blocks = new ulong[(alignedWidth / 4) * (alignedHeight / 4)];
        Etc1.CompressRgb(source, blocks, (uint)blocks.Length, alignedWidth);
        var output = new byte[blocks.Length * 8];
        for (var i = 0; i < blocks.Length; i++)
        {
            System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(output.AsSpan(i * 8, 8), blocks[i]);
        }

        return output;
    }
}
