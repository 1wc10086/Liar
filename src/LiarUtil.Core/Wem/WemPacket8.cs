namespace LiarUtil.Core.Wem;

internal readonly struct WemPacket8
{
    public WemPacket8(Stream stream, uint offset)
    {
        Offset = offset;
        stream.Seek(offset, SeekOrigin.Begin);
        Span<byte> buffer = stackalloc byte[8];
        stream.ReadExactly(buffer);
        Size = (uint)(buffer[0] | (buffer[1] << 8) | (buffer[2] << 16) | (buffer[3] << 24));
        Granule = (uint)(buffer[4] | (buffer[5] << 8) | (buffer[6] << 16) | (buffer[7] << 24));
    }

    public uint Offset { get; }

    public uint Size { get; }

    public uint Granule { get; }

    public uint HeaderSize => 8;

    public uint PayloadOffset => Offset + HeaderSize;

    public uint NextOffset => Offset + HeaderSize + Size;
}
