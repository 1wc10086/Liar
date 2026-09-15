using System.Text.Json;
using System.Text.Json.Serialization;
using LiarUtil.Core.PopCap;

namespace LiarUtil.Core.Trail.Json;

public sealed class TrailTrackNodeConverter : JsonConverter<TrailTrackNode>
{
    public override TrailTrackNode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException(LiarUtil.Core.Strings.TrailNodeMustObject);
        }

        var node = new TrailTrackNode();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return node;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException(LiarUtil.Core.Strings.TrailNodeAttributeInvalid);
            }

            var name = reader.GetString();
            reader.Read();
            switch (name)
            {
                case "Time":
                    node.Time = reader.GetSingle();
                    break;
                case "Value":
                    node.Low = reader.GetSingle();
                    node.High = node.Low;
                    break;
                case "Low":
                    node.Low = reader.GetSingle();
                    break;
                case "High":
                    node.High = reader.GetSingle();
                    break;
                case "Distribution":
                    node.Distribution = ReadCurve(ref reader);
                    break;
                case "Curve":
                    node.Curve = ReadCurve(ref reader);
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        throw new JsonException(LiarUtil.Core.Strings.TrailNodeIncomplete);
    }

    public override void Write(Utf8JsonWriter writer, TrailTrackNode value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("Time", value.Time);
        if (value.Low == value.High)
        {
            writer.WriteNumber("Value", value.Low);
        }
        else
        {
            writer.WriteNumber("Low", value.Low);
            writer.WriteNumber("High", value.High);
        }

        if (value.Distribution != PopCurve.Linear)
        {
            writer.WritePropertyName("Distribution");
            writer.WriteStringValue(PopCurves.Format(value.Distribution));
        }

        if (value.Curve != PopCurve.Linear)
        {
            writer.WritePropertyName("Curve");
            writer.WriteStringValue(PopCurves.Format(value.Curve));
        }

        writer.WriteEndObject();
    }

    private static PopCurve ReadCurve(ref Utf8JsonReader reader) => reader.TokenType switch
    {
        JsonTokenType.Number => (PopCurve)reader.GetInt32(),
        JsonTokenType.String => PopCurves.Parse(reader.GetString()),
        _ => throw new JsonException(LiarUtil.Core.Strings.CurveTypeMustStringOrNumber),
    };
}
