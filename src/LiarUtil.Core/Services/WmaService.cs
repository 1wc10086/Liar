using LiarUtil.Core.Wma;

namespace LiarUtil.Core.Services;

public sealed class WmaService
{
    public void Decode(string inputPath, string outputPath) =>
        FileHelper.WriteBytes(outputPath, WmaCodec.DecodeToWav(File.ReadAllBytes(inputPath)));
}
