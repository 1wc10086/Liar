using System.Diagnostics;
using LiarUtil.Core.Particles;
using LiarUtil.Core.PopFx;
using LiarUtil.Core.Reanim;
using LiarUtil.Core.Services;
using LiarUtil.Core.Trail;

namespace LiarUtil.Cli;

internal sealed class CliApplication
{
    private static readonly string[] PtxFormatChoices = ["ARGB", "ABGR", "ARGB_Padding"];
    private static readonly string[] XflLabelChoices = ["Image Name", "Short Name", "Label Name"];

    private readonly AppSettings _settings = AppSettings.Load();
    private readonly ProcessingService _processing = new(
        new RtonService(),
        new TextureService(),
        new RsbService(),
        new BnkService(),
        new PamService(),
        new PpfService(),
        new PopFxService(),
        new Cfw2Service(),
        new ParticleService(),
        new TrailService(),
        new ReanimService(),
        new DzService(),
        new PakService(),
        new ArcvService(),
        new XprService(),
        new AtlasService(),
        new NewtonService(),
        new RsbPatchService(),
        new FontWidgetDatService(),
        new PaxService(),
        new WemService(),
        new TextTableService(),
        new SpsService(),
        new SnrService(),
        new CafService(),
        new WmaService(),
        new XmService(),
        new XmaService(),
        new XnbAudioService(),
        new XnbFontService(),
        new Cfu2Service());

