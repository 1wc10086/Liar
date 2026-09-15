using System.Text.Json.Serialization;
using LiarUtil.Core.Cfu2.Models;

namespace LiarUtil.Core.Cfu2.Json;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(Cfu2FontWidget))]
internal sealed partial class Cfu2JsonContext : JsonSerializerContext;
