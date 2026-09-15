namespace LiarUtil.Core.Core.Binary;

public static class BitFields
{
    public static uint Extract(ulong value, int offset, int count)
    {
        if (count <= 0 || offset < 0 || offset + count > 64)
        {
            throw new ArgumentOutOfRangeException(nameof(count), string.Format(LiarUtil.Core.Strings.BitFieldOutOfRangeOffset0Count, offset, count));
        }

        var mask = count >= 64 ? ulong.MaxValue : (1UL << count) - 1;
        return (uint)((value >> offset) & mask);
    }

    public static ulong Insert(ulong value, int offset, int count, ulong field)
    {
        if (count <= 0 || offset < 0 || offset + count > 64)
        {
            throw new ArgumentOutOfRangeException(nameof(count), string.Format(LiarUtil.Core.Strings.BitFieldOutOfRangeOffset0Count, offset, count));
        }

        var mask = count >= 64 ? ulong.MaxValue : (1UL << count) - 1;
        return (value & ~(mask << offset)) | ((field & mask) << offset);
    }

    public static ulong Pack(params ReadOnlySpan<ulong> fieldsAndWidths)
    {
        if (fieldsAndWidths.Length % 2 != 0)
        {
            throw new ArgumentException(LiarUtil.Core.Strings.BitFieldParametersMustAppearInPairs, nameof(fieldsAndWidths));
        }

        var result = 0ul;
        var offset = 0;
        for (var index = 0; index < fieldsAndWidths.Length; index += 2)
        {
            var count = (int)fieldsAndWidths[index + 1];
            result = Insert(result, offset, count, fieldsAndWidths[index]);
            offset += count;
        }

        return result;
    }

    public static int BitsRequired(ulong value)
    {
        var bits = 0;
        while (value > 0)
        {
            bits++;
            value >>= 1;
        }

        return bits;
    }
}
