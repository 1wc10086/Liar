using System.Globalization;

namespace LiarUtil.Core.PopCap;

public static class ImageNameResolver
{
    public static string? ResolveName(string? name, int? id, ImageIdMap? images = null) =>
        !string.IsNullOrEmpty(name)
            ? name
            : id is { } value ? images?.FindName(value) ?? Format(value) : null;

    public static int ResolveId(string? name, int? id, ImageIdMap? images = null)
    {
        if (id is { } value)
        {
            return value;
        }

        if (string.IsNullOrEmpty(name))
        {
            return -1;
        }

        if (images?.FindId(name) is { } mapped)
        {
            return mapped;
        }

        return int.TryParse(name, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : -1;
    }

    public static string Format(int id) => id.ToString(CultureInfo.InvariantCulture);
}
