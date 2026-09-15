namespace LiarUtil.Core.Xpr;

public static class XprCodec
{
    public static void Unpack(string inputPath, string outputFolder) =>
        XprUnpack.Unpack(new XprUnpackOptions { InputPath = inputPath, OutputFolder = outputFolder });

    public static void Pack(string inputFolder, string outputPath) =>
        XprPack.Pack(new XprPackOptions { InputFolder = inputFolder, OutputPath = outputPath });
}
