using LiarUtil.Core.Cfw2;

namespace LiarUtil.Core.Services;

public sealed class Cfw2Service
{
    public void Decode(string inputPath, string outputPath)
    {
        var json = Cfw2Codec.DecodeToJson(File.ReadAllBytes(inputPath));
        WriteText(outputPath, json);
    }

    public void Encode(string inputPath, string outputPath)
    {
        var data = Cfw2Codec.EncodeFromJson(File.ReadAllText(inputPath));
        WriteBytes(outputPath, data);
    }

    private static void WriteText(string path, string text)
    {
        EnsureDirectory(path);
        File.WriteAllText(path, text);
    }

    private static void WriteBytes(string path, byte[] data)
    {
        EnsureDirectory(path);
        File.WriteAllBytes(path, data);
    }

    private static void EnsureDirectory(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}
