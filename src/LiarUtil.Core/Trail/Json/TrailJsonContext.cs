using System.Text.Json.Serialization;

namespace LiarUtil.Core.Trail.Json;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNameCaseInsensitive = true,
    NumberHandling = JsonNumberHandling.AllowReadingFromString,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(TrailFile))]
internal sealed partial class TrailJsonContext : JsonSerializerContext;
