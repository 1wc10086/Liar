namespace LiarUtil.Core.Pak.Models;

public sealed class PakDefinition
{
    public int Version { get; set; } = 1;

    public string ResourceFolder { get; set; } = PakFormat.ResourceFolderName;

    public bool PcEncrypted { get; set; }

    public bool TvVersion { get; set; }

    public bool WindowsPathSeparate { get; set; } = true;

    public bool Xbox360PtxAlign { get; set; }

    public bool ZlibCompress { get; set; }

    public bool XmemCompress { get; set; }

    public PakCompression DefaultCompression { get; set; } = PakCompression.Store;

    public Dictionary<string, PakCompression> CompressionByExtension { get; set; } =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".ptx"] = PakCompression.Zlib,
            [".compiled"] = PakCompression.Zlib,
            [".txt"] = PakCompression.Zlib,
            [".xml"] = PakCompression.Zlib,
            [".reanim"] = PakCompression.Zlib,
        };

    public List<PakFileDefinition> Files { get; set; } = [];
}
