using System.Text.Json.Serialization;

namespace LiarUtil.Core.Trail;

public sealed class TrailTracks
{
    public List<TrailTrackNode>? WidthOverLength { get; set; }

    public List<TrailTrackNode>? WidthOverTime { get; set; }

    public List<TrailTrackNode>? AlphaOverLength { get; set; }

    public List<TrailTrackNode>? AlphaOverTime { get; set; }

    public List<TrailTrackNode>? TrailDuration { get; set; }

    [JsonIgnore]
    public bool IsEmpty =>
        WidthOverLength is null && WidthOverTime is null && AlphaOverLength is null &&
        AlphaOverTime is null && TrailDuration is null;
}
