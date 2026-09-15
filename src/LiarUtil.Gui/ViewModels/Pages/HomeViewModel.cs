using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiarUtil.Core.Particles;
using LiarUtil.Core.PopFx;
using LiarUtil.Core.Reanim;
using LiarUtil.Core.Services;
using LiarUtil.Core.Trail;
using LiarUtil.Gui.Services;
using LiarUtil.Gui.ViewModels.Options;

namespace LiarUtil.Gui.ViewModels.Pages;

public sealed partial class HomeViewModel : ObservableObject
{
    private const int TrailFunction = 9;
    private const int ReanimFunction = 10;
    private const int DzFunction = 11;
    private const int PakFunction = 12;
    private const int ArcvFunction = 13;
    private const int XprFunction = 14;
    private const int AtlasFunction = 15;
    private const int NewtonFunction = 16;
    private const int RsbPatchFunction = 17;
    private const int FontWidgetDatFunction = 18;
    private const int PaxFunction = 19;
    private const int WemFunction = 20;
    private const int TextTableFunction = 21;
    private const int SpsFunction = 22;
    private const int SnrFunction = 23;
    private const int CafFunction = 24;
    private const int WmaFunction = 25;
    private const int XmFunction = 26;
    private const int XmaFunction = 27;
    private const int XnbAudioFunction = 28;
    private const int XnbFontFunction = 29;
    private const int Cfu2Function = 30;

    private readonly SnackbarService _snackbar;
    private readonly AppSettings _settings;
    private readonly FilePickerService _pickers;
    private readonly IProcessingService _processing;

    private readonly ComboBoxOptionViewModel _rtonEncoding = new() { Items = ["UTF-8", "EASCII"] };
    private readonly SegmentedOptionViewModel _textureMode = new("", "");
    private readonly SwitchOptionViewModel _textureHeader = new() { IsChecked = true };
    private readonly TextOptionViewModel _textureWidth = new();
    private readonly TextOptionViewModel _textureHeight = new();
    private readonly ComboBoxOptionViewModel _textureFormat = new() { Items = Catalog.TextureFormatsByMode[0] };
    private readonly ComboBoxOptionViewModel _rsbVersion = new() { Items = Catalog.RsbVersions, SelectedIndex = 1 };
    private readonly SwitchOptionViewModel _rsbExportResources = new() { IsChecked = true };
    private readonly SwitchOptionViewModel _rsbWriteTextureHeader = new() { IsChecked = true };
    private readonly ComboBoxOptionViewModel _pamVersion = new() { Items = ["1", "2", "3", "4", "5", "6"], SelectedIndex = 5 };
    private readonly TextOptionViewModel _pamXflResolution = new() { Text = "768" };
    private readonly ComboBoxOptionViewModel _bnkVersion = new() { Items = Catalog.BnkVersions, SelectedIndex = Catalog.BnkVersions.Length - 1 };
    private readonly ComboBoxOptionViewModel _ppfVersion = new() { Items = Catalog.PpfVersions };
    private readonly ComboBoxOptionViewModel _popFxNumber = new() { Items = Catalog.PopFxNumbers };
    private readonly ComboBoxOptionViewModel _popFxVariant = new() { Items = Catalog.PopFxVariants, SelectedIndex = Catalog.PopFxVariants.Length - 1 };
    private readonly SwitchOptionViewModel _particleCompression = new() { IsChecked = true };
    private readonly SwitchOptionViewModel _particleXml = new();
    private readonly ComboBoxOptionViewModel _particlePlatform = new() { Items = Catalog.ParticlePlatforms };
    private readonly SwitchOptionViewModel _trailCompression = new() { IsChecked = true };
    private readonly SwitchOptionViewModel _trailXml = new();
    private readonly ComboBoxOptionViewModel _trailPlatform = new() { Items = Catalog.TrailPlatforms };
    private readonly SwitchOptionViewModel _reanimCompression = new() { IsChecked = true };
    private readonly SwitchOptionViewModel _reanimXml = new();
    private readonly ComboBoxOptionViewModel _reanimPlatform = new() { Items = Catalog.ReanimPlatforms };
    private readonly TextOptionViewModel _atlasInfoPath = new() { Text = "" };
    private readonly ComboBoxOptionViewModel _atlasFormat = new() { Items = Catalog.AtlasFormats };
    private readonly TextOptionViewModel _atlasWidth = new() { Text = "2048" };
    private readonly TextOptionViewModel _atlasHeight = new() { Text = "2048" };
    private readonly TextOptionViewModel _rsbPatchPath = new() { Text = "" };
    private readonly ComboBoxOptionViewModel _rsbPatchNumber = new() { Items = Catalog.RsbPatchNumbers };
    private readonly SwitchOptionViewModel _rsbPatchRawPacket = new();
    private readonly ComboBoxOptionViewModel _textTableVersion = new() { Items = Catalog.TextTableVersions };

