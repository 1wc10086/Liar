using System.Text.Json.Serialization;

namespace LiarUtil.Core.Particles.Json;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(ParticleFile))]
internal sealed partial class ParticleJsonContext : JsonSerializerContext;
