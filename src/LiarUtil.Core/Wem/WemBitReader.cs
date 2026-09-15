namespace LiarUtil.Core.Wem;

internal sealed class WemBitReader(Stream stream)
{
    private int _bitBuffer;
    private int _bitsLeft;

    public long TotalBitsRead { get; private set; }

    public byte ReadBit()
    {
        if (_bitsLeft == 0)
        {
            _bitBuffer = stream.ReadByte();
            if (_bitBuffer < 0)
            {
                throw new WemException(LiarUtil.Core.Strings.EncounteredEndOfFileWhileReadingWEMData);
            }

            _bitsLeft = 8;
        }

        TotalBitsRead++;
        _bitsLeft--;
        return (byte)((_bitBuffer & (0x80 >> _bitsLeft)) != 0 ? 1 : 0);
    }

    public uint Read(int bitCount)
    {
        uint result = 0;
        for (var index = 0; index < bitCount; index++)
        {
            if (ReadBit() == 1)
            {
                result |= 1U << index;
            }
        }

        return result;
    }
}
