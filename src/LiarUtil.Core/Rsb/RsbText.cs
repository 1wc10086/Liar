using System.Text;
using LiarUtil.Core.Core.Binary;

namespace LiarUtil.Core.Rsb;

internal static class RsbText
{
    public static string ReadFixed(BufferReader reader, int size) =>
        Encoding.UTF8.GetString(reader.ReadBytes(size)).TrimEnd('\0');

    public static void WriteFixed(BufferWriter writer, string value, int size)
    {
        var encoded = Encoding.UTF8.GetBytes(value);
        if (encoded.Length >= size)
        {
            throw new InvalidDataException($"RSB identifier exceeds {size - 1} bytes: {value}");
        }

        var buffer = new byte[size];
        encoded.CopyTo(buffer, 0);
        writer.WriteBytes(buffer);
    }

    public static string ReadFourCharacterCode(BufferReader reader)
    {
        var bytes = reader.ReadBytes(4);
        if (reader.BigEndian)
        {
            Array.Reverse(bytes);
        }

        var builder = new StringBuilder(4);
        for (var i = 3; i >= 0; i--)
        {
            if (bytes[i] != 0)
            {
                builder.Append((char)bytes[i]);
            }
        }

        return builder.ToString();
    }

    public static void WriteFourCharacterCode(BufferWriter writer, string value)
    {
        Span<byte> buffer = stackalloc byte[4];
        buffer.Clear();
        var source = value.AsSpan();
        var count = Math.Min(source.Length, 4);
        for (var i = 0; i < count; i++)
        {
            buffer[i] = (byte)source[i];
        }

        if (!writer.BigEndian)
        {
            buffer.Reverse();
        }

        writer.WriteBytes(buffer);
    }

    public static string SanitizeFileName(string identifier)
    {
        var builder = new StringBuilder(identifier.Length);
        foreach (var character in identifier)
        {
            builder.Append(character is '/' or '\\' or ':' or '*' or '?' or '"' or '<' or '>' or '|' or '\0' ? '_' : character);
        }

        var result = builder.ToString().Trim();
        return result.Length == 0 ? "unnamed" : result;
    }
}
