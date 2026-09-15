namespace LiarUtil.Core.Xma;

public sealed class XmaException(string message, Exception? innerException = null)
    : Exception(message, innerException);
