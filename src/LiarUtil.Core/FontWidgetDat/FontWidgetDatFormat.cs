namespace LiarUtil.Core.FontWidgetDat;

internal static class FontWidgetDatFormat
{
    public const int Magic = -16777029;
    public const int HeaderSize = 12;
    public const int ReservedHeaderWordCount = 8;
    public const int TrailerSize = 4;
    public const ushort MinAscii = 32;
    public const ushort MaxAscii = 126;
    public const int MaxStringLength = 65535;

    public static readonly byte[] Trailer = [0xBB, 0x00, 0x00, 0xFF];
}
