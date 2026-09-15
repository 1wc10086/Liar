using System.Text.Json;
using System.Text.Json.Serialization;
using LiarUtil.Core.Reanim.Flash;

namespace LiarUtil.Core.Services;

public sealed class AppSettings
{
    public const string LanguageEnglish = "en";
    public const string LanguageChinese = "zh-Hans";

    [JsonIgnore]
    public string FilePath { get; private set; } = "";

    public string Language { get; set; } = LanguageEnglish;

    public string RsbPtxFormat { get; set; } = "ARGB";

    public string Ptx0Format { get; set; } = "ARGB";

    public string RtonKey { get; set; } = "com_popcap_pvz2_magento_product_2013_05_05";

    public string CdatKey { get; set; } = "AS23DSREPLKL335KO4439032N8345NF";

    public bool RsbConvertImages { get; set; }

    public bool RsbDeleteAfterConvert { get; set; }

    public float ReanimXflWidth { get; set; } = 80;

    public float ReanimXflHeight { get; set; } = 80;

    public double ReanimXflScaleX { get; set; } = 1;

    public double ReanimXflScaleY { get; set; } = 1;

    public int ReanimXflLabelName { get; set; }

    public int PamXflResolution { get; set; } = 768;

    public void Save()
    {
        if (string.IsNullOrEmpty(FilePath))
        {
            return;
        }

        try
        {
            FileHelper.WriteText(FilePath, JsonSerializer.Serialize(this, AppSettingsJsonContext.Default.AppSettings));
        }
        catch
        {
        }
    }

    public static AppSettings Load(string? path = null)
    {
        var filePath = path ?? ResolveDefaultPath();
        var settings = Read(filePath) ?? new AppSettings();
        settings.FilePath = filePath;
        return settings;
    }

    public static string ResolveDefaultPath()
    {
        if (OperatingSystem.IsAndroid())
        {
            return "/storage/emulated/0/Android/data/com.liarutil.app/setting.json";
        }

        if (OperatingSystem.IsIOS())
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "setting.json");
        }

        if (OperatingSystem.IsWindows())
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LiarUtil", "setting.json");
        }

        if (OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst())
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LiarUtil", "setting.json");
        }

        var xdg = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        if (!string.IsNullOrEmpty(xdg))
        {
            return Path.Combine(xdg, "liarutil", "setting.json");
        }

        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return !string.IsNullOrEmpty(profile)
            ? Path.Combine(profile, ".config", "liarutil", "setting.json")
            : Path.Combine(AppContext.BaseDirectory, "setting.json");
    }

    public XflWriterOptions ToXflOptions() => new()
    {
        Width = ReanimXflWidth,
        Height = ReanimXflHeight,
        ScaleX = ReanimXflScaleX,
        ScaleY = ReanimXflScaleY,
        UseLabelName = ReanimXflLabelName,
    };

    private static AppSettings? Read(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return null;
            }

            return JsonSerializer.Deserialize(File.ReadAllText(filePath), AppSettingsJsonContext.Default.AppSettings);
        }
        catch
        {
            return null;
        }
    }
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(AppSettings))]
internal sealed partial class AppSettingsJsonContext : JsonSerializerContext;
