using LiarUtil.Core.Trail;

namespace LiarUtil.Core.Services;

public sealed class TrailService
{
    public void Decode(string inputPath, string outputPath, TrailPlatform platform, bool useXml)
    {
        var data = File.ReadAllBytes(inputPath);
        var trail = TrailCodec.TryDecode(data, platform) ?? TrailCodec.DecodeAuto(data);
        var text = useXml ? TrailCodec.EncodeXml(trail) : TrailCodec.EncodeJson(trail);
        FileHelper.WriteText(outputPath, text);
    }

    public void Encode(string inputPath, string outputPath, TrailPlatform platform, bool useXml, bool compress)
    {
        var text = File.ReadAllText(inputPath);
        var trail = useXml ? TrailCodec.DecodeXml(text) : TrailCodec.DecodeJson(text);
        FileHelper.WriteBytes(outputPath, TrailCodec.Encode(trail, platform, compress));
    }
}
