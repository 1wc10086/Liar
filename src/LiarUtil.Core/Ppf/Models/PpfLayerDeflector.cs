namespace LiarUtil.Core.Ppf.Models;

public sealed class PpfLayerDeflector
{
    public string Name { get; set; } = "";
    public int Bounce { get; set; }
    public int Hit { get; set; }
    public int Thickness { get; set; }
    public bool Visible { get; set; }
    public PpfValue2 Position { get; set; } = new();
    public List<PpfValue2> Points { get; set; } = [];
    public PpfValue1 Active { get; set; } = new();
    public PpfValue1 Angle { get; set; } = new();
}
