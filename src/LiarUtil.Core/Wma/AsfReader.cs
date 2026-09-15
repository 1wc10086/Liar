using System.Buffers.Binary;

namespace LiarUtil.Core.Wma;

internal static class AsfReader
{
    private static ReadOnlySpan<byte> AsfHeader => [0x30, 0x26, 0xB2, 0x75, 0x8E, 0x66, 0xCF, 0x11, 0xA6, 0xD9, 0x00, 0xAA, 0x00, 0x62, 0xCE, 0x6C];

    private static ReadOnlySpan<byte> FileHeaderGuid => [0xA1, 0xDC, 0xAB, 0x8C, 0x47, 0xA9, 0xCF, 0x11, 0x8E, 0xE4, 0x00, 0xC0, 0x0C, 0x20, 0x53, 0x65];

    private static ReadOnlySpan<byte> StreamHeaderGuid => [0x91, 0x07, 0xDC, 0xB7, 0xB7, 0xA9, 0xCF, 0x11, 0x8E, 0xE6, 0x00, 0xC0, 0x0C, 0x20, 0x53, 0x65];

    private static ReadOnlySpan<byte> AudioStreamGuid => [0x40, 0x9E, 0x69, 0xF8, 0x4D, 0x5B, 0xCF, 0x11, 0xA8, 0xFD, 0x00, 0x80, 0x5F, 0x5C, 0x44, 0x2B];

    private static ReadOnlySpan<byte> DataHeaderGuid => [0x36, 0x26, 0xB2, 0x75, 0x8E, 0x66, 0xCF, 0x11, 0xA6, 0xD9, 0x00, 0xAA, 0x00, 0x62, 0xCE, 0x6C];

    internal static AsfInfo Parse(ReadOnlySpan<byte> data)
    {
        if (data.Length < 30 || !data[..16].SequenceEqual(AsfHeader))
        {
            throw new WmaException(LiarUtil.Core.Strings.InvalidASFHeader);
        }

        var position = 30;
        var packetSize = 0;
        var minPacketSize = 0;
        var preroll = 0;
        AsfInfo? audio = null;

        while (position + 24 <= data.Length)
        {
            var tag = data.Slice(position, 16);
            var size = BinaryPrimitives.ReadInt64LittleEndian(data[(position + 16)..]);
            if (size < 24)
            {
                throw new WmaException(LiarUtil.Core.Strings.InvalidASFHeader);
            }

            if (tag.SequenceEqual(FileHeaderGuid))
            {
                var at = position + 40;
                preroll = BinaryPrimitives.ReadInt32LittleEndian(data[(at + 40)..]);
                minPacketSize = BinaryPrimitives.ReadInt32LittleEndian(data[(at + 52)..]);
                packetSize = BinaryPrimitives.ReadInt32LittleEndian(data[(at + 56)..]);
            }
            else if (tag.SequenceEqual(StreamHeaderGuid))
            {
                audio ??= ParseStream(data.Slice(position, (int)Math.Min(size, data.Length - position)));
            }
            else if (tag.SequenceEqual(DataHeaderGuid))
            {
                position += 24 + 16 + 8 + 1 + 1;
                break;
            }

            position += (int)size;
        }

        if (audio is null || packetSize <= 0 || position > data.Length)
        {
            throw new WmaException(LiarUtil.Core.Strings.InvalidASFHeader);
        }

        return audio.WithContainer(packetSize, minPacketSize, preroll, position);
    }

    private static AsfInfo? ParseStream(ReadOnlySpan<byte> block)
    {
        if (block.Length < 78)
        {
            return null;
        }

        if (!block[24..40].SequenceEqual(AudioStreamGuid))
        {
            return null;
        }

        var at = 64;
        var typeSpecificSize = BinaryPrimitives.ReadInt32LittleEndian(block[at..]);
        at += 14;

        if (typeSpecificSize < 18 || at + typeSpecificSize > block.Length)
        {
            return null;
        }

        var format = block.Slice(at, typeSpecificSize);
        var codecTag = BinaryPrimitives.ReadUInt16LittleEndian(format);
        if (codecTag is not (0x160 or 0x161))
        {
            throw new WmaException(LiarUtil.Core.Strings.UnsupportedWMACodec);
        }

        var channels = BinaryPrimitives.ReadUInt16LittleEndian(format[2..]);
        var sampleRate = BinaryPrimitives.ReadInt32LittleEndian(format[4..]);
        var bitRate = BinaryPrimitives.ReadInt32LittleEndian(format[8..]) * 8;
        var blockAlign = BinaryPrimitives.ReadUInt16LittleEndian(format[12..]);
        var extraSize = BinaryPrimitives.ReadUInt16LittleEndian(format[16..]);
        if (extraSize > typeSpecificSize - 18)
        {
            extraSize = (ushort)(typeSpecificSize - 18);
        }

        if (channels is not (1 or 2) || sampleRate <= 0)
        {
            throw new WmaException(LiarUtil.Core.Strings.UnsupportedWMACodec);
        }

        return new AsfInfo
        {
            CodecTag = codecTag,
            Channels = channels,
            SampleRate = sampleRate,
            BitRate = bitRate,
            BlockAlign = blockAlign,
            ExtraData = format.Slice(18, extraSize).ToArray(),
            StreamId = 0,
            PacketSize = 0,
            MinPacketSize = 0,
            Preroll = 0,
            DataOffset = 0,
        };
    }
}
