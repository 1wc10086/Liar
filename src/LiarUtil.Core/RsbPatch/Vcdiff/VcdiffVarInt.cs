namespace LiarUtil.Core.RsbPatch;

internal static class VcdiffVarInt
{
    public static int Size(int value)
    {
        var length = 1;
        var remaining = (uint)value;
        while ((remaining >>= 7) != 0)
        {
            length++;
        }

        return length;
    }
}
