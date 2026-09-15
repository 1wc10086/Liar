namespace LiarUtil.Core.Sps;

internal sealed class EaLayer3Reader
{
    private readonly byte[] _data;

    public EaLayer3Reader(byte[] data, int bitOffset)
    {
        _data = data;
        Bit = bitOffset;
    }

    public int Bit { get; private set; }

    public int Read(int count)
    {
        var value = 0;
        for (var index = 0; index < count; index++)
        {
            value = (value << 1) | ((_data[Bit >> 3] >> (7 - (Bit & 7))) & 1);
            Bit++;
        }

        return value;
    }

    public void Skip(int count) => Bit += count;
}
