using LiarUtil.Core.Cfu2;

namespace LiarUtil.Core.Services;

public sealed class Cfu2Service
{
    public void Decode(string inputPath, string outputPath) =>
        FileHelper.WriteText(outputPath, Cfu2Codec.DecodeToJson(File.ReadAllBytes(inputPath)));

    public void Encode(string inputPath, string outputPath) =>
        FileHelper.WriteBytes(outputPath, Cfu2Codec.EncodeFromJson(File.ReadAllText(inputPath)));
}
