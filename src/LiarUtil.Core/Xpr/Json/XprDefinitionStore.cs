using System.Text.Json;
using System.Text.Json.Nodes;
using LiarUtil.Core.Core.IO;

namespace LiarUtil.Core.Xpr;

internal static class XprDefinitionStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    public static string FilePath(string folder) => Path.Combine(folder, XprFormat.DefinitionFileName);

    public static XprPackInfo Load(string folder)
    {
        var path = FilePath(folder);
        if (!File.Exists(path))
        {
            throw new XprException(string.Format(LiarUtil.Core.Strings.N0NotFound1, XprFormat.DefinitionFileName, folder));
        }

        var root = JsonNode.Parse(File.ReadAllText(path), nodeOptions: null,
            documentOptions: new JsonDocumentOptions { AllowTrailingCommas = true }) as JsonObject
            ?? throw new XprException(string.Format(LiarUtil.Core.Strings.N0ContentInvalid, XprFormat.DefinitionFileName));
        var content = root["Content"] as JsonObject ?? root;
        var info = JsonSerializer.Deserialize<XprPackInfo>(content.ToJsonString(), Options)
            ?? throw new XprException(string.Format(LiarUtil.Core.Strings.N0LacksContent, XprFormat.DefinitionFileName));
        info.RecordFiles ??= [];
        return info;
    }

    public static void Save(string folder, XprPackInfo info)
    {
        var definition = new XprDefinitionFile { Version = 0, Content = info };
        FileIO.WriteAllText(FilePath(folder), JsonSerializer.Serialize(definition, Options));
    }
}
