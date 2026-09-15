namespace LiarUtil.Core.Reanim;

public sealed class ReanimTrack
{
    public string? Name { get; set; }

    public List<ReanimTransform> Transforms { get; set; } = [];
}
