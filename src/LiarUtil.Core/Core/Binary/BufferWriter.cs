using System.Buffers.Binary;

namespace LiarUtil.Core.Core.Binary;

public sealed class BufferWriter
{
    private byte[] _buffer;
    private int _length;

    public BufferWriter(int capacity = 256)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);
        _buffer = new byte[Math.Max(capacity, 1)];
    }

    public BufferWriter(bool bigEndian) : this(256) => Order = bigEndian ? ByteOrder.Big : ByteOrder.Little;

    public BufferWriter(ByteOrder order) : this(256) => Order = order;

    public BufferWriter(int capacity, ByteOrder order) : this(capacity) => Order = order;

    public ByteOrder Order { get; set; } = ByteOrder.Little;

    public bool BigEndian
    {
        get => Order.IsBigEndian;
        set => Order = value ? ByteOrder.Big : ByteOrder.Little;
    }

    public int Position { get; set; }

    public int Length => _length;

    public ReadOnlySpan<byte> Span => _buffer.AsSpan(0, _length);

    public ReadOnlyMemory<byte> Memory => _buffer.AsMemory(0, _length);

    public byte this[int index]
    {
        get => _buffer[index];
        set
        {
            if (index < 0 || index >= _length)
            {
                throw BinaryErrors.Throw(BinaryErrors.PositionOutOfRange(index, _length));
            }

            _buffer[index] = value;
        }
    }

    public void Reset()
    {
        Position = 0;
        _length = 0;
    }

    public void SetLength(int length)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        Ensure(length);
        if (length > _length)
        {
            _buffer.AsSpan(_length, length - _length).Clear();
        }

        _length = length;
        if (Position > length)
        {
            Position = length;
        }
    }

    public void Clear() => _buffer.AsSpan(0, _length).Clear();

    public void WriteUInt8(byte value)
    {
        Span<byte> span = stackalloc byte[1];
        span[0] = value;
        Write(span);
    }

    public void WriteByte(byte value) => WriteUInt8(value);

    public void WriteInt8(sbyte value) => WriteUInt8(unchecked((byte)value));

    public void WriteBoolean(bool value) => WriteUInt8(value ? (byte)1 : (byte)0);

    public void WriteUInt16(ushort value)
    {
        Span<byte> span = stackalloc byte[sizeof(ushort)];
        if (Order.IsBigEndian)
        {
            BinaryPrimitives.WriteUInt16BigEndian(span, value);
        }
        else
        {
            BinaryPrimitives.WriteUInt16LittleEndian(span, value);
        }

        Write(span);
    }

    public void WriteInt16(short value)
    {
        Span<byte> span = stackalloc byte[sizeof(short)];
        if (Order.IsBigEndian)
        {
            BinaryPrimitives.WriteInt16BigEndian(span, value);
        }
        else
        {
            BinaryPrimitives.WriteInt16LittleEndian(span, value);
        }

        Write(span);
    }

    public void WriteUInt32(uint value)
    {
        Span<byte> span = stackalloc byte[sizeof(uint)];
        if (Order.IsBigEndian)
        {
            BinaryPrimitives.WriteUInt32BigEndian(span, value);
        }
        else
        {
            BinaryPrimitives.WriteUInt32LittleEndian(span, value);
        }

        Write(span);
    }

    public void WriteInt32(int value) => WriteUInt32(unchecked((uint)value));

    public void WriteUInt64(ulong value)
    {
        Span<byte> span = stackalloc byte[sizeof(ulong)];
        if (Order.IsBigEndian)
        {
            BinaryPrimitives.WriteUInt64BigEndian(span, value);
        }
        else
        {
            BinaryPrimitives.WriteUInt64LittleEndian(span, value);
        }

        Write(span);
    }

    public void WriteInt64(long value) => WriteUInt64(unchecked((ulong)value));

    public void WriteSingle(float value)
    {
        Span<byte> span = stackalloc byte[sizeof(float)];
        if (Order.IsBigEndian)
        {
            BinaryPrimitives.WriteSingleBigEndian(span, value);
        }
        else
        {
            BinaryPrimitives.WriteSingleLittleEndian(span, value);
        }

        Write(span);
    }

    public void WriteDouble(double value)
    {
        Span<byte> span = stackalloc byte[sizeof(double)];
        if (Order.IsBigEndian)
        {
            BinaryPrimitives.WriteDoubleBigEndian(span, value);
        }
        else
        {
            BinaryPrimitives.WriteDoubleLittleEndian(span, value);
        }

        Write(span);
    }

    public void WriteBytes(ReadOnlySpan<byte> value) => Write(value);

    public void WriteZeros(int count)
    {
        if (count <= 0)
        {
            return;
        }

        var end = Position + count;
        Ensure(end);
        _buffer.AsSpan(Position, count).Clear();
        Position = end;
        if (Position > _length)
        {
            _length = Position;
        }
    }

    public void WriteVarInt32(int value)
    {
        var remaining = unchecked((uint)value);
        while (remaining >= 0x80)
        {
            WriteUInt8((byte)(remaining | 0x80));
            remaining >>= 7;
        }

        WriteUInt8((byte)remaining);
    }

    public void WriteVarUInt32(uint value) => WriteVarInt32(unchecked((int)value));

    public void WriteVarInt64(long value)
    {
        var remaining = unchecked((ulong)value);
        while (remaining >= 0x80)
        {
            WriteUInt8((byte)(remaining | 0x80));
            remaining >>= 7;
        }

        WriteUInt8((byte)remaining);
    }

    public void WriteVarUInt64(ulong value) => WriteVarInt64(unchecked((long)value));

    public void WriteZigZag32(int value) => WriteVarUInt32(ZigZag.Encode32(value));

    public void WriteZigZag64(long value) => WriteVarUInt64(ZigZag.Encode64(value));

    public void WriteStringByInt32Head(string? value)
    {
        if (value is null)
        {
            WriteInt32(0);
            return;
        }

        var bytes = TextCodec.Encode(value);
        WriteInt32(bytes.Length);
        Write(bytes);
    }

    public void WriteStringByVarInt32Head(string? value)
    {
        if (value is null)
        {
            WriteVarInt32(0);
            return;
        }

        var bytes = TextCodec.Encode(value);
        WriteVarInt32(bytes.Length);
        Write(bytes);
    }

    public void WriteUtf16ByInt32Head(string? value)
    {
        if (value is null)
        {
            WriteInt32(0);
            return;
        }

        WriteInt32(value.Length);
        Write(TextCodec.Encode(value, TextFormat.Utf16));
    }

    public void WriteLatin1ByUInt32Head(string value)
    {
        var bytes = TextCodec.Encode(value, TextFormat.Latin1);
        WriteUInt32((uint)bytes.Length);
        Write(bytes);
    }

    public void WriteNullTerminated(string value, TextFormat format = TextFormat.Utf8)
    {
        Write(TextCodec.Encode(value, format));
        WriteUInt8(0);
    }

    public void WriteInt32At(int position, int value) => WriteUInt32At(position, unchecked((uint)value));

    public void WriteUInt32At(int position, uint value)
    {
        if (position < 0 || (long)position + sizeof(uint) > _length)
        {
            throw BinaryErrors.Throw(BinaryErrors.PositionOutOfRange(position, _length));
        }

        Span<byte> span = stackalloc byte[sizeof(uint)];
        if (Order.IsBigEndian)
        {
            BinaryPrimitives.WriteUInt32BigEndian(span, value);
        }
        else
        {
            BinaryPrimitives.WriteUInt32LittleEndian(span, value);
        }

        span.CopyTo(_buffer.AsSpan(position));
    }

    public void WriteBytesAt(int position, ReadOnlySpan<byte> value)
    {
        if (position < 0 || (long)position + value.Length > _length)
        {
            throw BinaryErrors.Throw(BinaryErrors.OutOfRange(position, value.Length, _length));
        }

        value.CopyTo(_buffer.AsSpan(position));
    }

    public void Resize(int count, byte value = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        Ensure(count);
        if (count > _length)
        {
            _buffer.AsSpan(_length, count - _length).Fill(value);
        }

        _length = count;
        if (Position > count)
        {
            Position = count;
        }
    }

    public void WritePad(byte value, int multiple)
    {
        if (multiple <= 0)
        {
            return;
        }

        var remainder = Position % multiple;
        if (remainder == 0)
        {
            return;
        }

        var count = multiple - remainder;
        Ensure(Position + count);
        _buffer.AsSpan(Position, count).Fill(value);
        Position += count;
        if (Position > _length)
        {
            _length = Position;
        }
    }

    public void WriteAligned(ReadOnlySpan<byte> value, int alignment, byte fill = 0)
    {
        Write(value);
        WritePad(fill, alignment);
    }

    public byte[] ToArray() => _buffer.AsSpan(0, _length).ToArray();

    public byte[] ToArrayAndReset()
    {
        var result = ToArray();
        Reset();
        return result;
    }

    private void Write(ReadOnlySpan<byte> value)
    {
        var end = Position + value.Length;
        Ensure(end);
        value.CopyTo(_buffer.AsSpan(Position));
        Position = end;
        if (Position > _length)
        {
            _length = Position;
        }
    }

    private void Ensure(int capacity)
    {
        if (capacity <= _buffer.Length)
        {
            return;
        }

        var size = (int)Math.Min(Array.MaxLength, Math.Max((long)_buffer.Length * 2, capacity));
        if (size < capacity)
        {
            throw BinaryErrors.Throw(BinaryErrors.TooLarge(capacity));
        }

        Array.Resize(ref _buffer, size);
    }
}
