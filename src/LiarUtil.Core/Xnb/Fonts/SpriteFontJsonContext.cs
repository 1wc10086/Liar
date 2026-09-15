using System.Text.Json.Serialization;

namespace LiarUtil.Core.Xnb.Fonts;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(SpriteFontData))]
internal sealed partial class SpriteFontJsonContext : JsonSerializerContext;
