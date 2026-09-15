using System.Text.Json.Serialization;

namespace LiarUtil.Core.Particles;

public sealed class ParticleTracks
{
    public List<ParticleTrackNode>? Duration { get; set; }

    public List<ParticleTrackNode>? LaunchSpeed { get; set; }

    public List<ParticleTrackNode>? LaunchAngle { get; set; }

    public List<ParticleTrackNode>? Red { get; set; }

    public List<ParticleTrackNode>? Green { get; set; }

    public List<ParticleTrackNode>? Blue { get; set; }

    public List<ParticleTrackNode>? Alpha { get; set; }

    public List<ParticleTrackNode>? Brightness { get; set; }

    public List<ParticleTrackNode>? SpinAngle { get; set; }

    public List<ParticleTrackNode>? SpinSpeed { get; set; }

    public List<ParticleTrackNode>? Scale { get; set; }

    public List<ParticleTrackNode>? Stretch { get; set; }

    public List<ParticleTrackNode>? CollisionReflect { get; set; }

    public List<ParticleTrackNode>? CollisionSpin { get; set; }

    public List<ParticleTrackNode>? ClipTop { get; set; }

    public List<ParticleTrackNode>? ClipBottom { get; set; }

    public List<ParticleTrackNode>? ClipLeft { get; set; }

    public List<ParticleTrackNode>? ClipRight { get; set; }

    public List<ParticleTrackNode>? AnimationRate { get; set; }

    [JsonIgnore]
    public bool IsEmpty =>
        Duration is null && LaunchSpeed is null && LaunchAngle is null && Red is null && Green is null &&
        Blue is null && Alpha is null && Brightness is null && SpinAngle is null && SpinSpeed is null &&
        Scale is null && Stretch is null && CollisionReflect is null && CollisionSpin is null &&
        ClipTop is null && ClipBottom is null && ClipLeft is null && ClipRight is null && AnimationRate is null;
}