    public async Task RunAsync()
    {
        Console.WriteLine("LiarUtil 2.0");
        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("a. Home");
            Console.WriteLine("b. Setting");
            Console.WriteLine("q. Quit");
            Console.Write("> ");
            var choice = (Console.ReadLine()?.Trim() ?? "").ToLowerInvariant();
            switch (choice)
            {
                case "a":
                    await HomeAsync();
                    break;
                case "b":
                    SettingsMenu();
                    break;
                case "q" or "quit" or "exit":
                    return;
            }
        }
    }

    private async Task HomeAsync()
    {
        var functions = FunctionCatalog.Functions;
        while (true)
        {
            Console.WriteLine();
            var names = functions.Select(f => f.Name).ToArray();
            var function = ConsolePrompt.Choose("Functions:", names, allowBack: true);
            if (function < 0)
            {
                return;
            }

            var entry = functions[function];
            var mode = entry.HasSingleMode
                ? 0
                : ConsolePrompt.Choose($"Modes for {entry.Name}:", entry.Modes, allowBack: true);
            if (mode < 0)
            {
                continue;
            }

            await ProcessFunctionAsync(function, mode);
        }
    }

    private async Task ProcessFunctionAsync(int function, int mode)
    {
        var inputLabel = function == FunctionCatalog.RsbPatchFunction ? "Original file" : "Input path";
        var outputLabel = function == FunctionCatalog.RsbPatchFunction ? "Modified file" : "Output path";
        var inputKind = FunctionCatalog.IsInputDirectory(function, mode) ? " (directory)" : " (file)";
        var outputKind = FunctionCatalog.IsOutputDirectory(function, mode) ? " (directory)" : " (file)";

        var inputPath = ConsolePrompt.Ask(inputLabel + inputKind);
        var outputPath = ConsolePrompt.Ask(outputLabel + outputKind);
        var request = BuildRequest(function, mode, inputPath, outputPath);

        Console.WriteLine("Processing...");
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await _processing.ProcessAsync(request);
            Console.WriteLine($"Done ({stopwatch.Elapsed.TotalSeconds:0.00}s)");
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Failed: {exception.Message}");
        }
    }

    private ProcessingRequest BuildRequest(int function, int mode, string inputPath, string outputPath)
    {
        var request = new ProcessingRequest
        {
            Function = function,
            Mode = mode,
            InputPath = inputPath,
            OutputPath = outputPath,
            XflOptions = _settings.ToXflOptions(),
            ConvertImages = _settings.RsbConvertImages,
            DeleteAfterConvert = _settings.RsbDeleteAfterConvert,
            Ptx0Format = _settings.Ptx0Format,
            RsbPtxFormat = _settings.RsbPtxFormat,
            RtonKey = _settings.RtonKey,
            CdatKey = _settings.CdatKey,
        };

        switch (function)
        {
            case FunctionCatalog.RtonFunction:
                if (mode is 0 or 1)
                {
                    request = request with { Encoding = ConsolePrompt.Choose("String encoding:", FunctionCatalog.RtonEncodings, allowBack: false) };
                }
                break;
            case FunctionCatalog.TextureFunction:
                var decode = ConsolePrompt.Choose("Operation:", ["Decode", "Encode"], allowBack: false) == 0;
                var useHeader = ConsolePrompt.Confirm("Use file header info", true);
                var formats = FunctionCatalog.TextureFormatsByMode[mode];
                request = request with { Decode = decode, UseHeader = useHeader };
                if (decode && !useHeader)
                {
                    request = request with
                    {
                        Width = ConsolePrompt.AskInt("Width", 0),
                        Height = ConsolePrompt.AskInt("Height", 0),
                    };
                }

                if (!decode || !useHeader)
                {
                    var format = ConsolePrompt.Choose("Texture format:", formats, allowBack: false);
                    request = request with { TextureFormat = formats[format] };
                }
                break;
            case FunctionCatalog.RsbFunction:
                var rsbVersion = ConsolePrompt.Choose("Version:", FunctionCatalog.RsbVersions, 1, allowBack: false);
                request = request with { RsbVersion = rsbVersion == 0 ? 1 : rsbVersion == 2 ? 4 : 3 };
                if (mode == 0)
                {
                    request = request with
                    {
                        ExportResources = ConsolePrompt.Confirm("Export resources", true),
                        WriteTextureHeader = ConsolePrompt.Confirm("Write texture header", true),
                    };
                }
                break;
            case FunctionCatalog.BnkFunction:
                var bnkVersion = ConsolePrompt.Choose("Version:", FunctionCatalog.BnkVersions, FunctionCatalog.BnkVersions.Length - 1, allowBack: false);
                request = request with { BnkVersion = uint.Parse(FunctionCatalog.BnkVersions[bnkVersion]) };
                break;
            case FunctionCatalog.PamFunction:
                if (mode <= 2)
                {
                    request = request with { Version = ConsolePrompt.Choose("Version:", FunctionCatalog.PamVersions, 5, allowBack: false) + 1 };
                }

                if (mode == 3)
                {
                    request = request with { PamXflResolution = ConsolePrompt.AskInt("Resolution", 768) };
                }
                break;
            case FunctionCatalog.PpfFunction:
                request = request with { PpfVersion = ConsolePrompt.Choose("Version:", FunctionCatalog.PpfVersions, allowBack: false) + 1 };
                break;
            case FunctionCatalog.PopFxFunction:
                _ = ConsolePrompt.Choose("Version:", FunctionCatalog.PopFxNumbers, allowBack: false);
                var variant = ConsolePrompt.Choose("Version variant:", FunctionCatalog.PopFxVariants, 2, allowBack: false);
                request = request with { Variant = variant is 0 or 1 ? (PopFxVariant)(variant + 1) : PopFxVariant.V3 };
                break;
            case FunctionCatalog.ParticleFunction:
                (var particlePlatform, var particleCompression, var particleXml) = PromptPlatformOptions(FunctionCatalog.ParticlePlatforms);
                request = request with
                {
                    ParticlePlatform = MapParticlePlatform(particlePlatform),
                    UseCompression = particleCompression,
                    UseXml = particleXml,
                };
                break;
            case FunctionCatalog.TrailFunction:
                (var trailPlatform, var trailCompression, var trailXml) = PromptPlatformOptions(FunctionCatalog.ParticlePlatforms);
                request = request with
                {
                    TrailPlatform = MapTrailPlatform(trailPlatform),
                    UseCompression = trailCompression,
                    UseXml = trailXml,
                };
                break;
            case FunctionCatalog.ReanimFunction:
                (var reanimPlatform, var reanimCompression, var reanimXml) = PromptPlatformOptions(FunctionCatalog.ReanimPlatforms);
                request = request with
                {
                    ReanimPlatform = MapReanimPlatform(reanimPlatform),
                    UseCompression = reanimCompression,
                    UseXml = reanimXml,
                };
                break;
            case FunctionCatalog.AtlasFunction:
                request = request with
                {
                    InfoPath = ConsolePrompt.Ask("Info descriptor file path"),
                    AtlasFormat = ConsolePrompt.Choose("Info descriptor format:", FunctionCatalog.AtlasFormats, allowBack: false),
                };
                if (mode == 1)
                {
                    request = request with
                    {
                        Width = ConsolePrompt.AskInt("Width", 2048),
                        Height = ConsolePrompt.AskInt("Height", 2048),
                    };
                }
                break;
            case FunctionCatalog.RsbPatchFunction:
                _ = ConsolePrompt.Choose("Version number:", FunctionCatalog.RsbPatchNumbers, allowBack: false);
                request = request with
                {
                    PatchPath = ConsolePrompt.Ask("Patch file path"),
                    UseRawPacket = ConsolePrompt.Confirm("Use raw packet", false),
                };
                break;
            case FunctionCatalog.TextTableFunction:
                request = request with { TextTableVersion = ConsolePrompt.Choose("Target version:", FunctionCatalog.TextTableVersions, allowBack: false) };
                break;
        }

        return request;
    }

    private static (int platform, bool compression, bool xml) PromptPlatformOptions(string[] platforms)
    {
        var platform = ConsolePrompt.Choose("Platform:", platforms, allowBack: false);
        var isWp = platforms[platform] == "WP_Xnb";
        var compression = !isWp && ConsolePrompt.Confirm("Use compression", true);
        var xml = ConsolePrompt.Confirm("Use Xml", false);
        return (platform, compression, xml);
    }

    private static ParticlePlatform MapParticlePlatform(int index) => index switch
    {
        1 => ParticlePlatform.Phone32,
        2 => ParticlePlatform.Phone64,
        3 => ParticlePlatform.Wp,
        4 => ParticlePlatform.GameConsole,
        5 => ParticlePlatform.Tv,
        _ => ParticlePlatform.Pc,
    };

    private static TrailPlatform MapTrailPlatform(int index) => index switch
    {
        1 => TrailPlatform.Phone32,
        2 => TrailPlatform.Phone64,
        3 => TrailPlatform.Wp,
        4 => TrailPlatform.GameConsole,
        5 => TrailPlatform.Tv,
        _ => TrailPlatform.Pc,
    };

    private static ReanimPlatform MapReanimPlatform(int index) => index switch
    {
        1 => ReanimPlatform.Phone32,
        2 => ReanimPlatform.Phone64,
        3 => ReanimPlatform.Wp,
        4 => ReanimPlatform.GameConsole,
        5 => ReanimPlatform.Tv,
        6 => ReanimPlatform.FlashXfl,
        _ => ReanimPlatform.Pc,
    };

    private void SettingsMenu()
    {
        while (true)
        {
            Console.WriteLine();
            Console.WriteLine($"1. Rsb PTX0/149 format [{_settings.RsbPtxFormat}]");
            Console.WriteLine($"2. PTX0/149 decode format [{_settings.Ptx0Format}]");
            Console.WriteLine($"3. RTON key [{_settings.RtonKey}]");
            Console.WriteLine($"4. CDAT key [{_settings.CdatKey}]");
            Console.WriteLine($"5. Rsb convert images [{(_settings.RsbConvertImages ? "on" : "off")}]");
            Console.WriteLine($"6. Delete source files after conversion [{(_settings.RsbDeleteAfterConvert ? "on" : "off")}]");
            Console.WriteLine($"7. Pam_Xfl resolution [{_settings.PamXflResolution}]");
            Console.WriteLine($"8. Reanim XFL canvas width [{_settings.ReanimXflWidth:0.###}]");
            Console.WriteLine($"9. Reanim XFL canvas height [{_settings.ReanimXflHeight:0.###}]");
            Console.WriteLine($"10. Reanim XFL X scale [{_settings.ReanimXflScaleX:0.###}]");
            Console.WriteLine($"11. Reanim XFL Y scale [{_settings.ReanimXflScaleY:0.###}]");
            Console.WriteLine($"12. Reanim XFL image name [{XflLabelChoices[_settings.ReanimXflLabelName + 1]}]");
            Console.WriteLine("0. Back");
            Console.Write("> ");
            var input = Console.ReadLine()?.Trim() ?? "";
            if (input == "0" || input.Length == 0)
            {
                return;
            }

            switch (input)
            {
                case "1":
                    _settings.RsbPtxFormat = PtxFormatChoices[ConsolePrompt.Choose("Rsb PTX0/149 format:", PtxFormatChoices, allowBack: false)];
                    break;
                case "2":
                    _settings.Ptx0Format = PtxFormatChoices[ConsolePrompt.Choose("PTX0/149 decode format:", PtxFormatChoices, allowBack: false)];
                    break;
                case "3":
                    _settings.RtonKey = ConsolePrompt.Ask("RTON key", _settings.RtonKey);
                    break;
                case "4":
                    _settings.CdatKey = ConsolePrompt.Ask("CDAT key", _settings.CdatKey);
                    break;
                case "5":
                    _settings.RsbConvertImages = ConsolePrompt.Confirm("Rsb convert images", _settings.RsbConvertImages);
                    break;
                case "6":
                    _settings.RsbDeleteAfterConvert = ConsolePrompt.Confirm("Delete after convert", _settings.RsbDeleteAfterConvert);
                    break;
                case "7":
                    _settings.PamXflResolution = ConsolePrompt.AskInt("Pam_Xfl resolution", _settings.PamXflResolution);
                    break;
                case "8":
                    _settings.ReanimXflWidth = (float)ConsolePrompt.AskDouble("Canvas width", _settings.ReanimXflWidth);
                    break;
                case "9":
                    _settings.ReanimXflHeight = (float)ConsolePrompt.AskDouble("Canvas height", _settings.ReanimXflHeight);
                    break;
                case "10":
                    _settings.ReanimXflScaleX = ConsolePrompt.AskDouble("X scale", _settings.ReanimXflScaleX);
                    break;
                case "11":
                    _settings.ReanimXflScaleY = ConsolePrompt.AskDouble("Y scale", _settings.ReanimXflScaleY);
                    break;
                case "12":
                    _settings.ReanimXflLabelName = ConsolePrompt.Choose("Image name:", XflLabelChoices, _settings.ReanimXflLabelName + 1, allowBack: false) - 1;
                    break;
                default:
                    Console.WriteLine("Invalid choice.");
                    continue;
            }

            _settings.Save();
            Console.WriteLine("Saved.");
        }
    }
}
