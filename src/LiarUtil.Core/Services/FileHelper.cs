namespace LiarUtil.Core.Services;

public static class FileHelper
{
    public static void EnsureDirectory(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    public static void WriteText(string path, string text)
    {
        EnsureDirectory(path);
        File.WriteAllText(path, text);
    }

    public static void WriteBytes(string path, byte[] data)
    {
        EnsureDirectory(path);
        File.WriteAllBytes(path, data);
    }
}
