namespace LiarUtil.Core.Xnb.Audio;

public sealed record WaveFormat
{
    public required WaveFormatTag Tag { get; init; }

    public required int Channels { get; init; }

    public required int SampleRate { get; init; }

    public required int AverageBytesPerSecond { get; init; }

    public required int BlockAlign { get; init; }

    public required int BitsPerSample { get; init; }

    public required int SamplesPerBlock { get; init; }

    public required IReadOnlyList<AdpcmCoefficient> Coefficients { get; init; }
}
