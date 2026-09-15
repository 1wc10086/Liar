using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LiarUtil.Core.Particles.Json;

public abstract class ParticleEnumConverter<TEnum> : JsonConverter<TEnum>
    where TEnum : struct, Enum
{
    public sealed override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            return (TEnum)Enum.ToObject(typeof(TEnum), reader.GetInt32());
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var text = reader.GetString();
            if (!string.IsNullOrEmpty(text) && Enum.TryParse<TEnum>(text, ignoreCase: true, out var value))
            {
                return value;
            }

            throw new JsonException(string.Format(LiarUtil.Core.Strings.Invalid01, typeof(TEnum).Name, text));
        }

        throw new JsonException(string.Format(LiarUtil.Core.Strings.Invalid0, typeof(TEnum).Name));
    }

    public sealed override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
    {
        var name = Enum.GetName(typeof(TEnum), value);
        if (name is not null)
        {
            writer.WriteStringValue(name);
            return;
        }

        writer.WriteNumberValue(Convert.ToInt32(value, CultureInfo.InvariantCulture));
    }
}
