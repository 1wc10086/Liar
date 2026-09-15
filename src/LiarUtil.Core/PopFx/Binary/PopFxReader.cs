using System.Text;
using LiarUtil.Core.Core.Binary;

namespace LiarUtil.Core.PopFx.Binary;

internal sealed class PopFxReader
{
    private static readonly UTF8Encoding Utf8 = new(false, true);

    private readonly BufferReader _reader;

    public PopFxReader(byte[] data) =>
        _reader = new BufferReader(data) { ErrorFactory = static _ => new InvalidDataException(LiarUtil.Core.Strings.POPFXDataInsufficient) };

    public void Seek(long position)
    {
        if (position < 0 || position > _reader.Length)
        {
            throw new InvalidDataException(string.Format(LiarUtil.Core.Strings.POPFXOffsetOutOfRange0, position));
        }
        _reader.Seek((int)position);
    }

    public uint ReadUInt32() => _reader.ReadUInt32();

    public string ReadString(long offset, long count)
    {
        if (count < 0 || count > int.MaxValue || offset < 0 || offset > _reader.Length - count)
        {
            throw new InvalidDataException(string.Format(LiarUtil.Core.Strings.POPFXStringOutOfRangeOffset0Length, offset, count));
        }
        return Utf8.GetString(_reader.Source, (int)offset, (int)count);
    }
}
