using LiarUtil.Core.Bnk;

namespace LiarUtil.Core.Services;

public sealed class BnkService
{
    public void Unpack(string inputPath, string outputFolder, uint version) =>
        BnkCodec.Unpack(inputPath, outputFolder, new BankVersion(version));

    public void Pack(string inputFolder, string outputPath, uint version) =>
        BnkCodec.Pack(inputFolder, outputPath, new BankVersion(version));
}
