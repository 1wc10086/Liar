using LiarUtil.Core.Core.Binary;

namespace LiarUtil.Core.Ppf.Encoding;

internal static class PpfStrings
{
    public static void Write(BufferWriter writer, string value)
    {
        var bytes = TextCodec.Encode(value);
        if (bytes.Length > byte.MaxValue)
        {
            throw new InvalidDataException(string.Format(Strings.PPFStringTooLong0Bytes, bytes.Length));
        }
        writer.WriteUInt8((byte)bytes.Length);
        writer.WriteBytes(bytes);
    }
}
