using System.Text.Json.Serialization;

namespace LiarUtil.Core.Pam;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(PamAnimation))]
internal sealed partial class PamJsonContext : JsonSerializerContext;
