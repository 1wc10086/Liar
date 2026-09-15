namespace LiarUtil.Core.Core.Binary;

public class BinaryException(string message, Exception? innerException = null) : Exception(message, innerException);

public static class BinaryErrors
{
    public static Func<string, Exception> Throw { get; } = static message => new BinaryException(message);

    public static Func<string, Exception> Factory<TException>() where TException : Exception =>
        static message => (TException)Activator.CreateInstance(typeof(TException), message)!;

    public static string Truncated(long offset, long needed, long remaining) =>
        string.Format(LiarUtil.Core.Strings.InsufficientDataLengthOffset0Requires1Bytes, offset, needed, remaining);

    public static string OutOfRange(long offset, long count, long length) =>
        string.Format(LiarUtil.Core.Strings.DataOutOfRangeOffset0Length1, offset, count, length);

    public static string PositionOutOfRange(long position, long length) =>
        string.Format(LiarUtil.Core.Strings.PositionOutOfRange01, position, length);

    public static string MarkerMismatch(long actual, long expected) =>
        string.Format(LiarUtil.Core.Strings.FormatMarkerMismatch0x0X8Expected0x, actual, expected);

    public static string MarkerMismatch() => LiarUtil.Core.Strings.FormatMarkerMismatch;

    public static string VarIntOverflow() => LiarUtil.Core.Strings.VarIntValueTooLarge;

    public static string TooLarge(long size) => string.Format(LiarUtil.Core.Strings.DataTooLarge0BytesExceedsMemoryLimit, size);
}
