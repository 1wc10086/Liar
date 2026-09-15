namespace LiarUtil.Core.RsbPatch;

internal sealed class VcdiffRollingHash
{
    private const uint Multiplier = 257;

    private const uint Base = 1u << 23;

    private readonly uint[] _removeTable = new uint[256];

    public VcdiffRollingHash(int windowSize)
    {
        var factor = 1u;
        for (var index = 0; index < windowSize - 1; index++)
        {
            factor = (factor * Multiplier) & (Base - 1);
        }

        var byteTimes = 0u;
        for (var value = 0; value < 256; value++)
        {
            _removeTable[value] = (0u - byteTimes) & (Base - 1);
            byteTimes = (byteTimes + factor) & (Base - 1);
        }
    }

    public static uint Compute(byte[] data, int offset, int size)
    {
        var hash = ((uint)data[offset] * Multiplier) + data[offset + 1];
        for (var index = 2; index < size; index++)
        {
            hash = ((hash * Multiplier) + data[offset + index]) & (Base - 1);
        }

        return hash;
    }

    public uint Update(uint oldHash, byte firstByte, byte newByte)
    {
        var partial = (oldHash + _removeTable[firstByte]) & (Base - 1);
        return ((partial * Multiplier) + newByte) & (Base - 1);
    }
}
