using LiarUtil.Core.Dz.Definitions;
using LiarUtil.Core.Dz.Packing;

namespace LiarUtil.Core.Dz;

internal sealed record DzPackOptions
{
    public required string InputFolder { get; init; }

    public required string OutputPath { get; init; }
}

internal static class DzPack
{
    public static void Pack(DzPackOptions options)
    {
        var definition = DzDefinitionSerializer.Load(options.InputFolder);
        var resourceFolder = Path.Combine(options.InputFolder, definition.ResourceFolder);
        if (!Directory.Exists(resourceFolder))
        {
            throw new DzException(string.Format(LiarUtil.Core.Strings.ResourceDirectoryNotFound0, resourceFolder));
        }

        var files = DzResourceScanner.Scan(resourceFolder);
        DzPackageBuilder.Create(definition, files).Write(options.OutputPath);
    }
}
