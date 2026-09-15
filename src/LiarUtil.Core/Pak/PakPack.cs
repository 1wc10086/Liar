using LiarUtil.Core.Pak.Definitions;
using LiarUtil.Core.Pak.Models;
using LiarUtil.Core.Pak.Packing;

namespace LiarUtil.Core.Pak;

internal sealed record PakPackOptions
{
    public required string InputFolder { get; init; }

    public required string OutputPath { get; init; }
}

internal static class PakPack
{
    public static void Pack(PakPackOptions options)
    {
        var definition = PakDefinitionSerializer.Load(options.InputFolder);
        var resourceFolder = ResolveResourceFolder(definition, options.InputFolder);
        if (definition.TvVersion)
        {
            PakTvZip.Pack(resourceFolder, options.OutputPath);
            return;
        }

        if (definition.XmemCompress && definition.PcEncrypted)
        {
            throw new PakException(LiarUtil.Core.Strings.XMemCompressionOnlySupportsHostPAKVersion);
        }

        definition.NormalizeRules();
        var files = PakResourceScanner.Scan(resourceFolder);
        new PakArchiveWriter(definition, files).Write(options.OutputPath);
    }

    private static string ResolveResourceFolder(PakDefinition definition, string inputFolder)
    {
        var name = definition.ResourceFolder;
        return string.IsNullOrWhiteSpace(name) || name == "." ? inputFolder : Path.Combine(inputFolder, name);
    }
}
