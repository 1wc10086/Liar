namespace LiarUtil.Core.PopFx.Models;

public sealed class PopFxTechnique
{
    public string Name { get; set; } = "";
    public uint Number { get; set; }
    public List<PopFxAnnotation> Annotations { get; set; } = [];
    public List<PopFxPass> Passes { get; set; } = [];
}
