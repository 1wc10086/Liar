namespace LiarUtil.Core.Rsb.Definitions;

internal sealed class RsbDefinitionSubgroup
{
    public string Identifier { get; set; } = "";

    public uint Pool { get; set; }

    public bool CompressGeneral { get; set; }

    public bool CompressTexture { get; set; } = true;

    public string? Packet { get; set; }

    public List<RsbDefinitionResource> Resources { get; set; } = [];
}
