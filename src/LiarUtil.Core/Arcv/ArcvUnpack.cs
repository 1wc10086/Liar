using LiarUtil.Core.Core.IO;

namespace LiarUtil.Core.Arcv;

internal static class ArcvUnpack
{
    public static void Unpack(ArcvUnpackOptions options)
    {
        var reader = new ArcvReader(File.ReadAllBytes(options.InputPath));
        Directory.CreateDirectory(options.OutputFolder);
        foreach (var entry in reader.Read())
        {
            var content = reader.ReadContent(entry);
            var fileName = ArcvFormat.FileName(entry.Checksum) + ArcvExtensions.DetectExtension(content);
            FileIO.WriteAllBytes(Path.Combine(options.OutputFolder, fileName), content);
        }
    }
}
