using System.Text.Json.Serialization;

namespace LiarUtil.Core.Reanim.Json;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNameCaseInsensitive = true,
    NumberHandling = JsonNumberHandling.AllowReadingFromString,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(ReanimFile))]
internal sealed partial class ReanimJsonContext : JsonSerializerContext;
