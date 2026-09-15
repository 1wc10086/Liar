using System.Text.Json;
using System.Text.Json.Serialization;
using LiarUtil.Core.Pak.Models;

namespace LiarUtil.Core.Pak.Json;

public sealed class PakCompressionJsonConverter : JsonConverter<PakCompression>
{
    public override PakCompression Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType switch
        {
            JsonTokenType.String => Parse(reader.GetString()),
            JsonTokenType.Number => ParseNumber(reader.GetUInt16()),
            JsonTokenType.True => PakCompression.Zlib,
            JsonTokenType.False => PakCompression.Store,
            _ => throw new JsonException(string.Format(LiarUtil.Core.Strings.InvalidPakCompressionMethod0, reader.TokenType)),
        };

    public override void Write(Utf8JsonWriter writer, PakCompression value, JsonSerializerOptions options) =>
        writer.WriteStringValue(Name(value));

    public static string Name(PakCompression value) => value == PakCompression.Zlib ? "Zlib" : "Store";

    public static PakCompression Parse(string? text) => text?.Trim().ToLowerInvariant() switch
    {
        null or "" or "store" or "stored" or "copy" or "none" or "raw" or "false" => PakCompression.Store,
        "zlib" or "zip" or "deflate" or "compress" or "true" => PakCompression.Zlib,
        _ => throw new JsonException(string.Format(LiarUtil.Core.Strings.InvalidPakCompressionMethod0, text)),
    };

    private static PakCompression ParseNumber(ushort value) => value switch
    {
        0 => PakCompression.Store,
        1 => PakCompression.Zlib,
        _ => throw new JsonException(string.Format(LiarUtil.Core.Strings.InvalidPakCompressionMethod0, value)),
    };
}
