namespace LiarUtil.Core.Dz;

public static class DzCodec
{
    public static void Unpack(string inputPath, string outputFolder) =>
        DzUnpack.Unpack(new DzUnpackOptions { InputPath = inputPath, OutputFolder = outputFolder });

    public static void Pack(string inputFolder, string outputPath) =>
        DzPack.Pack(new DzPackOptions { InputFolder = inputFolder, OutputPath = outputPath });
}
