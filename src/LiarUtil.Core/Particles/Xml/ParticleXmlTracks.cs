namespace LiarUtil.Core.Particles.Xml;

internal sealed record ParticleXmlTrack(
    string Element,
    Func<ParticleEmitter, List<ParticleTrackNode>?> Get,
    Action<ParticleEmitter, List<ParticleTrackNode>?> Set);

internal static class ParticleXmlTracks
{
    public static readonly ParticleXmlTrack[] All =
    [
        new("SystemDuration", e => e.System?.Duration, (e, v) => (e.System ??= new ParticleSystemTracks()).Duration = v),
        new("CrossFadeDuration", e => e.System?.CrossFadeDuration, (e, v) => (e.System ??= new ParticleSystemTracks()).CrossFadeDuration = v),
        new("SpawnRate", e => e.System?.SpawnRate, (e, v) => (e.System ??= new ParticleSystemTracks()).SpawnRate = v),
        new("SpawnMinActive", e => e.System?.SpawnMinActive, (e, v) => (e.System ??= new ParticleSystemTracks()).SpawnMinActive = v),
        new("SpawnMaxActive", e => e.System?.SpawnMaxActive, (e, v) => (e.System ??= new ParticleSystemTracks()).SpawnMaxActive = v),
        new("SpawnMaxLaunched", e => e.System?.SpawnMaxLaunched, (e, v) => (e.System ??= new ParticleSystemTracks()).SpawnMaxLaunched = v),
        new("EmitterRadius", e => e.System?.Radius, (e, v) => (e.System ??= new ParticleSystemTracks()).Radius = v),
        new("EmitterOffsetX", e => e.System?.OffsetX, (e, v) => (e.System ??= new ParticleSystemTracks()).OffsetX = v),
        new("EmitterOffsetY", e => e.System?.OffsetY, (e, v) => (e.System ??= new ParticleSystemTracks()).OffsetY = v),
        new("EmitterBoxX", e => e.System?.BoxX, (e, v) => (e.System ??= new ParticleSystemTracks()).BoxX = v),
        new("EmitterBoxY", e => e.System?.BoxY, (e, v) => (e.System ??= new ParticleSystemTracks()).BoxY = v),
        new("EmitterPath", e => e.System?.Path, (e, v) => (e.System ??= new ParticleSystemTracks()).Path = v),
        new("EmitterSkewX", e => e.System?.SkewX, (e, v) => (e.System ??= new ParticleSystemTracks()).SkewX = v),
        new("EmitterSkewY", e => e.System?.SkewY, (e, v) => (e.System ??= new ParticleSystemTracks()).SkewY = v),
        new("SystemRed", e => e.System?.Red, (e, v) => (e.System ??= new ParticleSystemTracks()).Red = v),
        new("SystemGreen", e => e.System?.Green, (e, v) => (e.System ??= new ParticleSystemTracks()).Green = v),
        new("SystemBlue", e => e.System?.Blue, (e, v) => (e.System ??= new ParticleSystemTracks()).Blue = v),
        new("SystemAlpha", e => e.System?.Alpha, (e, v) => (e.System ??= new ParticleSystemTracks()).Alpha = v),
        new("SystemBrightness", e => e.System?.Brightness, (e, v) => (e.System ??= new ParticleSystemTracks()).Brightness = v),
        new("ParticleDuration", e => e.Particle?.Duration, (e, v) => (e.Particle ??= new ParticleTracks()).Duration = v),
        new("LaunchSpeed", e => e.Particle?.LaunchSpeed, (e, v) => (e.Particle ??= new ParticleTracks()).LaunchSpeed = v),
        new("LaunchAngle", e => e.Particle?.LaunchAngle, (e, v) => (e.Particle ??= new ParticleTracks()).LaunchAngle = v),
        new("ParticleRed", e => e.Particle?.Red, (e, v) => (e.Particle ??= new ParticleTracks()).Red = v),
        new("ParticleGreen", e => e.Particle?.Green, (e, v) => (e.Particle ??= new ParticleTracks()).Green = v),
        new("ParticleBlue", e => e.Particle?.Blue, (e, v) => (e.Particle ??= new ParticleTracks()).Blue = v),
        new("ParticleAlpha", e => e.Particle?.Alpha, (e, v) => (e.Particle ??= new ParticleTracks()).Alpha = v),
        new("ParticleBrightness", e => e.Particle?.Brightness, (e, v) => (e.Particle ??= new ParticleTracks()).Brightness = v),
        new("ParticleSpinAngle", e => e.Particle?.SpinAngle, (e, v) => (e.Particle ??= new ParticleTracks()).SpinAngle = v),
        new("ParticleSpinSpeed", e => e.Particle?.SpinSpeed, (e, v) => (e.Particle ??= new ParticleTracks()).SpinSpeed = v),
        new("ParticleScale", e => e.Particle?.Scale, (e, v) => (e.Particle ??= new ParticleTracks()).Scale = v),
        new("ParticleStretch", e => e.Particle?.Stretch, (e, v) => (e.Particle ??= new ParticleTracks()).Stretch = v),
        new("CollisionReflect", e => e.Particle?.CollisionReflect, (e, v) => (e.Particle ??= new ParticleTracks()).CollisionReflect = v),
        new("CollisionSpin", e => e.Particle?.CollisionSpin, (e, v) => (e.Particle ??= new ParticleTracks()).CollisionSpin = v),
        new("ClipTop", e => e.Particle?.ClipTop, (e, v) => (e.Particle ??= new ParticleTracks()).ClipTop = v),
        new("ClipBottom", e => e.Particle?.ClipBottom, (e, v) => (e.Particle ??= new ParticleTracks()).ClipBottom = v),
        new("ClipLeft", e => e.Particle?.ClipLeft, (e, v) => (e.Particle ??= new ParticleTracks()).ClipLeft = v),
        new("ClipRight", e => e.Particle?.ClipRight, (e, v) => (e.Particle ??= new ParticleTracks()).ClipRight = v),
        new("AnimationRate", e => e.Particle?.AnimationRate, (e, v) => (e.Particle ??= new ParticleTracks()).AnimationRate = v),
    ];

    public static readonly Dictionary<string, ParticleXmlTrack> ByElement =
        All.ToDictionary(track => track.Element, StringComparer.Ordinal);
}
