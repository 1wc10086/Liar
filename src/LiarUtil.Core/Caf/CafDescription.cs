namespace LiarUtil.Core.Caf;

internal sealed class CafDescription
{
    public required double SampleRate { get; init; }

    public required string Format { get; init; }

    public required uint Flags { get; init; }

    public required uint BytesPerPacket { get; init; }

    public required uint FramesPerPacket { get; init; }

    public required uint Channels { get; init; }

    public required uint Bits { get; init; }
}
