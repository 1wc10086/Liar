using LiarUtil.Core.Atlas;

namespace LiarUtil.Core.Services;

public sealed class AtlasService
{
    public void Cut(int format, string inputFile, string outputFolder, string infoPath, string itemName) =>
        AtlasCodec.Cut(format, inputFile, outputFolder, infoPath, itemName);

    public void Splice(int format, string inputFolder, string outputFile, string infoPath, string itemName, int width, int height) =>
        AtlasCodec.Splice(format, inputFolder, outputFile, infoPath, itemName, width, height);
}
