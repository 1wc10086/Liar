namespace LiarUtil.Core.Trail.Binary;

internal sealed record TrailLayout(
    int Magic,
    int LeadingInt32Count,
    bool BigEndian,
    bool IntegerImage,
    bool HasImageResource,
    bool WrapOnEncode,
    bool UnwrapOnDecode,
    int CompressionHeaderBytes,
    int HeaderPaddingBytes,
    IReadOnlyList<string> TrackOrder);

internal static class TrailLayouts
{
    private static readonly string[] ClassicOrder =
    [
        "WidthOverLength", "WidthOverTime", "AlphaOverLength", "AlphaOverTime", "TrailDuration",
    ];

    private static readonly string[] WpOrder =
    [
        "TrailDuration", "WidthOverLength", "WidthOverTime", "AlphaOverLength", "AlphaOverTime",
    ];

    public static TrailLayout Get(TrailPlatform platform) => platform switch
    {
        TrailPlatform.Pc => new TrailLayout(
            Magic: unchecked((int)0xAB8B62B3), LeadingInt32Count: 2, BigEndian: false, IntegerImage: false,
            HasImageResource: false, WrapOnEncode: true, UnwrapOnDecode: true, CompressionHeaderBytes: 8, HeaderPaddingBytes: 40,
            TrackOrder: ClassicOrder),
        TrailPlatform.Phone32 => new TrailLayout(
            Magic: unchecked((int)0xAB8B62B3), LeadingInt32Count: 2, BigEndian: false, IntegerImage: true,
            HasImageResource: false, WrapOnEncode: true, UnwrapOnDecode: true, CompressionHeaderBytes: 8, HeaderPaddingBytes: 40,
            TrackOrder: ClassicOrder),
        TrailPlatform.Phone64 => new TrailLayout(
            Magic: unchecked((int)0x8488BC08), LeadingInt32Count: 3, BigEndian: false, IntegerImage: true,
            HasImageResource: false, WrapOnEncode: true, UnwrapOnDecode: true, CompressionHeaderBytes: 16, HeaderPaddingBytes: 84,
            TrackOrder: ClassicOrder),
        TrailPlatform.GameConsole => new TrailLayout(
            Magic: 0, LeadingInt32Count: 2, BigEndian: true, IntegerImage: false, HasImageResource: false,
            WrapOnEncode: false, UnwrapOnDecode: true, CompressionHeaderBytes: 8, HeaderPaddingBytes: 40, TrackOrder: ClassicOrder),
        TrailPlatform.Tv => new TrailLayout(
            Magic: unchecked((int)0xAB8B62B3), LeadingInt32Count: 2, BigEndian: false, IntegerImage: false,
            HasImageResource: true, WrapOnEncode: true, UnwrapOnDecode: true, CompressionHeaderBytes: 8, HeaderPaddingBytes: 40,
            TrackOrder: ClassicOrder),
        _ => throw new ArgumentOutOfRangeException(nameof(platform)),
    };

    public static bool IsWp(TrailPlatform platform) => platform == TrailPlatform.Wp;

    public static IReadOnlyList<string> WpTrackOrder => WpOrder;
}
