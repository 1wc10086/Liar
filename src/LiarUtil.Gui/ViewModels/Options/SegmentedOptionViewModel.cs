using CommunityToolkit.Mvvm.ComponentModel;

namespace LiarUtil.Gui.ViewModels.Options;

public sealed partial class SegmentedOptionViewModel : OptionViewModel
{
    public SegmentedOptionViewModel(params string[] contents)
    {
        Items = [.. contents.Select(content => new SegmentedItemViewModel(content, this))];
    }

    public IReadOnlyList<SegmentedItemViewModel> Items { get; }

    public int SelectedIndex
    {
        get
        {
            for (var i = 0; i < Items.Count; i++)
            {
                if (Items[i].IsChecked)
                {
                    return i;
                }
            }
            return -1;
        }
        set
        {
            for (var i = 0; i < Items.Count; i++)
            {
                Items[i].IsChecked = i == value;
            }
        }
    }

    internal void HandleChecked(SegmentedItemViewModel item)
    {
        foreach (var other in Items)
        {
            if (other != item)
            {
                other.IsChecked = false;
            }
        }
    }
}

public sealed partial class SegmentedItemViewModel : ObservableObject
{
    private readonly SegmentedOptionViewModel? _owner;

    public SegmentedItemViewModel(string content, SegmentedOptionViewModel? owner)
    {
        _content = content;
        _owner = owner;
    }

    [ObservableProperty]
    private string _content = "";

    [ObservableProperty]
    private bool _isChecked;

    partial void OnIsCheckedChanged(bool value)
    {
        if (value)
        {
            _owner?.HandleChecked(this);
        }
    }
}
