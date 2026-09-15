using System.Text.Json;
using LiarUtil.Core.Core.IO;
using LiarUtil.Core.Dz.Json;
using LiarUtil.Core.Dz.Models;

namespace LiarUtil.Core.Dz.Definitions;

internal static class DzDefinitionSerializer
{
    public static DzPackageDefinition CreateDefault() => new()
    {
        Version = 1,
        ResourceFolder = DzFormat.ResourceFolderName,
        DefaultMethod = DzCompressionMethod.Lzma,
        Compress = new Dictionary<string, DzCompressionMethod>(StringComparer.OrdinalIgnoreCase)
        {
            [".png"] = DzCompressionMethod.Store,
            [".jpg"] = DzCompressionMethod.Store,
            [".jpeg"] = DzCompressionMethod.Store,
            [".compiled"] = DzCompressionMethod.Store,
            [".txt"] = DzCompressionMethod.Zlib,
        },
        Resource = [],
    };

    public static string FilePath(string folder) => Path.Combine(folder, DzFormat.DefinitionFileName);

    public static DzPackageDefinition Load(string folder)
    {
        var path = FilePath(folder);
        if (!File.Exists(path))
        {
            throw new DzException(string.Format(LiarUtil.Core.Strings.N0NotFound1, DzFormat.DefinitionFileName, folder));
        }

        var definition = JsonSerializer.Deserialize(File.ReadAllText(path), DzDefinitionJsonContext.Default.DzPackageDefinition)
            ?? throw new DzException(string.Format(LiarUtil.Core.Strings.N0ContentEmpty, DzFormat.DefinitionFileName));
        Normalize(definition);
        return definition;
    }

    public static void Save(string folder, DzPackageDefinition definition)
    {
        Normalize(definition);
        FileIO.WriteAllText(FilePath(folder), JsonSerializer.Serialize(definition, DzDefinitionJsonContext.Default.DzPackageDefinition));
    }

    private static void Normalize(DzPackageDefinition definition)
    {
        definition.Compress = definition.Compress is null
            ? new Dictionary<string, DzCompressionMethod>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, DzCompressionMethod>(definition.Compress, StringComparer.OrdinalIgnoreCase);
        definition.Resource ??= [];
        foreach (var resource in definition.Resource)
        {
            resource.Path = DzPath.Normalize(resource.Path);
            resource.Chunk ??= [];
            if (resource.Chunk.Count == 0)
            {
                resource.Chunk.Add(new DzChunkDefinition());
            }
        }

        if (string.IsNullOrWhiteSpace(definition.ResourceFolder))
        {
            definition.ResourceFolder = DzFormat.ResourceFolderName;
        }
    }
}
