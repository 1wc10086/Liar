using LiarUtil.Core.Ppf;

namespace LiarUtil.Core.Services;

public sealed class PpfService
{
    public void Decode(string inputPath, string outputPath)
    {
        var json = PpfCodec.DecodeToJson(File.ReadAllBytes(inputPath));
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
        File.WriteAllText(outputPath, json);
    }

    public void Encode(string inputPath, string outputPath, int version)
    {
        var data = PpfCodec.EncodeFromJson(File.ReadAllText(inputPath), version);
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
        File.WriteAllBytes(outputPath, data);
    }
}
