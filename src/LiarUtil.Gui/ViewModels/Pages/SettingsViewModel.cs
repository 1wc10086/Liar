using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiarUtil.Core.Services;
using LiarUtil.Gui.Services;

namespace LiarUtil.Gui.ViewModels.Pages;

public sealed partial class SettingsViewModel : ObservableObject
{
    public static readonly string[] XflLabelOptions = ["Image Name", "Short Name", "Label Name"];

    private readonly AppSettings _settings;
    private ValueDialog? _activeDialog;

    [ObservableProperty]
    private bool _isValueDialogOpen;

    [ObservableProperty]
    private string _valueDialogTitle = "";

    [ObservableProperty]
    private string _valueDialogWatermark = "";

    [ObservableProperty]
    private string _valueInput = "";

    [ObservableProperty]
    private bool _isValueInvalid;

    [ObservableProperty]
    private bool _rsbConvertImages;

    [ObservableProperty]
    private bool _rsbDeleteAfterConvert;

    public SettingsViewModel(AppSettings settings, LocalizationService loc)
    {
        _settings = settings;
        Loc = loc;
        _rsbConvertImages = settings.RsbConvertImages;
        _rsbDeleteAfterConvert = settings.RsbDeleteAfterConvert;
    }

    public LocalizationService Loc { get; }

    public string PamXflResolution => _settings.PamXflResolution.ToString(CultureInfo.InvariantCulture);

    public AppSettings Settings => _settings;

    public string RsbPtxFormat => _settings.RsbPtxFormat;

    public string Ptx0Format => _settings.Ptx0Format;

    public string RtonKey => _settings.RtonKey;

    public string CdatKey => _settings.CdatKey;

    public string XflWidth => Number(_settings.ReanimXflWidth);

    public string XflHeight => Number(_settings.ReanimXflHeight);

    public string XflScaleX => Number(_settings.ReanimXflScaleX);

    public string XflScaleY => Number(_settings.ReanimXflScaleY);

    public string XflLabelName => XflLabelOptions[_settings.ReanimXflLabelName + 1];

    partial void OnRsbConvertImagesChanged(bool value)
    {
        _settings.RsbConvertImages = value;
        _settings.Save();
    }

    partial void OnRsbDeleteAfterConvertChanged(bool value)
    {
        _settings.RsbDeleteAfterConvert = value;
        _settings.Save();
    }

    partial void OnValueInputChanged(string value) => IsValueInvalid = !IsValueValid(value);

    public void SetLanguage(int index)
    {
        Loc.SetLanguage(index == 1 ? AppSettings.LanguageChinese : AppSettings.LanguageEnglish);
        OnPropertyChanged(nameof(Loc));
    }

    public void OpenRtonKey() => Show(ValueDialog.ForText(Loc.RtonKeyHeadline, Loc.Key, _settings.RtonKey, SetRtonKey));

    public void OpenCdatKey() => Show(ValueDialog.ForText(Loc.CdatKeyHeadline, Loc.Key, _settings.CdatKey, SetCdatKey));

    public void OpenXflWidth() => Show(ValueDialog.ForNumber(Loc.CanvasWidth, Loc.CanvasWidth, XflWidth,
        value => _settings.ReanimXflWidth = (float)value));

    public void OpenXflHeight() => Show(ValueDialog.ForNumber(Loc.CanvasHeight, Loc.CanvasHeight, XflHeight,
        value => _settings.ReanimXflHeight = (float)value));

    public void OpenXflScaleX() => Show(ValueDialog.ForNumber(Loc.ScaleX, Loc.ScaleX, XflScaleX,
        value => _settings.ReanimXflScaleX = value));

    public void OpenXflScaleY() => Show(ValueDialog.ForNumber(Loc.ScaleY, Loc.ScaleY, XflScaleY,
        value => _settings.ReanimXflScaleY = value));

    public void OpenPamXflResolution() => Show(ValueDialog.ForNumber(Loc.PamXflResolution, Loc.Resolution, PamXflResolution,
        value => _settings.PamXflResolution = (int)value));

    public void SetRsbPtxFormat(string value)
    {
        _settings.RsbPtxFormat = value;
        _settings.Save();
        Refresh();
    }

    public void SetPtx0Format(string value)
    {
        _settings.Ptx0Format = value;
        _settings.Save();
        Refresh();
    }

    public void SetXflLabelName(int index)
    {
        if (index < 0 || index >= XflLabelOptions.Length)
        {
            return;
        }
        _settings.ReanimXflLabelName = index - 1;
        _settings.Save();
        Refresh();
    }

    [RelayCommand]
    private void ConfirmValue()
    {
        if (_activeDialog is null || !_activeDialog.Validate(ValueInput))
        {
            IsValueInvalid = true;
            return;
        }
        _activeDialog.Apply(ValueInput);
        _settings.Save();
        Refresh();
        IsValueDialogOpen = false;
    }

    [RelayCommand]
    private void CancelValue() => IsValueDialogOpen = false;

    private void Show(ValueDialog dialog)
    {
        _activeDialog = dialog;
        ValueDialogTitle = dialog.Title;
        ValueDialogWatermark = dialog.Watermark;
        ValueInput = dialog.Value;
        IsValueInvalid = false;
        IsValueDialogOpen = true;
    }

    private void SetRtonKey(string value)
    {
        _settings.RtonKey = value;
    }

    private void SetCdatKey(string value)
    {
        _settings.CdatKey = value;
    }

    private bool IsValueValid(string value) => _activeDialog?.Validate(value) ?? true;

    private void Refresh()
    {
        OnPropertyChanged(nameof(RsbPtxFormat));
        OnPropertyChanged(nameof(Ptx0Format));
        OnPropertyChanged(nameof(RtonKey));
        OnPropertyChanged(nameof(CdatKey));
        OnPropertyChanged(nameof(PamXflResolution));
        OnPropertyChanged(nameof(XflWidth));
        OnPropertyChanged(nameof(XflHeight));
        OnPropertyChanged(nameof(XflScaleX));
        OnPropertyChanged(nameof(XflScaleY));
        OnPropertyChanged(nameof(XflLabelName));
    }

    private static string Number(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    private static string Number(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
}
