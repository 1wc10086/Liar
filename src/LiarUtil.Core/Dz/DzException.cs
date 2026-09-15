namespace LiarUtil.Core.Dz;

public sealed class DzException(string message, Exception? innerException = null)
    : Exception(message, innerException);
