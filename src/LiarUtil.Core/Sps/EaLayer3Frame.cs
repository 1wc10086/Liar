namespace LiarUtil.Core.Sps;

internal sealed class EaLayer3Frame
{
    public required int Offset { get; init; }

    public required int FrameSize { get; init; }

    public required int PreSize { get; init; }

    public required int PcmSamples { get; init; }

    public required int PcmOffset { get; init; }

    public required int OffsetMode { get; init; }

    public required int SampleRate { get; init; }

    public required int Channels { get; init; }

    public required bool Mpeg1 { get; init; }

    public required int VersionIndex { get; init; }

    public required int RateIndex { get; init; }

    public required int ChannelMode { get; init; }

    public required int ModeExtension { get; init; }

    public required int Granule { get; init; }

    public required int DataBit { get; init; }

    public required int OtherBits { get; init; }

    public required int[] Scfsi { get; init; }

    public required int[] MainBits { get; init; }

    public required int[] OtherBits1 { get; init; }

    public required int[] OtherBits2 { get; init; }

    public required byte[] Data { get; init; }
}
