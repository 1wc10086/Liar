using System.Buffers.Binary;

namespace LiarUtil.Core.Wma;

internal static class AsfPackets
{
    private const int FrameHeaderSize = 17;
    private const int PacketHeaderBaseSize = 9;

    internal static List<byte[]> Read(ReadOnlySpan<byte> data, AsfInfo info)
    {
        var packets = new List<byte[]>();
        var packetSize = info.PacketSize;
        var position = info.DataOffset;
        var fragOffset = 0;
        var fragData = Array.Empty<byte>();

        while (position + packetSize <= data.Length)
        {
            var packet = data.Slice(position, packetSize);
            var reader = new AsfCursor(packet);

            var prefix = reader.Byte();
            var rsize = PacketHeaderBaseSize;
            if ((prefix & 0x0f) == 2)
            {
                reader.UInt16();
                rsize += 2;
            }

            var flags = reader.Byte();
            var property = reader.Byte();

            var packetLength = ReadVariable(ref reader, flags >> 5, packetSize, ref rsize);
            ReadVariable(ref reader, flags >> 1, 0, ref rsize);
            var paddingSize = ReadVariable(ref reader, flags >> 3, 0, ref rsize);

            reader.UInt32();
            reader.UInt16();

            int segments;
            int segSizeType;
            if ((flags & 0x01) != 0)
            {
                segSizeType = reader.Byte();
                rsize++;
                segments = segSizeType & 0x3f;
            }
            else
            {
                segments = 1;
                segSizeType = 0x80;
            }

            var sizeLeft = packetLength - paddingSize - rsize;
            if (packetLength < info.MinPacketSize)
            {
                paddingSize += info.MinPacketSize - packetLength;
            }

            while (true)
            {
                if (sizeLeft < FrameHeaderSize || segments < 1)
                {
                    break;
                }

                var num = reader.Byte();
                segments--;
                var frameSize = 1;

                ReadVariable(ref reader, property >> 4, 0, ref frameSize);
                ReadVariable(ref reader, property >> 2, 0, ref frameSize);
                var replicatedSize = ReadVariable(ref reader, property, 0, ref frameSize);

                int objectSize;
                if (replicatedSize > 1)
                {
                    objectSize = reader.UInt32();
                    reader.UInt32();
                    if (replicatedSize > 8)
                    {
                        reader.Skip(replicatedSize - 8);
                    }

                    frameSize += replicatedSize;
                }
                else if (replicatedSize == 1)
                {
                    reader.Skip(1);
                    frameSize++;
                    objectSize = 0;
                }
                else
                {
                    objectSize = 0;
                }

                int payloadSize;
                if ((flags & 0x01) != 0)
                {
                    payloadSize = ReadVariable(ref reader, segSizeType >> 6, 0, ref frameSize);
                }
                else
                {
                    payloadSize = sizeLeft - frameSize;
                }

                sizeLeft -= frameSize;

                if (fragOffset == 0 && objectSize > 0)
                {
                    fragData = new byte[objectSize];
                }

                var needed = objectSize > 0 ? objectSize : payloadSize;
                var available = Math.Min(needed, packet.Length - reader.Offset);
                if (available <= 0)
                {
                    break;
                }

                var chunk = packet.Slice(reader.Offset, available);
                if (fragOffset + available <= fragData.Length)
                {
                    chunk.CopyTo(fragData.AsSpan(fragOffset));
                }

                reader.Skip(available);
                fragOffset += available;
                sizeLeft -= available;

                if (objectSize > 0 && fragOffset >= fragData.Length)
                {
                    packets.Add(fragData);
                    fragOffset = 0;
                    break;
                }

                if (segments < 1)
                {
                    break;
                }
            }

            position += packetSize;
        }

        return packets;
    }

    private static int ReadVariable(ref AsfCursor reader, int bits, int defaultValue, ref int size)
    {
        switch (bits & 3)
        {
            case 3:
                size += 4;
                return reader.UInt32();
            case 2:
                size += 2;
                return reader.UInt16();
            case 1:
                size += 1;
                return reader.Byte();
            default:
                return defaultValue;
        }
    }

    private ref struct AsfCursor(ReadOnlySpan<byte> data)
    {
        private readonly ReadOnlySpan<byte> _data = data;
        private int _offset = 0;

        internal readonly int Offset => _offset;

        internal byte Byte()
        {
            var value = _offset < _data.Length ? _data[_offset] : (byte)0;
            _offset++;
            return value;
        }

        internal int UInt16()
        {
            var value = _offset + 2 <= _data.Length ? BinaryPrimitives.ReadUInt16LittleEndian(_data[_offset..]) : 0;
            _offset += 2;
            return value;
        }

        internal int UInt32()
        {
            var value = _offset + 4 <= _data.Length ? (int)BinaryPrimitives.ReadUInt32LittleEndian(_data[_offset..]) : 0;
            _offset += 4;
            return value;
        }

        internal void Skip(int count) => _offset += count;
    }
}
