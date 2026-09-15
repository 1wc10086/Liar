using System.IO.Compression;

namespace LiarUtil.Core.Pak.Packing;

internal static class PakZlib
{
    public static byte[] Compress(ReadOnlySpan<byte> data)
    {
        using var output = new MemoryStream();
        using (var zlib = new ZLibStream(output, CompressionLevel.Optimal, leaveOpen: true))
        {
            zlib.Write(data);
        }

        return output.ToArray();
    }

    public static byte[] Decompress(ReadOnlySpan<byte> data, int expectedSize)
    {
        using var input = new MemoryStream(data.ToArray(), writable: false);
        using var zlib = new ZLibStream(input, CompressionMode.Decompress);
        using var output = expectedSize > 0 ? new MemoryStream(expectedSize) : new MemoryStream();
        zlib.CopyTo(output);
        return output.ToArray();
    }
}
