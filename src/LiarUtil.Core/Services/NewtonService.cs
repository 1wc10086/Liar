using LiarUtil.Core.Newton;

namespace LiarUtil.Core.Services;

public sealed class NewtonService
{
    public void Decode(string inputPath, string outputPath) =>
        FileHelper.WriteText(outputPath, NewtonCodec.Decode(File.ReadAllBytes(inputPath)));

    public void Encode(string inputPath, string outputPath) =>
        FileHelper.WriteBytes(outputPath, NewtonCodec.Encode(File.ReadAllText(inputPath)));
}
