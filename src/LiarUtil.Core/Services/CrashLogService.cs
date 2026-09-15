using System.Runtime.InteropServices;
using System.Text;

namespace LiarUtil.Core.Services;

public sealed class CrashLogService
{
    private const int MaximumFiles = 10;

    public static string LogDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "crash");

    public void Install()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Write("AppDomain.UnhandledException", e.ExceptionObject as Exception, e.IsTerminating);
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Write("TaskScheduler.UnobservedTaskException", e.Exception);
            e.SetObserved();
        };
    }

    public void Write(string source, Exception? exception, bool isTerminating = false)
    {
        try
        {
            Directory.CreateDirectory(LogDirectory);
            var builder = new StringBuilder();
            builder.AppendLine(string.Format(LiarUtil.Core.Strings.Time0YyyyMMDdHHMmSs, DateTime.Now));
            builder.AppendLine(string.Format(LiarUtil.Core.Strings.Source0, source));
            builder.AppendLine(string.Format(LiarUtil.Core.Strings.Terminating0, isTerminating));
            builder.AppendLine(string.Format(LiarUtil.Core.Strings.ProcessArchitecture0, RuntimeInformation.ProcessArchitecture));
            builder.AppendLine(string.Format(LiarUtil.Core.Strings.RuntimeVersion0, Environment.Version));
            AppendException(builder, exception);
            File.WriteAllText(LogFilePath(), builder.ToString());
            Trim();
        }
        catch
        {
        }
    }

    private static string LogFilePath() =>
        Path.Combine(LogDirectory, $"crash-{DateTime.Now:yyyyMMdd-HHmmssfff}.log");

    private static void AppendException(StringBuilder builder, Exception? exception)
    {
        for (var depth = 0; exception is not null; depth++)
        {
            builder.AppendLine(depth == 0 ? LiarUtil.Core.Strings.ExceptionStack : LiarUtil.Core.Strings.InnerException);
            builder.AppendLine(string.Format(LiarUtil.Core.Strings.Type0, exception.GetType().FullName));
            builder.AppendLine(string.Format(LiarUtil.Core.Strings.Message0, exception.Message));
            builder.AppendLine(string.Format(LiarUtil.Core.Strings.Stack0, exception.StackTrace ?? LiarUtil.Core.Strings.None));
            exception = exception.InnerException;
        }
    }

    private static void Trim()
    {
        var files = Directory.GetFiles(LogDirectory, "crash-*.log");
        if (files.Length <= MaximumFiles)
        {
            return;
        }
        foreach (var file in files.OrderBy(File.GetCreationTime).Take(files.Length - MaximumFiles))
        {
            File.Delete(file);
        }
    }
}
