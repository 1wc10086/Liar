using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using LiarUtil.Core.Services;
using Avalonia;
using Avalonia.Android;

namespace LiarUtil.Gui;

[Application]
public class MainApplication : AvaloniaAndroidApplication<App>
{
    public MainApplication(nint javaReference, JniHandleOwnership transfer)
        : base(javaReference, transfer)
    {
    }

    public override void OnCreate()
    {
        base.OnCreate();
        InstallCrashHandler();
    }

    private void InstallCrashHandler()
    {
        var crashLog = new CrashLogService();
        crashLog.Install();
        AndroidEnvironment.UnhandledExceptionRaiser += (_, e) =>
            crashLog.Write("AndroidEnvironment.UnhandledException", e.Exception, !e.Handled);
        Java.Lang.Thread.DefaultUncaughtExceptionHandler =
            new CrashHandler(crashLog, Java.Lang.Thread.DefaultUncaughtExceptionHandler);
    }

    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder)
            .WithInterFont()
            .With(FontDefaults.Options);
    }
}

[Activity(
    Theme = "@style/AppTheme",
    MainLauncher = true,
    Exported = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity
{
    protected override void OnResume()
    {
        base.OnResume();

        if (OperatingSystem.IsAndroidVersionAtLeast(30))
        {
            if (!Android.OS.Environment.IsExternalStorageManager)
            {
                var intent = new Intent(Android.Provider.Settings.ActionManageAppAllFilesAccessPermission,
                    Android.Net.Uri.Parse($"package:{PackageName}"));
                StartActivity(intent);
            }
        }
        else if (Build.VERSION.SdkInt >= BuildVersionCodes.M &&
                 CheckSelfPermission(Android.Manifest.Permission.WriteExternalStorage) != Permission.Granted)
        {
            RequestPermissions([Android.Manifest.Permission.WriteExternalStorage], 0);
        }
    }
}
