namespace LiarUtil.Core.Ppf.Models;

public sealed class PpfValue1
{
    public bool Control { get; set; }
    public List<PpfValue1Point> Points { get; set; } = [];
}
