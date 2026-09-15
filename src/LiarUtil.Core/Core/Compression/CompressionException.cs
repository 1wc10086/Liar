namespace LiarUtil.Core.Core.Compression;

public sealed class CompressionException(string message, Exception? innerException = null)
    : Exception(message, innerException);
