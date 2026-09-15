using System.Globalization;
using System.Text;

namespace LiarUtil.Core.Rton.Json;

internal static class JsonWriter
{
    public static byte[] WritePretty(JsonValue root)
    {
        var buffer = new List<byte>(4096);
        WriteValue(buffer, root, 0);
        return [.. buffer];
    }

    private static void WriteValue(List<byte> buffer, JsonValue value, int depth)
    {
        switch (value)
        {
            case JsonObject obj:
                WriteObject(buffer, obj, depth);
                break;
            case JsonArray array:
                WriteArray(buffer, array, depth);
                break;
            case JsonString str:
                WriteString(buffer, str.Bytes);
                break;
            case JsonNumber number:
                var text = number.Kind switch
                {
                    NumberKind.Signed => number.Signed.ToString(CultureInfo.InvariantCulture),
                    NumberKind.Unsigned => number.Unsigned.ToString(CultureInfo.InvariantCulture),
                    _ => YyjsonNumberFormatter.FormatReal(number.Real),
                };
                WriteAscii(buffer, text);
                break;
            case JsonBool boolean:
                WriteAscii(buffer, boolean.Value ? "true" : "false");
                break;
            case JsonNull:
                WriteAscii(buffer, "null");
                break;
        }
    }

    private static void WriteObject(List<byte> buffer, JsonObject obj, int depth)
    {
        if (obj.Entries.Count == 0)
        {
            WriteAscii(buffer, "{}");
            return;
        }

        buffer.Add((byte)'{');
        for (var i = 0; i < obj.Entries.Count; i++)
        {
            var (key, value) = obj.Entries[i];
            buffer.Add((byte)'\n');
            WriteIndent(buffer, depth + 1);
            WriteString(buffer, key);
            buffer.Add((byte)':');
            buffer.Add((byte)' ');
            WriteValue(buffer, value, depth + 1);
            if (i + 1 < obj.Entries.Count)
            {
                buffer.Add((byte)',');
            }
        }
        buffer.Add((byte)'\n');
        WriteIndent(buffer, depth);
        buffer.Add((byte)'}');
    }

    private static void WriteArray(List<byte> buffer, JsonArray array, int depth)
    {
        if (array.Items.Count == 0)
        {
            WriteAscii(buffer, "[]");
            return;
        }

        buffer.Add((byte)'[');
        for (var i = 0; i < array.Items.Count; i++)
        {
            buffer.Add((byte)'\n');
            WriteIndent(buffer, depth + 1);
            WriteValue(buffer, array.Items[i], depth + 1);
            if (i + 1 < array.Items.Count)
            {
                buffer.Add((byte)',');
            }
        }
        buffer.Add((byte)'\n');
        WriteIndent(buffer, depth);
        buffer.Add((byte)']');
    }

    private static void WriteString(List<byte> buffer, ReadOnlySpan<byte> bytes)
    {
        buffer.Add((byte)'"');
        foreach (var b in bytes)
        {
            switch (b)
            {
                case 0x22:
                    WriteAscii(buffer, "\\\"");
                    break;
                case 0x5C:
                    WriteAscii(buffer, "\\\\");
                    break;
                case 0x08:
                    WriteAscii(buffer, "\\b");
                    break;
                case 0x09:
                    WriteAscii(buffer, "\\t");
                    break;
                case 0x0A:
                    WriteAscii(buffer, "\\n");
                    break;
                case 0x0C:
                    WriteAscii(buffer, "\\f");
                    break;
                case 0x0D:
                    WriteAscii(buffer, "\\r");
                    break;
                default:
                    if (b < 0x20)
                    {
                        WriteAscii(buffer, "\\u");
                        buffer.AddRange(Encoding.ASCII.GetBytes(b.ToString("x4", CultureInfo.InvariantCulture)));
                    }
                    else
                    {
                        buffer.Add(b);
                    }
                    break;
            }
        }
        buffer.Add((byte)'"');
    }

    private static void WriteIndent(List<byte> buffer, int depth)
    {
        for (var i = 0; i < depth * 2; i++)
        {
            buffer.Add((byte)' ');
        }
    }

    private static void WriteAscii(List<byte> buffer, string text) => buffer.AddRange(Encoding.ASCII.GetBytes(text));
}
