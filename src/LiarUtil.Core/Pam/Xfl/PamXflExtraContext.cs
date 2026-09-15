using System.Text.Json.Serialization;

namespace LiarUtil.Core.Pam.Xfl;

[JsonSourceGenerationOptions(WriteIndented = true, AllowTrailingCommas = true)]
[JsonSerializable(typeof(PamXflExtra))]
internal sealed partial class PamXflExtraContext : JsonSerializerContext;
