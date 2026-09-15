using System.Text.Json;
using System.Text.Json.Serialization;

namespace LiarUtil.Core.Rsb.Definitions;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(RsbDefinition))]
internal partial class RsbDefinitionJsonContext : JsonSerializerContext
{
}

internal static class RsbDefinitionSerializer
{
    public static string FilePath(string folder) => Path.Combine(folder, RsbLayout.DefinitionFileName);

    public static RsbDefinition Load(string folder)
    {
        var path = FilePath(folder);
        if (!File.Exists(path))
        {
            throw new InvalidDataException($"No {RsbLayout.DefinitionFileName} found in {folder}");
        }

        using var stream = File.OpenRead(path);
        return JsonSerializer.Deserialize(stream, RsbDefinitionJsonContext.Default.RsbDefinition)
            ?? throw new InvalidDataException($"Invalid {RsbLayout.DefinitionFileName}");
    }

    public static void Save(string folder, RsbDefinition definition)
    {
        Directory.CreateDirectory(folder);
        using var stream = File.Create(FilePath(folder));
        JsonSerializer.Serialize(stream, definition, RsbDefinitionJsonContext.Default.RsbDefinition);
    }
}
