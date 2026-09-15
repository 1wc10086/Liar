namespace LiarUtil.Core.Pam.Xfl;

internal static class PamXflTransform
{
    private const double AngleTolerance = 1e-2;

    public static PamXflMatrix FromVariant(PamTransform transform) => transform switch
    {
        PamTranslateTransform translate => new PamXflMatrix(1, 0, 0, 1, translate.X, translate.Y),
        PamRotateTransform rotate => FromRotate(rotate),
        PamMatrixTransform matrix => new PamXflMatrix(matrix.A, matrix.B, matrix.C, matrix.D, matrix.X, matrix.Y),
        _ => throw new PamXflException(string.Format(LiarUtil.Core.Strings.UnknownPAMTransformType0, transform.GetType().Name)),
    };

    public static PamXflMatrix FromRotate(PamRotateTransform rotate)
    {
        var cos = Math.Cos(rotate.Angle);
        var sin = Math.Sin(rotate.Angle);
        return new PamXflMatrix(cos, sin, -sin, cos, rotate.X, rotate.Y);
    }

    public static PamTransform ToVariant(PamXflMatrix matrix)
    {
        if (matrix.IsTranslation)
        {
            return new PamTranslateTransform { X = (float)matrix.X, Y = (float)matrix.Y };
        }

        if (TryGetVariantAngle(matrix) is { } angle)
        {
            return new PamRotateTransform { Angle = angle, X = (float)matrix.X, Y = (float)matrix.Y };
        }

        return new PamMatrixTransform
        {
            A = (float)matrix.A,
            B = (float)matrix.B,
            C = (float)matrix.C,
            D = (float)matrix.D,
            X = (float)matrix.X,
            Y = (float)matrix.Y,
        };
    }

    public static PamRotateTransform ToRotate(PamXflMatrix matrix)
    {
        if (!matrix.IsRotation)
        {
            throw new PamXflException(LiarUtil.Core.Strings.ImageTransformNotPureRotationMatrixAndCannot);
        }

        var angle = Math.Atan2(matrix.B, matrix.A);
        if (Math.Abs(Math.Cos(angle) - matrix.A) > AngleTolerance || Math.Abs(Math.Sin(angle) - matrix.B) > AngleTolerance)
        {
            throw new PamXflException(LiarUtil.Core.Strings.ImageTransformDeviatesTooMuchFromRotationMatrix);
        }

        return new PamRotateTransform { Angle = (float)angle, X = (float)matrix.X, Y = (float)matrix.Y };
    }

    private static float? TryGetVariantAngle(PamXflMatrix matrix)
    {
        if (!matrix.IsRotation)
        {
            return null;
        }

        var acos = Math.Acos(matrix.A);
        var asin = Math.Asin(matrix.B);
        return Math.Abs(Math.Abs(acos) - Math.Abs(asin)) <= AngleTolerance ? (float)asin : null;
    }
}
