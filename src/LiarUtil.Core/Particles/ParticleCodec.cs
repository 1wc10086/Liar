using System.Text.Json;
using LiarUtil.Core.Particles.Binary;
using LiarUtil.Core.Particles.Json;
using LiarUtil.Core.Particles.Xml;

namespace LiarUtil.Core.Particles;

public static class ParticleCodec
{
    public static ParticleFile Decode(byte[] data, ParticlePlatform platform) =>
        ParticleBinaryCodec.Decode(data, platform);

    public static byte[] Encode(ParticleFile file, ParticlePlatform platform, bool compress = true) =>
        ParticleBinaryCodec.Encode(file, platform, compress);

    public static string DecodeToJson(byte[] data, ParticlePlatform platform) =>
        JsonSerializer.Serialize(Decode(data, platform), ParticleJsonContext.Default.ParticleFile);

    public static byte[] EncodeFromJson(string json, ParticlePlatform platform, bool compress = true)
    {
        var file = JsonSerializer.Deserialize(json, ParticleJsonContext.Default.ParticleFile)
            ?? throw new InvalidDataException(LiarUtil.Core.Strings.ParticleJSONContentEmpty);
        return Encode(file, platform, compress);
    }

    public static string DecodeToXml(byte[] data, ParticlePlatform platform) =>
        ParticleXmlWriter.Write(Decode(data, platform));

    public static byte[] EncodeFromXml(string xml, ParticlePlatform platform, bool compress = true) =>
        Encode(ParticleXmlReader.Read(xml), platform, compress);
}
