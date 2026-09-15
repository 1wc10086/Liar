using LiarUtil.Core.Xnb;

namespace LiarUtil.Core.Services;

public sealed class XnbAudioService
{
    public void Decode(string inputPath, string outputPath) =>
        FileHelper.WriteBytes(outputPath, XnbCodec.DecodeToWav(File.ReadAllBytes(inputPath)));
}
