namespace LiarUtil.Core.Rsb;

internal static class RsbConstants
{
    public const int RsbMagic = 1920164401;
    public const int RsbMagicBigEndian = 828535666;
    public const int RsgpMagic = 1920165744;
    public const int SmfMagic = -559022380;
    public const int ResourceDefinitionMagic = 1919251249;

    public const int HeaderSize = 108;
    public const int HeaderSizeV4 = 112;

    public const uint GroupRecordSize = 0x484;
    public const uint GroupRecordSizeV1 = 0x284;
    public const uint SubgroupRecordSize = 0xCC;
    public const uint SubgroupRecordSizeV1 = 0xC4;
    public const uint PoolRecordSize = 0x98;
    public const uint TextureRecordSize = 0x10;
    public const uint TextureRecordSizeAlpha = 0x14;
    public const uint TextureRecordSizeAlphaScale = 0x18;

    public const uint RsgpFileListBegin = 0x5C;
    public const int GroupIdentifierSize = 0x80;

    public static long AlignTo4K(long offset) =>
        offset % 0x1000 == 0 ? offset : offset + 0x1000 - (offset % 0x1000);
}
