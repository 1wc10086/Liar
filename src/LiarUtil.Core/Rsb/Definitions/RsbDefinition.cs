using LiarUtil.Core.Rsb.Resources;

namespace LiarUtil.Core.Rsb.Definitions;

internal sealed class RsbDefinition
{
    public bool BigEndian { get; set; }

    public string CompressionLevel { get; set; } = RsbCompressionLevel.Optimal;

    public bool WholeFileCompressed { get; set; }

    public bool SpecialPool { get; set; }

    public uint? TextureRecordSize { get; set; }

    public List<RsbDefinitionPool> Pools { get; set; } = [];

    public List<RsbDefinitionSubgroup> Subgroups { get; set; } = [];

    public List<RsbDefinitionGroup> Groups { get; set; } = [];

    public List<RsbResourceDefinition> ResourceDefinitions { get; set; } = [];
}