    [ObservableProperty]
    private int _selectedFunction;

    [ObservableProperty]
    private int _selectedMode;

    [ObservableProperty]
    private string _inputPath = "";

    [ObservableProperty]
    private string _outputPath = "";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusText = "";

    [ObservableProperty]
    private IReadOnlyList<string> _modes = [];

    [ObservableProperty]
    private string _inputLabel = "";

    [ObservableProperty]
    private string _outputLabel = "";

    public LocalizationService Loc { get; }

    public IReadOnlyList<string> Functions { get; } =
    [
        "Rton", "Texture", "Rsb", "Bnk", "Pam", "Ppf", "Popfx", "Cfw2", "Particle", "Trail", "Reanim",
        "Dz", "Pak", "Arcv", "Xpr", "Atlas", "Newton", "RsbPatch", "FontWidgetDat", "Pax",
        "Wem", "TextTable", "Sps", "Snr", "Caf", "Wma", "Xm", "Xma", "Xnb-Audio", "Xnb-Fonts", "Cfu2",
    ];

    public ObservableCollection<OptionViewModel> Options { get; } = [];

    public HomeViewModel(SnackbarService snackbar, AppSettings settings, LocalizationService loc, FilePickerService pickers, IProcessingService processing)
    {
        _snackbar = snackbar;
        _settings = settings;
        _pickers = pickers;
        _processing = processing;
        Loc = loc;
        loc.LanguageChanged += OnLanguageChanged;

        foreach (var item in _textureMode.Items)
        {
            item.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(SegmentedItemViewModel.IsChecked))
                {
                    RefreshVisibility();
                }
            };
        }

        _textureHeader.PropertyChanged += (_, e) => RefreshOn(e, nameof(SwitchOptionViewModel.IsChecked));
        _particlePlatform.PropertyChanged += (_, e) => RefreshOn(e, nameof(ComboBoxOptionViewModel.SelectedIndex));
        _trailPlatform.PropertyChanged += (_, e) => RefreshOn(e, nameof(ComboBoxOptionViewModel.SelectedIndex));
        _reanimPlatform.PropertyChanged += (_, e) => RefreshOn(e, nameof(ComboBoxOptionViewModel.SelectedIndex));
        _pamXflResolution.PropertyChanged += (_, e) => RefreshOn(e, nameof(TextOptionViewModel.Text));

        ApplyTexts();
        OnSelectedFunctionChanged(0);
    }

    private bool IsTextureDecode => _textureMode.Items[0].IsChecked;

    private bool IsInputDirectory => SelectedFunction switch
    {
        2 or DzFunction or PakFunction or ArcvFunction or XprFunction => SelectedMode == 1,
        4 => SelectedMode == 3,
        AtlasFunction => SelectedMode == 1,
        _ => false,
    };

    private bool IsOutputDirectory => SelectedFunction switch
    {
        2 or DzFunction or PakFunction or ArcvFunction or XprFunction => SelectedMode == 0,
        4 => SelectedMode == 2,
        AtlasFunction => SelectedMode == 0,
        _ => false,
    };

    private ParticlePlatform SelectedParticlePlatform => _particlePlatform.SelectedIndex switch
    {
        1 => ParticlePlatform.Phone32,
        2 => ParticlePlatform.Phone64,
        3 => ParticlePlatform.Wp,
        4 => ParticlePlatform.GameConsole,
        5 => ParticlePlatform.Tv,
        _ => ParticlePlatform.Pc,
    };

    private TrailPlatform SelectedTrailPlatform => _trailPlatform.SelectedIndex switch
    {
        1 => TrailPlatform.Phone32,
        2 => TrailPlatform.Phone64,
        3 => TrailPlatform.Wp,
        4 => TrailPlatform.GameConsole,
        5 => TrailPlatform.Tv,
        _ => TrailPlatform.Pc,
    };

    private ReanimPlatform SelectedReanimPlatform => _reanimPlatform.SelectedIndex switch
    {
        1 => ReanimPlatform.Phone32,
        2 => ReanimPlatform.Phone64,
        3 => ReanimPlatform.Wp,
        4 => ReanimPlatform.GameConsole,
        5 => ReanimPlatform.Tv,
        6 => ReanimPlatform.FlashXfl,
        _ => ReanimPlatform.Pc,
    };

    private string SelectedTextureFormat
    {
        get
        {
            var formats = Catalog.TextureFormatsByMode[SelectedMode];
            var index = _textureFormat.SelectedIndex;
            return index >= 0 && index < formats.Length ? formats[index] : "";
        }
    }

    private int SelectedRsbVersion => _rsbVersion.SelectedIndex switch
    {
        0 => 1,
        2 => 4,
        _ => 3,
    };

    partial void OnSelectedFunctionChanged(int value)
    {
        Modes = value switch
        {
            1 => Catalog.TextureModes,
            2 or DzFunction or PakFunction or ArcvFunction or XprFunction => [Loc.Unpack, Loc.Pack],
            3 => [Loc.Decode, Loc.Encode],
            4 => [Loc.Decode, Loc.Encode, Loc.ToFlash, Loc.FromFlash],
            5 => [Loc.Decode, Loc.Encode],
            6 => [Loc.Decode, Loc.Encode],
            7 => [Loc.Decode, Loc.Encode],
            8 => [Loc.Decode, Loc.Encode],
            TrailFunction => [Loc.Decode, Loc.Encode],
            ReanimFunction => [Loc.Decode, Loc.Encode],
            AtlasFunction => [Loc.Split, Loc.Merge],
            NewtonFunction => [Loc.Decode, Loc.Encode],
            RsbPatchFunction => [Loc.Decode, Loc.Encode],
            FontWidgetDatFunction => [Loc.Decode, Loc.Encode],
            PaxFunction => [Loc.Decode, Loc.Encode],
            WemFunction => [Loc.Decode],
            TextTableFunction => [Loc.Convert],
            SpsFunction => [Loc.Decode, Loc.Encode],
            SnrFunction => [Loc.Decode, Loc.Encode],
            CafFunction => [Loc.Decode],
            WmaFunction => [Loc.Decode],
            XmFunction => [Loc.Decode],
            XmaFunction => [Loc.Decode, Loc.Encode],
            XnbAudioFunction => [Loc.Decode],
            XnbFontFunction => [Loc.Decode, Loc.Encode],
            Cfu2Function => [Loc.Decode, Loc.Encode],
            _ => [Loc.Decode, Loc.Encode, Loc.Encrypt, Loc.Decrypt],
        };
        SelectedMode = 0;
        InputLabel = value == RsbPatchFunction ? Loc.OriginalFile : Loc.InputPath;
        OutputLabel = value == RsbPatchFunction ? Loc.ModifiedFile : Loc.OutputPath;
        ReplaceOptions(value);
        RefreshVisibility();
    }

    partial void OnSelectedModeChanged(int value)
    {
        if (SelectedFunction == 1)
        {
            _textureFormat.Items = Catalog.TextureFormatsByMode[value];
            _textureFormat.SelectedIndex = 0;
        }
        RefreshVisibility();
    }

    [RelayCommand]
    private async Task BrowseInputAsync()
    {
        var path = IsInputDirectory ? await _pickers.PickFolderAsync() : await _pickers.PickFileAsync();
        if (!string.IsNullOrEmpty(path))
        {
            InputPath = path;
        }
    }

    [RelayCommand]
    private async Task BrowseOutputAsync()
    {
        var path = IsOutputDirectory ? await _pickers.PickFolderAsync() : await _pickers.SaveFileAsync();
        if (!string.IsNullOrEmpty(path))
        {
            OutputPath = path;
        }
    }

    [RelayCommand]
    private async Task ProcessAsync()
    {
        if (IsBusy)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(InputPath) || string.IsNullOrWhiteSpace(OutputPath))
        {
            _snackbar.Show(Loc.FillInputOutputPaths);
            return;
        }

        if (SelectedFunction == AtlasFunction && string.IsNullOrWhiteSpace(_atlasInfoPath.Text))
        {
            _snackbar.Show(Loc.FillInfoPath);
            return;
        }

        if (SelectedFunction == RsbPatchFunction && string.IsNullOrWhiteSpace(_rsbPatchPath.Text))
        {
            _snackbar.Show(Loc.FillPatchPath);
            return;
        }

        var request = new ProcessingRequest
        {
            Function = SelectedFunction,
            Mode = SelectedMode,
            InputPath = InputPath,
            OutputPath = OutputPath,
            XflOptions = _settings.ToXflOptions(),
            InfoPath = _atlasInfoPath.Text,
            PatchPath = _rsbPatchPath.Text,
            UseRawPacket = _rsbPatchRawPacket.IsChecked,
            Decode = IsTextureDecode,
            UseHeader = _textureHeader.IsChecked,
            ConvertImages = _settings.RsbConvertImages,
            DeleteAfterConvert = _settings.RsbDeleteAfterConvert,
            ExportResources = _rsbExportResources.IsChecked,
            WriteTextureHeader = _rsbWriteTextureHeader.IsChecked,
            Encoding = _rtonEncoding.SelectedIndex,
            Width = ParseInt(SelectedFunction == AtlasFunction ? _atlasWidth.Text : _textureWidth.Text),
            Height = ParseInt(SelectedFunction == AtlasFunction ? _atlasHeight.Text : _textureHeight.Text),
            Version = _pamVersion.SelectedIndex + 1,
            PamXflResolution = ParseInt(_pamXflResolution.Text),
            PpfVersion = _ppfVersion.SelectedIndex + 1,
            Variant = ParsePopFxVariant(),
            RsbVersion = SelectedRsbVersion,
            BnkVersion = ParseBnkVersion(),
            AtlasFormat = _atlasFormat.SelectedIndex,
            TextureFormat = SelectedTextureFormat,
            Ptx0Format = _settings.Ptx0Format,
            RsbPtxFormat = _settings.RsbPtxFormat,
            RtonKey = _settings.RtonKey,
            CdatKey = _settings.CdatKey,
            ParticlePlatform = SelectedParticlePlatform,
            TrailPlatform = SelectedTrailPlatform,
            ReanimPlatform = SelectedReanimPlatform,
            TextTableVersion = _textTableVersion.SelectedIndex,
            UseXml = IsXmlSelected,
            UseCompression = IsCompressionSelected,
        };

        IsBusy = true;
        StatusText = Loc.Processing;
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await _processing.ProcessAsync(request);
            StatusText = Loc.ProcessingDone;
            _snackbar.Show(string.Format(Loc.ElapsedSeconds, stopwatch.Elapsed.TotalSeconds));
        }
        catch (Exception exception)
        {
            StatusText = Loc.ProcessingFailed;
            _snackbar.Show(string.Format(Loc.ProcessingFailedMessage, exception.Message));
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool IsXmlSelected => SelectedFunction switch
    {
        8 => _particleXml.IsChecked,
        TrailFunction => _trailXml.IsChecked,
        ReanimFunction => _reanimXml.IsChecked,
        _ => false,
    };

    private bool IsCompressionSelected => SelectedFunction switch
    {
        8 => _particleCompression.IsChecked,
        TrailFunction => _trailCompression.IsChecked,
        ReanimFunction => _reanimCompression.IsChecked,
        _ => true,
    };

    private void ReplaceOptions(int function)
    {
        Options.Clear();
        foreach (var option in GetOptions(function))
        {
            Options.Add(option);
        }
    }

    private IEnumerable<OptionViewModel> GetOptions(int function) => function switch
    {
        0 => [_rtonEncoding],
        1 => [_textureMode, _textureHeader, _textureWidth, _textureHeight, _textureFormat],
        2 => [_rsbVersion, _rsbExportResources, _rsbWriteTextureHeader],
        3 => [_bnkVersion],
        4 => [_pamVersion, _pamXflResolution],
        5 => [_ppfVersion],
        6 => [_popFxNumber, _popFxVariant],
        7 => [],
        8 => [_particleCompression, _particleXml, _particlePlatform],
        TrailFunction => [_trailCompression, _trailXml, _trailPlatform],
        ReanimFunction => [_reanimCompression, _reanimXml, _reanimPlatform],
        AtlasFunction => [_atlasInfoPath, _atlasFormat, _atlasWidth, _atlasHeight],
        RsbPatchFunction => [_rsbPatchPath, _rsbPatchNumber, _rsbPatchRawPacket],
        TextTableFunction => [_textTableVersion],
        XnbAudioFunction or XnbFontFunction or Cfu2Function => [],
        _ => [],
    };

    private void RefreshVisibility()
    {
        foreach (var option in Options)
        {
            option.IsVisible = IsOptionVisible(option);
        }
    }

    private bool IsOptionVisible(OptionViewModel option) => SelectedFunction switch
    {
        0 => option == _rtonEncoding && SelectedMode is 0 or 1,
        4 => IsPamOptionVisible(option),
        1 => option switch
        {
            TextOptionViewModel => IsTextureDecode && !_textureHeader.IsChecked,
            ComboBoxOptionViewModel => !IsTextureDecode || !_textureHeader.IsChecked,
            _ => true,
        },
        2 => option == _rsbVersion || SelectedMode == 0,
        8 => option != _particleCompression || SelectedParticlePlatform != ParticlePlatform.Wp,
        TrailFunction => option != _trailCompression || SelectedTrailPlatform != TrailPlatform.Wp,
        ReanimFunction => IsReanimOptionVisible(option),
        AtlasFunction => IsAtlasOptionVisible(option),
        _ => true,
    };

    private bool IsPamOptionVisible(OptionViewModel option) => option == _pamXflResolution
        ? SelectedMode == 3
        : SelectedMode <= 2;

    private bool IsReanimOptionVisible(OptionViewModel option)
    {
        if (SelectedReanimPlatform == ReanimPlatform.FlashXfl)
        {
            return option is ComboBoxOptionViewModel;
        }
        return option != _reanimCompression || SelectedReanimPlatform != ReanimPlatform.Wp;
    }

    private bool IsAtlasOptionVisible(OptionViewModel option) =>
        option != _atlasWidth && option != _atlasHeight || SelectedMode == 1;

    private void RefreshOn(PropertyChangedEventArgs e, string propertyName)
    {
        if (e.PropertyName == propertyName)
        {
            RefreshVisibility();
        }
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        ApplyTexts();
        OnSelectedFunctionChanged(SelectedFunction);
    }

    private void ApplyTexts()
    {
        _rtonEncoding.Header = Loc.StringEncoding;
        _textureMode.Header = Loc.Operation;
        _textureMode.Items[0].Content = Loc.Decode;
        _textureMode.Items[1].Content = Loc.Encode;
        _textureHeader.Header = Loc.UseFileHeaderInfo;
        _textureWidth.Header = Loc.Length;
        _textureWidth.Watermark = Loc.Length;
        _textureHeight.Header = Loc.Width;
        _textureHeight.Watermark = Loc.Width;
        _textureFormat.Header = Loc.TextureFormat;
        _rsbVersion.Header = Loc.Version;
        _rsbExportResources.Header = Loc.ExportResources;
        _rsbWriteTextureHeader.Header = Loc.WriteTextureHeader;
        _pamVersion.Header = Loc.Version;
        _pamXflResolution.Header = Loc.Resolution;
        _pamXflResolution.Watermark = Loc.Resolution;
        _bnkVersion.Header = Loc.Version;
        _ppfVersion.Header = Loc.Version;
        _popFxNumber.Header = Loc.Version;
        _popFxVariant.Header = Loc.VersionVariant;
        _particleCompression.Header = Loc.UseCompression;
        _particleXml.Header = Loc.UseXml;
        _particlePlatform.Header = Loc.Platform;
        _trailCompression.Header = Loc.UseCompression;
        _trailXml.Header = Loc.UseXml;
        _trailPlatform.Header = Loc.Platform;
        _reanimCompression.Header = Loc.UseCompression;
        _reanimXml.Header = Loc.UseXml;
        _reanimPlatform.Header = Loc.Platform;
        _atlasInfoPath.Header = Loc.InfoFile;
        _atlasInfoPath.Watermark = Loc.InfoFilePath;
        _atlasFormat.Header = Loc.InfoFormat;
        _atlasWidth.Header = Loc.Length;
        _atlasWidth.Watermark = Loc.Length;
        _atlasHeight.Header = Loc.Width;
        _atlasHeight.Watermark = Loc.Width;
        _rsbPatchPath.Header = Loc.PatchFile;
        _rsbPatchPath.Watermark = Loc.PatchFilePath;
        _rsbPatchNumber.Header = Loc.VersionNumber;
        _rsbPatchRawPacket.Header = Loc.UseRawPacket;
        _textTableVersion.Header = Loc.TargetVersion;
    }

    private PopFxVariant ParsePopFxVariant()
    {
        var index = _popFxVariant.SelectedIndex;
        return index is 0 or 1 ? (PopFxVariant)(index + 1) : PopFxVariant.V3;
    }

    private uint ParseBnkVersion()
    {
        var index = _bnkVersion.SelectedIndex;
        if (index < 0 || index >= Catalog.BnkVersions.Length)
        {
            return 150;
        }
        return uint.Parse(Catalog.BnkVersions[index]);
    }

    private static int ParseInt(string value) => int.TryParse(value, out var result) ? result : 0;
}
