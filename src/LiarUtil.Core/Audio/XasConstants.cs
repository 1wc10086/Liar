namespace LiarUtil.Core.Audio;

internal static class XasConstants
{
    public const int SamplesPerChunk = 128;
    public const int SamplesPerSubChunk = 32;
    public const int SubChunkCount = 4;
    public const int ChunkSize = 76;
    public const int SamplesPerEncodedPair = 2;

    public static readonly short[][] Coefficients =
    [
        [0, 0],
        [240, 0],
        [460, -208],
        [392, -220],
    ];
}
