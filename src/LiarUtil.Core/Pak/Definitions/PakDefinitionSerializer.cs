using System.Text.Json;
using LiarUtil.Core.Core.IO;
using LiarUtil.Core.Pak.Json;
using LiarUtil.Core.Pak.Models;

namespace LiarUtil.Core.Pak.Definitions;

internal static class PakDefinitionSerializer
{
    public static string FilePath(string folder) => Path.Combine(folder, PakFormat.DefinitionFileName);

    public static PakDefinition Load(string folder)
    {
        var path = FilePath(folder);
        if (!File.Exists(path))
        {
            throw new PakException(string.Format(LiarUtil.Core.Strings.N0NotFound1, PakFormat.DefinitionFileName, folder));
        }

        var definition = JsonSerializer.Deserialize(File.ReadAllText(path), PakDefinitionJsonContext.Default.PakDefinition)
            ?? throw new PakException(string.Format(LiarUtil.Core.Strings.N0ContentEmpty, PakFormat.DefinitionFileName));
        Normalize(definition);
        return definition;
    }

    public static void Save(string folder, PakDefinition definition)
    {
        Normalize(definition);
        FileIO.WriteAllText(FilePath(folder), JsonSerializer.Serialize(definition, PakDefinitionJsonContext.Default.PakDefinition));
    }

    private static void Normalize(PakDefinition definition)
    {
        var rules = new Dictionary<string, PakCompression>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in definition.CompressionByExtension)
        {
            if (!string.IsNullOrWhiteSpace(pair.Key))
            {
                rules[PakExtensions.Normalize(pair.Key)] = pair.Value;
            }
        }

        definition.CompressionByExtension = rules;
        var files = new List<PakFileDefinition>();
        foreach (var file in definition.Files)
        {
            file.Path = PakPath.Normalize(file.Path);
            if (file.Path.Length == 0)
            {
                continue;
            }

            if (file.FileTime is null or <= 0 || file.FileTime == PakFormat.DefaultFileTime)
            {
                file.FileTime = null;
            }

            files.Add(file);
        }

        definition.Files = files;
        if (string.IsNullOrWhiteSpace(definition.ResourceFolder))
        {
            definition.ResourceFolder = PakFormat.ResourceFolderName;
        }

        if (definition.Version <= 0)
        {
            definition.Version = 1;
        }
    }
}
