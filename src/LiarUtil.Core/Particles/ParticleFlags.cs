using System.Text.Json.Serialization;
using LiarUtil.Core.Particles.Json;

namespace LiarUtil.Core.Particles;

[Flags]
[JsonConverter(typeof(ParticleFlagsConverter))]
public enum ParticleFlags
{
    None = 0,
    RandomLaunchSpin = 1 << 0,
    AlignLaunchSpin = 1 << 1,
    AlignToPixel = 1 << 2,
    SystemLoops = 1 << 3,
    ParticleLoops = 1 << 4,
    ParticlesDontFollow = 1 << 5,
    RandomStartTime = 1 << 6,
    DieIfOverloaded = 1 << 7,
    Additive = 1 << 8,
    FullScreen = 1 << 9,
    SoftwareOnly = 1 << 10,
    HardwareOnly = 1 << 11,
}
