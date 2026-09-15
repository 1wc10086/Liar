using System.Text;

namespace LiarUtil.Core.Core.Binary;

public enum TextFormat
{
    Utf8 = 0,
    Latin1 = 1,
    Ascii = 2,
    Utf16 = 3,
}

public static class TextCodec
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    extension(TextFormat format)
    {
        public System.Text.Encoding Encoding => format switch
        {
            TextFormat.Latin1 => System.Text.Encoding.Latin1,
            TextFormat.Ascii => System.Text.Encoding.ASCII,
            TextFormat.Utf16 => System.Text.Encoding.Unicode,
            _ => System.Text.Encoding.UTF8,
        };

        public bool IsSingleByte => format is not TextFormat.Utf16;

        public string DisplayName => format switch
        {
            TextFormat.Latin1 => "Latin1",
            TextFormat.Ascii => "ASCII",
            TextFormat.Utf16 => "UTF-16",
            _ => "UTF-8",
        };
    }

    public static string Decode(ReadOnlySpan<byte> bytes, TextFormat format = TextFormat.Utf8) =>
        bytes.IsEmpty ? "" : format.Encoding.GetString(bytes);

    public static byte[] Encode(string value, TextFormat format = TextFormat.Utf8) =>
        value.Length == 0 ? [] : format.Encoding.GetBytes(value);

    public static string DecodeLenient(ReadOnlySpan<byte> bytes, TextFormat fallback = TextFormat.Latin1)
    {
        if (bytes.IsEmpty)
        {
            return "";
        }

        try
        {
            return StrictUtf8.GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            return fallback.Encoding.GetString(bytes);
        }
    }

    public static byte[] EncodeMinimal(string value, TextFormat wide = TextFormat.Utf8)
    {
        if (value.Length == 0)
        {
            return [];
        }

        return value.Any(static character => character > 0xFF)
            ? wide.Encoding.GetBytes(value)
            : System.Text.Encoding.Latin1.GetBytes(value);
    }

    public static byte[] EncodeAsciiOrUtf8(string value)
    {
        if (value.Length == 0)
        {
            return [];
        }

        return value.Any(static character => character > 0x7F)
            ? System.Text.Encoding.UTF8.GetBytes(value)
            : System.Text.Encoding.ASCII.GetBytes(value);
    }

    public static byte[] Latin1ToUtf8(ReadOnlySpan<byte> input)
    {
        if (input.IsEmpty)
        {
            return [];
        }

        var result = new byte[input.Length * 2];
        var length = 0;
        foreach (var value in input)
        {
            if (value < 0x80)
            {
                result[length++] = value;
                continue;
            }

            result[length++] = (byte)(0xC0 | (value >> 6));
            result[length++] = (byte)(0x80 | (value & 0x3F));
        }

        return result[..length];
    }

    public static byte[] Utf8ToLatin1(ReadOnlySpan<byte> input)
    {
        if (input.IsEmpty)
        {
            return [];
        }

        var result = new byte[input.Length];
        var length = 0;
        var index = 0;
        while (index < input.Length)
        {
            var value = input[index];
            if (value < 0x80)
            {
                result[length++] = value;
                index++;
            }
            else if ((value & 0xE0) == 0xC0 && index + 1 < input.Length && (input[index + 1] & 0xC0) == 0x80)
            {
                var codePoint = (uint)((value & 0x1F) << 6) | (uint)(input[index + 1] & 0x3F);
                result[length++] = codePoint <= 0xFF ? (byte)codePoint : (byte)'?';
                index += 2;
            }
            else if ((value & 0xF0) == 0xE0 && index + 2 < input.Length)
            {
                result[length++] = (byte)'?';
                index += 3;
            }
            else if ((value & 0xF8) == 0xF0 && index + 3 < input.Length)
            {
                result[length++] = (byte)'?';
                index += 4;
            }
            else
            {
                index++;
            }
        }

        return result[..length];
    }

    public static bool IsAscii(ReadOnlySpan<char> value)
    {
        foreach (var character in value)
        {
            if (character > 0x7F)
            {
                return false;
            }
        }

        return true;
    }

    public static bool IsLatin1(ReadOnlySpan<char> value)
    {
        foreach (var character in value)
        {
            if (character > 0xFF)
            {
                return false;
            }
        }

        return true;
    }

    public static bool IsValidUtf8(ReadOnlySpan<byte> bytes)
    {
        try
        {
            StrictUtf8.GetString(bytes);
            return true;
        }
        catch (DecoderFallbackException)
        {
            return false;
        }
    }

    public static int Utf16Length(string value)
    {
        var count = 0;
        foreach (var character in value)
        {
            count += char.IsHighSurrogate(character) ? 2 : 1;
        }

        return count;
    }

    public static string RequireSingleByte(string value, string context)
    {
        if (!IsLatin1(value))
        {
            throw new BinaryException(string.Format(LiarUtil.Core.Strings.N0StringContainsCharactersThatCannotEncoded, context));
        }

        return value;
    }
}
