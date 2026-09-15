using System.IO.Compression;
using LiarUtil.Core.Core.Compression;
using LiarUtil.Core.Core.Hashing;

namespace LiarUtil.Core.RsbPatch;

internal static class RsbPatchCompression
{
    public static byte[] Hash(ReadOnlySpan<byte> data) => Hashes.Md5(data);

    public static byte[] Compress(ReadOnlySpan<byte> data) => ZlibCodec.Compress(data, CompressionLevel.SmallestSize);

    public static byte[] Decompress(ReadOnlySpan<byte> data, int expectedSize) =>
        ZlibCodec.DecompressExact(data, expectedSize);
}
