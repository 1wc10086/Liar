namespace LiarUtil.Core.Ppf.Models;

public sealed class PpfLayerForce
{
    public string Name { get; set; } = "";
    public bool Visible { get; set; }
    public PpfValue2 Position { get; set; } = new();
    public PpfValue1 Active { get; set; } = new();
    public PpfValue1 Strength { get; set; } = new();
    public PpfValue1 Width { get; set; } = new();
    public PpfValue1 Height { get; set; } = new();
    public PpfValue1 Angle { get; set; } = new();
    public PpfValue1 Direction { get; set; } = new();
    public PpfValue1 Unknown1 { get; set; } = new();
}
