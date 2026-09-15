namespace LiarUtil.Core.Bnk;

internal sealed partial class BankContext
{
    public BitFields Bits8() => new(this, 8);

    public BitFields Bits16() => new(this, 16);

    public BitFields Bits32() => new(this, 32);

    internal ulong ReadBitsRaw(int width) => width switch
    {
        8 => _reader!.ReadUInt8(),
        16 => _reader!.ReadUInt16(),
        32 => _reader!.ReadUInt32(),
        _ => throw BnkException.Invalid(LiarUtil.Core.Strings.BitField, _reader!.Position, string.Format(LiarUtil.Core.Strings.UnsupportedBitWidth0, width)),
    };

    internal void WriteBitsRaw(ulong raw, int width)
    {
        switch (width)
        {
            case 8:
                _writer!.WriteUInt8((byte)raw);
                break;
            case 16:
                _writer!.WriteUInt16((ushort)raw);
                break;
            case 32:
                _writer!.WriteUInt32((uint)raw);
                break;
            default:
                throw BnkException.Invalid(LiarUtil.Core.Strings.BitField, _writer!.Length, string.Format(LiarUtil.Core.Strings.UnsupportedBitWidth0, width));
        }
    }
}
