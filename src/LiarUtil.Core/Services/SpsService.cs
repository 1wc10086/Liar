using LiarUtil.Core.Sps;

namespace LiarUtil.Core.Services;

public sealed class SpsService
{
    public void Decode(string inputPath, string outputPath) =>
        FileHelper.WriteBytes(outputPath, SpsCodec.DecodeToWav(File.ReadAllBytes(inputPath)));

    public void Encode(string inputPath, string outputPath) =>
        FileHelper.WriteBytes(outputPath, SpsCodec.EncodeFromWav(File.ReadAllBytes(inputPath)));
}
