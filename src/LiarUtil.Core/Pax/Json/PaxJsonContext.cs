using System.Text.Json.Serialization;
using LiarUtil.Core.Pax.Models;

namespace LiarUtil.Core.Pax.Json;

[JsonSourceGenerationOptions(WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(PaxFile))]
internal sealed partial class PaxJsonContext : JsonSerializerContext;
