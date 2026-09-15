using System.Buffers.Binary;

namespace LiarUtil.Core.Wem;

internal sealed class PackedCodebookLibrary
{
    private readonly byte[] _data;
    private readonly uint[] _offsets;

    public PackedCodebookLibrary(byte[] data)
    {
        if (data.Length < 8)
        {
            throw new WemException(LiarUtil.Core.Strings.PackedCodebookFileInvalid);
        }

        var offsetOffset = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(data.Length - 4));
        if (offsetOffset > data.Length - 4 || (data.Length - offsetOffset) % 4 != 0)
        {
            throw new WemException(LiarUtil.Core.Strings.PackedCodebookFileInvalid);
        }

        Count = (data.Length - (int)offsetOffset) / 4;
        _offsets = new uint[Count];
        _data = data[..(int)offsetOffset];
        for (var index = 0; index < Count; index++)
        {
            _offsets[index] = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan((int)offsetOffset + index * 4));
        }
    }

    public int Count { get; }

    public byte[] GetCodebook(uint index)
    {
        if (index >= Count - 1)
        {
            throw new WemException(LiarUtil.Core.Strings.CodebookIndexInvalid);
        }

        var size = (int)GetCodebookSize(index);
        var start = (int)_offsets[index];
        if (start < 0 || size < 0 || start + size > _data.Length)
        {
            throw new WemException(LiarUtil.Core.Strings.CodebookIndexInvalid);
        }

        return _data[start..(start + size)];
    }

    public uint GetCodebookSize(uint index)
    {
        if (index >= Count - 1)
        {
            throw new WemException(LiarUtil.Core.Strings.CodebookIndexInvalid);
        }

        return _offsets[index + 1] - _offsets[index];
    }
}
