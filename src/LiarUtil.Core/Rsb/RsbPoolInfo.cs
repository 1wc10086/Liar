using LiarUtil.Core.Core.Binary;

namespace LiarUtil.Core.Rsb;

internal sealed class RsbPoolInfo
{
    public string Identifier = "";
    public uint TextureDataOffset;
    public uint TextureDataSize;
    public uint InstanceCount = 1;
    public uint Flags;
    public uint TextureCount;
    public uint TextureBegin;

    public static RsbPoolInfo Read(BufferReader reader)
    {
        var result = new RsbPoolInfo { Identifier = RsbText.ReadFixed(reader, RsbConstants.GroupIdentifierSize) };
        result.TextureDataOffset = reader.ReadUInt32();
        result.TextureDataSize = reader.ReadUInt32();
        result.InstanceCount = reader.ReadUInt32();
        result.Flags = reader.ReadUInt32();
        result.TextureCount = reader.ReadUInt32();
        result.TextureBegin = reader.ReadUInt32();
        return result;
    }

    public void Write(BufferWriter writer)
    {
        RsbText.WriteFixed(writer, Identifier, RsbConstants.GroupIdentifierSize);
        writer.WriteUInt32(TextureDataOffset);
        writer.WriteUInt32(TextureDataSize);
        writer.WriteUInt32(InstanceCount);
        writer.WriteUInt32(Flags);
        writer.WriteUInt32(TextureCount);
        writer.WriteUInt32(TextureBegin);
    }
}
