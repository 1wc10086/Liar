using CommunityToolkit.Mvvm.ComponentModel;

namespace LiarUtil.Gui.ViewModels.Options;

public sealed partial class TextOptionViewModel : OptionViewModel
{
    [ObservableProperty]
    private string _watermark = "";

    [ObservableProperty]
    private string _text = "0";
}
