using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using LiarUtil.Core.Services;

namespace LiarUtil.Gui.Services;

public sealed partial class LocalizationService : ObservableObject
{
    private readonly AppSettings _settings;

    public LocalizationService(AppSettings settings)
    {
        _settings = settings;
        ApplyCulture(settings.Language);
    }

    public event EventHandler? LanguageChanged;

    public string CurrentLanguageName => _settings.Language == AppSettings.LanguageChinese ? GuiStrings.LanguageChinese : GuiStrings.LanguageEnglish;

    public void SetLanguage(string language)
    {
        ApplyCulture(language);
        if (_settings.Language != language)
        {
            _settings.Language = language;
            _settings.Save();
        }

        OnPropertyChanged(string.Empty);
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    private static void ApplyCulture(string language)
    {
        var culture = language == AppSettings.LanguageChinese ? new CultureInfo("zh-Hans") : CultureInfo.InvariantCulture;
        GuiStrings.Culture = culture;
        LiarUtil.Core.Strings.Culture = culture;
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }

    public string NavHome => GuiStrings.NavHome;
    public string NavSettings => GuiStrings.NavSettings;
    public string Function => GuiStrings.Function;
    public string Mode => GuiStrings.Mode;
    public string InputPath => GuiStrings.InputPath;
    public string OutputPath => GuiStrings.OutputPath;
    public string OriginalFile => GuiStrings.OriginalFile;
    public string ModifiedFile => GuiStrings.ModifiedFile;
    public string StartProcessing => GuiStrings.StartProcessing;
    public string Processing => GuiStrings.Processing;
    public string ProcessingDone => GuiStrings.ProcessingDone;
    public string ProcessingFailed => GuiStrings.ProcessingFailed;
    public string ElapsedSeconds => GuiStrings.ElapsedSeconds;
    public string ProcessingFailedMessage => GuiStrings.ProcessingFailedMessage;
    public string FillInputOutputPaths => GuiStrings.FillInputOutputPaths;
    public string FillInfoPath => GuiStrings.FillInfoPath;
    public string FillPatchPath => GuiStrings.FillPatchPath;
    public string StringEncoding => GuiStrings.StringEncoding;
    public string Operation => GuiStrings.Operation;
    public string Decode => GuiStrings.Decode;
    public string Encode => GuiStrings.Encode;
    public string Encrypt => GuiStrings.Encrypt;
    public string Decrypt => GuiStrings.Decrypt;
    public string Unpack => GuiStrings.Unpack;
    public string Pack => GuiStrings.Pack;
    public string Split => GuiStrings.Split;
    public string Merge => GuiStrings.Merge;
    public string Convert => GuiStrings.Convert;
    public string ToFlash => GuiStrings.ToFlash;
    public string FromFlash => GuiStrings.FromFlash;
    public string UseFileHeaderInfo => GuiStrings.UseFileHeaderInfo;
    public string Length => GuiStrings.Length;
    public string Width => GuiStrings.Width;
    public string TextureFormat => GuiStrings.TextureFormat;
    public string Version => GuiStrings.Version;
    public string ExportResources => GuiStrings.ExportResources;
    public string WriteTextureHeader => GuiStrings.WriteTextureHeader;
    public string Resolution => GuiStrings.Resolution;
    public string VersionVariant => GuiStrings.VersionVariant;
    public string UseCompression => GuiStrings.UseCompression;
    public string UseXml => GuiStrings.UseXml;
    public string Platform => GuiStrings.Platform;
    public string InfoFile => GuiStrings.InfoFile;
    public string InfoFilePath => GuiStrings.InfoFilePath;
    public string InfoFormat => GuiStrings.InfoFormat;
    public string PatchFile => GuiStrings.PatchFile;
    public string PatchFilePath => GuiStrings.PatchFilePath;
    public string VersionNumber => GuiStrings.VersionNumber;
    public string UseRawPacket => GuiStrings.UseRawPacket;
    public string TargetVersion => GuiStrings.TargetVersion;
    public string General => GuiStrings.General;
    public string Language => GuiStrings.Language;
    public string LanguageEnglish => GuiStrings.LanguageEnglish;
    public string LanguageChinese => GuiStrings.LanguageChinese;
    public string RsbPtxHeadline => GuiStrings.RsbPtxHeadline;
    public string ConvertImages => GuiStrings.ConvertImages;
    public string DeleteAfterConvert => GuiStrings.DeleteAfterConvert;
    public string Ptx0Headline => GuiStrings.Ptx0Headline;
    public string RtonKeyHeadline => GuiStrings.RtonKeyHeadline;
    public string CdatKeyHeadline => GuiStrings.CdatKeyHeadline;
    public string Key => GuiStrings.Key;
    public string CanvasWidth => GuiStrings.CanvasWidth;
    public string CanvasHeight => GuiStrings.CanvasHeight;
    public string ScaleX => GuiStrings.ScaleX;
    public string ScaleY => GuiStrings.ScaleY;
    public string GeneratedImageName => GuiStrings.GeneratedImageName;
    public string PamXflGeneration => GuiStrings.PamXflGeneration;
    public string ReanimXflGeneration => GuiStrings.ReanimXflGeneration;
    public string PamXflResolution => GuiStrings.PamXflResolution;
    public string Cancel => GuiStrings.Cancel;
    public string Confirm => GuiStrings.Confirm;
    public string InvalidInput => GuiStrings.InvalidInput;
    public string Browse => GuiStrings.Browse;
    public string SelectFile => GuiStrings.SelectFile;
    public string SelectFolder => GuiStrings.SelectFolder;
    public string SelectOutput => GuiStrings.SelectOutput;
    public string PickedCopiedToPrivate => GuiStrings.PickedCopiedToPrivate;
    public string OutputRedirectedToPrivate => GuiStrings.OutputRedirectedToPrivate;
}
