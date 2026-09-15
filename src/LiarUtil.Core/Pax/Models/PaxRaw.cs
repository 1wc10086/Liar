namespace LiarUtil.Core.Pax.Models;

public readonly record struct PaxRawValue(double First, double Second);

public readonly record struct PaxRawFrame(int Frame, double First, double Second);

public readonly record struct PaxRawKeyframe(int Marker, int SourceFrame, int Frame, double First, double Second);
