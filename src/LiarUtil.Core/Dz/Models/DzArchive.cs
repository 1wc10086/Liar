namespace LiarUtil.Core.Dz.Models;

internal sealed class DzArchive
{
    public string[] FileNames { get; set; } = [];

    public string[] FolderNames { get; set; } = [""];

    public string?[] ArchiveNames { get; set; } = [null];

    public DzChunkInfo[] Chunks { get; set; } = [];

    public ushort ArchiveCount { get; set; } = 1;
}
