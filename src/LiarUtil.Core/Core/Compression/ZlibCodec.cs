using System.IO.Compression;

namespace LiarUtil.Core.Core.Compression;

public static class ZlibCodec
{
    public static byte[] Compress(ReadOnlySpan<byte> data, CompressionLevel level = CompressionLevel.Optimal) =>
        data.IsEmpty ? EmptyPayload(level) : CompressCore(data, level);

    public static byte[] Compress(ReadOnlySpan<byte> data, int level)
    {
        var requested = FromLevel(level);
        return data.IsEmpty ? EmptyPayload(level) : CompressCore(data, requested);
    }

    public static byte[] Decompress(ReadOnlySpan<byte> data, int expectedSize = 0)
    {
        using var source = new MemoryStream(data.ToArray(), false);
        using var zlib = new ZLibStream(source, CompressionMode.Decompress);
        return StreamCodec.Read(zlib, expectedSize);
    }

    public static byte[] DecompressExact(ReadOnlySpan<byte> data, int expectedSize)
    {
        if (data.IsEmpty)
        {
            return [];
        }

        using var source = new MemoryStream(data.ToArray(), false);
        using var zlib = new ZLibStream(source, CompressionMode.Decompress);
        var result = new byte[expectedSize];
        zlib.ReadExactly(result);
        return result;
    }

    public static byte[] DecompressVerified(ReadOnlySpan<byte> data, int expectedSize)
    {
        if (data.IsEmpty)
        {
            return [];
        }

        using var source = new MemoryStream(data.ToArray(), false);
        using var zlib = new ZLibStream(source, CompressionMode.Decompress);
        var result = new byte[expectedSize];
        zlib.ReadExactly(result);
        if (zlib.ReadByte() != -1)
        {
            throw new CompressionException(LiarUtil.Core.Strings.ZlibDecompressedDataLengthDoesNotMatchExpectation);
        }

        return result;
    }

    internal static CompressionLevel FromLevel(int level) => level switch
    {
        <= 1 => CompressionLevel.Fastest,
        >= 9 => CompressionLevel.SmallestSize,
        _ => CompressionLevel.Optimal,
    };

    private static byte[] EmptyPayload(CompressionLevel level) =>
    [
        0x78,
        level switch
        {
            CompressionLevel.Fastest => (byte)0x01,
            CompressionLevel.SmallestSize => (byte)0xDA,
            _ => (byte)0x9C,
        },
        0x03, 0x00, 0x00, 0x00, 0x00, 0x01,
    ];

    private static byte[] EmptyPayload(int level) =>
    [
        0x78,
        level <= 1 ? (byte)0x01 : level >= 9 ? (byte)0xDA : (byte)0x9C,
        0x03, 0x00, 0x00, 0x00, 0x00, 0x01,
    ];

    private static byte[] CompressCore(ReadOnlySpan<byte> data, CompressionLevel level)
    {
        using var output = new MemoryStream();
        using (var zlib = new ZLibStream(output, level, leaveOpen: true))
        {
            zlib.Write(data);
        }

        return output.ToArray();
    }
}
