using SharpCompress.Compressors;
using SharpCompress.Compressors.BZip2;

namespace LiarUtil.Core.Core.Compression;

public static class Bzip2Codec
{
    public static byte[] Compress(ReadOnlySpan<byte> data)
    {
        using var output = new MemoryStream();
        using (var bzip2 = BZip2Stream.Create(output, CompressionMode.Compress, false, leaveOpen: true))
        {
            bzip2.Write(data);
            bzip2.Finish();
        }

        return output.ToArray();
    }

    public static byte[] Decompress(ReadOnlySpan<byte> data, int expectedSize = 0)
    {
        using var source = new MemoryStream(data.ToArray(), false);
        using var bzip2 = BZip2Stream.Create(source, CompressionMode.Decompress, false);
        return StreamCodec.Read(bzip2, expectedSize);
    }
}
