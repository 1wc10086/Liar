namespace LiarUtil.Core.Wem;

public sealed class WemException(string message, Exception? innerException = null)
    : Exception(message, innerException);
