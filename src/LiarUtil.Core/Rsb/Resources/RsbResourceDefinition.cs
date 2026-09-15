namespace LiarUtil.Core.Rsb.Resources;

internal sealed class RsbResourceDefinition
{
    public string Identifier { get; set; } = "";

    public List<RsbResourceDefinitionGroup> Groups { get; set; } = [];
}
