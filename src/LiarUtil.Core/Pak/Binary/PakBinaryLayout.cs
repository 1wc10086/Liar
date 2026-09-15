namespace LiarUtil.Core.Pak.Binary;

internal static class PakBinaryLayout
{
    public const int MagicSize = sizeof(int);

    public const int VersionSize = sizeof(int);

    public const int HeaderSize = MagicSize + VersionSize;

    public const int CompressionSizeFieldSize = sizeof(int);

    public const int UncompressedSizeFieldSize = sizeof(int);

    public const int FileTimeSize = sizeof(long);

    public const int EntryFlagSize = sizeof(byte);

    public const int NameLengthSize = sizeof(byte);

    public const int PlainRecordTail = CompressionSizeFieldSize + FileTimeSize;

    public const int CompressedRecordTail = UncompressedSizeFieldSize + PlainRecordTail;

    public const int ToConsoleAlignment = 8;

    public const int Xbox360Alignment = 0x1000;

    public const int X360PaddingThreshold = 8;
}
