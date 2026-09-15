using LiarUtil.Core.Core.Binary;

namespace LiarUtil.Core.Pak.Binary;

internal static class PakPadding
{
    public static void Write(BufferWriter writer, int alignment)
    {
        var offset = (int)(writer.Position & (alignment - 1));
        var shortBlock = alignment - sizeof(ushort);
        var padding = offset switch
        {
            0 => shortBlock,
            _ when offset > shortBlock => alignment * 2 - sizeof(ushort) - offset,
            _ => shortBlock - offset,
        };
        writer.WriteUInt16(unchecked((ushort)padding));
        writer.WriteZeros(padding);
    }

    public static bool Read(BufferReader reader, out int padding)
    {
        padding = reader.ReadUInt16();
        reader.Skip(padding);
        return padding > PakBinaryLayout.X360PaddingThreshold;
    }
}
