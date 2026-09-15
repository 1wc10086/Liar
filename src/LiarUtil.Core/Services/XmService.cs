using LiarUtil.Core.Xm;

namespace LiarUtil.Core.Services;

public sealed class XmService
{
    public void Decode(string inputPath, string outputPath) =>
        FileHelper.WriteBytes(outputPath, XmCodec.DecodeToWav(File.ReadAllBytes(inputPath)));
}
