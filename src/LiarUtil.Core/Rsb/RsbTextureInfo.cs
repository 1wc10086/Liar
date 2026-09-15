using LiarUtil.Core.Core.Binary;

namespace LiarUtil.Core.Rsb;

internal sealed class RsbTextureInfo
{
    public uint Width;
    public uint Height;
    public uint Pitch;
    public uint Format;
    public uint AdditionalByteCount;
    public uint Scale;

    public static RsbTextureInfo Read(BufferReader reader, uint recordSize)
    {
        var result = new RsbTextureInfo
        {
            Width = reader.ReadUInt32(),
            Height = reader.ReadUInt32(),
            Pitch = reader.ReadUInt32(),
            Format = reader.ReadUInt32(),
        };
        if (recordSize >= RsbConstants.TextureRecordSizeAlpha)
        {
            result.AdditionalByteCount = reader.ReadUInt32();
            result.Scale = recordSize == RsbConstants.TextureRecordSizeAlphaScale
                ? reader.ReadUInt32()
                : result.AdditionalByteCount == 0 ? 0u : 0x64u;
        }

        return result;
    }

    public void Write(BufferWriter writer, uint recordSize)
    {
        writer.WriteUInt32(Width);
        writer.WriteUInt32(Height);
        writer.WriteUInt32(Pitch);
        writer.WriteUInt32(Format);
        if (recordSize >= RsbConstants.TextureRecordSizeAlpha)
        {
            writer.WriteUInt32(AdditionalByteCount);
            if (recordSize == RsbConstants.TextureRecordSizeAlphaScale)
            {
                writer.WriteUInt32(Scale);
            }
        }
    }
}
