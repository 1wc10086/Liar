using System.Text.Json.Serialization;
using LiarUtil.Core.PopCap;
using LiarUtil.Core.Trail.Json;

namespace LiarUtil.Core.Trail;

[JsonConverter(typeof(TrailTrackNodeConverter))]
public sealed class TrailTrackNode
{
    public float Time { get; set; }

    public float Low { get; set; }

    public float High { get; set; }

    public PopCurve Distribution { get; set; } = PopCurve.Linear;

    public PopCurve Curve { get; set; } = PopCurve.Linear;
}
