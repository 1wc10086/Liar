namespace LiarUtil.Core.Sps;

internal sealed class EaLayer3BitWriter(int capacity)
{
    private byte[] _bytes = new byte[capacity];

    public int Bit { get; private set; }

    public void Write(int count, int value)
    {
        if ((Bit + count + 7) / 8 > _bytes.Length)
        {
            Array.Resize(ref _bytes, (Bit + count) / 8 + 64);
        }

        for (var bit = count - 1; bit >= 0; bit--)
        {
            if (((value >> bit) & 1) != 0)
            {
                _bytes[Bit >> 3] |= (byte)(1 << (7 - (Bit & 7)));
            }

            Bit++;
        }
    }

    public byte[] ToFrame(int frameSize)
    {
        Array.Resize(ref _bytes, frameSize);
        return _bytes;
    }

    public void CopyBits(EaLayer3Reader reader, int count)
    {
        for (var index = 0; index < count; index++)
        {
            Write(1, reader.Read(1));
        }
    }
}
