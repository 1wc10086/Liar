using LiarUtil.Core.Pak.Models;

namespace LiarUtil.Core.Pak.Definitions;

internal static class PakCompressionResolver
{
    public static PakCompression Resolve(PakDefinition definition, string relativePath)
    {
        var normalized = PakPath.Normalize(relativePath);
        if (FindFile(definition, normalized) is { Compression: { } explicitCompression })
        {
            return explicitCompression;
        }

        return definition.CompressionByExtension.TryGetValue(PakExtensions.Normalize(Path.GetExtension(normalized)), out var byExtension)
            ? byExtension
            : definition.DefaultCompression;
    }

    public static long ResolveFileTime(PakDefinition definition, string relativePath) =>
        FindFile(definition, PakPath.Normalize(relativePath)) is { FileTime: > 0 } file
            ? file.FileTime!.Value
            : PakFormat.DefaultFileTime;

    private static PakFileDefinition? FindFile(PakDefinition definition, string normalized) =>
        definition.Files.FirstOrDefault(candidate =>
            string.Equals(candidate.Path, normalized, StringComparison.OrdinalIgnoreCase));
}
