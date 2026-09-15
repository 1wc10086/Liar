namespace LiarUtil.Core.RsbPatch;

internal sealed class VcdiffAddressCache
{
    public const byte SelfMode = 0;

    public const byte HereMode = 1;

    public const byte FirstNearMode = 2;

    public const byte FirstSameMode = 6;

    public const byte LastMode = 8;

    private const int NearCacheSize = 4;

    private const int SameCacheSize = 3;

    private readonly long[] _nearCache = new long[NearCacheSize];

    private readonly long[] _sameCache = new long[SameCacheSize * 256];

    private int _nextSlot;

    public static bool WriteAddressAsVarInt(byte mode) => mode is < FirstSameMode or > LastMode;

    public byte EncodeAddress(long address, long hereAddress, out long encodedAddress)
    {
        encodedAddress = 0;
        if (address < 0 || address >= hereAddress)
        {
            return SelfMode;
        }

        var samePosition = (int)(address % (SameCacheSize * 256));
        if (_sameCache[samePosition] == address)
        {
            UpdateCache(address);
            encodedAddress = samePosition % 256;
            return (byte)(FirstSameMode + (samePosition / 256));
        }

        var bestMode = SelfMode;
        var bestEncoded = address;

        var hereEncoded = hereAddress - address;
        if (hereEncoded < bestEncoded)
        {
            bestMode = HereMode;
            bestEncoded = hereEncoded;
        }

        for (var index = 0; index < NearCacheSize; index++)
        {
            var nearEncoded = address - _nearCache[index];
            if (nearEncoded >= 0 && nearEncoded < bestEncoded)
            {
                bestMode = (byte)(FirstNearMode + index);
                bestEncoded = nearEncoded;
            }
        }

        UpdateCache(address);
        encodedAddress = bestEncoded;
        return bestMode;
    }

    private void UpdateCache(long address)
    {
        _nearCache[_nextSlot] = address;
        _nextSlot = (_nextSlot + 1) % NearCacheSize;
        _sameCache[address % (SameCacheSize * 256)] = address;
    }
}
