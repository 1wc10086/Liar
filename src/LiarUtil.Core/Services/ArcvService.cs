using LiarUtil.Core.Arcv;

namespace LiarUtil.Core.Services;

public sealed class ArcvService
{
    public void Unpack(string inputPath, string outputFolder) => ArcvCodec.Unpack(inputPath, outputFolder);

    public void Pack(string inputFolder, string outputPath) => ArcvCodec.Pack(inputFolder, outputPath);
}
