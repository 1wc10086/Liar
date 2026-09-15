namespace LiarUtil.Core.Pak;

public static class PakCodec
{
    public static void Unpack(string inputPath, string outputFolder) =>
        PakUnpack.Unpack(new PakUnpackOptions { InputPath = inputPath, OutputFolder = outputFolder });

    public static void Pack(string inputFolder, string outputPath) =>
        PakPack.Pack(new PakPackOptions { InputFolder = inputFolder, OutputPath = outputPath });
}
