using System.Text;
using LiarUtil.Core.Core.Binary;

namespace LiarUtil.Core.RsbPatch;

internal sealed class RsbPatchReader(byte[] data)
{
    private readonly BufferReader _reader = new(data) { ErrorFactory = static message => new RsbPatchException(message) };

    public int Position
    {
        get => _reader.Position;
        set => _reader.Position = value;
    }

    public uint ReadUInt32()
    {
        Require(sizeof(uint));
        return _reader.ReadUInt32();
    }

    public byte[] ReadBytes(int count)
    {
        Require(count);
        return _reader.ReadBytes(count);
    }

    public string ReadFixedName(int size)
    {
        var bytes = ReadBytes(size);
        var terminator = bytes.AsSpan().IndexOf((byte)0);
        return Encoding.Latin1.GetString(terminator < 0 ? bytes : bytes.AsSpan(0, terminator));
    }

    public void Skip(int count)
    {
        Require(count);
        _reader.Skip(count);
    }

    private void Require(int count)
    {
        if (count < 0 || Position < 0 || Position > data.Length - count)
        {
            throw new RsbPatchException(string.Format(LiarUtil.Core.Strings.RsbPatchDataIncompleteExpected0Bytes1Remaining, count, data.Length - Position));
        }
    }
}

internal sealed class RsbPatchWriter(int capacity = 4096)
{
    private readonly BufferWriter _writer = new(capacity);

    public int Length => _writer.Length;

    public byte[] ToArray() => _writer.ToArray();

    public void WriteUInt32(uint value) => _writer.WriteUInt32(value);

    public void WriteBytes(ReadOnlySpan<byte> value) => _writer.WriteBytes(value);

    public void WriteZeros(int count) => _writer.WriteZeros(count);

    public int Reserve(int count)
    {
        var position = _writer.Length;
        WriteZeros(count);
        return position;
    }

    public void WritePadding(int unit = RsbPatchFormat.PaddingUnit) => WriteZeros(RsbPatchFormat.Padding(_writer.Length, unit));

    public void WriteName(string value, int size)
    {
        var bytes = Encoding.Latin1.GetBytes(value);
        if (bytes.Length >= size)
        {
            throw new RsbPatchException(string.Format(LiarUtil.Core.Strings.RsbPatchNameExceeds0Bytes1, size - 1, value));
        }

        WriteBytes(bytes);
        WriteZeros(size - bytes.Length);
    }

    public void Overwrite(int position, ReadOnlySpan<byte> value)
    {
        if (position < 0 || value.Length > _writer.Length - position)
        {
            throw new ArgumentOutOfRangeException(nameof(position));
        }

        _writer.WriteBytesAt(position, value);
    }
}
