namespace LiarUtil.Core.PopFx.Encoding;

internal sealed class PopFxReferenceComparer<T> : IEqualityComparer<T> where T : class
{
    public static PopFxReferenceComparer<T> Instance { get; } = new();

    public bool Equals(T? left, T? right) => ReferenceEquals(left, right);

    public int GetHashCode(T value) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(value);
}
