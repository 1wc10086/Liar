namespace LiarUtil.Core.Arcv;

internal sealed record ArcvPackOptions
{
    public required string InputFolder { get; init; }

    public required string OutputPath { get; init; }
}
