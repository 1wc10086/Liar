using LiarUtil.Core.Dz.Compression;
using LiarUtil.Core.Dz.Models;

namespace LiarUtil.Core.Dz.Definitions;

internal static class DzMethodResolver
{
    public static DzCompressionMethod Resolve(
        DzPackageDefinition definition,
        string relativePath,
        IReadOnlyDictionary<string, DzCompressionMethod> resourceMethods)
    {
        var normalized = DzPath.Normalize(relativePath);
        if (resourceMethods.TryGetValue(normalized, out var method))
        {
            return DzCompressor.Normalize(method);
        }

        var extension = Path.GetExtension(normalized);
        if (extension.Length > 0 && definition.Compress.TryGetValue(extension, out var extensionMethod))
        {
            return DzCompressor.Normalize(extensionMethod);
        }

        return DzCompressor.Normalize(definition.DefaultMethod);
    }

    public static Dictionary<string, DzCompressionMethod> BuildResourceMethods(DzPackageDefinition definition)
    {
        var result = new Dictionary<string, DzCompressionMethod>(StringComparer.OrdinalIgnoreCase);
        foreach (var resource in definition.Resource)
        {
            if (!string.IsNullOrWhiteSpace(resource.Path))
            {
                result[DzPath.Normalize(resource.Path)] = resource.Chunk.Count > 0
                    ? resource.Chunk[0].Flag
                    : definition.DefaultMethod;
            }
        }

        return result;
    }
}
