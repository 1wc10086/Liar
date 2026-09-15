using System.Buffers.Binary;

namespace LiarUtil.Core.Wem;

internal sealed class OggPageWriter(Stream output)
{
    private const int HeaderSize = 27;
    private const int MaxSegments = 255;
    private const int SegmentSize = 255;
    private const int PageBytes = HeaderSize + MaxSegments + SegmentSize * MaxSegments;

    private readonly byte[] _page = new byte[PageBytes];

    private int _bitBuffer;
    private int _bitsStored;
    private int _payloadBytes;
    private bool _first = true;
    private bool _continued;
    private ulong _granule;
    private uint _sequenceNumber;

    public void WriteBit(byte value)
    {
        if (value == 1)
        {
            _bitBuffer |= 1 << _bitsStored;
        }

        _bitsStored++;
        if (_bitsStored == 8)
        {
            FlushBits();
        }
    }

    public void WriteBits(long value, int count)
    {
        for (var index = 0; index < count; index++)
        {
            WriteBit((byte)(((value >> index) & 1) != 0 ? 1 : 0));
        }
    }

    public void WriteVorbisHeader(byte type)
    {
        WriteBits(type, 8);
        foreach (var item in "vorbis"u8)
        {
            WriteBits(item, 8);
        }
    }

    public void SetGranule(ulong granule) => _granule = granule;

    public void FlushPage(bool nextContinued = false, bool last = false)
    {
        if (_payloadBytes != MaxSegments * SegmentSize)
        {
            FlushBits();
        }

        if (_payloadBytes != 0)
        {
            var segments = (_payloadBytes + SegmentSize) / SegmentSize;
            if (segments == MaxSegments + 1)
            {
                segments = MaxSegments;
            }

            for (var index = 0; index < _payloadBytes; index++)
            {
                _page[HeaderSize + segments + index] = _page[HeaderSize + MaxSegments + index];
            }

            _page[0] = (byte)'O';
            _page[1] = (byte)'g';
            _page[2] = (byte)'g';
            _page[3] = (byte)'S';
            _page[4] = 0;
            _page[5] = (byte)((_continued ? 1 : 0) | (_first ? 2 : 0) | (last ? 4 : 0));
            BinaryPrimitives.WriteUInt64LittleEndian(_page.AsSpan(6), _granule);
            _page[14] = 1;
            BinaryPrimitives.WriteUInt32LittleEndian(_page.AsSpan(18), _sequenceNumber);
            _page[22] = 0;
            _page[23] = 0;
            _page[24] = 0;
            _page[25] = 0;
            _page[26] = (byte)segments;
            for (var index = 0; index < segments; index++)
            {
                var remaining = _payloadBytes - index * SegmentSize;
                _page[HeaderSize + index] = remaining >= SegmentSize ? (byte)SegmentSize : (byte)remaining;
            }

            var length = HeaderSize + segments + _payloadBytes;
            BinaryPrimitives.WriteUInt32LittleEndian(_page.AsSpan(22), WemCrc32.Compute(_page.AsSpan(0, length)));
            output.Write(_page.AsSpan(0, length));
            _sequenceNumber++;
            _first = false;
            _continued = nextContinued;
            _payloadBytes = 0;
        }
    }

    private void FlushBits()
    {
        if (_bitsStored == 0)
        {
            return;
        }

        if (_payloadBytes == SegmentSize * MaxSegments)
        {
            throw new WemException(LiarUtil.Core.Strings.InsufficientOGGPacketSpace);
        }

        _page[HeaderSize + MaxSegments + _payloadBytes] = (byte)_bitBuffer;
        _payloadBytes++;
        _bitBuffer = 0;
        _bitsStored = 0;
    }
}
