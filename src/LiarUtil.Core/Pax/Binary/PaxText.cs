using System.Text;

namespace LiarUtil.Core.Pax.Binary;

internal static class PaxText
{
    public static string Decode(ReadOnlySpan<byte> bytes)
    {
        var builder = new StringBuilder(bytes.Length);
        var index = 0;
        while (index < bytes.Length)
        {
            var first = bytes[index++];
            if (first < 0x80)
            {
                builder.Append((char)first);
                continue;
            }

            var count = first < 0xE0 ? 2 : first < 0xF0 ? 3 : 4;
            if (index + count - 1 > bytes.Length)
            {
                throw new PaxException(LiarUtil.Core.Strings.PAXStringNotValidUTF8Sequence);
            }

            var codePoint = first & (count == 2 ? 0x1F : count == 3 ? 0x0F : 0x07);
            for (var offset = 1; offset < count; offset++)
            {
                var next = bytes[index++];
                if ((next & 0xC0) != 0x80)
                {
                    throw new PaxException(LiarUtil.Core.Strings.PAXStringNotValidUTF8Sequence);
                }
                codePoint = (codePoint << 6) | (next & 0x3F);
            }

            if (codePoint <= 0xFFFF)
            {
                builder.Append((char)codePoint);
                continue;
            }

            codePoint -= 0x10000;
            builder.Append((char)(0xD800 | (codePoint >> 10)));
            builder.Append((char)(0xDC00 | (codePoint & 0x3FF)));
        }

        return builder.ToString();
    }

    public static byte[] Encode(string value)
    {
        var bytes = new List<byte>(value.Length);
        for (var index = 0; index < value.Length; index++)
        {
            var codePoint = (int)value[index];
            if (codePoint is >= 0xD800 and <= 0xDBFF && index + 1 < value.Length)
            {
                var low = value[index + 1];
                if (low is >= (char)0xDC00 and <= (char)0xDFFF)
                {
                    codePoint = 0x10000 + ((codePoint - 0xD800) << 10) + low - 0xDC00;
                    index++;
                }
            }

            switch (codePoint)
            {
                case < 0x80:
                    bytes.Add((byte)codePoint);
                    break;
                case < 0x800:
                    bytes.Add((byte)(0xC0 | (codePoint >> 6)));
                    bytes.Add((byte)(0x80 | (codePoint & 0x3F)));
                    break;
                case < 0x10000:
                    bytes.Add((byte)(0xE0 | (codePoint >> 12)));
                    bytes.Add((byte)(0x80 | ((codePoint >> 6) & 0x3F)));
                    bytes.Add((byte)(0x80 | (codePoint & 0x3F)));
                    break;
                default:
                    bytes.Add((byte)(0xF0 | (codePoint >> 18)));
                    bytes.Add((byte)(0x80 | ((codePoint >> 12) & 0x3F)));
                    bytes.Add((byte)(0x80 | ((codePoint >> 6) & 0x3F)));
                    bytes.Add((byte)(0x80 | (codePoint & 0x3F)));
                    break;
            }
        }

        return [.. bytes];
    }

    public static string Display(string text)
    {
        var terminator = text.IndexOf('\0');
        return terminator < 0 ? text : text[..terminator];
    }

    public static string Encoded(string displayText, string? encodedText) =>
        encodedText is not null && Display(encodedText) == displayText ? encodedText : displayText;
}
