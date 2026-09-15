namespace LiarUtil.Core.Xm;

public sealed class XmException(string message, Exception? innerException = null)
    : Exception(message, innerException);
