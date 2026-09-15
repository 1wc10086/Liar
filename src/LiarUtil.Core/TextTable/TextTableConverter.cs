using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace LiarUtil.Core.TextTable;

public static class TextTableConverter
{
    private static readonly JsonWriterOptions WriterOptions = new()
    {
        Indented = true,
    };

    public static string Convert(ReadOnlySpan<byte> source, TextTableVersion destinationVersion) =>
        Convert(source, DetectVersion(source), destinationVersion);

    public static string Convert(ReadOnlySpan<byte> source, TextTableVersion sourceVersion, TextTableVersion destinationVersion)
    {
        var text = DecodeSource(source);
        var values = sourceVersion switch
        {
            TextTableVersion.Text => ParseText(text),
            TextTableVersion.JsonMap => ParseJson(text, list: false),
            TextTableVersion.JsonList => ParseJson(text, list: true),
            _ => throw new TextTableException(LiarUtil.Core.Strings.SourceVersionInvalid),
        };
        return destinationVersion switch
        {
            TextTableVersion.Text => EncodeText(values),
            TextTableVersion.JsonMap => EncodeJson(values, list: false),
            TextTableVersion.JsonList => EncodeJson(values, list: true),
            _ => throw new TextTableException(LiarUtil.Core.Strings.TargetVersionInvalid),
        };
    }

    public static TextTableVersion DetectVersion(ReadOnlySpan<byte> source)
    {
        try
        {
            var node = ParseNode(DecodeSource(source));
            return FindValues(node) switch
            {
                JsonArray => TextTableVersion.JsonList,
                JsonObject => TextTableVersion.JsonMap,
                _ => throw new TextTableException(LiarUtil.Core.Strings.SourceFileInvalid),
            };
        }
        catch (Exception exception) when (exception is TextTableException or JsonException)
        {
            return TextTableVersion.Text;
        }
    }

    private static string DecodeSource(ReadOnlySpan<byte> source)
    {
        if (source.Length >= 2
            && ((source[0] == 255 && source[1] == 254) || (source[0] == 254 && source[1] == 255)))
        {
            throw new TextTableException(LiarUtil.Core.Strings.UnsupportedCharacterSetUTF16);
        }

        return new UTF8Encoding(false).GetString(source);
    }

    private static Dictionary<string, string> ParseText(string text) => TextTableTextParser.Parse(text);

    private static Dictionary<string, string> ParseJson(string text, bool list)
    {
        var node = ParseNode(text);
        var values = FindValues(node) ?? throw new TextTableException(LiarUtil.Core.Strings.SourceFileInvalid);
        var result = new Dictionary<string, string>();
        if (list)
        {
            var array = values as JsonArray ?? throw new TextTableException(LiarUtil.Core.Strings.SourceFileInvalid);
            if (array.Count % 2 != 0)
            {
                throw new TextTableException(LiarUtil.Core.Strings.ListLengthInvalid);
            }

            for (var index = 0; index < array.Count; index += 2)
            {
                var key = ReadString(array[index]);
                var value = ReadString(array[index + 1]);
                result[key] = value;
            }

            return result;
        }

        var map = values as JsonObject ?? throw new TextTableException(LiarUtil.Core.Strings.SourceFileInvalid);
        foreach (var item in map)
        {
            result[item.Key] = ReadString(item.Value);
        }

        return result;
    }

    private static string ReadString(JsonNode? node) =>
        node is JsonValue value && value.TryGetValue<string>(out var text)
            ? text
            : throw new TextTableException(LiarUtil.Core.Strings.MappedMemberInvalid);

    private static string EncodeText(Dictionary<string, string> values)
    {
        var builder = new StringBuilder();
        var first = true;
        foreach (var item in values)
        {
            if (!first)
            {
                builder.Append('\n');
            }

            first = false;
            builder.Append('[').Append(item.Key).Append("]\n").Append(item.Value).Append('\n');
        }

        return builder.ToString();
    }

    private static string EncodeJson(Dictionary<string, string> values, bool list)
    {
        JsonNode payload;
        if (list)
        {
            var array = new JsonArray();
            foreach (var item in values)
            {
                array.Add(item.Key);
                array.Add(item.Value);
            }

            payload = array;
        }
        else
        {
            var map = new JsonObject();
            foreach (var item in values)
            {
                map[item.Key] = item.Value;
            }

            payload = map;
        }

        var document = new JsonObject
        {
            ["version"] = 1,
            ["objects"] = new JsonArray
            {
                new JsonObject
                {
                    ["aliases"] = new JsonArray { "LawnStringsData" },
                    ["objclass"] = "LawnStringsData",
                    ["objdata"] = new JsonObject { ["LocStringValues"] = payload },
                },
            },
        };
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, WriterOptions))
        {
            document.WriteTo(writer);
        }

        return new UTF8Encoding(false).GetString(stream.ToArray());
    }

    private static JsonNode? FindValues(JsonNode? root) =>
        root?["objects"] is JsonArray objects && objects.Count > 0
            ? objects[0]?["objdata"]?["LocStringValues"]
            : null;

    private static JsonNode ParseNode(string text)
    {
        var stripped = StripComments(text);
        try
        {
            return JsonNode.Parse(stripped, nodeOptions: null, documentOptions: new JsonDocumentOptions { AllowTrailingCommas = true })
                ?? throw new TextTableException(LiarUtil.Core.Strings.JSONRootNodeInvalid);
        }
        catch (JsonException exception)
        {
            throw new TextTableException(string.Format(LiarUtil.Core.Strings.JSONContentInvalid0, exception.Message));
        }
    }

    private static string StripComments(string text)
    {
        var builder = new StringBuilder(text.Length);
        var quoted = false;
        var escaped = false;
        for (var index = 0; index < text.Length; index++)
        {
            var character = text[index];
            if (quoted)
            {
                builder.Append(character);
                if (escaped)
                {
                    escaped = false;
                }
                else if (character == '\\')
                {
                    escaped = true;
                }
                else if (character == '"')
                {
                    quoted = false;
                }

                continue;
            }

            if (character == '"')
            {
                quoted = true;
                builder.Append(character);
            }
            else if (character == '/' && index + 1 < text.Length && text[index + 1] == '/')
            {
                while (++index < text.Length && text[index] != '\n' && text[index] != '\r')
                {
                }

                if (index < text.Length)
                {
                    builder.Append(text[index]);
                }
            }
            else if (character == '/' && index + 1 < text.Length && text[index + 1] == '*')
            {
                index += 2;
                while (index < text.Length && (text[index] != '*' || index + 1 >= text.Length || text[index + 1] != '/'))
                {
                    index++;
                }

                if (index >= text.Length)
                {
                    throw new TextTableException(LiarUtil.Core.Strings.SourceFileInvalid);
                }

                index++;
            }
            else
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }
}
