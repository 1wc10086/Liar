using LiarUtil.Core.Particles;

namespace LiarUtil.Core.Services;

public sealed class ParticleService
{
    public void Decode(string inputPath, string outputPath, ParticlePlatform platform, bool useXml)
    {
        var data = File.ReadAllBytes(inputPath);
        var text = useXml
            ? ParticleCodec.DecodeToXml(data, platform)
            : ParticleCodec.DecodeToJson(data, platform);
        WriteText(outputPath, text);
    }

    public void Encode(string inputPath, string outputPath, ParticlePlatform platform, bool useXml, bool compress)
    {
        var text = File.ReadAllText(inputPath);
        var data = useXml
            ? ParticleCodec.EncodeFromXml(text, platform, compress)
            : ParticleCodec.EncodeFromJson(text, platform, compress);
        WriteBytes(outputPath, data);
    }

    private static void WriteText(string path, string text)
    {
        EnsureDirectory(path);
        File.WriteAllText(path, text);
    }

    private static void WriteBytes(string path, byte[] data)
    {
        EnsureDirectory(path);
        File.WriteAllBytes(path, data);
    }

    private static void EnsureDirectory(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}
