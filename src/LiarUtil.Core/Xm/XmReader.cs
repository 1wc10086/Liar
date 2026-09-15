using System.Text;

namespace LiarUtil.Core.Xm;

internal readonly ref struct XmReader
{
    private readonly ReadOnlySpan<byte> _data;

    private readonly int _bound;

    public XmReader(ReadOnlySpan<byte> data, int bound)
    {
        _data = data;
        _bound = bound < 0 ? 0 : bound > data.Length ? data.Length : bound;
    }

    public XmReader(ReadOnlySpan<byte> data)
        : this(data, data.Length)
    {
    }

    public byte U8(int offset) => (uint)offset < (uint)_bound ? _data[offset] : (byte)0;

    public ushort U16(int offset) => (ushort)(U8(offset) | (U8(offset + 1) << 8));

    public ushort U16Be(int offset) => (ushort)((U8(offset) << 8) | U8(offset + 1));

    public uint U32(int offset) => (uint)(U16(offset) | (U16(offset + 2) << 16));

    public uint U32Be(int offset) => (uint)((U16Be(offset) << 16) | U16Be(offset + 2));

    public sbyte S8(int offset) => (sbyte)U8(offset);

    public XmReader WithBound(int bound) => new(_data, bound);

    public string Text(int offset, int length, int maxLength)
    {
        var copy = length < maxLength ? length : maxLength;
        if (copy < 0)
        {
            copy = 0;
        }

        var available = offset < 0 ? 0 : _data.Length - offset;
        if (available > 0)
        {
            if (copy > available)
            {
                copy = available;
            }
        }
        else
        {
            copy = 0;
        }

        if (copy == 0)
        {
            return string.Empty;
        }

        var slice = _data.Slice(offset, copy);
        var end = slice.IndexOf((byte)0);
        if (end >= 0)
        {
            slice = slice.Slice(0, end);
        }

        return slice.IsEmpty ? string.Empty : Encoding.Latin1.GetString(slice);
    }
}
