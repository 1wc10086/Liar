namespace LiarUtil.Core.Wma;

internal sealed class AsfInfo
{
    internal required int PacketSize { get; init; }

    internal required int MinPacketSize { get; init; }

    internal required int Preroll { get; init; }

    internal required int DataOffset { get; init; }

    internal required int CodecTag { get; init; }

    internal required int Channels { get; init; }

    internal required int SampleRate { get; init; }

    internal required int BitRate { get; init; }

    internal required int BlockAlign { get; init; }

    internal required byte[] ExtraData { get; init; }

    internal required int StreamId { get; init; }

    internal int Version => CodecTag == 0x160 ? 1 : 2;

    internal AsfInfo WithContainer(int packetSize, int minPacketSize, int preroll, int dataOffset) => new()
    {
        CodecTag = CodecTag,
        Channels = Channels,
        SampleRate = SampleRate,
        BitRate = BitRate,
        BlockAlign = BlockAlign,
        ExtraData = ExtraData,
        StreamId = StreamId,
        PacketSize = packetSize,
        MinPacketSize = minPacketSize,
        Preroll = preroll,
        DataOffset = dataOffset,
    };
}
