namespace LiarUtil.Core.Pak.Definitions;

internal static class PakExtensions
{
    public static string Normalize(string? extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return "";
        }

        var normalized = extension.Trim().ToLowerInvariant();
        return normalized.StartsWith('.') ? normalized : $".{normalized}";
    }
}
