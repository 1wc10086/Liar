using LiarUtil.Core.Core.Binary;

namespace LiarUtil.Core.Core.Compression;

public static class CompressedBlob
{
    public const int Magic = unchecked((int)0xDEADFED4);

    public static byte[] Unwrap(byte[] data, int headerBytes, bool bigEndian)
    {
        if (data.Length < sizeof(int))
        {
            return data;
        }

        var reader = new BufferReader(data) { BigEndian = bigEndian };
        if (reader.PeekInt32() != Magic || data.Length <= headerBytes)
        {
            return data;
        }

        return ZlibCodec.Decompress(data.AsSpan(headerBytes));
    }

    public static byte[] Wrap(byte[] body, int headerBytes, bool bigEndian, bool compress)
    {
        if (!compress || headerBytes <= 0)
        {
            return body;
        }

        var writer = new BufferWriter(headerBytes + body.Length) { BigEndian = bigEndian };
        writer.WriteInt32(Magic);
        if (headerBytes == 16)
        {
            writer.WriteInt32(0);
            writer.WriteInt32(body.Length);
            writer.WriteInt32(0);
        }
        else
        {
            writer.WriteInt32(body.Length);
        }

        writer.WriteBytes(ZlibCodec.Compress(body, 6));
        return writer.ToArray();
    }
}
