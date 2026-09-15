using System.Text;
using LiarUtil.Core.Core.Binary;

namespace LiarUtil.Core.FontWidgetDat.Binary;

internal static class FontWidgetDatStrings
{
    public static string Read(BufferReader reader, string label)
    {
        var length = reader.ReadUInt16();
        var bytes = reader.ReadSpan(length);
        foreach (var value in bytes)
        {
            if (value < FontWidgetDatFormat.MinAscii || value > FontWidgetDatFormat.MaxAscii)
            {
                throw new FontWidgetDatException(string.Format(Strings.Illegal0String, label));
            }
        }
        return System.Text.Encoding.ASCII.GetString(bytes);
    }

    public static void Write(BufferWriter writer, string value, string name)
    {
        if (value.Length > FontWidgetDatFormat.MaxStringLength)
        {
            throw new FontWidgetDatException(string.Format(Strings.N0MustASCIIStringNoLongerThan65535, name));
        }

        writer.WriteUInt16((ushort)value.Length);
        foreach (var character in value)
        {
            if (character is < (char)FontWidgetDatFormat.MinAscii or > (char)FontWidgetDatFormat.MaxAscii)
            {
                throw new FontWidgetDatException(string.Format(Strings.N0MustPrintableASCIIText, name));
            }
            writer.WriteUInt8((byte)character);
        }
    }

    public static string? Character(ushort codePoint) =>
        codePoint is >= 0xD800 and <= 0xDFFF ? null : ((char)codePoint).ToString();
}
