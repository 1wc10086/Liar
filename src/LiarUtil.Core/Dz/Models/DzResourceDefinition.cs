namespace LiarUtil.Core.Dz.Models;

public sealed class DzResourceDefinition
{
    public string Path { get; set; } = "";

    public List<DzChunkDefinition> Chunk { get; set; } = [];
}
