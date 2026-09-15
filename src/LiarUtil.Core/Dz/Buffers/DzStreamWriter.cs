using LiarUtil.Core.Core.Binary;

namespace LiarUtil.Core.Dz.Buffers;

internal sealed class DzStreamWriter(Stream stream)
{
    private readonly BufferWriter _scalars = new(sizeof(ulong));

    public long Position
    {
        get => stream.Position;
        set => stream.Position = value;
    }

    public void WriteByte(byte value) => stream.WriteByte(value);

    public void WriteUInt16(ushort value)
    {
        _scalars.Reset();
        _scalars.WriteUInt16(value);
        stream.Write(_scalars.Span);
    }

    public void WriteUInt32(uint value)
    {
        _scalars.Reset();
        _scalars.WriteUInt32(value);
        stream.Write(_scalars.Span);
    }

    public void WriteBytes(ReadOnlySpan<byte> value) => stream.Write(value);

    public void WriteName(string value)
    {
        stream.Write(TextCodec.EncodeAsciiOrUtf8(value));
        stream.WriteByte(0);
    }

    public void WriteZeros(long count)
    {
        Span<byte> buffer = stackalloc byte[512];
        buffer.Clear();
        while (count > 0)
        {
            var length = (int)Math.Min(count, buffer.Length);
            stream.Write(buffer[..length]);
            count -= length;
        }
    }
}
