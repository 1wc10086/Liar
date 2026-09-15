namespace LiarUtil.Core.Xnb;

internal static class XnbErrors
{
    public static Exception Throw(string message) => new XnbException(message);
}
