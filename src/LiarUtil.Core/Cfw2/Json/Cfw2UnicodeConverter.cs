using System.Text.Json;
using System.Text.Json.Serialization;
using LiarUtil.Core.Cfw2.Models;

namespace LiarUtil.Core.Cfw2.Json;

internal sealed class Cfw2UnicodeConverter : JsonConverter<Cfw2Unicode>
{
    public override Cfw2Unicode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetUInt16(out var value))
        {
            return new(value);
        }
        if (reader.TokenType == JsonTokenType.String)
        {
            return new(Parse(reader.GetString()));
        }
        throw new JsonException(LiarUtil.Core.Strings.CFW2CharacterMustSingleCharacterStringOrNumber);
    }

    public override void Write(Utf8JsonWriter writer, Cfw2Unicode value, JsonSerializerOptions options)
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
            throw new JsonException(LiarUtil.Core.Strings.CFW2CharacterStringLengthMust1);
        }
        return text[0];
    }
}
