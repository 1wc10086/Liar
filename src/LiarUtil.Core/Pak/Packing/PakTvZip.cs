using System.IO.Compression;

namespace LiarUtil.Core.Pak.Packing;

internal static class PakTvZip
{
    public static void Pack(string resourceFolder, string outputPath)
    {
        if (File.Exists(outputPath))
        {
            File.Delete(outputPath);
        }

        ZipFile.CreateFromDirectory(resourceFolder, outputPath, CompressionLevel.Optimal, false);
    }

    public static void Unpack(string inputPath, string resourceFolder)
    {
        Directory.CreateDirectory(resourceFolder);
        ZipFile.ExtractToDirectory(inputPath, resourceFolder, true);
    }
}
