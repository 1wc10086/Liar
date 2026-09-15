using LiarUtil.Core.PopCap;

namespace LiarUtil.Core.Trail;

public sealed class TrailFile
{
    public int? MaxPoints { get; set; }

    public float? MinPointDistance { get; set; }

    public bool Loops { get; set; }

    public ImageReference? Image { get; set; }

    public string? ImageResource { get; set; }

    public TrailTracks Tracks { get; set; } = new();
}
