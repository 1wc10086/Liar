using LiarUtil.Core.Core.Binary;

namespace LiarUtil.Core.Xpr;

internal readonly record struct XprFileEntry(uint Type, uint FileOffset, uint FileSize, uint PathOffset)
{
    public const int Size = XprFormat.EntrySize;

    public static XprFileEntry Read(ReadOnlySpan<byte> data)
    {
        if (data.Length < Size)
        {
            throw new XprException("XPR index entry is truncated");
        }

        var reader = new BufferReader(data, ByteOrder.Big);
        return new XprFileEntry(reader.ReadUInt32(), reader.ReadUInt32(), reader.ReadUInt32(), reader.ReadUInt32());
    }

    public void Write(Span<byte> data)
    {
        if (data.Length < Size)
        {
            throw new XprException("XPR index entry buffer is too small");
        }

        var writer = new BufferWriter(Size) { Order = ByteOrder.Big };
        writer.WriteUInt32(Type);
        writer.WriteUInt32(FileOffset);
        writer.WriteUInt32(FileSize);
        writer.WriteUInt32(PathOffset);
        writer.Span.CopyTo(data);
    }
}
