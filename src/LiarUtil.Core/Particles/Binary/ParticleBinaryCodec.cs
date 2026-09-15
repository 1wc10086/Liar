using LiarUtil.Core.Core.Binary;
using LiarUtil.Core.Core.Compression;

namespace LiarUtil.Core.Particles.Binary;

internal static partial class ParticleBinaryCodec
{
    private const int CompressedMagic = unchecked((int)0xDEADFED4);
    private const int Phone64Magic = unchecked((int)0xE09295E9);
    private const int BodyMagic = 1092589901;

    private static readonly byte[] WpMagic = [0x58, 0x4E, 0x42, 0x6D, 0x05, 0x00];

    private static readonly byte[] WpInfo =
    [
        0x01, 0x24, 0x53, 0x65, 0x78, 0x79, 0x2E, 0x54, 0x6F, 0x64, 0x4C, 0x69, 0x62, 0x2E, 0x53, 0x65,
        0x78, 0x79, 0x50, 0x61, 0x72, 0x74, 0x69, 0x63, 0x6C, 0x65, 0x52, 0x65, 0x61, 0x64, 0x65, 0x72,
        0x2C, 0x20, 0x4C, 0x41, 0x57, 0x4E, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01,
    ];

    public static ParticleFile Decode(byte[] data, ParticlePlatform platform)
    {
        var file = platform == ParticlePlatform.Wp ? ReadWp(data) : ReadBinary(data, platform);
        Normalize(file);
        return file;
    }

    public static byte[] Encode(ParticleFile file, ParticlePlatform platform, bool compress)
    {
        if (platform == ParticlePlatform.Wp)
        {
            return WriteWp(file);
        }

        var profile = ParticleBinaryProfiles.Get(platform);
        var body = WriteBinary(file, profile);
        return compress ? Compress(body, profile) : body;
    }

    private static byte[] Decompress(byte[] input, ParticleBinaryProfile profile)
    {
        if (input.Length < sizeof(int))
        {
            return input;
        }

        var reader = new BufferReader(input, profile.BigEndian);
        if (reader.PeekInt32() != CompressedMagic)
        {
            return input;
        }

        var offset = profile.RecordSize == 0x2B0 ? 16 : 8;
        return input.Length <= offset ? input : ZlibCodec.Decompress(input.AsSpan(offset));
    }

    private static byte[] Compress(byte[] body, ParticleBinaryProfile profile)
    {
        var writer = new BufferWriter(profile.BigEndian);
        writer.WriteInt32(CompressedMagic);
        if (profile.RecordSize == 0x2B0)
        {
            writer.WriteInt32(0);
            writer.WriteInt32(body.Length);
            writer.WriteInt32(0);
        }
        else
        {
            writer.WriteInt32(body.Length);
        }

        writer.WriteBytes(ZlibCodec.Compress(body, 9));
        return writer.ToArray();
    }

    private static void Normalize(ParticleFile file)
    {
        foreach (var emitter in file.Emitters)
        {
            if (emitter.Image is { } image && image.Name is null && image.Id is null && image.Resource is null &&
                image.Columns is null && image.Rows is null && image.Frames is null && image.Animated is null)
            {
                emitter.Image = null;
            }

            if (emitter.System is { IsEmpty: true })
            {
                emitter.System = null;
            }

            if (emitter.Particle is { IsEmpty: true })
            {
                emitter.Particle = null;
            }
        }
    }
}
