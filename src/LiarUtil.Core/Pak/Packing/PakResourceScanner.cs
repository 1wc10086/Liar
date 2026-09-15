using LiarUtil.Core.Pak.Definitions;

namespace LiarUtil.Core.Pak.Packing;

internal static class PakResourceScanner
{
    public static IReadOnlyList<PakResourceFile> Scan(string resourceFolder)
    {
        var root = Path.GetFullPath(resourceFolder);
        if (!Directory.Exists(root))
        {
            throw new PakException(string.Format(LiarUtil.Core.Strings.ResourceDirectoryNotFound0, resourceFolder));
        }

        var files = new List<PakResourceFile>();
        Collect(root, root, files);
        return files;
    }

    private static void Collect(string root, string directory, List<PakResourceFile> files)
    {
        foreach (var child in Directory.EnumerateDirectories(directory).OrderBy(path => path, StringComparer.Ordinal))
        {
            Collect(root, child, files);
        }

        foreach (var path in Directory.EnumerateFiles(directory).OrderBy(path => path, StringComparer.Ordinal))
        {
            files.Add(new PakResourceFile(PakPath.Normalize(Path.GetRelativePath(root, path)), path));
        }
    }
}
