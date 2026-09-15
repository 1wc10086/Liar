using LiarUtil.Core.Caf;

namespace LiarUtil.Core.Services;

public sealed class CafService
{
    public void Decode(string inputPath, string outputPath) =>
        FileHelper.WriteBytes(outputPath, CafCodec.DecodeToWav(File.ReadAllBytes(inputPath)));
}
