namespace AstcSharp.Encoding;

internal ref struct EndpointPreparationCache
{
    public const int Capacity = 17 * (32 + 16 + 16 + 144);
    private readonly Span<int> offsets;
    private readonly Span<int> storage;
    private int used;

    public EndpointPreparationCache(Span<int> offsets, Span<int> storage)
    {
        this.offsets = offsets;
        this.storage = storage;
        used = 0;
        Clear();
    }

    public void Clear()
    {
        offsets.Fill(-1);
        used = 0;
    }

    public bool Restore(int range, scoped Span<int> colors, scoped Span<int> low, scoped Span<int> high, scoped Span<int> weights)
    {
        var offset = offsets[range];
        if (offset < 0)
        {
            return false;
        }

        storage.Slice(offset, colors.Length).CopyTo(colors);
        offset += colors.Length;
        storage.Slice(offset, low.Length).CopyTo(low);
        offset += low.Length;
        storage.Slice(offset, high.Length).CopyTo(high);
        offset += high.Length;
        storage.Slice(offset, weights.Length).CopyTo(weights);
        return true;
    }

    public void Store(int range, scoped ReadOnlySpan<int> colors, scoped ReadOnlySpan<int> low, scoped ReadOnlySpan<int> high, scoped ReadOnlySpan<int> weights)
    {
        offsets[range] = used;
        colors.CopyTo(storage[used..]);
        used += colors.Length;
        low.CopyTo(storage[used..]);
        used += low.Length;
        high.CopyTo(storage[used..]);
        used += high.Length;
        weights.CopyTo(storage[used..]);
        used += weights.Length;
    }
}
