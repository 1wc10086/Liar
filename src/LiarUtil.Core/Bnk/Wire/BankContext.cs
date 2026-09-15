using LiarUtil.Core.Core.Binary;

namespace LiarUtil.Core.Bnk;

internal enum SizeKind
{
    U8,
    U16,
    U32,
}

internal delegate void ElementExchange<T>(BankContext context, ref T item);

internal sealed partial class BankContext
{
    private readonly BufferReader? _reader;
    private readonly BufferWriter? _writer;

    public BankContext(BufferReader reader, BankVersion version)
    {
        _reader = reader;
        _writer = null;
        Version = version;
    }

    public BankContext(BufferWriter writer, BankVersion version)
    {
        _reader = null;
        _writer = writer;
        Version = version;
    }

    public BankVersion Version { get; }

    public bool Reading => _reader is not null;

    public BufferReader Reader => _reader!;

    public BufferWriter Writer => _writer!;

    public BankContext Slice(int length) => new(_reader!.Slice(length), Version);

    public static BankContext ForWrite(BankVersion version) => new(BankWire.Writer(), version);

    public byte[] ToArray() => _writer!.ToArray();
}
