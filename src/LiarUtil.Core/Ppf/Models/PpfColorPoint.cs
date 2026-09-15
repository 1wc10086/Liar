namespace LiarUtil.Core.Ppf.Models;

public sealed class PpfColorPoint
{
    public float Time { get; set; }
    public PpfRgb Value { get; set; } = new();
}
