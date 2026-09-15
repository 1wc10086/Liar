using System.Text.Json.Serialization;
using LiarUtil.Core.Particles.Json;

namespace LiarUtil.Core.Particles;

[JsonConverter(typeof(ParticleEmitterTypeConverter))]
public enum ParticleEmitterType
{
    Circle = 0,
    Box = 1,
    BoxPath = 2,
    CirclePath = 3,
    CircleEvenSpacing = 4,
}
