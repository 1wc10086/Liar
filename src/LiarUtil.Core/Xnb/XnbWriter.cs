using LiarUtil.Core.Core.Binary;

namespace LiarUtil.Core.Xnb;

public static class XnbWriter
{
    private const int HeaderSize = 10;

    public static byte[] Write(
        char platform,
        int version,
        XnbFlags flags,
        IReadOnlyList<XnbTypeReader> typeReaders,
        byte[] payload)
    {
        ArgumentNullException.ThrowIfNull(typeReaders);
        ArgumentNullException.ThrowIfNull(payload);
        if (version is not (4 or 5))
        {
            throw new XnbException(string.Format(LiarUtil.Core.Strings.UnsupportedXNBVersion0, version));
        }

        var table = WriteTypeReaders(typeReaders);
        var size = HeaderSize + table.Length + payload.Length;
        var writer = new BufferWriter(size);
        writer.WriteUInt8((byte)'X');
        writer.WriteUInt8((byte)'N');
        writer.WriteUInt8((byte)'B');
        writer.WriteUInt8((byte)platform);
        writer.WriteUInt8((byte)version);
        writer.WriteUInt8((byte)flags);
        writer.WriteUInt32((uint)size);
        writer.WriteBytes(table);
        writer.WriteBytes(payload);
        return writer.ToArray();
    }

    private static byte[] WriteTypeReaders(IReadOnlyList<XnbTypeReader> typeReaders)
    {
        var writer = new BufferWriter();
        writer.WriteVarUInt32((uint)typeReaders.Count);
        foreach (var typeReader in typeReaders)
        {
            var name = TextCodec.Encode(typeReader.Name, TextFormat.Utf8);
            writer.WriteVarUInt32((uint)name.Length);
            writer.WriteBytes(name);
            writer.WriteInt32(typeReader.Version);
        }

        writer.WriteVarUInt32(0);
        return writer.ToArray();
    }
}
