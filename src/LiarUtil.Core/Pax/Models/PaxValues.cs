namespace LiarUtil.Core.Pax.Models;

public sealed class PaxPoint
{
    public double X { get; set; }

    public double Y { get; set; }
}

public sealed class PaxRotation
{
    public double Degrees { get; set; }

    public double Radians => Degrees * Math.PI / 180;
}

public sealed class PaxLoop
{
    public PaxLoopType Type { get; set; }

    public int Frame { get; set; }
}
