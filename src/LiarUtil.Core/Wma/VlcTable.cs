namespace LiarUtil.Core.Wma;

internal sealed class VlcTable
{
    private int[] _table;
    private int _count;

    private VlcTable()
    {
        _table = new int[2 * 1024];
        _count = 0;
        Bits = 9;
    }

    internal int Bits { get; }

    internal static VlcTable Build(uint[] codes, byte[] bits)
    {
        var table = new VlcTable();
        table.BuildLevel(9, codes.Length, codes, bits, 0u, 0);
        return table;
    }

    private int Alloc(int size)
    {
        var index = _count;
        _count += size;
        if (2 * _count > _table.Length)
        {
            Array.Resize(ref _table, Math.Max(2 * _count, _table.Length * 2));
        }

        return index;
    }

    private int BuildLevel(int tableBits, int codeCount, uint[] codes, byte[] bits, uint prefix, int prefixLength)
    {
        var tableSize = 1 << tableBits;
        var tableIndex = Alloc(tableSize);

        for (var i = 0; i < tableSize; i++)
        {
            _table[2 * (tableIndex + i)] = -1;
            _table[2 * (tableIndex + i) + 1] = 0;
        }

        for (var i = 0; i < codeCount; i++)
        {
            var length = (int)bits[i];
            if (length == 0)
            {
                continue;
            }

            var code = codes[i];
            length -= prefixLength;
            if (length <= 0 || (code >> length) != prefix)
            {
                continue;
            }

            if (length <= tableBits)
            {
                var slot = (int)((code << (tableBits - length)) & (uint)(tableSize - 1));
                var span = 1 << (tableBits - length);
                for (var k = 0; k < span; k++)
                {
                    _table[2 * (tableIndex + slot)] = i;
                    _table[2 * (tableIndex + slot) + 1] = length;
                    slot++;
                }
            }
            else
            {
                length -= tableBits;
                var slot = (int)((code >> length) & (uint)((1 << tableBits) - 1));
                var existing = -_table[2 * (tableIndex + slot) + 1];
                if (length > existing)
                {
                    existing = length;
                }

                _table[2 * (tableIndex + slot) + 1] = -existing;
            }
        }

        for (var i = 0; i < tableSize; i++)
        {
            var length = _table[2 * (tableIndex + i) + 1];
            if (length >= 0)
            {
                continue;
            }

            var needed = -length;
            if (needed > tableBits)
            {
                needed = tableBits;
                _table[2 * (tableIndex + i) + 1] = -needed;
            }

            var sub = BuildLevel(needed, codeCount, codes, bits, (prefix << tableBits) | (uint)i, prefixLength + tableBits);
            _table[2 * (tableIndex + i)] = sub;
        }

        return tableIndex;
    }

    internal int Decode(ref BitReader reader)
    {
        var index = reader.Peek(Bits);
        var code = _table[2 * index];
        var length = _table[2 * index + 1];

        if (length < 0)
        {
            reader.Skip(Bits);
            var width = -length;
            index = reader.Peek(width) + code;
            code = _table[2 * index];
            length = _table[2 * index + 1];

            if (length < 0)
            {
                reader.Skip(width);
                width = -length;
                index = reader.Peek(width) + code;
                code = _table[2 * index];
                length = _table[2 * index + 1];
            }
        }

        reader.Skip(length);
        return code;
    }
}
