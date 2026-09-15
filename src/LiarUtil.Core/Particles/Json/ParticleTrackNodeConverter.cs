using System.Text.Json;
using System.Text.Json.Serialization;
using LiarUtil.Core.PopCap;

namespace LiarUtil.Core.Particles.Json;

public sealed class ParticleTrackNodeConverter : JsonConverter<ParticleTrackNode>
{
    public override ParticleTrackNode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException(LiarUtil.Core.Strings.ParticleTrackNodeMustObject);
        }

        var node = new ParticleTrackNode();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return node;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException(LiarUtil.Core.Strings.ParticleTrackNodeAttributeInvalid);
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

        throw new JsonException(LiarUtil.Core.Strings.ParticleTrackNodeIncomplete);
    }

    public override void Write(Utf8JsonWriter writer, ParticleTrackNode value, JsonSerializerOptions options)
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
            WriteCurve(writer, "Distribution", value.Distribution);
        }

        if (value.Curve != PopCurve.Linear)
        {
            WriteCurve(writer, "Curve", value.Curve);
        }

        writer.WriteEndObject();
    }

    private static PopCurve ReadCurve(ref Utf8JsonReader reader)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            return (PopCurve)reader.GetInt32();
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var text = reader.GetString();
            if (!string.IsNullOrEmpty(text) && Enum.TryParse<PopCurve>(text, ignoreCase: true, out var curve))
            {
                return curve;
            }

            throw new JsonException(string.Format(LiarUtil.Core.Strings.InvalidParticleCurve0, text));
        }

        throw new JsonException(LiarUtil.Core.Strings.ParticleCurveMustStringOrNumber);
    }

    private static void WriteCurve(Utf8JsonWriter writer, string name, PopCurve curve)
    {
        writer.WritePropertyName(name);
        var text = Enum.GetName(typeof(PopCurve), curve);
        if (text is not null)
        {
            writer.WriteStringValue(text);
            return;
        }

        writer.WriteNumberValue((int)curve);
    }
}
