using LiarUtil.Core.Pax;

namespace LiarUtil.Core.Services;

public sealed class PaxService
{
    public void Decode(string inputPath, string outputPath) =>
        FileHelper.WriteText(outputPath, PaxCodec.DecodeToJson(File.ReadAllBytes(inputPath)));

    public void Encode(string inputPath, string outputPath) =>
        FileHelper.WriteBytes(outputPath, PaxCodec.EncodeFromJson(File.ReadAllText(inputPath)));
}
