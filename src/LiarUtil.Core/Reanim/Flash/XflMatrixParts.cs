namespace LiarUtil.Core.Reanim.Flash;

internal readonly struct XflMatrixParts(double xAngle, double yAngle, double scaleX, double scaleY)
{
    private const double Pi = Math.PI;

    public double XAngle { get; } = xAngle;

    public double YAngle { get; } = yAngle;

    public double ScaleX { get; } = scaleX;

    public double ScaleY { get; } = scaleY;

    public static XflMatrixParts Decompose(XflMatrix matrix)
    {
        var xAngle = Math.Atan2(matrix.B, matrix.A);
        var scaleX = Math.Sqrt((matrix.A * matrix.A) + (matrix.B * matrix.B));
        var determinant = (matrix.A * matrix.D) - (matrix.B * matrix.C);
        var scaleY = Math.Sqrt((matrix.C * matrix.C) + (matrix.D * matrix.D));
        if (determinant < 0)
        {
            scaleY = -scaleY;
        }
        var yAngle = Math.Atan2(matrix.C, matrix.D);
        if (scaleY < 0)
        {
            yAngle += Pi;
        }
        return new XflMatrixParts(xAngle, yAngle, scaleX, scaleY);
    }

    public static double Unwrap(double start, double end)
    {
        while (start - end > Math.PI)
        {
            end += Math.PI * 2;
        }
        while (start - end < -Math.PI)
        {
            end -= Math.PI * 2;
        }
        return end;
    }

    public static double Lerp(double start, double end, double progress) => start + ((end - start) * progress);

    public static double Clamp01(double value) => Math.Max(0, Math.Min(1, value));
}
