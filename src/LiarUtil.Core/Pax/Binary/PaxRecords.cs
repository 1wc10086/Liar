using LiarUtil.Core.Core.Binary;

namespace LiarUtil.Core.Pax.Binary;

internal static class PaxRecords
{
    public static int ReadCount(BufferReader reader, string field)
    {
        var value = reader.ReadInt32();
        return value is >= 0 and <= PaxFormat.MaxRecords
            ? value
            : throw new PaxException(string.Format(LiarUtil.Core.Strings.InvalidPAX0, field));
    }

    public static string ReadString(BufferReader reader, string field)
    {
        var length = ReadCount(reader, field + " length");
        if (length > reader.Remaining)
        {
            throw new PaxException(string.Format(LiarUtil.Core.Strings.PAX0LengthInvalid, field));
        }
        return PaxText.Decode(reader.ReadSpan(length));
    }

    public static void WriteString(BufferWriter writer, string value)
    {
        var bytes = PaxText.Encode(value);
        writer.WriteInt32(bytes.Length);
        writer.WriteBytes(bytes);
    }

    public static void WriteSizedString(BufferWriter writer, string value, int? length, string field)
    {
        var bytes = PaxText.Encode(value);
        var size = length ?? bytes.Length;
        if (size < bytes.Length)
        {
            throw new PaxException(string.Format(LiarUtil.Core.Strings.PAX0ExceedsEncodableLength, field));
        }

        writer.WriteInt32(size);
        writer.WriteBytes(bytes);
        writer.WriteZeros(size - bytes.Length);
    }

    public static int Utf8Length(string value) => PaxText.Encode(value).Length;
}
