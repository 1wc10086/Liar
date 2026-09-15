namespace LiarUtil.Core.Reanim;

public sealed class ReanimFile
{
    public sbyte? DoScale { get; set; }

    public float Fps { get; set; } = 12f;

    public List<ReanimTrack> Tracks { get; set; } = [];
}
