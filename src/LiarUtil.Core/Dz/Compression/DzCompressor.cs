using LiarUtil.Core.Core.Compression;
using LiarUtil.Core.Dz.Models;

namespace LiarUtil.Core.Dz.Compression;

internal static class DzCompressor
{
    public static DzCompressionMethod Normalize(DzCompressionMethod method)
    {
        if ((method & DzCompressionMethod.Dz) != 0)
        {
            return (method & ~DzCompressionMethod.Dz) | DzCompressionMethod.Store;
        }

        if ((method & (DzCompressionMethod.CombineBuffer | DzCompressionMethod.Mp3 | DzCompressionMethod.Jpeg | DzCompressionMethod.RandomAccess)) != 0)
        {
            return DzCompressionMethod.Store;
        }

        return method is DzCompressionMethod.None ? DzCompressionMethod.Store : method;
    }

    public static byte[] Compress(ReadOnlySpan<byte> data, DzCompressionMethod method)
    {
        if ((method & DzCompressionMethod.Zlib) != 0)
        {
            return GzipCodec.Compress(data);
        }

        if ((method & DzCompressionMethod.Bzip2) != 0)
        {
            return Bzip2Codec.Compress(data);
        }

        if ((method & DzCompressionMethod.Zero) != 0)
        {
            return [];
        }

        if ((method & DzCompressionMethod.Store) != 0)
        {
            return data.ToArray();
        }

        if ((method & DzCompressionMethod.Lzma) != 0)
        {
            return LzmaCodec.Compress(data);
        }

        return data.ToArray();
    }

    public static byte[] Decompress(ReadOnlySpan<byte> data, DzCompressionMethod method, int size)
    {
        if ((method & DzCompressionMethod.Zlib) != 0)
        {
            return DeflateCodec.DecompressGzipOrRaw(data, size);
        }

        if ((method & DzCompressionMethod.Bzip2) != 0)
        {
            return Bzip2Codec.Decompress(data);
        }

        if ((method & DzCompressionMethod.Zero) != 0)
        {
            return new byte[Math.Max(size, 0)];
        }

        if ((method & DzCompressionMethod.Store) != 0)
        {
            return data.ToArray();
        }

        if ((method & DzCompressionMethod.Lzma) != 0)
        {
            return LzmaCodec.Decompress(data);
        }

        return data.ToArray();
    }
}
