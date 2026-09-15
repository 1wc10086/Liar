namespace LiarUtil.Core.Dz.Models;

public sealed class DzPackageDefinition
{
    public int Version { get; set; } = 1;

    public string ResourceFolder { get; set; } = DzFormat.ResourceFolderName;

    public DzCompressionMethod DefaultMethod { get; set; } = DzCompressionMethod.Lzma;

    public Dictionary<string, DzCompressionMethod> Compress { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public List<DzResourceDefinition> Resource { get; set; } = [];
}
