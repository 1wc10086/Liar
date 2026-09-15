namespace LiarUtil.Core.Trail;

internal static class TrailTracksExtensions
{
    public static List<TrailTrackNode>? Get(this TrailTracks tracks, string name) => name switch
    {
        "WidthOverLength" => tracks.WidthOverLength,
        "WidthOverTime" => tracks.WidthOverTime,
        "AlphaOverLength" => tracks.AlphaOverLength,
        "AlphaOverTime" => tracks.AlphaOverTime,
        "TrailDuration" => tracks.TrailDuration,
        _ => throw new ArgumentOutOfRangeException(nameof(name)),
    };

    public static void Set(this TrailTracks tracks, string name, List<TrailTrackNode>? nodes)
    {
        switch (name)
        {
            case "WidthOverLength":
                tracks.WidthOverLength = nodes;
                break;
            case "WidthOverTime":
                tracks.WidthOverTime = nodes;
                break;
            case "AlphaOverLength":
                tracks.AlphaOverLength = nodes;
                break;
            case "AlphaOverTime":
                tracks.AlphaOverTime = nodes;
                break;
            case "TrailDuration":
                tracks.TrailDuration = nodes;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(name));
        }
    }
}
