namespace LiarUtil.Core.Arcv;

internal sealed record ArcvUnpackOptions
{
    public required string InputPath { get; init; }

    public required string OutputFolder { get; init; }
}
