namespace LiarUtil.Core.Pak.Definitions;

internal static class PakPath
{
    public static string Normalize(string? path) => (path ?? "").Replace('\\', '/').Trim('/');

    public static string ToArchiveFormat(string? path, bool windowsSeparator)
    {
        var normalized = Normalize(path);
        return windowsSeparator ? normalized.Replace('/', '\\') : normalized;
    }

    public static string ToLocal(string root, string? relativePath)
    {
        string[] parts = [.. Normalize(relativePath).Split('/', StringSplitOptions.RemoveEmptyEntries)];
        if (parts.Length == 0)
        {
            throw new PakException(string.Format(LiarUtil.Core.Strings.ResourcePathIllegal0, relativePath));
        }

        foreach (var part in parts)
        {
            if (part is "." or ".." || part.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                throw new PakException(string.Format(LiarUtil.Core.Strings.ResourcePathIllegal0, relativePath));
            }
        }

        return Path.Combine([root, .. parts]);
    }
}
