using LiarUtil.Core.Xma;

namespace LiarUtil.Core.Services;

public sealed class XmaService
{
    public void Decode(string inputPath, string outputPath) =>
        FileHelper.WriteBytes(outputPath, XmaCodec.DecodeToWav(File.ReadAllBytes(inputPath)));

    public void Encode(string inputPath, string outputPath) =>
        FileHelper.WriteBytes(outputPath, XmaCodec.Encode(File.ReadAllBytes(inputPath)));
}
