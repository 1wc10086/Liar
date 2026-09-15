namespace LiarUtil.Core.Pam.Xfl;

internal readonly record struct PamXflMatrix(double A, double B, double C, double D, double X, double Y)
{
    public static PamXflMatrix Identity { get; } = new(1, 0, 0, 1, 0, 0);

    public bool IsTranslation => A == 1.0 && B == 0.0 && C == 0.0 && D == 1.0;

    public bool IsRotation => A == D && B == -C;
}
