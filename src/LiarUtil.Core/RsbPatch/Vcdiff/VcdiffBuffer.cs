namespace LiarUtil.Core.RsbPatch;

internal sealed class VcdiffBuffer(int capacity = 256)
{
    private byte[] _buffer = new byte[Math.Max(capacity, 16)];

    public int Count { get; private set; }

    public byte this[int index]
    {
        get => _buffer[index];
        set => _buffer[index] = value;
    }

    public void Add(byte value)
    {
        Ensure(Count + 1);
        _buffer[Count++] = value;
    }

    public void AddRange(ReadOnlySpan<byte> value)
    {
        if (value.IsEmpty)
        {
            return;
        }

        Ensure(Count + value.Length);
        value.CopyTo(_buffer.AsSpan(Count));
        Count += value.Length;
    }

    public void WriteVarInt(int value)
    {
        if (value < 0)
        {
            throw new RsbPatchException(LiarUtil.Core.Strings.VCDiffVariableLengthIntegerCannotNegative);
        }

        Span<byte> groups = stackalloc byte[5];
        var count = 0;
        var remaining = (uint)value;
        do
        {
            groups[count++] = (byte)(remaining & 0x7F);
            remaining >>= 7;
        }
        while (remaining != 0);

        Ensure(Count + count);
        for (var index = count - 1; index >= 0; index--)
        {
            _buffer[Count++] = index == 0 ? groups[index] : (byte)(groups[index] | 0x80);
        }
    }

    public void AddBuffer(VcdiffBuffer value) => AddRange(value.AsSpan());

    public ReadOnlySpan<byte> AsSpan() => _buffer.AsSpan(0, Count);

    public byte[] ToArray() => _buffer.AsSpan(0, Count).ToArray();

    private void Ensure(int required)
    {
        if (required <= _buffer.Length)
        {
            return;
        }

        Array.Resize(ref _buffer, Math.Max(_buffer.Length * 2, required));
    }
}
