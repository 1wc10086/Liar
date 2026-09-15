namespace LiarUtil.Core.Particles;

public sealed class ParticleField
{
    public ParticleFieldType? Type { get; set; }

    public List<ParticleTrackNode>? X { get; set; }

    public List<ParticleTrackNode>? Y { get; set; }
}
