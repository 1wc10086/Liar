namespace LiarUtil.Core.Audio;

public sealed class AudioData
{
    public required int SampleRate { get; init; }

    public required short[] Samples { get; init; }

    public required int Channels { get; init; }

    public int FrameCount => Channels <= 0 ? 0 : Samples.Length / Channels;
}
