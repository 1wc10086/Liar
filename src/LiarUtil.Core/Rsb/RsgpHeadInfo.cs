using LiarUtil.Core.Core.Binary;

namespace LiarUtil.Core.Rsb;

internal sealed class RsgpHeadInfo
{
    public int Magic = RsbConstants.RsgpMagic;
    public int Version = 3;
    public uint Flags = 1;
    public uint FileOffset;
    public uint GeneralOffset;
    public uint GeneralCompressedSize;
    public uint GeneralSize;
    public uint TextureOffset;
    public uint TextureCompressedSize;
    public uint TextureSize;
    public uint FileListLength;
    public uint FileListBegin = RsbConstants.RsgpFileListBegin;

    public void Read(BufferReader reader)
    {
        Magic = reader.ReadInt32();
        Version = reader.ReadInt32();
        _ = reader.ReadInt32();
        _ = reader.ReadInt32();
        Flags = reader.ReadUInt32();
        FileOffset = reader.ReadUInt32();
        GeneralOffset = reader.ReadUInt32();
        GeneralCompressedSize = reader.ReadUInt32();
        GeneralSize = reader.ReadUInt32();
        _ = reader.ReadInt32();
        TextureOffset = reader.ReadUInt32();
        TextureCompressedSize = reader.ReadUInt32();
        TextureSize = reader.ReadUInt32();
        _ = reader.ReadInt32();
        _ = reader.ReadInt32();
        _ = reader.ReadInt32();
        _ = reader.ReadInt32();
        _ = reader.ReadInt32();
        FileListLength = reader.ReadUInt32();
        FileListBegin = reader.ReadUInt32();
        _ = reader.ReadInt32();
        _ = reader.ReadInt32();
        _ = reader.ReadInt32();
    }

    public void Write(BufferWriter writer)
    {
        writer.WriteInt32(Magic);
        writer.WriteInt32(Version);
        writer.WriteInt32(0);
        writer.WriteInt32(0);
        writer.WriteUInt32(Flags);
        writer.WriteUInt32(FileOffset);
        writer.WriteUInt32(GeneralOffset);
        writer.WriteUInt32(GeneralCompressedSize);
        writer.WriteUInt32(GeneralSize);
        writer.WriteInt32(0);
        writer.WriteUInt32(TextureOffset);
        writer.WriteUInt32(TextureCompressedSize);
        writer.WriteUInt32(TextureSize);
        writer.WriteInt32(0);
        writer.WriteInt32(0);
        writer.WriteInt32(0);
        writer.WriteInt32(0);
        writer.WriteInt32(0);
        writer.WriteUInt32(FileListLength);
        writer.WriteUInt32(FileListBegin);
        writer.WriteInt32(0);
        writer.WriteInt32(0);
        writer.WriteInt32(0);
    }
}
