namespace LiarUtil.Core.Rsb.Definitions;

internal sealed class RsbDefinitionGroup
{
    public string Identifier { get; set; } = "";

    public List<RsbDefinitionSubgroupReference> Subgroups { get; set; } = [];
}

internal sealed class RsbDefinitionSubgroupReference
{
    public uint Index { get; set; }

    public uint Resolution { get; set; }

    public string Locale { get; set; } = "";
}
