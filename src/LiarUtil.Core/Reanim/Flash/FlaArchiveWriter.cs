using System.IO.Compression;
using LiarUtil.Core.Core.IO;

namespace LiarUtil.Core.Reanim.Flash;

internal static class FlaArchiveWriter
{
    private const string TemporaryDirectoryPrefix = "LiarUtil-Fla-";

    public static void Write(ReanimFile reanim, string outFile, XflWriterOptions options)
    {
        var temporaryDirectory = Path.Combine(Path.GetTempPath(), TemporaryDirectoryPrefix + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporaryDirectory);
        try
        {
            XflWriter.Write(reanim, temporaryDirectory, options);
            using var output = new FileStream(outFile, FileMode.Create, FileAccess.Write, FileShare.None);
            using var archive = new ZipArchive(output, ZipArchiveMode.Create, false);
            foreach (var file in Directory.GetFiles(temporaryDirectory, "*", SearchOption.AllDirectories).OrderBy(path => path, StringComparer.Ordinal))
            {
                var entryName = FileIO.ToPortablePath(Path.GetRelativePath(temporaryDirectory, file));
                var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
                using var source = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var destination = entry.Open();
                source.CopyTo(destination);
            }
        }
        finally
        {
            DeleteTemporaryDirectory(temporaryDirectory);
        }
    }

    private static void DeleteTemporaryDirectory(string path)
    {
        var fullPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var tempRoot = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (fullPath.StartsWith(tempRoot, comparison)
            && Path.GetFileName(fullPath).StartsWith(TemporaryDirectoryPrefix, StringComparison.Ordinal)
            && Directory.Exists(fullPath))
        {
            Directory.Delete(fullPath, true);
        }
    }
}
