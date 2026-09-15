using System.Text.Json.Serialization;

namespace LiarUtil.Core.Particles;

public sealed class ParticleSystemTracks
{
    public List<ParticleTrackNode>? Duration { get; set; }

    public List<ParticleTrackNode>? CrossFadeDuration { get; set; }

    public List<ParticleTrackNode>? SpawnRate { get; set; }

    public List<ParticleTrackNode>? SpawnMinActive { get; set; }

    public List<ParticleTrackNode>? SpawnMaxActive { get; set; }

    public List<ParticleTrackNode>? SpawnMaxLaunched { get; set; }

    public List<ParticleTrackNode>? Radius { get; set; }

    public List<ParticleTrackNode>? OffsetX { get; set; }

    public List<ParticleTrackNode>? OffsetY { get; set; }

    public List<ParticleTrackNode>? BoxX { get; set; }

    public List<ParticleTrackNode>? BoxY { get; set; }

    public List<ParticleTrackNode>? Path { get; set; }

    public List<ParticleTrackNode>? SkewX { get; set; }

    public List<ParticleTrackNode>? SkewY { get; set; }

    public List<ParticleTrackNode>? Red { get; set; }

    public List<ParticleTrackNode>? Green { get; set; }

    public List<ParticleTrackNode>? Blue { get; set; }

    public List<ParticleTrackNode>? Alpha { get; set; }

    public List<ParticleTrackNode>? Brightness { get; set; }

    [JsonIgnore]
    public bool IsEmpty =>
        Duration is null && CrossFadeDuration is null && SpawnRate is null && SpawnMinActive is null &&
        SpawnMaxActive is null && SpawnMaxLaunched is null && Radius is null && OffsetX is null &&
        OffsetY is null && BoxX is null && BoxY is null && Path is null && SkewX is null && SkewY is null &&
        Red is null && Green is null && Blue is null && Alpha is null && Brightness is null;
}
