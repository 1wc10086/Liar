namespace LiarUtil.Core.Particles;

public sealed class ParticleEmitter
{
    public string? Name { get; set; }

    public ParticleImage? Image { get; set; }

    public ParticleFlags? Flags { get; set; }

    public ParticleEmitterType? EmitterType { get; set; }

    public string? OnDuration { get; set; }

    public ParticleSystemTracks? System { get; set; }

    public ParticleTracks? Particle { get; set; }

    public List<ParticleField>? Fields { get; set; }

    public List<ParticleField>? SystemFields { get; set; }
}
