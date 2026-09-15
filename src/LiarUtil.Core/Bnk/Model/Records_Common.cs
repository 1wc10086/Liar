namespace LiarUtil.Core.Bnk.Model;

internal sealed class CoordinatePoint
{
    public Position2<double> Position = default;
    public Curve Curve = default;
}

internal sealed class CoordinateIdentifierPoint
{
    public Position2<double, long> Position = default;
    public Curve Curve = default;
}

internal sealed class Parameter
{
    public long Identifier = 0L;
    public ParameterCategory Category = default;
}
