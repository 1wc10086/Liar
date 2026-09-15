using System.Text.Json;
using System.Text.Json.Serialization;
using LiarUtil.Core.Dz.Models;

namespace LiarUtil.Core.Dz.Json;

public sealed class DzCompressionMethodJsonConverter : JsonConverter<DzCompressionMethod>
{
    private static readonly (DzCompressionMethod Value, string Name)[] Flags =
    [
        (DzCompressionMethod.CombineBuffer, "CombineBuffer"),
        (DzCompressionMethod.Dz, "Dz"),
        (DzCompressionMethod.Zlib, "Zlib"),
        (DzCompressionMethod.Bzip2, "Bzip2"),
        (DzCompressionMethod.Mp3, "Mp3"),
        (DzCompressionMethod.Jpeg, "Jpeg"),
        (DzCompressionMethod.Zero, "Zero"),
        (DzCompressionMethod.Store, "Store"),
        (DzCompressionMethod.Lzma, "Lzma"),
        (DzCompressionMethod.RandomAccess, "RandomAccess"),
    ];

    private static readonly Dictionary<string, DzCompressionMethod> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["none"] = DzCompressionMethod.None,
        ["combuf"] = DzCompressionMethod.CombineBuffer,
        ["combinebuffer"] = DzCompressionMethod.CombineBuffer,
        ["dz"] = DzCompressionMethod.Dz,
        ["dzip"] = DzCompressionMethod.Dz,
        ["zlib"] = DzCompressionMethod.Zlib,
        ["gzip"] = DzCompressionMethod.Zlib,
        ["deflate"] = DzCompressionMethod.Zlib,
        ["bzip"] = DzCompressionMethod.Bzip2,
        ["bzip2"] = DzCompressionMethod.Bzip2,
        ["mp3"] = DzCompressionMethod.Mp3,
        ["jpeg"] = DzCompressionMethod.Jpeg,
        ["jpg"] = DzCompressionMethod.Jpeg,
        ["zero"] = DzCompressionMethod.Zero,
        ["zerodout"] = DzCompressionMethod.Zero,
        ["store"] = DzCompressionMethod.Store,
        ["stored"] = DzCompressionMethod.Store,
        ["copy"] = DzCompressionMethod.Store,
        ["copycoded"] = DzCompressionMethod.Store,
        ["raw"] = DzCompressionMethod.Store,
        ["lzma"] = DzCompressionMethod.Lzma,
        ["randomaccess"] = DzCompressionMethod.RandomAccess,
    };

    public override DzCompressionMethod Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType switch
        {
            JsonTokenType.Number => (DzCompressionMethod)reader.GetUInt16(),
            JsonTokenType.String => Parse(reader.GetString()),
            _ => throw new JsonException($"Invalid Dz compression method token: {reader.TokenType}"),
        };

    public override void Write(Utf8JsonWriter writer, DzCompressionMethod value, JsonSerializerOptions options)
    {
        if (value is DzCompressionMethod.None)
        {
            writer.WriteStringValue("Store");
            return;
        }

        var remaining = value;
        var names = new List<string>();
        foreach (var (flag, name) in Flags)
        {
            if ((remaining & flag) != flag)
            {
                continue;
            }

            names.Add(name);
            remaining &= ~flag;
        }

        if (remaining != DzCompressionMethod.None)
        {
            writer.WriteNumberValue((ushort)value);
            return;
        }

        writer.WriteStringValue(string.Join('|', names));
    }

    public static DzCompressionMethod Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return DzCompressionMethod.Store;
        }

        var value = DzCompressionMethod.None;
        foreach (var token in text.Split(['|', ',', '+', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var normalized = token.Replace("_", "").Replace("-", "");
            if (Aliases.TryGetValue(normalized, out var alias))
            {
                value |= alias;
                continue;
            }

            if (Enum.TryParse<DzCompressionMethod>(normalized, true, out var parsed))
            {
                value |= parsed;
                continue;
            }

            if (ushort.TryParse(normalized, out var numeric))
            {
                value |= (DzCompressionMethod)numeric;
                continue;
            }

            throw new JsonException($"Invalid Dz compression method: {text}");
        }

        return value;
    }
}
