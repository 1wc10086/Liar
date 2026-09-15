using LiarUtil.Core.Core.Binary;

namespace LiarUtil.Core.Cfu2;

internal static class Cfu2Strings
{
    public static Exception Error(string message) => new InvalidDataException(message);

    public static string Read(BufferReader reader)
    {
        var length = reader.ReadUInt32();
        if (length > (uint)reader.Remaining)
        {
            throw new InvalidDataException(string.Format(Strings.CFU2StringLengthOutOfRange0, length));
        }
        return TextCodec.Decode(reader.ReadSpan((int)length), TextFormat.Latin1);
    }

    public static void Write(BufferWriter writer, string value)
    {
        if (value.Any(static character => character > byte.MaxValue))
        {
            throw new InvalidDataException(Strings.CFU2StringContainsCharactersThatCannotEncoded);
        }
        var bytes = TextCodec.Encode(value, TextFormat.Latin1);
        writer.WriteUInt32((uint)bytes.Length);
        writer.WriteBytes(bytes);
    }
}
