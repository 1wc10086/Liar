namespace AstcSharp;

public sealed record AstcEncoderOptions
{
    public int MaxDegreeOfParallelism { get; init; } = -1;

    public CancellationToken CancellationToken { get; init; }
}
