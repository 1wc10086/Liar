namespace LiarUtil.Core.Ppf.Models;

public sealed class PpfLayerBlocker
{
    public string Name { get; set; } = "";
    public PpfValue2 Position { get; set; } = new();
    public PpfValue1 Active { get; set; } = new();
    public PpfValue1 Angle { get; set; } = new();
    public List<PpfValue2> Points { get; set; } = [];
    public int Unknown1 { get; set; }
    public int Unknown2 { get; set; }
    public int Unknown3 { get; set; }
    public int Unknown4 { get; set; }
    public int Unknown5 { get; set; }
}
