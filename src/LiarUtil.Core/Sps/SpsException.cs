namespace LiarUtil.Core.Sps;

public sealed class SpsException(string message, Exception? innerException = null)
    : Exception(message, innerException);
