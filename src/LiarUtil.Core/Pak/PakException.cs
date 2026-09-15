namespace LiarUtil.Core.Pak;

public sealed class PakException(string message, Exception? innerException = null)
    : Exception(message, innerException);
