using LiarUtil.Core.Xnb.Audio;

namespace LiarUtil.Core.Xnb;

public sealed record SoundEffectData
{
    public required WaveFormat Format { get; init; }

    public required byte[] Data { get; init; }

    public required int LoopStart { get; init; }

    public required int LoopLength { get; init; }

    public required int DurationMilliseconds { get; init; }
}
