namespace LiarUtil.Core.Reanim.Flash;

public static class FlashXflService
{
    public static bool IsArchive(string path) =>
        File.Exists(path) && XflProject.IsZipXfl(path);

    public static bool IsProject(string path) =>
        Directory.Exists(path) || File.Exists(Path.Combine(path, "DOMDocument.xml"));

    public static ReanimFile Decode(string path) =>
        IsArchive(path) ? XflReanimDecoder.DecodeArchive(path) : XflReanimDecoder.Decode(path);

    public static void Encode(ReanimFile reanim, string path, XflWriterOptions? options = null)
    {
        options ??= new XflWriterOptions();
        var extension = Path.GetExtension(path);
        if (string.Equals(extension, ".fla", StringComparison.OrdinalIgnoreCase)
            || (File.Exists(path) && !Directory.Exists(path)))
        {
            FlaArchiveWriter.Write(reanim, path, options);
            return;
        }
        XflWriter.Write(reanim, path, options);
    }
}
