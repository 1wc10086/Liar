using LiarUtil.Core.Core.Binary;

namespace LiarUtil.Core.PopFx.Binary;

internal sealed class PopFxWriter
{
    private readonly BufferWriter _writer;

    public PopFxWriter(int length)
    {
        _writer = new BufferWriter(length);
        _writer.SetLength(length);
    }

    public int Length => _writer.Length;

    public void WriteUInt32(long offset, uint value)
    {
        Require(offset, sizeof(uint));
        _writer.WriteUInt32At((int)offset, value);
    }

    public void WriteBytes(long offset, ReadOnlySpan<byte> value)
    {
        Require(offset, value.Length);
        _writer.WriteBytesAt((int)offset, value);
    }

    public byte[] ToArray() => _writer.ToArray();

    private void Require(long offset, int count)
    {
        if (offset < 0 || count < 0 || offset > _writer.Length - count)
        {
            throw new InvalidOperationException(LiarUtil.Core.Strings.POPFXWriteOutOfRange);
        }
    }
}
