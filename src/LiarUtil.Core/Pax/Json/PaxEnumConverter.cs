using System.Text.Json;
using System.Text.Json.Serialization;
using LiarUtil.Core.Pax.Models;

namespace LiarUtil.Core.Pax.Json;

public abstract class PaxEnumConverter<TEnum> : JsonConverter<TEnum>
    where TEnum : struct, Enum
{
    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var text = reader.GetString();
        if (!string.IsNullOrEmpty(text) && Enum.TryParse<TEnum>(text, ignoreCase: true, out var value))
        {
            return value;
        }
        throw new JsonException(string.Format(LiarUtil.Core.Strings.Invalid01, typeof(TEnum).Name, text));
    }

    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options) =>
        writer.WriteStringValue(Camel(Enum.GetName(value)));
    
    private static string Camel(string? name) =>
        string.IsNullOrEmpty(name) ? "" : char.ToLowerInvariant(name[0]) + name[1..];
}

public sealed class PaxLayerTypeConverter : PaxEnumConverter<PaxLayerType>;

public sealed class PaxTerminatorKindConverter : PaxEnumConverter<PaxTerminatorKind>;

public sealed class PaxLoopTypeConverter : PaxEnumConverter<PaxLoopType>;
