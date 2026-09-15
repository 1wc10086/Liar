namespace LiarUtil.Core.Dz.Definitions;

internal static class DzPath
{
    public static string Normalize(string? path) => (path ?? "").Replace('\\', '/').Trim('/');

    public static string ToArchiveFormat(string? path) => Normalize(path).Replace('/', '\\');

    public static string FromArchiveFormat(string? path) => (path ?? "").Replace('\\', '/');

    public static string Combine(string folder, string name)
    {
        var normalizedFolder = Normalize(FromArchiveFormat(folder));
        var normalizedName = Normalize(FromArchiveFormat(name));
        if (normalizedName.Length == 0)
        {
            return normalizedFolder;
        }
        return normalizedFolder.Length == 0 ? normalizedName : $"{normalizedFolder}/{normalizedName}";
    }

    public static string WithMultiIndex(string relativePath, int multiIndex)
    {
        if (multiIndex == 0)
        {
            return relativePath;
        }

        var extension = Path.GetExtension(relativePath);
        var stem = extension.Length > 0 ? relativePath[..^extension.Length] : relativePath;
        return $"{stem}_multi_{multiIndex}{extension}";
    }

    public static string ToLocal(string root, string? relativePath)
    {
        var normalized = Normalize(FromArchiveFormat(relativePath));
        string[] parts = [.. normalized.Split('/', StringSplitOptions.RemoveEmptyEntries)];
        if (parts.Length == 0)
        {
            throw new DzException(string.Format(LiarUtil.Core.Strings.ResourcePathIllegal0, relativePath));
        }

        foreach (var part in parts)
        {
            if (part is "." or ".." || part.Contains(':') || part.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                throw new DzException(string.Format(LiarUtil.Core.Strings.ResourcePathIllegal0, relativePath));
            }
        }

        return Path.Combine([root, .. parts]);
    }
}
