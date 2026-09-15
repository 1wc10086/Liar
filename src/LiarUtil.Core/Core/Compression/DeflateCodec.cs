using System.IO.Compression;

namespace LiarUtil.Core.Core.Compression;

public static class DeflateCodec
{
    private const int GzipHeaderSize = 10;

    public static byte[] Compress(ReadOnlySpan<byte> data, CompressionLevel level = CompressionLevel.Optimal)
    {
        using var output = new MemoryStream();
        using (var deflate = new DeflateStream(output, level, leaveOpen: true))
        {
            deflate.Write(data);
        }

        return output.ToArray();
    }

    public static byte[] Decompress(ReadOnlySpan<byte> data, int expectedSize = 0) =>
        StreamCodec.Read(new DeflateStream(new MemoryStream(data.ToArray(), false), CompressionMode.Decompress), expectedSize);

    public static byte[] DecompressGzipOrRaw(ReadOnlySpan<byte> data, int expectedSize = 0)
    {
        try
        {
            return GzipCodec.Decompress(data, expectedSize);
        }
        catch (InvalidDataException)
        {
            if (data.Length <= GzipHeaderSize)
            {
                throw;
            }

            return Decompress(data[GzipHeaderSize..], expectedSize);
        }
    }
}
