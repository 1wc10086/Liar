using LiarUtil.Core.Snr;

namespace LiarUtil.Core.Services;

public sealed class SnrService
{
    public void Decode(string inputPath, string outputPath) =>
        FileHelper.WriteBytes(outputPath, SnrCodec.DecodeToWav(File.ReadAllBytes(inputPath)));

    public void Encode(string inputPath, string outputPath) =>
        FileHelper.WriteBytes(outputPath, SnrCodec.EncodeFromWav(File.ReadAllBytes(inputPath)));
}
