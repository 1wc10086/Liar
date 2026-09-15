using System.Text.Json;
using System.Text.Json.Serialization;

namespace LiarUtil.Core.PopFx.Json;

public sealed class PopFxCodeConverter : JsonConverter<string>
{
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return reader.GetString() ?? "";
        }
        if (reader.TokenType == JsonTokenType.StartArray)
        {
            var lines = new List<string>();
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                if (reader.TokenType != JsonTokenType.String)
                {
                    throw new JsonException(LiarUtil.Core.Strings.POPFXCodeLineMustString);
                }
                lines.Add(reader.GetString() ?? "");
            }
            return string.Join("\n", lines);
        }
        throw new JsonException(LiarUtil.Core.Strings.POPFXCodeMustStringOrStringArray);
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var line in value.Split('\n'))
        {
            writer.WriteStringValue(line);
        }
        writer.WriteEndArray();
    }
}
