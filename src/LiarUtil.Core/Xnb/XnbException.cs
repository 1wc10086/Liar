namespace LiarUtil.Core.Xnb;

public sealed class XnbException(string message, Exception? innerException = null)
    : Exception(message, innerException);
