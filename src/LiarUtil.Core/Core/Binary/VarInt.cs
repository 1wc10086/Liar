namespace LiarUtil.Core.Core.Binary;

public static class VarInt
{
    public static int Size(uint value)
    {
        var size = 1;
        while (value >= 0x80)
        {
            size++;
            value >>= 7;
        }
        return size;
    }

    public static int Size(ulong value)
    {
        var size = 1;
        while (value >= 0x80)
        {
            size++;
            value >>= 7;
        }
        return size;
    }

    public static bool TryRead(ReadOnlySpan<byte> source, out ulong value, out int consumed)
    {
        value = 0;
        consumed = 0;
        var shift = 0;
        foreach (var current in source)
        {
            consumed++;
            value |= (ulong)(current & 0x7F) << shift;
            if ((current & 0x80) == 0)
            {
                return true;
            }
            shift += 7;
            if (shift > 63)
            {
                break;
            }
        }

        value = 0;
        consumed = 0;
        return false;
    }
}
