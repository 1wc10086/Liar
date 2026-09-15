namespace LiarUtil.Core.Wem;

internal readonly struct WemPacket
{
    private readonly uint _offset;
    private readonly ushort _size;
    private readonly uint _granule;
    private readonly bool _noGranule;

    public WemPacket(Stream stream, uint offset, bool noGranule)
    {
        _offset = offset;
        _noGranule = noGranule;
        _size = 0xFFFF;
        stream.Seek(offset, SeekOrigin.Begin);
        Span<byte> sizeBuffer = stackalloc byte[2];
        stream.ReadExactly(sizeBuffer);
        _size = (ushort)(sizeBuffer[0] | (sizeBuffer[1] << 8));
        if (noGranule)
        {
            _granule = 0;
            return;
        }

        Span<byte> granuleBuffer = stackalloc byte[4];
        stream.ReadExactly(granuleBuffer);
        _granule = (uint)(granuleBuffer[0] | (granuleBuffer[1] << 8) | (granuleBuffer[2] << 16) | (granuleBuffer[3] << 24));
    }

    public uint HeaderSize => _noGranule ? 2U : 6U;

    public uint PayloadOffset => _offset + HeaderSize;

    public uint Size => _size;

    public uint Granule => _granule;

    public uint NextOffset => _offset + HeaderSize + _size;
}
