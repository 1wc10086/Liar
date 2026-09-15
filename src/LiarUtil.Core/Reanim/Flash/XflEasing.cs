namespace LiarUtil.Core.Reanim.Flash;

internal static class XflEasing
{
    private const double Pi = Math.PI;

    public static double Evaluate(string? easingName, double progress)
    {
        var p = XflMatrixParts.Clamp01(progress);
        var easing = Normalize(easingName);
        return easing switch
        {
            "stepped" => 0,
            "quadraticin" => p * p,
            "quadraticout" => -p * (p - 2),
            "quadraticinout" => p * 2 < 1
                ? p * 2 * p * 2 / 2
                : (-((p * 2) - 1) * ((p * 2) - 3) + 1) / 2,
            "cubicin" => p * p * p,
            "cubicout" => (p - 1) * (p - 1) * (p - 1) + 1,
            "cubicinout" => p * 2 < 1
                ? p * 2 * p * 2 * p * 2 / 2
                : (Math.Pow((p * 2) - 2, 3) + 2) / 2,
            "quarticin" => Math.Pow(p, 4),
            "quarticout" => -Math.Pow(p - 1, 4) + 1,
            "quarticinout" => p * 2 < 1
                ? Math.Pow(p * 2, 4) / 2
                : (-Math.Pow((p * 2) - 2, 4) + 2) / 2,
            "quinticin" => Math.Pow(p, 5),
            "quinticout" => Math.Pow(p - 1, 5) + 1,
            "quinticinout" => p * 2 < 1
                ? Math.Pow(p * 2, 5) / 2
                : (Math.Pow((p * 2) - 2, 5) + 2) / 2,
            "sinusoidalin" => -Math.Cos(p * Pi / 2) + 1,
            "sinusoidalout" => Math.Sin(p * Pi / 2),
            "sinusoidalinout" => (-Math.Cos(p * Pi) / 2) + 0.5,
            "exponentialin" => p == 0 ? 0 : Math.Pow(2, 10 * (p - 1)),
            "exponentialout" => p == 1 ? 1 : -Math.Pow(2, -10 * p) + 1,
            "exponentialinout" => ExponentialInOut(p),
            "circularin" => -(Math.Sqrt(Math.Max(0, 1 - (p * p))) - 1),
            "circularout" => Math.Sqrt(Math.Max(0, 1 - ((p - 1) * (p - 1)))),
            "circularinout" => p * 2 < 1
                ? -(Math.Sqrt(Math.Max(0, 1 - (p * 2 * p * 2))) - 1) / 2
                : (Math.Sqrt(Math.Max(0, 1 - Math.Pow((p * 2) - 2, 2))) + 1) / 2,
            "backin" => BackIn(p),
            "backout" => BackOut(p),
            "backinout" => BackInOut(p),
            "bounceout" => BounceOut(p),
            "bouncein" => 1 - BounceOut(1 - p),
            "bounceinout" => p < 0.5
                ? (1 - BounceOut(1 - (p * 2))) / 2
                : (BounceOut((p * 2) - 1) + 1) / 2,
            "elasticin" or "elasticout" or "elasticinout" => Elastic(easing, p),
            _ => p,
        };
    }

    private static double ExponentialInOut(double p)
    {
        if (p is 0 or 1)
        {
            return p;
        }
        var q = p * 2;
        return q < 1
            ? Math.Pow(2, 10 * (q - 1)) / 2
            : (-Math.Pow(2, -10 * (q - 1)) + 2) / 2;
    }

    private static double BackIn(double p)
    {
        const double s = 1.70158;
        return p * p * (((s + 1) * p) - s);
    }

    private static double BackOut(double p)
    {
        const double s = 1.70158;
        var q = p - 1;
        return (q * q * (((s + 1) * q) + s)) + 1;
    }

    private static double BackInOut(double p)
    {
        var q = p * 2;
        const double s = 1.70158 * 1.525;
        if (q < 1)
        {
            return q * q * (((s + 1) * q) - s) / 2;
        }
        q -= 2;
        return ((q * q * (((s + 1) * q) + s)) + 2) / 2;
    }

    private static double BounceOut(double value)
    {
        var p = value;
        if (p < 1 / 2.75)
        {
            return 7.5625 * p * p;
        }
        if (p < 2 / 2.75)
        {
            p -= 1.5 / 2.75;
            return (7.5625 * p * p) + 0.75;
        }
        if (p < 2.5 / 2.75)
        {
            p -= 2.25 / 2.75;
            return (7.5625 * p * p) + 0.9375;
        }
        p -= 2.625 / 2.75;
        return (7.5625 * p * p) + 0.984375;
    }

    private static double Elastic(string easing, double p)
    {
        if (p <= 0.00001 || p >= 0.999)
        {
            return p;
        }
        var period = easing == "elasticinout" ? 0.45 : 0.3;
        var offset = period / 4;
        if (easing == "elasticin")
        {
            var q = p - 1;
            return -(Math.Pow(2, 10 * q) * Math.Sin((q - offset) * 2 * Pi / period));
        }
        if (easing == "elasticout")
        {
            return (Math.Pow(2, -10 * p) * Math.Sin((p - offset) * 2 * Pi / period)) + 1;
        }
        var value = p * 2;
        if (value < 1)
        {
            value -= 1;
            return -0.5 * Math.Pow(2, 10 * value) * Math.Sin((value - offset) * 2 * Pi / period);
        }
        value -= 1;
        return (0.5 * Math.Pow(2, -10 * value) * Math.Sin((value - offset) * 2 * Pi / period)) + 1;
    }

    public static string Normalize(string? value) =>
        (value ?? "").Replace("_", "").Replace("-", "").ToLowerInvariant();
}
