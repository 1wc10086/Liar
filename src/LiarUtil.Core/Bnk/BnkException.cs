namespace LiarUtil.Core.Bnk;

public sealed class BnkException : Exception
{
    public BnkException(string message) : base(message) { }

    internal static BnkException Truncated(string context, long offset, long needed, long remaining) =>
        new(string.Format(LiarUtil.Core.Strings.N0TruncatedAtOffset1Need2Bytes, context, offset, needed, remaining));

    internal static BnkException Invalid(string context, long offset, string message) =>
        new(string.Format(LiarUtil.Core.Strings.N0InvalidAtOffset12, context, offset, message));

    internal static BnkException LimitExceeded(string resource, long requested, long limit) =>
        new(string.Format(LiarUtil.Core.Strings.N0ExceedsLimitRequested1Maximum2, resource, requested, limit));
}
