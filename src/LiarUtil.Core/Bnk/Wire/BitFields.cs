using LiarUtil.Core.Bnk.Model;
using CoreBits = LiarUtil.Core.Core.Binary.BitFields;

namespace LiarUtil.Core.Bnk;

internal ref struct BitFields
{
    private readonly BankContext _context;
    private readonly int _width;
    private ulong _raw;
    private int _shift;

    internal BitFields(BankContext context, int width)
    {
        _context = context;
        _width = width;
        _shift = 0;
        _raw = context.Reading ? context.ReadBitsRaw(width) : 0;
    }

    public void Bit(ref bool value)
    {
        if (_context.Reading)
        {
            value = CoreBits.Extract(_raw, _shift, 1) != 0;
        }
        else if (value)
        {
            _raw |= 1UL << _shift;
        }

        _shift++;
    }

    public void Const(bool value)
    {
        if (_context.Reading)
        {
            if ((CoreBits.Extract(_raw, _shift, 1) != 0) != value)
            {
                throw BnkException.Invalid(LiarUtil.Core.Strings.BitField, 0, LiarUtil.Core.Strings.ConstantBitsMismatch);
            }
        }
        else if (value)
        {
            _raw |= 1UL << _shift;
        }

        _shift++;
    }

    public void Enum<T>(ref T value, IWireEnum<T> wire) where T : struct
    {
        var width = wire.Width(_context.Version);
        if (_context.Reading)
        {
            value = wire.FromRaw(_context.Version, (int)CoreBits.Extract(_raw, _shift, width));
        }
        else
        {
            _raw |= (ulong)wire.ToRaw(_context.Version, value) << _shift;
        }

        _shift += width;
    }

    public void Done(bool ignoreReserved = true)
    {
        if (_context.Reading)
        {
            var reservedWidth = _width - _shift;
            if (!ignoreReserved && reservedWidth > 0)
            {
                var reserved = CoreBits.Extract(_raw, _shift, reservedWidth);
                if (reserved != 0)
                {
                    throw BnkException.Invalid(LiarUtil.Core.Strings.BitField, 0, string.Format(LiarUtil.Core.Strings.ReservedBitsAreNonZero0x0X, reserved));
                }
            }
        }
        else
        {
            _context.WriteBitsRaw(_raw, _width);
        }
    }
}
