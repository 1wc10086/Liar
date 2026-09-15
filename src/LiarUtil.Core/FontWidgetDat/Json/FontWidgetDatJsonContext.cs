using System.Text.Json.Serialization;
using LiarUtil.Core.FontWidgetDat.Models;

namespace LiarUtil.Core.FontWidgetDat.Json;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(FontWidgetDatFile))]
internal sealed partial class FontWidgetDatJsonContext : JsonSerializerContext;
