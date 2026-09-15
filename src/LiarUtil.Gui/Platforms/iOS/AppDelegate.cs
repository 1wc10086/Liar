using Avalonia;
using Avalonia.iOS;
using Foundation;

namespace LiarUtil.Gui;

[Register(nameof(AppDelegate))]
public partial class AppDelegate : AvaloniaAppDelegate<App>
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder) =>
        base.CustomizeAppBuilder(builder)
            .WithInterFont()
            .With(FontDefaults.Options);
}
