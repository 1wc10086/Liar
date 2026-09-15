using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace LiarUtil.Gui;

public static class GuiStrings
{
    private static readonly ResourceManager s_resourceManager = new("LiarUtil.Gui.GuiStrings", typeof(GuiStrings).Assembly);

    public static CultureInfo? Culture { get; set; }

    public static string NavHome => GetString();
    public static string NavSettings => GetString();
    public static string Function => GetString();
    public static string Mode => GetString();
    public static string InputPath => GetString();
    public static string OutputPath => GetString();
    public static string OriginalFile => GetString();
    public static string ModifiedFile => GetString();
    public static string StartProcessing => GetString();
    public static string Processing => GetString();
    public static string ProcessingDone => GetString();
    public static string ProcessingFailed => GetString();
    public static string ElapsedSeconds => GetString();
    public static string ProcessingFailedMessage => GetString();
    public static string FillInputOutputPaths => GetString();
    public static string FillInfoPath => GetString();
    public static string FillPatchPath => GetString();
    public static string StringEncoding => GetString();
    public static string Operation => GetString();
    public static string Decode => GetString();
    public static string Encode => GetString();
    public static string Encrypt => GetString();
    public static string Decrypt => GetString();
    public static string Unpack => GetString();
    public static string Pack => GetString();
    public static string Split => GetString();
    public static string Merge => GetString();
    public static string Convert => GetString();
    public static string ToFlash => GetString();
    public static string FromFlash => GetString();
    public static string UseFileHeaderInfo => GetString();
    public static string Length => GetString();
    public static string Width => GetString();
    public static string TextureFormat => GetString();
    public static string Version => GetString();
    public static string ExportResources => GetString();
    public static string WriteTextureHeader => GetString();
    public static string Resolution => GetString();
    public static string VersionVariant => GetString();
    public static string UseCompression => GetString();
    public static string UseXml => GetString();
    public static string Platform => GetString();
    public static string InfoFile => GetString();
    public static string InfoFilePath => GetString();
    public static string InfoFormat => GetString();
    public static string PatchFile => GetString();
    public static string PatchFilePath => GetString();
    public static string VersionNumber => GetString();
    public static string UseRawPacket => GetString();
    public static string TargetVersion => GetString();
    public static string General => GetString();
    public static string Language => GetString();
    public static string LanguageEnglish => GetString();
    public static string LanguageChinese => GetString();
    public static string RsbPtxHeadline => GetString();
    public static string ConvertImages => GetString();
    public static string DeleteAfterConvert => GetString();
    public static string Ptx0Headline => GetString();
    public static string RtonKeyHeadline => GetString();
    public static string CdatKeyHeadline => GetString();
    public static string Key => GetString();
    public static string CanvasWidth => GetString();
    public static string CanvasHeight => GetString();
    public static string ScaleX => GetString();
    public static string ScaleY => GetString();
    public static string GeneratedImageName => GetString();
    public static string PamXflGeneration => GetString();
    public static string ReanimXflGeneration => GetString();
    public static string PamXflResolution => GetString();
    public static string Cancel => GetString();
    public static string Confirm => GetString();
    public static string InvalidInput => GetString();
    public static string Browse => GetString();
    public static string SelectFile => GetString();
    public static string SelectFolder => GetString();
    public static string SelectOutput => GetString();
    public static string PickedCopiedToPrivate => GetString();
    public static string OutputRedirectedToPrivate => GetString();

    private static string GetString([CallerMemberName] string name = "") =>
        s_resourceManager.GetString(name, Culture) ?? name;
}
