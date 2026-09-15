namespace LiarUtil.Core.Caf;

public sealed class CafException(string message, Exception? innerException = null)
    : Exception(message, innerException);
