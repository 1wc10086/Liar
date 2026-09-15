namespace LiarUtil.Core.Audio;

public sealed class AudioException(string message, Exception? innerException = null)
    : Exception(message, innerException);
