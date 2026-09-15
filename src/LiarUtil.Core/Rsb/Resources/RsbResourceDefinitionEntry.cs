namespace LiarUtil.Core.Rsb.Resources;

internal sealed class RsbResourceDefinitionEntry
{
    public ushort Type { get; set; }

    public string Identifier { get; set; } = "";

    public string Path { get; set; } = "";

    public RsbResourceDefinitionTexture? Texture { get; set; }

    public Dictionary<string, string>? Properties { get; set; }
}
