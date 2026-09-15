namespace LiarUtil.Core.Xnb;

public sealed record XnbHeader
{
    public required char Platform { get; init; }

    public required int Version { get; init; }

    public required XnbFlags Flags { get; init; }

    public required uint FileSize { get; init; }

    public bool IsCompressed => Flags.HasFlag(XnbFlags.Compressed);
}
