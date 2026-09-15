using LiarUtil.Core.Core.IO;
using LiarUtil.Core.Dz.Binary;
using LiarUtil.Core.Dz.Definitions;
using LiarUtil.Core.Dz.Models;

namespace LiarUtil.Core.Dz;

internal sealed record DzUnpackOptions
{
    public required string InputPath { get; init; }

    public required string OutputFolder { get; init; }
}

internal static class DzUnpack
{
    public static void Unpack(DzUnpackOptions options)
    {
        using var mainStream = File.OpenRead(options.InputPath);
        var archive = DzArchiveReader.Read(mainStream);
        var definition = DzDefinitionSerializer.CreateDefault();
        var resourceRoot = Path.Combine(options.OutputFolder, definition.ResourceFolder);
        Directory.CreateDirectory(resourceRoot);

        using var streams = new DzArchiveStreams(mainStream, options.InputPath, archive);
        foreach (var chunk in archive.Chunks)
        {
            if (!chunk.IsReferenced || chunk.FileIndex >= archive.FileNames.Length)
            {
                continue;
            }

            var relativePath = ResolveRelativePath(archive, chunk);
            var localPath = DzPath.ToLocal(resourceRoot, relativePath);
            FileIO.WriteAllBytes(localPath, DzChunkReader.Read(streams[chunk.ArchiveIndex], chunk));
            definition.Resource.Add(new DzResourceDefinition
            {
                Path = relativePath,
                Chunk = [new DzChunkDefinition { Flag = chunk.Method }],
            });
        }

        DzDefinitionSerializer.Save(options.OutputFolder, definition);
    }

    private static string ResolveRelativePath(DzArchive archive, DzChunkInfo chunk)
    {
        var fileName = DzPath.FromArchiveFormat(archive.FileNames[chunk.FileIndex]);
        var folder = chunk.FolderIndex < archive.FolderNames.Length
            ? DzPath.FromArchiveFormat(archive.FolderNames[chunk.FolderIndex])
            : "";
        return DzPath.WithMultiIndex(DzPath.Combine(folder, fileName), chunk.MultiIndex);
    }
}
