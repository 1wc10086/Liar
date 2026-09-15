using System.Text.Json;
using System.Text.Json.Serialization;
using LiarUtil.Core.PopCap;

namespace LiarUtil.Core.Reanim.Json;

public sealed class ReanimTransformJsonConverter : JsonConverter<ReanimTransform>
{
    public override ReanimTransform Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException(LiarUtil.Core.Strings.ReanimTransformMustObject);
        }

        var transform = new ReanimTransform();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return transform;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException(LiarUtil.Core.Strings.ReanimTransformAttributeInvalid);
            }

            var name = reader.GetString();
            reader.Read();
            switch (name)
            {
                case "Position":
                    ReadPair(ref reader, "X", "Y",
                        (x, y) =>
                        {
                            transform.X = x;
                            transform.Y = y;
                        });
                    break;
                case "Skew":
                    ReadPair(ref reader, "X", "Y",
                        (x, y) =>
                        {
                            transform.SkewX = x;
                            transform.SkewY = y;
                        });
                    break;
                case "Scale":
                    ReadPair(ref reader, "X", "Y",
                        (x, y) =>
                        {
                            transform.ScaleX = x;
                            transform.ScaleY = y;
                        });
                    break;
                case "Alpha":
                    transform.Alpha = ReadNullableFloat(ref reader);
                    break;
                case "Frame":
                    transform.Frame = ReadNullableFloat(ref reader);
                    break;
                case "Image":
                case "i":
                    transform.Image = ReadImage(ref reader);
                    break;
                case "Image2":
                case "i2":
                    transform.Image2 = ReadImage(ref reader);
                    break;
                case "Resource":
                case "resource":
                    transform.ImageResource = ReadNullableString(ref reader);
                    break;
                case "Resource2":
                case "resource2":
                    transform.Image2Resource = ReadNullableString(ref reader);
                    break;
                case "Font":
                case "font":
                    transform.Font = ReadNullableString(ref reader);
                    break;
                case "Text":
                case "text":
                    transform.Text = ReadNullableString(ref reader);
                    break;
                case "X":
                    transform.X = ReadNullableFloat(ref reader);
                    break;
                case "Y":
                    transform.Y = ReadNullableFloat(ref reader);
                    break;
                case "SkewX":
                    transform.SkewX = ReadNullableFloat(ref reader);
                    break;
                case "SkewY":
                    transform.SkewY = ReadNullableFloat(ref reader);
                    break;
                case "ScaleX":
                    transform.ScaleX = ReadNullableFloat(ref reader);
                    break;
                case "ScaleY":
                    transform.ScaleY = ReadNullableFloat(ref reader);
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        throw new JsonException(LiarUtil.Core.Strings.ReanimTransformIncomplete);
    }

    public override void Write(Utf8JsonWriter writer, ReanimTransform value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        WritePair(writer, "Position", value.X, value.Y);
        WritePair(writer, "Skew", value.SkewX, value.SkewY);
        WritePair(writer, "Scale", value.ScaleX, value.ScaleY);
        WriteNumber(writer, "Alpha", value.Alpha);
        WriteNumber(writer, "Frame", value.Frame);
        WriteImage(writer, "Image", value.Image);
        WriteString(writer, "Resource", value.ImageResource);
        WriteImage(writer, "Image2", value.Image2);
        WriteString(writer, "Resource2", value.Image2Resource);
        WriteString(writer, "Font", value.Font);
        WriteString(writer, "Text", value.Text);
        writer.WriteEndObject();
    }

    private static void WritePair(Utf8JsonWriter writer, string name, float? x, float? y)
    {
        if (x is null && y is null)
        {
            return;
        }
        writer.WriteStartObject(name);
        WriteNumber(writer, "X", x);
        WriteNumber(writer, "Y", y);
        writer.WriteEndObject();
    }

    private static void WriteNumber(Utf8JsonWriter writer, string name, float? value)
    {
        if (value is { } number)
        {
            writer.WriteNumber(name, number);
        }
    }

    private static void WriteString(Utf8JsonWriter writer, string name, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            writer.WriteString(name, value);
        }
    }

    private static void WriteImage(Utf8JsonWriter writer, string name, ImageReference? image)
    {
        if (image is null)
        {
            return;
        }
        writer.WritePropertyName(name);
        ImageReferenceJsonConverter.WriteValue(writer, image);
    }

    private static void ReadPair(ref Utf8JsonReader reader, string firstName, string secondName, Action<float?, float?> assign)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return;
        }
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException(LiarUtil.Core.Strings.ReanimTransformComponentMustObject);
        }
        float? first = null;
        float? second = null;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                assign(first, second);
                return;
            }
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException(LiarUtil.Core.Strings.ReanimTransformComponentAttributeInvalid);
            }
            var property = reader.GetString();
            reader.Read();
            if (string.Equals(property, firstName, StringComparison.OrdinalIgnoreCase))
            {
                first = ReadNullableFloat(ref reader);
            }
            else if (string.Equals(property, secondName, StringComparison.OrdinalIgnoreCase))
            {
                second = ReadNullableFloat(ref reader);
            }
            else
            {
                reader.Skip();
            }
        }
        throw new JsonException(LiarUtil.Core.Strings.ReanimTransformComponentIncomplete);
    }

    private static float? ReadNullableFloat(ref Utf8JsonReader reader) => reader.TokenType switch
    {
        JsonTokenType.Number => reader.GetSingle(),
        JsonTokenType.String when float.TryParse(reader.GetString(), System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var value) => value,
        _ => null,
    };

    private static string? ReadNullableString(ref Utf8JsonReader reader) =>
        reader.TokenType == JsonTokenType.String ? reader.GetString() : null;

    private static ImageReference? ReadImage(ref Utf8JsonReader reader) =>
        reader.TokenType == JsonTokenType.Null ? null : ImageReferenceJsonConverter.ReadValue(ref reader);
}
