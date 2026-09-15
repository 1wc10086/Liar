using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using LiarUtil.Core.Services;
using LiarUtil.Gui.Services;
using LiarUtil.Gui.ViewModels.Pages;
using Material3.Avalonia.Icons;

namespace LiarUtil.Gui.ViewModels;

public sealed record NavigationDestination(string Label, PathIcon Icon);

public sealed partial class MainViewModel : ObservableObject
{
    private readonly IReadOnlyList<object> _pages;

    [ObservableProperty]
    private int _selectedIndex;

    [ObservableProperty]
    private object _currentPage;

    [ObservableProperty]
    private string _title;

    [ObservableProperty]
    private IReadOnlyList<NavigationDestination> _destinations;

    public LocalizationService Loc { get; }

    public MainViewModel(SnackbarService snackbarService, AppSettings settings, LocalizationService loc, FilePickerService pickers, IProcessingService processing)
    {
        Loc = loc;
        _pages =
        [
            new HomeViewModel(snackbarService, settings, loc, pickers, processing),
            new SettingsViewModel(settings, loc),
        ];

        _destinations = BuildDestinations();
        _currentPage = _pages[0];
        _title = loc.NavHome;
        loc.LanguageChanged += OnLanguageChanged;
    }

    partial void OnSelectedIndexChanged(int value)
    {
        CurrentPage = _pages[value];
        Title = value == 0 ? Loc.NavHome : Loc.NavSettings;
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        Destinations = BuildDestinations();
        Title = SelectedIndex == 0 ? Loc.NavHome : Loc.NavSettings;
    }

    private IReadOnlyList<NavigationDestination> BuildDestinations() =>
    [
        new(Loc.NavHome, new PathIcon { Data = MaterialSymbols.Home }),
        new(Loc.NavSettings, new PathIcon { Data = MaterialSymbols.Settings }),
    ];
}
