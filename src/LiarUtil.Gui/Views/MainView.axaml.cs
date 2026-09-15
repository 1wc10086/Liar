using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using LiarUtil.Gui.Services;
using Material3.Avalonia.Controls;

namespace LiarUtil.Gui.Views;

public partial class MainView : UserControl
{
    private const double WideLayoutBreakpoint = 600;

    private FilePickerService? _pickers;

    public MainView()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        if (TopLevel.GetTopLevel(this) is { } topLevel)
        {
            _pickers?.Attach(topLevel);
        }

        if (TopLevel.GetTopLevel(this) is { } topLevel2 && topLevel2.InsetsManager is { } insets)
        {
            insets.SafeAreaChanged += OnSafeAreaChanged;
            insets.DisplayEdgeToEdgePreference = true;
            ApplySafeArea(insets.SafeAreaPadding);
        }
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);

        var wide = e.NewSize.Width >= WideLayoutBreakpoint;
        RailHost.IsVisible = wide;
        BottomBarHost.IsVisible = !wide;

        if (TopLevel.GetTopLevel(this)?.InsetsManager is { } insets)
        {
            ApplySafeArea(insets.SafeAreaPadding);
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        if (TopLevel.GetTopLevel(this)?.InsetsManager is { } insets)
        {
            insets.SafeAreaChanged -= OnSafeAreaChanged;
        }
    }

    public void AttachSnackbar(SnackbarService service)
    {
        service.Attach(this.FindControl<SnackbarHost>("SnackbarHost")!);
    }

    public void AttachPickers(FilePickerService service)
    {
        _pickers = service;
        if (TopLevel.GetTopLevel(this) is { } topLevel)
        {
            service.Attach(topLevel);
        }
    }

    private void OnSafeAreaChanged(object? sender, SafeAreaChangedArgs e)
    {
        ApplySafeArea(e.SafeAreaPadding);
    }

    private void ApplySafeArea(Thickness padding)
    {
        TopBarHost.Padding = new Thickness(0, padding.Top, 0, 0);
        BottomBarHost.Padding = new Thickness(0, 0, 0, padding.Bottom);
        RailHost.Padding = new Thickness(padding.Left, 0, 0, 0);
    }
}
