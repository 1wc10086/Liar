using LiarUtil.Core.Dz;

namespace LiarUtil.Core.Services;

public sealed class DzService
{
    public void Unpack(string inputPath, string outputFolder) => DzCodec.Unpack(inputPath, outputFolder);

    public void Pack(string inputFolder, string outputPath) => DzCodec.Pack(inputFolder, outputPath);
}
