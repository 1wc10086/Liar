using System.Globalization;

namespace LiarUtil.Core.Core.Xml;

public static class XmlNumbers
{
    public static float ParseFloat(string? value) => ParseFloat(value, 0f);

    public static float ParseFloat(string? value, float fallback) =>
        float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? result : fallback;

    public static double ParseDouble(string? value) => ParseDouble(value, 0d);

    public static double ParseDouble(string? value, double fallback) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? result : fallback;

    public static int ParseInt(string? value, int fallback) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) ? result : fallback;

    public static long ParseLong(string? value, long fallback) =>
        long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) ? result : fallback;

    public static bool TryFloat(string? value, out float result) =>
        float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);

    public static bool TryDouble(string? value, out double result) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);

    public static bool TryInt(string? value, out int result) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);

    public static bool ParseFlag(string? value) => value?.Trim() is not (null or "0" or "false" or "False");

    public static string Format(float value) => value.ToString(CultureInfo.InvariantCulture);

    public static string Format(double value) => value.ToString(CultureInfo.InvariantCulture);

    public static string Format(int value) => value.ToString(CultureInfo.InvariantCulture);

    public static string FormatTrimmed(float value)
    {
        var text = Format(value);
        return text.StartsWith("0.", StringComparison.Ordinal) ? text[1..] : text;
    }
}
