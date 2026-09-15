namespace LiarUtil.Core.Pak.Models;

internal sealed class PakEntry
{
    public required string Name { get; init; }

    public int CompressedSize { get; set; }

    public int UncompressedSize { get; set; }

    public long FileTime { get; set; } = PakFormat.DefaultFileTime;

    public bool IsCompressed => UncompressedSize != 0;
}

internal sealed class PakArchive
{
    public required IReadOnlyList<PakEntry> Entries { get; init; }

    public bool HasCompressionSize { get; set; }
}
