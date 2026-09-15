using LiarUtil.Core.Core.Binary;
using LiarUtil.Core.Ppf.Models;

namespace LiarUtil.Core.Ppf.Encoding;

internal sealed partial class PpfEncoder
{
    private static readonly byte[] MagicMarker = [0x04, (byte)'P', (byte)'P', (byte)'F', (byte)'1'];

    private readonly BufferWriter _writer = new();

    public byte[] Encode(PpfEffect effect)
    {
        _writer.WriteBytes(MagicMarker);
        _writer.WriteUInt32(PpfCodec.Version);
        WriteEffect(effect);
        return _writer.ToArray();
    }

    private void WriteEffect(PpfEffect value)
    {
        PpfStrings.Write(_writer, value.Note);
        WriteList(value.Textures, WriteTexture);
        WriteList(value.Emitters, WriteEmitter);
        WriteList(value.Layers, WriteLayer);
        WriteColor(value.BackgroundColor);
        _writer.WriteInt32(value.Unknown1);
        _writer.WriteInt32(value.Unknown2);
        _writer.WriteInt16(value.FrameRate);
        _writer.WriteInt16(value.Unknown3);
        _writer.WriteInt16(value.Unknown4);
        _writer.WriteInt16(value.Unknown5);
        WriteSize(value.Size);
        _writer.WriteInt32(value.Unknown6);
        _writer.WriteInt32(value.Unknown7);
        _writer.WriteInt32(value.Unknown8);
        _writer.WriteInt32(value.Unknown9);
        _writer.WriteInt32(value.Unknown10);
        WriteFrameRange(value.FrameRange);
        PpfStrings.Write(_writer, value.Unknown11);
        _writer.WriteByte(value.Unknown12);
        _writer.WriteInt16(value.Unknown13);
        _writer.WriteInt16(value.Unknown14);
        if (value.StartupState is { } startupState)
        {
            var payload = new PpfStartupStateWriter(value).Write(startupState);
            _writer.WriteUInt32((uint)payload.Length);
            _writer.WriteBytes(payload);
        }
        else
        {
            _writer.WriteUInt32(0);
        }
    }

    private void WriteColor(PpfColor value)
    {
        _writer.WriteUInt32(value.Red);
        _writer.WriteUInt32(value.Green);
        _writer.WriteUInt32(value.Blue);
    }

    private void WriteSize(PpfSize value)
    {
        _writer.WriteInt32(value.Width);
        _writer.WriteInt32(value.Height);
    }

    private void WriteFrameRange(PpfFrameRange value)
    {
        _writer.WriteInt32(value.Begin);
        _writer.WriteInt32(value.End);
    }

    private void WriteVector2(PpfVector2 value)
    {
        _writer.WriteSingle(value.X);
        _writer.WriteSingle(value.Y);
    }

    private void WriteControlValue(PpfControlValue value)
    {
        WriteVector2(value.Start);
        WriteVector2(value.End);
    }

    private void WriteList<T>(IReadOnlyCollection<T> items, Action<T> writeItem)
    {
        WriteCount(items.Count);
        foreach (var item in items)
        {
            writeItem(item);
        }
    }

    private void WriteStringList(IReadOnlyCollection<string> items)
    {
        WriteCount(items.Count);
        foreach (var item in items)
        {
            PpfStrings.Write(_writer, item);
        }
    }

    private void WriteCount(int count)
    {
        if (count is < 0 or > ushort.MaxValue)
        {
            throw new InvalidDataException(string.Format(LiarUtil.Core.Strings.PPFListCountInvalid0, count));
        }
        _writer.WriteUInt16((ushort)count);
    }

    private void WriteFixedBytes(byte[] value, int count, string name)
    {
        if (value.Length != count)
        {
            throw new InvalidDataException(string.Format(LiarUtil.Core.Strings.PPF0LengthMust12, name, count, value.Length));
        }
        _writer.WriteBytes(value);
    }
}
