namespace LiarUtil.Core.Wma;

internal ref struct BitReader
{
    private readonly ReadOnlySpan<byte> _data;
    private int _index;

    internal BitReader(ReadOnlySpan<byte> data)
    {
        _data = data;
        _index = 0;
    }

    internal readonly int Position => _index;

    private readonly uint Cache()
    {
        var offset = _index >> 3;
        var value = 0u;
        for (var i = 0; i < 4; i++)
        {
            var at = offset + i;
            value = (value << 8) | (at < _data.Length ? _data[at] : 0u);
        }

        return value << (_index & 7);
    }

    internal readonly int Peek(int count) => count <= 0 ? 0 : (int)(Cache() >> (32 - count));

    internal int Read(int count)
    {
        if (count <= 0)
        {
            return 0;
        }

        var value = (int)(Cache() >> (32 - count));
        _index += count;
        return value;
    }

    internal void Skip(int count) => _index += count;

    internal void Align()
    {
        var count = -_index & 7;
        if (count != 0)
        {
            _index += count;
        }
    }
}
