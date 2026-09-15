using LiarUtil.Core.Reanim;
using LiarUtil.Core.Reanim.Flash;

namespace LiarUtil.Core.Services;

public sealed class ReanimService
{
    public void Decode(string inputPath, string outputPath, ReanimPlatform platform, bool useXml)
    {
        if (platform == ReanimPlatform.FlashXfl)
        {
            var reanim = FlashXflService.Decode(inputPath);
            FileHelper.WriteText(outputPath, ReanimCodec.EncodeJson(reanim));
            return;
        }

        var data = File.ReadAllBytes(inputPath);
        var model = ReanimCodec.TryDecode(data, platform) ?? ReanimCodec.DecodeAuto(data);
        FileHelper.WriteText(outputPath, useXml ? ReanimCodec.EncodeXml(model) : ReanimCodec.EncodeJson(model));
    }

    public void Encode(string inputPath, string outputPath, ReanimPlatform platform, bool useXml, bool compress, XflWriterOptions? xflOptions = null)
    {
        var text = File.ReadAllText(inputPath);
        if (platform == ReanimPlatform.FlashXfl)
        {
            FlashXflService.Encode(ReanimCodec.DecodeJson(text), outputPath, xflOptions ?? new XflWriterOptions());
            return;
        }

        var reanim = useXml ? ReanimCodec.DecodeXml(text) : ReanimCodec.DecodeJson(text);
        FileHelper.WriteBytes(outputPath, ReanimCodec.Encode(reanim, platform, compress));
    }
}
