using System.Text.Json.Serialization;
using LiarUtil.Core.Particles.Json;

namespace LiarUtil.Core.Particles;

[JsonConverter(typeof(ParticleFieldTypeConverter))]
public enum ParticleFieldType
{
    Invalid = 0,
    Friction = 1,
    Acceleration = 2,
    Attractor = 3,
    MaxVelocity = 4,
    Velocity = 5,
    Position = 6,
    SystemPosition = 7,
    GroundConstraint = 8,
    Shake = 9,
    Circle = 10,
    Away = 11,
}
