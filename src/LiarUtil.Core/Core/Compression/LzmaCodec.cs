using System.Buffers.Binary;
using SharpCompress.Compressors.LZMA;

namespace LiarUtil.Core.Core.Compression;

public static class LzmaCodec
{
    private const int PropertiesSize = 5;
    private const int HeaderSize = PropertiesSize + sizeof(long);

    public static byte[] Compress(ReadOnlySpan<byte> data)
    {
        using var output = new MemoryStream();
        using (var lzma = LzmaStream.Create(new LzmaEncoderProperties(), false, output))
        {
            output.Write(lzma.Properties);
            Span<byte> size = stackalloc byte[sizeof(long)];
            BinaryPrimitives.WriteInt64LittleEndian(size, data.Length);
            output.Write(size);
            lzma.Write(data);
        }

        return output.ToArray();
    }

    public static byte[] Decompress(ReadOnlySpan<byte> data)
    {
        if (data.Length < HeaderSize)
        {
            throw new CompressionException(LiarUtil.Core.Strings.LZMADataLengthInsufficient);
        }

        var recorded = BinaryPrimitives.ReadInt64LittleEndian(data[PropertiesSize..]);
        if (recorded < 0 || recorded > int.MaxValue)
        {
            throw new CompressionException(LiarUtil.Core.Strings.InvalidLZMARecordLength);
        }

        var payload = data[HeaderSize..].ToArray();
        using var source = new MemoryStream(payload, false);
        using var lzma = LzmaStream.Create(data[..PropertiesSize].ToArray(), source, payload.Length, recorded);
        return StreamCodec.Read(lzma, (int)recorded);
    }
}
