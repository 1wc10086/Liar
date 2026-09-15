using LiarUtil.Core.Dz.Compression;
using LiarUtil.Core.Dz.Models;

namespace LiarUtil.Core.Dz.Binary;

internal static class DzChunkReader
{
    public static byte[] Read(Stream stream, DzChunkInfo chunk)
    {
        var method = chunk.Method;
        if ((method & DzCompressionMethod.Dz) != 0)
        {
            return ReadRaw(stream, chunk, checked((int)chunk.RecordedSize));
        }

        if ((method & DzCompressionMethod.Zero) != 0)
        {
            return new byte[chunk.Size];
        }

        if ((method & (DzCompressionMethod.Zlib | DzCompressionMethod.Bzip2 | DzCompressionMethod.Lzma)) != 0)
        {
            return Decompress(ReadRaw(stream, chunk, ResolveStoredSize(stream, chunk)), chunk);
        }

        return ReadRaw(stream, chunk, checked((int)chunk.Size));
    }

    private static byte[] Decompress(byte[] payload, DzChunkInfo chunk)
    {
        var data = DzCompressor.Decompress(payload, chunk.Method, checked((int)chunk.Size));
        if (data.Length != chunk.Size)
        {
            Array.Resize(ref data, checked((int)chunk.Size));
        }

        return data;
    }

    private static int ResolveStoredSize(Stream stream, DzChunkInfo chunk) =>
        chunk.PayloadSize >= 0
            ? chunk.PayloadSize
            : checked((int)Math.Max(0, stream.Length - chunk.Offset));

    private static byte[] ReadRaw(Stream stream, DzChunkInfo chunk, int count)
    {
        stream.Position = chunk.Offset;
        var buffer = new byte[count];
        var offset = 0;
        while (offset < count)
        {
            var read = stream.Read(buffer, offset, count - offset);
            if (read <= 0)
            {
                throw new DzException(LiarUtil.Core.Strings.DataLengthInsufficient);
            }
            offset += read;
        }

        return buffer;
    }
}
