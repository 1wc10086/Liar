using System.Text.Json;
using System.Text.Json.Serialization;

namespace LiarUtil.Core.PopCap;

public sealed class PopCurveJsonConverter : JsonConverter<PopCurve>
{
    public override PopCurve Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType switch
        {
            JsonTokenType.Number => (PopCurve)reader.GetInt32(),
            JsonTokenType.String => PopCurves.Parse(reader.GetString()),
            _ => throw new JsonException(LiarUtil.Core.Strings.CurveTypeMustStringOrNumber),
        };

    public override void Write(Utf8JsonWriter writer, PopCurve value, JsonSerializerOptions options) =>
        writer.WriteStringValue(PopCurves.Format(value));
}
