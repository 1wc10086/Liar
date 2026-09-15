using System.Text.Json.Serialization;
using LiarUtil.Core.Pak.Models;

namespace LiarUtil.Core.Pak.Json;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(PakDefinition))]
internal sealed partial class PakDefinitionJsonContext : JsonSerializerContext;
