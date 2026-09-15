using LiarUtil.Core.Core.Binary;

namespace LiarUtil.Core.Rsb;

internal sealed class RsbSubgroupInfo
{
    public string Identifier = "";
    public uint Offset;
    public uint Size;
    public uint PoolIndex;
    public uint Compression;
    public uint ContentOffset;
    public uint GeneralOffset;
    public uint GeneralCompressedSize;
    public uint GeneralSize;
    public uint GeneralPoolSize;
    public uint TextureOffset;
    public uint TextureCompressedSize;
    public uint TextureSize;
    public uint TexturePoolSize;
    public uint TextureCount;
    public uint TextureBegin;

    public static RsbSubgroupInfo Read(BufferReader reader, RsbVersion version)
    {
        var result = new RsbSubgroupInfo { Identifier = RsbText.ReadFixed(reader, RsbConstants.GroupIdentifierSize) };
        result.Offset = reader.ReadUInt32();
        result.Size = reader.ReadUInt32();
        result.PoolIndex = reader.ReadUInt32();
        result.Compression = reader.ReadUInt32();
        result.ContentOffset = reader.ReadUInt32();
        result.GeneralOffset = reader.ReadUInt32();
        result.GeneralCompressedSize = reader.ReadUInt32();
        result.GeneralSize = reader.ReadUInt32();
        result.GeneralPoolSize = reader.ReadUInt32();
        result.TextureOffset = reader.ReadUInt32();
        result.TextureCompressedSize = reader.ReadUInt32();
        result.TextureSize = reader.ReadUInt32();
        result.TexturePoolSize = reader.ReadUInt32();
        reader.Position += 16;
        if (version.HasSubgroupTextureRange())
        {
            result.TextureCount = reader.ReadUInt32();
            result.TextureBegin = reader.ReadUInt32();
        }

        return result;
    }

    public void Write(BufferWriter writer, RsbVersion version)
    {
        RsbText.WriteFixed(writer, Identifier, RsbConstants.GroupIdentifierSize);
        writer.WriteUInt32(Offset);
        writer.WriteUInt32(Size);
        writer.WriteUInt32(PoolIndex);
        writer.WriteUInt32(Compression);
        writer.WriteUInt32(ContentOffset);
        writer.WriteUInt32(GeneralOffset);
        writer.WriteUInt32(GeneralCompressedSize);
        writer.WriteUInt32(GeneralSize);
        writer.WriteUInt32(GeneralPoolSize);
        writer.WriteUInt32(TextureOffset);
        writer.WriteUInt32(TextureCompressedSize);
        writer.WriteUInt32(TextureSize);
        writer.WriteUInt32(TexturePoolSize);
        for (var i = 0; i < 4; i++)
        {
            writer.WriteUInt32(0);
        }

        if (version.HasSubgroupTextureRange())
        {
            writer.WriteUInt32(TextureCount);
            writer.WriteUInt32(TextureBegin);
        }
    }
}
