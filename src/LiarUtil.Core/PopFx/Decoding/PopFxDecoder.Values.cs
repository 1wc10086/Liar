using LiarUtil.Core.PopFx.Binary;
using LiarUtil.Core.PopFx.Models;

namespace LiarUtil.Core.PopFx.Decoding;

internal sealed partial class PopFxDecoder
{
    private PopFxValue ReadValue(in PopFxValueRecord raw)
    {
        if (raw.Type > 8)
        {
            throw new InvalidDataException(string.Format(LiarUtil.Core.Strings.POPFXValueTypeInvalid0, raw.Type));
        }
        var type = (PopFxValueType)raw.Type;
        var value = new PopFxValue { Type = type };
        switch (type)
        {
            case PopFxValueType.Float:
                value.Number = ToSingle(raw.Word0);
                value.Padding = Padding(raw.Word1, raw.Word2, raw.Word3);
                break;
            case PopFxValueType.Int:
                value.Integer = unchecked((int)raw.Word0);
                value.Padding = Padding(raw.Word1, raw.Word2, raw.Word3);
                break;
            case PopFxValueType.Float2:
                value.Numbers = [ToSingle(raw.Word0), ToSingle(raw.Word1)];
                value.Padding = Padding(raw.Word2, raw.Word3);
                break;
            case PopFxValueType.Float3:
                value.Numbers = [ToSingle(raw.Word0), ToSingle(raw.Word1), ToSingle(raw.Word2)];
                value.Padding = Padding(raw.Word3);
                break;
            case PopFxValueType.Float4:
                value.Numbers = [ToSingle(raw.Word0), ToSingle(raw.Word1), ToSingle(raw.Word2), ToSingle(raw.Word3)];
                break;
            case PopFxValueType.Int2:
                value.Integers = [unchecked((int)raw.Word0), unchecked((int)raw.Word1)];
                value.Padding = Padding(raw.Word2, raw.Word3);
                break;
            case PopFxValueType.Int3:
                value.Integers = [unchecked((int)raw.Word0), unchecked((int)raw.Word1), unchecked((int)raw.Word2)];
                value.Padding = Padding(raw.Word3);
                break;
            case PopFxValueType.Int4:
                value.Integers = [unchecked((int)raw.Word0), unchecked((int)raw.Word1), unchecked((int)raw.Word2), unchecked((int)raw.Word3)];
                break;
            case PopFxValueType.String:
                value.Text = StringAt(raw.Word0);
                value.Padding = Padding(raw.Word1, raw.Word2, raw.Word3);
                break;
        }
        return value;
    }

    private static float ToSingle(uint value) => BitConverter.UInt32BitsToSingle(value);

    private static uint[]? Padding(params uint[] words)
    {
        foreach (var word in words)
        {
            if (word != 0)
            {
                return words;
            }
        }
        return null;
    }
}
