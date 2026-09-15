using CommunityToolkit.Mvvm.ComponentModel;

namespace LiarUtil.Gui.ViewModels.Options;

public abstract partial class OptionViewModel : ObservableObject
{
    [ObservableProperty]
    private string _header = "";

    [ObservableProperty]
    private bool _isVisible = true;
}
