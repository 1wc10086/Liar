using LiarUtil.Core.Xnb.Fonts;

namespace LiarUtil.Core.Services;

public sealed class XnbFontService
{
    public void Decode(string inputPath, string outputPath) =>
        FileHelper.WriteText(outputPath, XnbFontCodec.DecodeToJson(File.ReadAllBytes(inputPath)));

    public void Encode(string inputPath, string outputPath) =>
        FileHelper.WriteBytes(outputPath, XnbFontCodec.EncodeFromJson(File.ReadAllText(inputPath)));
}
