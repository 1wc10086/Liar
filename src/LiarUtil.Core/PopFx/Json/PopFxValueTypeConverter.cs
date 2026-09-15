using System.Text.Json;
using System.Text.Json.Serialization;
using LiarUtil.Core.PopFx.Models;

namespace LiarUtil.Core.PopFx.Json;

public sealed class PopFxValueTypeConverter : JsonConverter<PopFxValueType>
{
    public override PopFxValueType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            var number = reader.GetInt32();
            if (Enum.IsDefined((PopFxValueType)number))
            {
                return (PopFxValueType)number;
            }
            throw new JsonException(string.Format(LiarUtil.Core.Strings.POPFXValueTypeInvalid0, number));
        }
        if (reader.TokenType == JsonTokenType.String)
        {
            var text = reader.GetString() ?? "";
            if (Enum.TryParse<PopFxValueType>(text, true, out var type) && Enum.IsDefined(type))
            {
                return type;
            }
            throw new JsonException(string.Format(LiarUtil.Core.Strings.POPFXValueTypeInvalid0, text));
        }
        throw new JsonException(LiarUtil.Core.Strings.POPFXValueTypeMustStringOrNumber);
    }

    public override void Write(Utf8JsonWriter writer, PopFxValueType value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString());
}
