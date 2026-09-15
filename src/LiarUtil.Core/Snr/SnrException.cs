namespace LiarUtil.Core.Snr;

public sealed class SnrException(string message, Exception? innerException = null)
    : Exception(message, innerException);
