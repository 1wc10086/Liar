namespace LiarUtil.Core.RsbPatch;

public sealed class RsbPatchException(string message, Exception? innerException = null)
    : Exception(message, innerException);
