using LiarUtil.Core.FontWidgetDat;

namespace LiarUtil.Core.Services;

public sealed class FontWidgetDatService
{
    public void Decode(string inputPath, string outputPath) =>
        FileHelper.WriteText(outputPath, FontWidgetDatCodec.DecodeToJson(File.ReadAllBytes(inputPath)));

    public void Encode(string inputPath, string outputPath) =>
        FileHelper.WriteBytes(outputPath, FontWidgetDatCodec.EncodeFromJson(File.ReadAllText(inputPath)));
}
