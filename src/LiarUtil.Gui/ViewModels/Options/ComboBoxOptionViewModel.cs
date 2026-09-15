using CommunityToolkit.Mvvm.ComponentModel;

namespace LiarUtil.Gui.ViewModels.Options;

public sealed partial class ComboBoxOptionViewModel : OptionViewModel
{
    [ObservableProperty]
    private IReadOnlyList<string> _items = [];

    [ObservableProperty]
    private int _selectedIndex;
}
