namespace LiarUtil.Core.RsbPatch;

internal static class VcdiffFormat
{
    public const byte SourceFlag = 0x01;

    public const byte StandardHeader = 0x00;

    public const byte ExtendedHeader = 0x53;

    private static readonly byte[] StandardDelta = { 0xD6, 0xC3, 0xC4, StandardHeader, 0x00 };

    private static readonly byte[] ExtendedDelta = { 0xD6, 0xC3, 0xC4, ExtendedHeader, 0x00 };

    public static byte[] DeltaHeader(bool interleaved) => (byte[])(interleaved ? ExtendedDelta : StandardDelta).Clone();
}
