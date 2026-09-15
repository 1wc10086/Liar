namespace LiarUtil.Core.Ppf.Models;

public sealed class PpfEmitter
{
    public string Name { get; set; } = "";
    public bool KeepInOrder { get; set; }
    public bool OldestInFront { get; set; }
    public List<PpfEmitterParticle> Particles { get; set; } = [];
    public PpfEmitterValues Values { get; set; } = new();
    public int Unknown1 { get; set; }
    public int Unknown2 { get; set; }
    public int Unknown3 { get; set; }
    public int Unknown4 { get; set; }
    public int Unknown5 { get; set; }
}
