using System.Text.Json;
using System.Text.Json.Serialization;

namespace LiarUtil.Core.PopCap;

public sealed class ImageReferenceJsonConverter : JsonConverter<ImageReference>
{
    public override ImageReference Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        ReadValue(ref reader);

    public override void Write(Utf8JsonWriter writer, ImageReference value, JsonSerializerOptions options) =>
        WriteValue(writer, value);

    public static ImageReference ReadValue(ref Utf8JsonReader reader) => reader.TokenType switch
    {
        JsonTokenType.String => new ImageReference { Name = reader.GetString() },
        JsonTokenType.Number => new ImageReference { Id = reader.GetInt32() },
        JsonTokenType.Null => throw new JsonException(LiarUtil.Core.Strings.ImageReferenceCannotNull),
        _ => throw new JsonException(LiarUtil.Core.Strings.ImageReferenceMustStringOrNumber),
    };

    public static void WriteValue(Utf8JsonWriter writer, ImageReference? value)
    {
        if (!string.IsNullOrEmpty(value?.Name))
        {
            writer.WriteStringValue(value.Name);
            return;
        }

        if (value?.Id is { } id)
        {
            writer.WriteNumberValue(id);
            return;
        }

        writer.WriteNullValue();
    }
}
