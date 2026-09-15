using CommunityToolkit.Mvvm.ComponentModel;

namespace LiarUtil.Gui.ViewModels.Options;

public sealed partial class SwitchOptionViewModel : OptionViewModel
{
    [ObservableProperty]
    private bool _isChecked;
}
