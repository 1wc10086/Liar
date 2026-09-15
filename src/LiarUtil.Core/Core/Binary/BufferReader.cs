using System.Buffers.Binary;

namespace LiarUtil.Core.Core.Binary;

public sealed class BufferReader
{
    private readonly byte[] _data;
    private readonly int _start;
    private readonly int _end;
    private int _position;

    public BufferReader(byte[] data) : this(data, 0, data.Length)
    {
    }

    public BufferReader(byte[] data, int start, int count)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentOutOfRangeException.ThrowIfNegative(start);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (start > data.Length || count > data.Length - start)
        {
            throw BinaryErrors.Throw(BinaryErrors.OutOfRange(start, count, data.Length));
        }

        _data = data;
        _start = start;
        _end = start + count;
        _position = start;
    }

    public BufferReader(ReadOnlySpan<byte> data, ByteOrder order = ByteOrder.Little)
        : this(data.ToArray(), 0, data.Length) => Order = order;

    public BufferReader(byte[] data, bool bigEndian)
        : this(data, 0, data.Length) => Order = bigEndian ? ByteOrder.Big : ByteOrder.Little;

    public BufferReader(byte[] data, ByteOrder order)
        : this(data, 0, data.Length) => Order = order;

    public ByteOrder Order { get; set; } = ByteOrder.Little;

    public bool BigEndian
    {
        get => Order.IsBigEndian;
        set => Order = value ? ByteOrder.Big : ByteOrder.Little;
    }

    public Func<string, Exception> ErrorFactory { get; set; } = BinaryErrors.Throw;

    public bool Lenient { get; init; }

    public bool HasOverflow { get; private set; }

    public int Position
    {
        get => _position - _start;
        set
        {
            if (value < 0 || value > _end - _start)
            {
                throw ErrorFactory(BinaryErrors.PositionOutOfRange(value, _end - _start));
            }

            _position = _start + value;
        }
    }

    public int AbsolutePosition => _position;

    public int Length => _end - _start;

    public int Remaining => _end - _position;

    public bool AtEnd => _position >= _end;

    public ReadOnlySpan<byte> RemainingSpan => _data.AsSpan(_position, _end - _position);

    public byte[] Source => _data;

    public void Seek(int position) => Position = position;

    public void Skip(long count)
    {
        var remaining = _end - _position;
        if (count < 0 || count > remaining)
        {
            if (Lenient)
            {
                HasOverflow = true;
                _position = count < 0 ? _start : _end;
                return;
            }

            throw ErrorFactory(BinaryErrors.Truncated(Position, count, remaining));
        }

        _position += (int)count;
    }

    public void Reset() => _position = _start;

    public byte ReadUInt8()
    {
        if (!Require(1))
        {
            return 0;
        }

        return _data[_position++];
    }

    public byte ReadByte() => ReadUInt8();

    public sbyte ReadInt8() => unchecked((sbyte)ReadUInt8());

    public bool ReadBoolean() => ReadUInt8() != 0;

    public ushort ReadUInt16()
    {
        if (!Require(sizeof(ushort)))
        {
            return 0;
        }

        var span = _data.AsSpan(_position, sizeof(ushort));
        _position += sizeof(ushort);
        return Order.IsBigEndian
            ? BinaryPrimitives.ReadUInt16BigEndian(span)
            : BinaryPrimitives.ReadUInt16LittleEndian(span);
    }

    public short ReadInt16()
    {
        if (!Require(sizeof(short)))
        {
            return 0;
        }

        var span = _data.AsSpan(_position, sizeof(short));
        _position += sizeof(short);
        return Order.IsBigEndian
            ? BinaryPrimitives.ReadInt16BigEndian(span)
            : BinaryPrimitives.ReadInt16LittleEndian(span);
    }

    public uint ReadUInt32()
    {
        if (!Require(sizeof(uint)))
        {
            return 0;
        }

        var span = _data.AsSpan(_position, sizeof(uint));
        _position += sizeof(uint);
        return Order.IsBigEndian
            ? BinaryPrimitives.ReadUInt32BigEndian(span)
            : BinaryPrimitives.ReadUInt32LittleEndian(span);
    }

    public int ReadInt32()
    {
        if (!Require(sizeof(int)))
        {
            return 0;
        }

        var span = _data.AsSpan(_position, sizeof(int));
        _position += sizeof(int);
        return Order.IsBigEndian
            ? BinaryPrimitives.ReadInt32BigEndian(span)
            : BinaryPrimitives.ReadInt32LittleEndian(span);
    }

    public ulong ReadUInt64()
    {
        if (!Require(sizeof(ulong)))
        {
            return 0;
        }

        var span = _data.AsSpan(_position, sizeof(ulong));
        _position += sizeof(ulong);
        return Order.IsBigEndian
            ? BinaryPrimitives.ReadUInt64BigEndian(span)
            : BinaryPrimitives.ReadUInt64LittleEndian(span);
    }

    public long ReadInt64()
    {
        if (!Require(sizeof(long)))
        {
            return 0;
        }

        var span = _data.AsSpan(_position, sizeof(long));
        _position += sizeof(long);
        return Order.IsBigEndian
            ? BinaryPrimitives.ReadInt64BigEndian(span)
            : BinaryPrimitives.ReadInt64LittleEndian(span);
    }

    public float ReadSingle()
    {
        if (!Require(sizeof(float)))
        {
            return 0;
        }

        var span = _data.AsSpan(_position, sizeof(float));
        _position += sizeof(float);
        return Order.IsBigEndian
            ? BinaryPrimitives.ReadSingleBigEndian(span)
            : BinaryPrimitives.ReadSingleLittleEndian(span);
    }

    public double ReadDouble()
    {
        if (!Require(sizeof(double)))
        {
            return 0;
        }

        var span = _data.AsSpan(_position, sizeof(double));
        _position += sizeof(double);
        return Order.IsBigEndian
            ? BinaryPrimitives.ReadDoubleBigEndian(span)
            : BinaryPrimitives.ReadDoubleLittleEndian(span);
    }

    public byte PeekUInt8()
    {
        if (!Require(1))
        {
            return 0;
        }

        return _data[_position];
    }

    public ushort PeekUInt16()
    {
        var position = _position;
        var value = ReadUInt16();
        _position = position;
        return value;
    }

    public int PeekInt32()
    {
        var position = _position;
        var value = ReadInt32();
        _position = position;
        return value;
    }

    public uint PeekUInt32()
    {
        var position = _position;
        var value = ReadUInt32();
        _position = position;
        return value;
    }

    public ReadOnlySpan<byte> ReadSpan(int count)
    {
        if (!Require(count))
        {
            return default;
        }

        var span = _data.AsSpan(_position, count);
        _position += count;
        return span;
    }

    public ReadOnlySpan<byte> ReadSpan(long count) => count is < 0 or > int.MaxValue
        ? throw ErrorFactory(BinaryErrors.OutOfRange(Position, count, Remaining))
        : ReadSpan((int)count);

    public byte[] ReadBytes(int count) => ReadSpan(count).ToArray();

    public byte[] ReadBytes(long count) => ReadSpan(count).ToArray();

    public ReadOnlySpan<byte> Rest()
    {
        var span = _data.AsSpan(_position, _end - _position);
        _position = _end;
        return span;
    }

    public byte[] RestBytes() => Rest().ToArray();

    public bool RequireLength(ulong length)
    {
        if (length <= (ulong)Remaining)
        {
            return false;
        }

        if (Lenient)
        {
            HasOverflow = true;
            return true;
        }

        throw ErrorFactory(BinaryErrors.Truncated(Position, (long)length, Remaining));
    }

    public int ReadVarInt32() => unchecked((int)ReadVarUInt32());

    public uint ReadVarUInt32()
    {
        var result = 0u;
        var shift = 0;
        while (true)
        {
            if (shift > 35)
            {
                throw ErrorFactory(BinaryErrors.VarIntOverflow());
            }

            var value = ReadUInt8();
            result |= (uint)(value & 0x7F) << shift;
            if ((value & 0x80) == 0)
            {
                return result;
            }

            shift += 7;
        }
    }

    public long ReadVarInt64() => unchecked((long)ReadVarUInt64());

    public ulong ReadVarUInt64()
    {
        var result = 0ul;
        var shift = 0;
        while (true)
        {
            if (shift > 63)
            {
                throw ErrorFactory(BinaryErrors.VarIntOverflow());
            }

            var value = ReadUInt8();
            result |= (ulong)(value & 0x7F) << shift;
            if ((value & 0x80) == 0)
            {
                return result;
            }

            shift += 7;
        }
    }

    public int ReadZigZag32() => ZigZag.Decode32(ReadVarUInt32());

    public long ReadZigZag64() => ZigZag.Decode64(ReadVarUInt64());

    public string? ReadString(int byteCount) =>
        byteCount <= 0 ? null : TextCodec.Decode(ReadSpan(byteCount));

    public string? ReadStringByInt32Head() => ReadString(ReadInt32());

    public string? ReadStringByVarInt32Head() => ReadString(ReadVarInt32());

    public string? ReadUtf16ByInt32Head()
    {
        var characters = ReadInt32();
        return characters <= 0
            ? null
            : TextCodec.Decode(ReadSpan(characters * sizeof(char)), TextFormat.Utf16);
    }

    public string ReadLatin1ByUInt32Head()
    {
        var length = ReadUInt32();
        return length > (uint)Remaining
            ? throw ErrorFactory(BinaryErrors.Truncated(Position, length, Remaining))
            : TextCodec.Decode(ReadSpan((int)length), TextFormat.Latin1);
    }

    public string ReadStringByByteHead()
    {
        var length = ReadUInt8();
        return length == 0 ? "" : TextCodec.Decode(ReadSpan(length));
    }

    public string ReadStrictUtf8(int byteCount) => TextCodec.DecodeLenient(ReadSpan(byteCount));

    public string ReadNullTerminated(TextFormat format = TextFormat.Utf8)
    {
        var start = _position;
        while (_position < _end && _data[_position] != 0)
        {
            _position++;
        }

        var text = TextCodec.Decode(_data.AsSpan(start, _position - start), format);
        if (_position < _end)
        {
            _position++;
        }

        return text;
    }

    public void ExpectInt32(int expected)
    {
        var value = ReadInt32();
        if (value != expected)
        {
            throw ErrorFactory(BinaryErrors.MarkerMismatch(unchecked((uint)value), unchecked((uint)expected)));
        }
    }

    public void ExpectUInt32(uint expected)
    {
        var value = ReadUInt32();
        if (value != expected)
        {
            throw ErrorFactory(BinaryErrors.MarkerMismatch(value, expected));
        }
    }

    public void ExpectBytes(ReadOnlySpan<byte> expected)
    {
        if (!Require(expected.Length))
        {
            return;
        }

        if (!_data.AsSpan(_position, expected.Length).SequenceEqual(expected))
        {
            throw ErrorFactory(BinaryErrors.MarkerMismatch());
        }

        _position += expected.Length;
    }

    private bool Require(int count)
    {
        if (count >= 0 && _position <= _end - count)
        {
            return true;
        }

        if (Lenient)
        {
            HasOverflow = true;
            _position = count < 0 ? _start : _end;
            return false;
        }

        throw ErrorFactory(BinaryErrors.Truncated(Position, count, Remaining));
    }
}
