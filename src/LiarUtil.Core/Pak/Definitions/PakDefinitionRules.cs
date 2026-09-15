using LiarUtil.Core.Pak.Models;

namespace LiarUtil.Core.Pak.Definitions;

internal static class PakDefinitionRules
{
    public static void InferCompressionRules(this PakDefinition definition)
    {
        definition.DefaultCompression = definition.Files
            .Select(CompressionOf)
            .DefaultIfEmpty(PakCompression.Store)
            .GroupBy(method => method)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key)
            .Select(group => group.Key)
            .First();

        definition.CompressionByExtension = definition.Files
            .Where(file => ExtensionOf(file.Path).Length > 0)
            .GroupBy(file => ExtensionOf(file.Path))
            .ToDictionary(group => group.Key, UniformCompression, StringComparer.OrdinalIgnoreCase);

        foreach (var file in definition.Files)
        {
            file.Compression ??= definition.DefaultCompression;
            if (file.FileTime is null or <= 0 || file.FileTime == PakFormat.DefaultFileTime)
            {
                file.FileTime = null;
            }
        }
    }

    public static void NormalizeRules(this PakDefinition definition) =>
        definition.CompressionByExtension = new Dictionary<string, PakCompression>(
            definition.CompressionByExtension
                .Where(pair => !string.IsNullOrWhiteSpace(pair.Key))
                .Select(pair => new KeyValuePair<string, PakCompression>(PakExtensions.Normalize(pair.Key), pair.Value)),
            StringComparer.OrdinalIgnoreCase);

    private static PakCompression UniformCompression(IEnumerable<PakFileDefinition> files)
    {
        var methods = files.Select(CompressionOf).Distinct().ToArray();
        return methods.Length == 1 ? methods[0] : PakCompression.Store;
    }

    private static PakCompression CompressionOf(PakFileDefinition file) =>
        file.Compression ?? PakCompression.Store;

    private static string ExtensionOf(string path) =>
        PakExtensions.Normalize(Path.GetExtension(PakPath.Normalize(path)));
}
