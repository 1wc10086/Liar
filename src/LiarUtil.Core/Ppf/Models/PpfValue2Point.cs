namespace LiarUtil.Core.Ppf.Models;

public sealed class PpfValue2Point
{
    public int Time { get; set; }
    public PpfVector2 Value { get; set; } = new();
    public PpfControlValue ControlValue { get; set; } = new();
}
