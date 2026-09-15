using LiarUtil.Core.Atlas.Formats;

namespace LiarUtil.Core.Atlas;

public static class AtlasCodec
{
    public static void Cut(int format, string inputFile, string outputFolder, string infoPath, string? itemName) =>
        AtlasFormatProvider.Cut(AtlasFormatProvider.Parse(format), inputFile, outputFolder, infoPath, itemName);

    public static void Splice(int format, string inputFolder, string outputFile, string infoPath, string? itemName, int width, int height) =>
        AtlasFormatProvider.Splice(AtlasFormatProvider.Parse(format), inputFolder, outputFile, infoPath, itemName, width, height);
}
