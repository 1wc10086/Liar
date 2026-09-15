namespace LiarUtil.Core.Ppf.Models;

public sealed class PpfStartupParticleDefState
{
    public float NumberAccumulator { get; set; }
    public float CurrentNumberVariation { get; set; }
    public int ParticlesEmitted { get; set; }
    public int Ticks { get; set; }
}
