using System.Buffers.Binary;

namespace LiarUtil.Core.Caf;

internal static class CafReader
{
    private const int DescriptionLength = 32;

    public static (CafDescription Description, byte[] Data, byte[] Cookie, byte[] PacketTable) Parse(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 8 || !Matches(bytes, 0, "caff"))
        {
            throw new CafException(LiarUtil.Core.Strings.InputNotCAFFile);
        }

        if (BinaryPrimitives.ReadUInt16BigEndian(bytes[4..]) != 1)
        {
            throw new CafException(LiarUtil.Core.Strings.UnsupportedCAFVersion);
        }

        var offset = 8;
        byte[]? description = null;
        byte[]? dataChunk = null;
        byte[] cookie = [];
        byte[] packetTable = [];
        while (offset < bytes.Length)
        {
            if (bytes.Length - offset < 12)
            {
                throw new CafException(LiarUtil.Core.Strings.ChunkHeaderTruncated);
            }

            var type = ReadFourCc(bytes, offset);
            var size = ReadInt64(bytes[(offset + 4)..]);
            var dataOffset = offset + 12;
            if (size < 0)
            {
                if (type != "data")
                {
                    throw new CafException(LiarUtil.Core.Strings.IndefiniteLengthOnlyValidForDataChunk);
                }

                size = bytes.Length - dataOffset;
            }

            if (size > bytes.Length - dataOffset)
            {
                throw new CafException(LiarUtil.Core.Strings.ChunkExceedsFileSize);
            }

            var payload = bytes.Slice(dataOffset, (int)size);
            switch (type)
            {
                case "desc":
                    description = payload.ToArray();
                    break;
                case "data":
                    dataChunk = payload.ToArray();
                    break;
                case "kuki":
                    cookie = payload.ToArray();
                    break;
                case "pakt":
                    packetTable = payload.ToArray();
                    break;
            }

            offset = dataOffset + (int)size;
        }

        if (description is null || dataChunk is null)
        {
            throw new CafException(LiarUtil.Core.Strings.MissingDescOrDataChunk);
        }

        if (description.Length != DescriptionLength)
        {
            throw new CafException(LiarUtil.Core.Strings.DescChunkLengthInvalid);
        }

        if (dataChunk.Length < 4)
        {
            throw new CafException(LiarUtil.Core.Strings.DataChunkInvalid);
        }

        var sampleRate = BinaryPrimitives.ReadDoubleBigEndian(description);
        var format = ReadFourCc(description, 8);
        if (!double.IsFinite(sampleRate) || sampleRate <= 0 || sampleRate > 192000)
        {
            throw new CafException(LiarUtil.Core.Strings.SampleRateInvalid);
        }

        var parsed = new CafDescription
        {
            SampleRate = sampleRate,
            Format = format,
            Flags = BinaryPrimitives.ReadUInt32BigEndian(description[12..]),
            BytesPerPacket = BinaryPrimitives.ReadUInt32BigEndian(description[16..]),
            FramesPerPacket = BinaryPrimitives.ReadUInt32BigEndian(description[20..]),
            Channels = BinaryPrimitives.ReadUInt32BigEndian(description[24..]),
            Bits = BinaryPrimitives.ReadUInt32BigEndian(description[28..]),
        };
        if (parsed.Channels is 0 or > 8)
        {
            throw new CafException(LiarUtil.Core.Strings.UnsupportedChannelCount);
        }

        return (parsed, dataChunk[4..], cookie, packetTable);
    }

    public static int[] ReadPacketSizes(CafDescription description, int dataLength, byte[] packetTable)
    {
        if (description.BytesPerPacket != 0)
        {
            var packetSize = (int)description.BytesPerPacket;
            if (dataLength % packetSize != 0)
            {
                throw new CafException(LiarUtil.Core.Strings.CompressedDataDoesNotContainCompletePacket);
            }

            var fixedSizes = new int[dataLength / packetSize];
            for (var index = 0; index < fixedSizes.Length; index++)
            {
                fixedSizes[index] = packetSize;
            }

            return fixedSizes;
        }

        if (packetTable.Length < 24)
        {
            throw new CafException(LiarUtil.Core.Strings.VariableLengthCompressedCAFRequiresPaktChunk);
        }

        var count = BinaryPrimitives.ReadUInt64BigEndian(packetTable);
        if (count > 10_000_000)
        {
            throw new CafException(LiarUtil.Core.Strings.TooManyPackets);
        }

        var sizes = new int[(int)count];
        var offset = 24;
        for (var index = 0; index < sizes.Length; index++)
        {
            var value = 0L;
            var groups = 0;
            while (true)
            {
                if (offset >= packetTable.Length || groups++ == 10)
                {
                    throw new CafException(LiarUtil.Core.Strings.PaktPacketSizeInvalid);
                }

                var item = packetTable[offset++];
                value = value * 128 + (item & 127);
                if (value > uint.MaxValue)
                {
                    throw new CafException(LiarUtil.Core.Strings.PacketTooLarge);
                }

                if ((item & 128) == 0)
                {
                    break;
                }
            }

            sizes[index] = (int)value;
        }

        var total = 0L;
        foreach (var size in sizes)
        {
            total += size;
        }

        if (total != dataLength)
        {
            throw new CafException(LiarUtil.Core.Strings.PaktPacketSizeDoesNotMatchDataLength);
        }

        return sizes;
    }

    private static long ReadInt64(ReadOnlySpan<byte> data)
    {
        var high = BinaryPrimitives.ReadInt32BigEndian(data);
        var low = BinaryPrimitives.ReadUInt32BigEndian(data[4..]);
        if (high < 0)
        {
            return -1;
        }

        if (high > 0x1FFFFF)
        {
            throw new CafException(LiarUtil.Core.Strings.ChunkTooLarge);
        }

        return high * 4294967296L + low;
    }

    private static string ReadFourCc(ReadOnlySpan<byte> data, int offset) =>
        new([(char)data[offset], (char)data[offset + 1], (char)data[offset + 2], (char)data[offset + 3]]);

    private static bool Matches(ReadOnlySpan<byte> data, int offset, string value) =>
        offset + value.Length <= data.Length && ReadFourCc(data, offset) == value;
}
