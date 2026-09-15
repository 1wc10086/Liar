using System.Text.Json.Serialization;
using LiarUtil.Core.Particles.Json;
using LiarUtil.Core.PopCap;

namespace LiarUtil.Core.Particles;

[JsonConverter(typeof(ParticleTrackNodeConverter))]
public sealed class ParticleTrackNode
{
    public float Time { get; set; }

    public float Low { get; set; }

    public float High { get; set; }

    public PopCurve Distribution { get; set; } = PopCurve.Linear;

    public PopCurve Curve { get; set; } = PopCurve.Linear;
}
