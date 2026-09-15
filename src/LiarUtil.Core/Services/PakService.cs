using LiarUtil.Core.Pak;

namespace LiarUtil.Core.Services;

public sealed class PakService
{
    public void Unpack(string inputPath, string outputFolder) => PakCodec.Unpack(inputPath, outputFolder);

    public void Pack(string inputFolder, string outputPath) => PakCodec.Pack(inputFolder, outputPath);
}
