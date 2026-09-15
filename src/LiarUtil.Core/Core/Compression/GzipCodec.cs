using System.IO.Compression;

namespace LiarUtil.Core.Core.Compression;

public static class GzipCodec
{
    public static byte[] Compress(ReadOnlySpan<byte> data, CompressionLevel level = CompressionLevel.Optimal)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, level, leaveOpen: true))
        {
            gzip.Write(data);
        }

        return output.ToArray();
    }

    public static byte[] Decompress(ReadOnlySpan<byte> data, int expectedSize = 0) =>
        StreamCodec.Read(new GZipStream(new MemoryStream(data.ToArray(), false), CompressionMode.Decompress), expectedSize);
}
