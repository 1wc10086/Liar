using System.Globalization;
using System.Text.Json.Serialization;

namespace LiarUtil.Core.PopCap;

[JsonConverter(typeof(PopCurveJsonConverter))]
public enum PopCurve
{
    Constant = 0,
    Linear = 1,
    EaseIn = 2,
    EaseOut = 3,
    EaseInOut = 4,
    EaseInOutWeak = 5,
    FastInOut = 6,
    FastInOutWeak = 7,
    WeakFastInOut = 8,
    Bounce = 9,
    BounceFastMiddle = 10,
    BounceSlowMiddle = 11,
    SinWave = 12,
    EaseSinWave = 13,
}

public static class PopCurves
{
    private const string TodCurvesPrefix = "TodCurves(";

    public static string Format(PopCurve curve) => Enum.IsDefined(curve)
        ? Enum.GetName(curve) ?? ((int)curve).ToString(CultureInfo.InvariantCulture)
        : $"{TodCurvesPrefix}{(int)curve})";

    public static PopCurve Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return PopCurve.Linear;
        }

        var value = text.Trim();
        if (Enum.TryParse<PopCurve>(value, ignoreCase: true, out var curve) && Enum.IsDefined(curve))
        {
            return curve;
        }

        if (value.StartsWith(TodCurvesPrefix, StringComparison.OrdinalIgnoreCase) && value.EndsWith(')'))
        {
            var inner = value[TodCurvesPrefix.Length..^1];
            if (int.TryParse(inner, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
            {
                return (PopCurve)number;
            }
        }

        throw new InvalidDataException(string.Format(LiarUtil.Core.Strings.InvalidCurveType0, text));
    }
}
