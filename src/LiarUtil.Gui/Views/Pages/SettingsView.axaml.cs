using Avalonia.Controls;
using Avalonia.Input;
using LiarUtil.Gui.ViewModels.Pages;

namespace LiarUtil.Gui.Views.Pages;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    private void OnLanguagePressed(object? sender, PointerPressedEventArgs e) =>
        ShowChoiceMenu(sender, [ViewModel.Loc.LanguageEnglish, ViewModel.Loc.LanguageChinese], value =>
            ViewModel.SetLanguage(value == ViewModel.Loc.LanguageChinese ? 1 : 0));

    private void OnRsbPtxPressed(object? sender, PointerPressedEventArgs e) =>
        ShowChoiceMenu(sender, ["ARGB", "ABGR", "ARGB_Padding"], value => ViewModel.SetRsbPtxFormat(value));

    private void OnPtx0Pressed(object? sender, PointerPressedEventArgs e) =>
        ShowChoiceMenu(sender, ["ARGB", "ABGR", "ARGB_Padding"], value => ViewModel.SetPtx0Format(value));

    private void OnRtonKeyPressed(object? sender, PointerPressedEventArgs e) => ViewModel.OpenRtonKey();

    private void OnCdatKeyPressed(object? sender, PointerPressedEventArgs e) => ViewModel.OpenCdatKey();

    private void OnPamXflResolutionPressed(object? sender, PointerPressedEventArgs e) => ViewModel.OpenPamXflResolution();

    private void OnXflWidthPressed(object? sender, PointerPressedEventArgs e) => ViewModel.OpenXflWidth();

    private void OnXflHeightPressed(object? sender, PointerPressedEventArgs e) => ViewModel.OpenXflHeight();

    private void OnXflScaleXPressed(object? sender, PointerPressedEventArgs e) => ViewModel.OpenXflScaleX();

    private void OnXflScaleYPressed(object? sender, PointerPressedEventArgs e) => ViewModel.OpenXflScaleY();

    private void OnXflLabelNamePressed(object? sender, PointerPressedEventArgs e) =>
        ShowChoiceMenu(sender, SettingsViewModel.XflLabelOptions, value => ViewModel.SetXflLabelName(IndexOf(value)));

    private SettingsViewModel ViewModel => (SettingsViewModel)DataContext!;

    private static int IndexOf(string value)
    {
        for (var index = 0; index < SettingsViewModel.XflLabelOptions.Length; index++)
        {
            if (SettingsViewModel.XflLabelOptions[index] == value)
            {
                return index;
            }
        }
        return 0;
    }

    private static void ShowChoiceMenu(object? sender, IReadOnlyList<string> options, Action<string> apply)
    {
        var flyout = new MenuFlyout();
        foreach (var option in options)
        {
            var item = new MenuItem { Header = option };
            item.Click += (_, _) => apply(option);
            flyout.Items.Add(item);
        }

        flyout.ShowAt((Control)sender!);
    }
}
