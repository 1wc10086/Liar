namespace LiarUtil.Core.Pak.Models;

public sealed class PakFileDefinition
{
    public string Path { get; set; } = "";

    public PakCompression? Compression { get; set; }

    public long? FileTime { get; set; }
}
