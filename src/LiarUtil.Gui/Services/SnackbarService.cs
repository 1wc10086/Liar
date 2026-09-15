using Material3.Avalonia.Controls;

namespace LiarUtil.Gui.Services;

public sealed class SnackbarService
{
    private SnackbarHost? _host;

    public void Attach(SnackbarHost host)
    {
        _host = host;
    }

    public void Show(string message, string? actionText = null, Action? onAction = null)
    {
        _host?.Show(message, actionText, TimeSpan.FromSeconds(3), onAction);
    }
}
