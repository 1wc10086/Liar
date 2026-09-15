namespace LiarUtil.Core.Particles.Binary;

internal sealed record ParticleBinaryProfile(
    ParticlePlatform Platform,
    bool BigEndian,
    bool HasImagePath,
    bool IntegerImage,
    int RecordSize,
    int FieldMarker,
    int FieldTypePaddingBytes);

internal static class ParticleBinaryProfiles
{
    public static ParticleBinaryProfile Get(ParticlePlatform platform) => platform switch
    {
        ParticlePlatform.Pc => new(ParticlePlatform.Pc, false, false, false, 0x164, 0x14, 16),
        ParticlePlatform.Tv => new(ParticlePlatform.Tv, false, true, false, 0x164, 0x14, 16),
        ParticlePlatform.Phone32 => new(ParticlePlatform.Phone32, false, false, true, 0x164, 0x14, 16),
        ParticlePlatform.Phone64 => new(ParticlePlatform.Phone64, false, false, true, 0x2B0, 0x28, 36),
        ParticlePlatform.GameConsole => new(ParticlePlatform.GameConsole, true, false, false, 0x164, 0x14, 16),
        _ => new(ParticlePlatform.Wp, false, false, false, 0, 0, 0),
    };
}
