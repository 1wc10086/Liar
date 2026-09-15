namespace LiarUtil.Core.Arcv;

public static class ArcvCodec
{
    public static void Unpack(string inputPath, string outputFolder) =>
        ArcvUnpack.Unpack(new ArcvUnpackOptions { InputPath = inputPath, OutputFolder = outputFolder });

    public static void Pack(string inputFolder, string outputPath) =>
        ArcvPack.Pack(new ArcvPackOptions { InputFolder = inputFolder, OutputPath = outputPath });
}
