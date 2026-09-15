namespace LiarUtil.Core.Core.IO;

public static class FileIO
{
    public static void EnsureDirectory(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    public static void WriteAllBytes(string path, ReadOnlySpan<byte> data)
    {
        EnsureDirectory(path);
        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        stream.Write(data);
    }

    public static void WriteAllText(string path, string text)
    {
        AssertWritable(path);
        EnsureDirectory(path);
        File.WriteAllText(path, text);
    }

    public static byte[] ReadAllBytes(string path)
    {
        AssertReadable(path);
        return File.ReadAllBytes(path);
    }

    public static string ReadAllText(string path)
    {
        AssertReadable(path);
        return File.ReadAllText(path);
    }

    public static void AssertReadable(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(string.Format(LiarUtil.Core.Strings.FileDoesNotExist0, path), path);
        }
    }

    public static void AssertWritable(string path)
    {
        if (Directory.Exists(path))
        {
            throw new IOException(string.Format(LiarUtil.Core.Strings.TargetAlreadyDirectory0, path));
        }
    }

    public static string NormalizeSeparators(string path) => path.Replace('\\', Path.DirectorySeparatorChar);

    public static string ToPortablePath(string path) => path.Replace('\\', '/');

    public static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var builder = new System.Text.StringBuilder(name.Length);
        foreach (var character in name)
        {
            builder.Append(Array.IndexOf(invalid, character) >= 0 ? '_' : character);
        }

        return builder.ToString();
    }
}
