namespace LiarUtil.Core.Rsb.Resources;

internal sealed class RsbResourceDefinitionGroup
{
    public string Identifier { get; set; } = "";

    public int Resolution { get; set; }

    public string Locale { get; set; } = "";

    public List<RsbResourceDefinitionEntry> Resources { get; set; } = [];
}
