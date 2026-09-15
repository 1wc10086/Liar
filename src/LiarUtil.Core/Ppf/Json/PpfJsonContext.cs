using System.Text.Json.Serialization;
using LiarUtil.Core.Ppf.Models;

namespace LiarUtil.Core.Ppf.Json;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(PpfEffect))]
internal sealed partial class PpfJsonContext : JsonSerializerContext;
