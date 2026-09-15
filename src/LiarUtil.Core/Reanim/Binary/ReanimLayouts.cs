namespace LiarUtil.Core.Reanim.Binary;

internal enum ReanimStringField
{
    Image,
    ImageId,
    ImageResource,
    Image2,
    Image2Resource,
    Font,
    Text,
}

internal sealed record ReanimLayout(
    int Magic,
    int LeadingInt32Count,
    bool BigEndian,
    bool WrapOnEncode,
    bool UnwrapOnDecode,
    int CompressionHeaderBytes,
    int MarkerPrefixInts,
    int Marker,
    int TrackInfoPrefixBytes,
    int TrackInfoSuffixBytes,
    int TransformStride,
    IReadOnlyList<ReanimStringField> StringFields);

internal static class ReanimLayouts
{
    private static readonly ReanimStringField[] ClassicFields =
    [
        ReanimStringField.Image, ReanimStringField.Font, ReanimStringField.Text,
    ];

    private static readonly ReanimStringField[] IntegerImageFields =
    [
        ReanimStringField.ImageId, ReanimStringField.Font, ReanimStringField.Text,
    ];

    private static readonly ReanimStringField[] TvFields =
    [
        ReanimStringField.Image, ReanimStringField.ImageResource, ReanimStringField.Image2,
        ReanimStringField.Image2Resource, ReanimStringField.Font, ReanimStringField.Text,
    ];

    public static ReanimLayout Get(ReanimPlatform platform) => platform switch
    {
        ReanimPlatform.Pc => new ReanimLayout(
            Magic: unchecked((int)0xB393B4C0), LeadingInt32Count: 2, BigEndian: false,
            WrapOnEncode: true, UnwrapOnDecode: true, CompressionHeaderBytes: 8,
            MarkerPrefixInts: 1, Marker: 0xC, TrackInfoPrefixBytes: 8, TrackInfoSuffixBytes: 0,
            TransformStride: 0x2C, StringFields: ClassicFields),
        ReanimPlatform.Phone32 => new ReanimLayout(
            Magic: unchecked((int)0xFF2565B5), LeadingInt32Count: 2, BigEndian: false,
            WrapOnEncode: true, UnwrapOnDecode: true, CompressionHeaderBytes: 8,
            MarkerPrefixInts: 1, Marker: 0x10, TrackInfoPrefixBytes: 12, TrackInfoSuffixBytes: 0,
            TransformStride: 0x2C, StringFields: IntegerImageFields),
        ReanimPlatform.Phone64 => new ReanimLayout(
            Magic: unchecked((int)0xC046E570), LeadingInt32Count: 3, BigEndian: false,
            WrapOnEncode: true, UnwrapOnDecode: true, CompressionHeaderBytes: 16,
            MarkerPrefixInts: 2, Marker: 0x20, TrackInfoPrefixBytes: 24, TrackInfoSuffixBytes: 4,
            TransformStride: 0x38, StringFields: IntegerImageFields),
        ReanimPlatform.GameConsole => new ReanimLayout(
            Magic: 0, LeadingInt32Count: 2, BigEndian: true,
            WrapOnEncode: false, UnwrapOnDecode: true, CompressionHeaderBytes: 8,
            MarkerPrefixInts: 1, Marker: 0xC, TrackInfoPrefixBytes: 8, TrackInfoSuffixBytes: 0,
            TransformStride: 0x2C, StringFields: ClassicFields),
        ReanimPlatform.Tv => new ReanimLayout(
            Magic: 0, LeadingInt32Count: 2, BigEndian: false,
            WrapOnEncode: true, UnwrapOnDecode: true, CompressionHeaderBytes: 8,
            MarkerPrefixInts: 1, Marker: 0x14, TrackInfoPrefixBytes: 12, TrackInfoSuffixBytes: 4,
            TransformStride: 0x30, StringFields: TvFields),
        _ => throw new ArgumentOutOfRangeException(nameof(platform)),
    };

    public static bool IsWp(ReanimPlatform platform) => platform == ReanimPlatform.Wp;
}
