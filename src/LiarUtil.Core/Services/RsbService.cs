using LiarUtil.Core.Rsb;

namespace LiarUtil.Core.Services;

public sealed class RsbService
{
    public void Unpack(
        string inputPath,
        string outputFolder,
        RsbVersion version,
        bool exportResources,
        bool writeTextureHeader,
        bool convertImages,
        bool deleteAfterConvert,
        string ptxFormat)
    {
        RsbUnpack.Unpack(new RsbUnpackOptions
        {
            InputPath = inputPath,
            OutputFolder = outputFolder,
            Version = version,
            ExportResources = exportResources,
            WriteTextureHeader = writeTextureHeader,
            ConvertImages = convertImages,
            DeleteAfterConvert = deleteAfterConvert,
            PtxFormat = ptxFormat,
        });
    }

    public void Pack(string inputFolder, string outputPath, RsbVersion version, string ptxFormat)
    {
        RsbPack.Pack(new RsbPackOptions
        {
            InputFolder = inputFolder,
            OutputPath = outputPath,
            Version = version,
            PtxFormat = ptxFormat,
        });
    }
}
