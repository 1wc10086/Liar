using System.Text.Json;
using System.Text.Json.Serialization;
using LiarUtil.Core.Cfu2.Models;

namespace LiarUtil.Core.Cfu2.Json;

internal sealed class Cfu2UnicodeConverter : JsonConverter<Cfu2Unicode>
{
    public override Cfu2Unicode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetUInt16(out var value))
        {
            return new(value);
        }
        if (reader.TokenType == JsonTokenType.String)
        {
            return new(Parse(reader.GetString()));
        }
        throw new JsonException(LiarUtil.Core.Strings.CFU2CharacterMustSingleCharacterStringOrNumber);
    }

    public override void Write(Utf8JsonWriter writer, Cfu2Unicode value, JsonSerializerOptions options)
    {
        if (char.IsSurrogate((char)value.Value))
        {
            writer.WriteNumberValue(value.Value);
            return;
        }
        writer.WriteStringValue(((char)value.Value).ToString());
    }

    private static ushort Parse(string? text)
    {
        if (text is not { Length: 1 })
        {
            throw new JsonException(LiarUtil.Core.Strings.CFU2CharacterStringLengthMust1);
        }
        return text[0];
    }
}
