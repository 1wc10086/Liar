using LiarUtil.Core.PopFx;

namespace LiarUtil.Core.Services;

public sealed class PopFxService
{
    public void Decode(string inputPath, string outputPath, PopFxVariant variant)
    {
        var json = PopFxCodec.DecodeToJson(File.ReadAllBytes(inputPath), variant);
        WriteText(outputPath, json);
    }

    public void Encode(string inputPath, string outputPath, PopFxVariant variant)
    {
        var data = PopFxCodec.EncodeFromJson(File.ReadAllText(inputPath), variant);
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
