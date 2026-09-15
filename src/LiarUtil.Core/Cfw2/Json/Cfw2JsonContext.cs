using System.Text.Json.Serialization;
using LiarUtil.Core.Cfw2.Models;

namespace LiarUtil.Core.Cfw2.Json;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(Cfw2FontWidget))]
internal sealed partial class Cfw2JsonContext : JsonSerializerContext;
