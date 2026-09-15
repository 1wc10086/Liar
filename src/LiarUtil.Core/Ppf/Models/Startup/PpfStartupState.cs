namespace LiarUtil.Core.Ppf.Models;

public sealed class PpfStartupState
{
    public ushort Version { get; set; }
    public float Frame { get; set; }
    public bool EmitAfterTimeline { get; set; }
    public PpfTransform EmitterTransform { get; set; } = new();
    public PpfTransform DrawTransform { get; set; } = new();
    public List<PpfStartupLayerState> Layers { get; set; } = [];
}
