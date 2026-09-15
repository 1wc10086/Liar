using System.Text;

using LiarUtil.Core.Services;
namespace LiarUtil.Gui;

public sealed class CrashHandler : Java.Lang.Object, Java.Lang.Thread.IUncaughtExceptionHandler
{
    private readonly CrashLogService _crashLog;
    private readonly Java.Lang.Thread.IUncaughtExceptionHandler? _previous;

    public CrashHandler(CrashLogService crashLog, Java.Lang.Thread.IUncaughtExceptionHandler? previous)
    {
        _crashLog = crashLog;
        _previous = previous;
    }

    public void UncaughtException(Java.Lang.Thread thread, Java.Lang.Throwable error)
    {
        _crashLog.Write("Java.UncaughtException", ToException(error), true);
        _previous?.UncaughtException(thread, error);
    }

    public static Exception ToException(Java.Lang.Throwable? throwable) =>
        throwable is null ? new Exception("(null)") : new JavaException(throwable);

    private sealed class JavaException(Java.Lang.Throwable throwable) : Exception
    {
        public override string Message => throwable.Message ?? throwable.GetType().Name;

        public override string? StackTrace
        {
            get
            {
                var builder = new StringBuilder(throwable.StackTrace ?? "");
                for (var cause = throwable.Cause;
                     cause is not null && !ReferenceEquals(cause, throwable);
                     cause = cause.Cause)
                {
                    builder.AppendLine().Append("Caused by: ").Append(cause.ToString());
                }
                return builder.ToString();
            }
        }
    }
}
