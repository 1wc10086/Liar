namespace LiarUtil.Core.Ppf.Models;

public sealed class PpfValue1Point
{
    public float Time { get; set; }
    public float Value { get; set; }
    public PpfControlValue ControlValue { get; set; } = new();
}
