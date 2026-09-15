using LiarUtil.Core.Dz.Definitions;

namespace LiarUtil.Core.Dz.Packing;

internal static class DzResourceScanner
{
    public static IReadOnlyList<DzResourceFile> Scan(string resourceFolder)
    {
        var root = Path.GetFullPath(resourceFolder);
        return
        [
            .. Directory
                .EnumerateFiles(root, "*", SearchOption.AllDirectories)
                .Select(path => new DzResourceFile(DzPath.Normalize(Path.GetRelativePath(root, path)), path))
                .OrderBy(file => file.RelativePath, StringComparer.Ordinal),
        ];
    }
}
