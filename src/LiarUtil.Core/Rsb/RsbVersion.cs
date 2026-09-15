namespace LiarUtil.Core.Rsb;

public enum RsbVersion
{
    V1 = 1,
    V3 = 3,
    V4 = 4,
}

internal static class RsbVersionExtensions
{
    public static RsbVersion Parse(int value) => value switch
    {
        1 => RsbVersion.V1,
        3 => RsbVersion.V3,
        4 => RsbVersion.V4,
        _ => throw new InvalidDataException($"Unsupported RSB version: {value}"),
    };

    public static bool HasSubgroupLocale(this RsbVersion version) => version != RsbVersion.V1;

    public static bool HasSubgroupTextureRange(this RsbVersion version) => version != RsbVersion.V1;

    public static uint GroupRecordSize(this RsbVersion version) =>
        version == RsbVersion.V1 ? RsbConstants.GroupRecordSizeV1 : RsbConstants.GroupRecordSize;

    public static uint SubgroupRecordSize(this RsbVersion version) =>
        version == RsbVersion.V1 ? RsbConstants.SubgroupRecordSizeV1 : RsbConstants.SubgroupRecordSize;

    public static uint HeaderFlag(this RsbVersion version) => version == RsbVersion.V1 ? 1u : 0u;

    public static int HeaderSize(this RsbVersion version) =>
        version == RsbVersion.V4 ? RsbConstants.HeaderSizeV4 : RsbConstants.HeaderSize;
}
