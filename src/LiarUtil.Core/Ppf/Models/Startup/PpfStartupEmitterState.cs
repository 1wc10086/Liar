namespace LiarUtil.Core.Ppf.Models;

public sealed class PpfStartupEmitterState
{
    public bool HasTransform { get; set; }
    public PpfTransform Transform { get; set; } = new();
    public bool WasActive { get; set; }
    public bool WithinLifeFrame { get; set; }
    public List<PpfStartupParticleDefState> ParticleDefStates { get; set; } = [];
    public List<PpfStartupParticleDefState> FreeParticleDefStates { get; set; } = [];
    public List<PpfStartupFreeEmitterState> FreeEmitters { get; set; } = [];
    public List<PpfStartupParticleState> Particles { get; set; } = [];
}
