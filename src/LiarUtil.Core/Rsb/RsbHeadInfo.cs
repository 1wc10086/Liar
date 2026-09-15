using LiarUtil.Core.Core.Binary;

namespace LiarUtil.Core.Rsb;

internal sealed class RsbHeadInfo
{
    public RsbVersion Version = RsbVersion.V3;
    public uint ContentLength;
    public uint ResourcePathLength;
    public uint ResourcePathBegin;
    public uint SubgroupNameLength;
    public uint SubgroupNameBegin;
    public uint SubgroupCount;
    public uint SubgroupInfoBegin;
    public uint SubgroupInfoSize = RsbConstants.SubgroupRecordSize;
    public uint GroupCount;
    public uint GroupInfoBegin;
    public uint GroupInfoSize = RsbConstants.GroupRecordSize;
    public uint GroupNameLength;
    public uint GroupNameBegin;
    public uint PoolCount;
    public uint PoolInfoBegin;
    public uint PoolInfoSize = RsbConstants.PoolRecordSize;
    public uint TextureCount;
    public uint TextureInfoBegin;
    public uint TextureInfoSize = RsbConstants.TextureRecordSize;
    public uint ResourceDefinitionBegin;
    public uint ResourceDefinitionDataBegin;
    public uint ResourceDefinitionStringBegin;
    public uint ContentWithoutDefinitions;

    public static RsbHeadInfo Read(BufferReader reader)
    {
        var magic = reader.ReadInt32();
        if (magic != RsbConstants.RsbMagic)
        {
            throw new InvalidDataException("Invalid RSB magic marker");
        }

        var result = new RsbHeadInfo { Version = RsbVersionExtensions.Parse(reader.ReadInt32()) };
        _ = reader.ReadUInt32();
        result.ContentLength = reader.ReadUInt32();
        result.ResourcePathLength = reader.ReadUInt32();
        result.ResourcePathBegin = reader.ReadUInt32();
        _ = reader.ReadUInt32();
        _ = reader.ReadUInt32();
        result.SubgroupNameLength = reader.ReadUInt32();
        result.SubgroupNameBegin = reader.ReadUInt32();
        result.SubgroupCount = reader.ReadUInt32();
        result.SubgroupInfoBegin = reader.ReadUInt32();
        result.SubgroupInfoSize = reader.ReadUInt32();
        result.GroupCount = reader.ReadUInt32();
        result.GroupInfoBegin = reader.ReadUInt32();
        result.GroupInfoSize = reader.ReadUInt32();
        result.GroupNameLength = reader.ReadUInt32();
        result.GroupNameBegin = reader.ReadUInt32();
        result.PoolCount = reader.ReadUInt32();
        result.PoolInfoBegin = reader.ReadUInt32();
        result.PoolInfoSize = reader.ReadUInt32();
        result.TextureCount = reader.ReadUInt32();
        result.TextureInfoBegin = reader.ReadUInt32();
        result.TextureInfoSize = reader.ReadUInt32();
        result.ResourceDefinitionBegin = reader.ReadUInt32();
        result.ResourceDefinitionDataBegin = reader.ReadUInt32();
        result.ResourceDefinitionStringBegin = reader.ReadUInt32();
        if (result.Version == RsbVersion.V4)
        {
            result.ContentWithoutDefinitions = reader.ReadUInt32();
        }

        return result;
    }

    public void Write(BufferWriter writer)
    {
        writer.WriteInt32(RsbConstants.RsbMagic);
        writer.WriteInt32((int)Version);
        writer.WriteUInt32(Version.HeaderFlag());
        writer.WriteUInt32(ContentLength);
        writer.WriteUInt32(ResourcePathLength);
        writer.WriteUInt32(ResourcePathBegin);
        writer.WriteUInt32(0);
        writer.WriteUInt32(0);
        writer.WriteUInt32(SubgroupNameLength);
        writer.WriteUInt32(SubgroupNameBegin);
        writer.WriteUInt32(SubgroupCount);
        writer.WriteUInt32(SubgroupInfoBegin);
        writer.WriteUInt32(SubgroupInfoSize);
        writer.WriteUInt32(GroupCount);
        writer.WriteUInt32(GroupInfoBegin);
        writer.WriteUInt32(GroupInfoSize);
        writer.WriteUInt32(GroupNameLength);
        writer.WriteUInt32(GroupNameBegin);
        writer.WriteUInt32(PoolCount);
        writer.WriteUInt32(PoolInfoBegin);
        writer.WriteUInt32(PoolInfoSize);
        writer.WriteUInt32(TextureCount);
        writer.WriteUInt32(TextureInfoBegin);
        writer.WriteUInt32(TextureInfoSize);
        writer.WriteUInt32(ResourceDefinitionBegin);
        writer.WriteUInt32(ResourceDefinitionDataBegin);
        writer.WriteUInt32(ResourceDefinitionStringBegin);
        if (Version == RsbVersion.V4)
        {
            var size = ContentWithoutDefinitions != 0
                ? ContentWithoutDefinitions
                : ResourceDefinitionBegin != 0 ? ResourceDefinitionBegin : ContentLength;
            writer.WriteUInt32(size);
        }
    }
}
