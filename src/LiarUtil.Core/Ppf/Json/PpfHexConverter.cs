using System.Text.Json;
using System.Text.Json.Serialization;

namespace LiarUtil.Core.Ppf.Json;

public sealed class PpfHexConverter : JsonConverter<byte[]>
{
    public override byte[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException(LiarUtil.Core.Strings.PPFHexadecimalDataMustString);
        }
        var text = reader.GetString() ?? "";
        var bytes = new List<byte>(text.Length / 2);
        var high = -1;
        foreach (var character in text)
        {
            if (character is ' ' or '\t' or '\r' or '\n' or '-' or ',')
            {
                continue;
            }
            var value = ToHex(character);
            if (high < 0)
            {
                high = value;
                continue;
            }
            bytes.Add((byte)((high << 4) | value));
            high = -1;
        }
        if (high >= 0)
        {
            throw new JsonException(LiarUtil.Core.Strings.PPFHexadecimalDataLengthInvalid);
        }
        return [.. bytes];
    }

    public override void Write(Utf8JsonWriter writer, byte[] value, JsonSerializerOptions options) =>
        writer.WriteStringValue(string.Join(' ', value.Select(static item => item.ToString("X2"))));

    private static int ToHex(char character) => character switch
    {
        >= '0' and <= '9' => character - '0',
        >= 'a' and <= 'f' => character - 'a' + 10,
        >= 'A' and <= 'F' => character - 'A' + 10,
        _ => throw new JsonException(string.Format(LiarUtil.Core.Strings.PPFHexadecimalCharacterInvalid0, character)),
    };
}
