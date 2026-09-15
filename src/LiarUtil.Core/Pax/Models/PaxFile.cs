namespace LiarUtil.Core.Pax.Models;

public sealed class PaxFile
{
    public int Version { get; set; }

    public List<PaxFootage> Footages { get; set; } = [];

    public List<PaxComposition> Compositions { get; set; } = [];

    public PaxTerminator? Terminator { get; set; }
}

public sealed class PaxTerminator
{
    public int Offset { get; set; }

    public int Tag { get; set; }

    public PaxTerminatorKind Name { get; set; }
}
