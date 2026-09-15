using System.Text.Json.Serialization;
using LiarUtil.Core.Dz.Models;

namespace LiarUtil.Core.Dz.Json;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault)]
[JsonSerializable(typeof(DzPackageDefinition))]
internal sealed partial class DzDefinitionJsonContext : JsonSerializerContext;
