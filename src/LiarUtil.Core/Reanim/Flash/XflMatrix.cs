namespace LiarUtil.Core.Reanim.Flash;

internal readonly struct XflMatrix(double a, double b, double c, double d, double tx, double ty)
{
    public static XflMatrix Identity => new(1, 0, 0, 1, 0, 0);

    public double A { get; } = a;

    public double B { get; } = b;

    public double C { get; } = c;

    public double D { get; } = d;

    public double Tx { get; } = tx;

    public double Ty { get; } = ty;

    public static XflMatrix Concat(XflMatrix inner, XflMatrix outer) => new(
        (outer.A * inner.A) + (outer.C * inner.B),
        (outer.B * inner.A) + (outer.D * inner.B),
        (outer.A * inner.C) + (outer.C * inner.D),
        (outer.B * inner.C) + (outer.D * inner.D),
        (outer.A * inner.Tx) + (outer.C * inner.Ty) + outer.Tx,
        (outer.B * inner.Tx) + (outer.D * inner.Ty) + outer.Ty);
}
