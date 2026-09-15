namespace LiarUtil.Core.Ppf.Models;

public sealed class PpfStartupFreeEmitterState
{
    public short FreeListIndex { get; set; }
    public PpfStartupParticleState Particle { get; set; } = new();
    public List<PpfStartupParticleDefState> ParticleDefStates { get; set; } = [];
    public List<PpfStartupParticleState> Particles { get; set; } = [];
}
