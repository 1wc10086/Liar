using LiarUtil.Core.Core.Binary;

namespace LiarUtil.Core.Bnk;

internal static class BankWire
{
    public static readonly Func<string, Exception> Failure = static message => new BnkException(message);

    public static BufferReader Reader(byte[] data) => new(data) { ErrorFactory = Failure };

    public static BufferReader Reader(byte[] data, int start, int count) =>
        new(data, start, count) { ErrorFactory = Failure };

    public static BufferWriter Writer(int capacity = 4096) => new(capacity);

    extension(BufferReader reader)
    {
        public BufferReader Slice(int count)
        {
            if (count < 0 || count > reader.Remaining)
            {
                throw BnkException.Truncated(LiarUtil.Core.Strings.Stream, reader.Position, count, reader.Remaining);
            }

            var slice = BankWire.Reader(reader.Source, reader.AbsolutePosition, count);
            reader.Skip(count);
            return slice;
        }

        public byte[] ReadAt(int offset, int size)
        {
            if (offset < 0 || (long)offset + size > reader.Length)
            {
                throw BnkException.Truncated(LiarUtil.Core.Strings.Stream, offset, size, reader.Length - offset);
            }

            var start = reader.AbsolutePosition - reader.Position + offset;
            return reader.Source.AsSpan(start, size).ToArray();
        }
    }
}
