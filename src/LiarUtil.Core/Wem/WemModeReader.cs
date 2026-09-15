namespace LiarUtil.Core.Wem;

internal static class WemModeReader
{
    public static int Read(byte[] data, int offset, int modeBits, bool modPackets)
    {
        if (modeBits == 0)
        {
            return 0;
        }

        var value = 0;
        var bit = offset * 8 + (modPackets ? 0 : 1);
        for (var index = 0; index < modeBits; index++)
        {
            if (((data[bit >> 3] >> (bit & 7)) & 1) != 0)
            {
                value |= 1 << index;
            }

            bit++;
        }

        return value;
    }
}
