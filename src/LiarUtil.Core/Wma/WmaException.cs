namespace LiarUtil.Core.Wma;

public sealed class WmaException(string message, Exception? innerException = null)
    : Exception(message, innerException);
