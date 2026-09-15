using LiarUtil.Core.Core.Binary;

namespace LiarUtil.Core.Bnk;

internal sealed partial class BankContext
{
    public void Str8(ref string value) => Str(ref value, SizeKind.U8, false);

    public void Str32(ref string value) => Str(ref value, SizeKind.U32, false);

    public void Str32Zero(ref string value) => Str(ref value, SizeKind.U32, true);

    public void StrNull(ref string value)
    {
        if (_reader is not null)
        {
            var span = _reader.RemainingSpan;
            var index = span.IndexOf((byte)0);
            if (index < 0)
            {
                throw BnkException.Invalid(Strings.String, _reader.Position, Strings.MissingTerminator);
            }

            value = TextCodec.Decode(span[..index], TextFormat.Latin1);
            _reader.Skip(index + 1);
        }
        else
        {
            _writer!.WriteBytes(TextCodec.Encode(value, TextFormat.Latin1));
            _writer.WriteUInt8(0);
        }
    }

    private void Str(ref string value, SizeKind kind, bool zeroed)
    {
        if (_reader is not null)
        {
            var length = (int)ReadSize(kind);
            if (!zeroed)
            {
                value = TextCodec.Decode(_reader.ReadSpan(length), TextFormat.Latin1);
            }
            else
            {
                value = TextCodec.Decode(_reader.ReadSpan(length - 1), TextFormat.Latin1);
                _reader.Skip(1);
            }
        }
        else
        {
            var bytes = TextCodec.Encode(value, TextFormat.Latin1);
            WriteSize(kind, zeroed ? (ulong)bytes.Length + 1 : (ulong)bytes.Length);
            _writer!.WriteBytes(bytes);
            if (zeroed)
            {
                _writer.WriteUInt8(0);
            }
        }
    }
}
