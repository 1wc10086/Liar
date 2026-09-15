namespace LiarUtil.Core.Arcv;

public static class ArcvFormat
{
    public const int HeaderSize = 12;

    public const int EntrySize = 12;

    public const int Alignment = 4;

    public const byte AlignmentPadding = 0xAC;

    public const int NameLength = 10;

    public static readonly byte[] MagicBytes = "ARCV"u8.ToArray();

    public static string FileName(uint checksum) => checksum.ToString().PadLeft(NameLength, '0');
}
