namespace LiarUtil.Core.RsbPatch;

internal static class RsbPatchFormat
{
    public const uint PatchMagic = 0x52534250u;

    public const uint BundleMagic = 0x72736231u;

    public const uint PacketMagic = 0x72736770u;

    public const int PatchVersion = 1;

    public const int BundleVersion = 4;

    public const int PackageInformationSize = 40;

    public const int PacketInformationSize = 152;

    public const int PacketNameSize = 128;

    public const int HashSize = 16;

    public const int IdentifierSize = 128;

    public const int SubgroupRecordSize = 0xCC;

    public const int PaddingUnit = 0x1000;

    public const uint CompressionGeneral = 0x02u;

    public const uint CompressionTexture = 0x01u;

    public static int Padding(int position, int unit = PaddingUnit)
    {
        var remainder = position % unit;
        return remainder == 0 ? 0 : unit - remainder;
    }

    public static int Align(int position, int unit = PaddingUnit) => position + Padding(position, unit);

    public static void EnsurePatchVersion(int version)
    {
        if (version != PatchVersion)
        {
            throw new RsbPatchException(string.Format(LiarUtil.Core.Strings.UnsupportedPatchVersion0, version));
        }
    }

    public static void EnsureBundleVersion(int version)
    {
        if (version != BundleVersion)
        {
            throw new RsbPatchException(string.Format(LiarUtil.Core.Strings.RsbPatchOnlySupportsRSBVersion0CurrentFile, BundleVersion, version));
        }
    }
}
