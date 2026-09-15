using LiarUtil.Core.Xpr;

namespace LiarUtil.Core.Services;

public sealed class XprService
{
    public void Unpack(string inputPath, string outputFolder) => XprCodec.Unpack(inputPath, outputFolder);

    public void Pack(string inputFolder, string outputPath) => XprCodec.Pack(inputFolder, outputPath);
}
