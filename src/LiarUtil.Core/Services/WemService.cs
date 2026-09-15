using LiarUtil.Core.Wem;

namespace LiarUtil.Core.Services;

public sealed class WemService
{
    public void Decode(string inputPath, string outputPath) =>
        FileHelper.WriteBytes(outputPath, WemToOgg.ConvertToWav(File.ReadAllBytes(inputPath)));
}
