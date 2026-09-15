namespace LiarUtil.Core.Ppf.Models;

public sealed class PpfValue2
{
    public float Unknown1 { get; set; }
    public float Unknown2 { get; set; }
    public bool Control { get; set; }
    public List<PpfValue2Point> Points { get; set; } = [];
}
