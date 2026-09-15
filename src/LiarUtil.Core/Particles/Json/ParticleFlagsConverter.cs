using System.Text.Json;
using System.Text.Json.Serialization;

namespace LiarUtil.Core.Particles.Json;

public sealed class ParticleFlagsConverter : JsonConverter<ParticleFlags>
{
    public override ParticleFlags Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            return (ParticleFlags)reader.GetInt32();
        }

        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException(LiarUtil.Core.Strings.ParticleMarkerMustArrayOrNumber);
        }

        var flags = ParticleFlags.None;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                return flags;
            }

            if (reader.TokenType == JsonTokenType.Number)
            {
                flags |= (ParticleFlags)reader.GetInt32();
                continue;
            }

            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException(LiarUtil.Core.Strings.ParticleMarkerItemMustStringOrNumber);
            }

            var name = reader.GetString();
            var index = Array.FindIndex(ParticleNames.Flags, item => string.Equals(item, name, StringComparison.OrdinalIgnoreCase));
            if (index < 0)
            {
                throw new JsonException(string.Format(LiarUtil.Core.Strings.UnknownParticleMarker0, name));
            }

            flags |= (ParticleFlags)(1 << index);
        }

        throw new JsonException(LiarUtil.Core.Strings.ParticleMarkerArrayIncomplete);
    }

    public override void Write(Utf8JsonWriter writer, ParticleFlags value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        var known = ParticleFlags.None;
        for (var index = 0; index < ParticleNames.Flags.Length; index++)
        {
            var flag = (ParticleFlags)(1 << index);
            known |= flag;
            if ((value & flag) == flag)
            {
                writer.WriteStringValue(ParticleNames.Flags[index]);
            }
        }

        var unknown = value & ~known;
        if (unknown != ParticleFlags.None)
        {
            writer.WriteNumberValue((int)unknown);
        }

        writer.WriteEndArray();
    }
}
