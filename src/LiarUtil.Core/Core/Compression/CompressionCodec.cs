using System.IO.Compression;

namespace LiarUtil.Core.Core.Compression;

public static class CompressionCodec
{
    extension(CompressionAlgorithm algorithm)
    {
        public bool IsStore => algorithm is CompressionAlgorithm.Store;

        public string DisplayName => algorithm switch
        {
            CompressionAlgorithm.Deflate => "Deflate",
            CompressionAlgorithm.Zlib => "Zlib",
            CompressionAlgorithm.Gzip => "Gzip",
            CompressionAlgorithm.Bzip2 => "Bzip2",
            CompressionAlgorithm.Lzma => "Lzma",
            _ => "Store",
        };
    }

    public static byte[] Compress(
        ReadOnlySpan<byte> data,
        CompressionAlgorithm algorithm,
        CompressionLevel level = CompressionLevel.Optimal) => algorithm switch
    {
        CompressionAlgorithm.Deflate => DeflateCodec.Compress(data, level),
        CompressionAlgorithm.Zlib => ZlibCodec.Compress(data, level),
        CompressionAlgorithm.Gzip => GzipCodec.Compress(data, level),
        CompressionAlgorithm.Bzip2 => Bzip2Codec.Compress(data),
        CompressionAlgorithm.Lzma => LzmaCodec.Compress(data),
        _ => data.ToArray(),
    };

    public static byte[] Decompress(
        ReadOnlySpan<byte> data,
        CompressionAlgorithm algorithm,
        int expectedSize = 0) => algorithm switch
    {
        CompressionAlgorithm.Deflate => DeflateCodec.Decompress(data, expectedSize),
        CompressionAlgorithm.Zlib => ZlibCodec.Decompress(data, expectedSize),
        CompressionAlgorithm.Gzip => GzipCodec.Decompress(data, expectedSize),
        CompressionAlgorithm.Bzip2 => Bzip2Codec.Decompress(data, expectedSize),
        CompressionAlgorithm.Lzma => LzmaCodec.Decompress(data),
        _ => data.ToArray(),
    };

    public static bool TryDecompress(
        ReadOnlySpan<byte> data,
        CompressionAlgorithm algorithm,
        out byte[] result,
        int expectedSize = 0)
    {
        try
        {
            result = Decompress(data, algorithm, expectedSize);
            return true;
        }
        catch (Exception exception) when (exception is InvalidDataException or CompressionException or IOException)
        {
            result = [];
            return false;
        }
    }
}
