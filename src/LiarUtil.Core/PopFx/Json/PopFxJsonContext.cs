using System.Text.Json.Serialization;
using LiarUtil.Core.PopFx.Models;

namespace LiarUtil.Core.PopFx.Json;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(PopFxFile))]
internal sealed partial class PopFxJsonContext : JsonSerializerContext;
