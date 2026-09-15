namespace LiarUtil.Core.Pax.Models;

public sealed class PaxPointKeyframe
{
    public int? Marker { get; set; }

    public int? SourceFrame { get; set; }

    public int Frame { get; set; }

    public PaxPoint? Value { get; set; }
}

public sealed class PaxRotationKeyframe
{
    public int? Marker { get; set; }

    public int? SourceFrame { get; set; }

    public int Frame { get; set; }

    public PaxRotation? Value { get; set; }
}

public sealed class PaxNumberKeyframe
{
    public int? Marker { get; set; }

    public int? SourceFrame { get; set; }

    public int Frame { get; set; }

    public double Value { get; set; }
}
