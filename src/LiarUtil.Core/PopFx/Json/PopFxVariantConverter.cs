using System.Text.Json;
using System.Text.Json.Serialization;

namespace LiarUtil.Core.PopFx.Json;

public sealed class PopFxVariantConverter : JsonConverter<PopFxVariant>
{
    public override PopFxVariant Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var text = reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Number => reader.GetInt32().ToString(),
            _ => throw new JsonException(LiarUtil.Core.Strings.POPFXVersionVariantMustStringOrNumber),
        };
        var normalized = (text ?? "").Trim();
        if (normalized.StartsWith('v') || normalized.StartsWith('V'))
        {
            normalized = normalized[1..];
        }
        if (normalized is "1" or "2" or "3")
        {
            return (PopFxVariant)int.Parse(normalized);
        }
        if (Enum.TryParse<PopFxVariant>(normalized, true, out var variant))
        {
            return variant;
        }
        throw new JsonException(string.Format(LiarUtil.Core.Strings.POPFXVersionVariantInvalid0, text));
    }

    public override void Write(Utf8JsonWriter writer, PopFxVariant value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString());
}
