namespace AstcSharp.Encoding;

internal static class FastSearchPolicy
{
    public static readonly bool EnableDualPlane = false;

    public const int LdrEarlyOutPerSampleError = 25;

    public const int DecimationRefinementIterations = 2;

    public const int PrincipalAxisIterations = 4;
}
