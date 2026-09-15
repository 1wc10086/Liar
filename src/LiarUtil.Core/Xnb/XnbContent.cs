namespace LiarUtil.Core.Xnb;

public sealed record XnbContent
{
    public required XnbHeader Header { get; init; }

    public required IReadOnlyList<XnbTypeReader> TypeReaders { get; init; }

    public required XnbTypeReader PrimaryReader { get; init; }

    public required byte[] Data { get; init; }

    public required int Offset { get; init; }

    public required int Length { get; init; }
}
