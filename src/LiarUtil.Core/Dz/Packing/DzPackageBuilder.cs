using LiarUtil.Core.Dz.Binary;
using LiarUtil.Core.Dz.Definitions;
using LiarUtil.Core.Dz.Models;

namespace LiarUtil.Core.Dz.Packing;

internal sealed class DzPackageBuilder(
    DzPackageDefinition definition,
    IReadOnlyDictionary<string, DzCompressionMethod> fileMethods,
    IReadOnlyList<DzResourceFile> files)
{
    public void Write(string outputPath) => DzArchiveWriter.Write(outputPath, BuildArchive(), files);

    public static DzPackageBuilder Create(DzPackageDefinition definition, IReadOnlyList<DzResourceFile> files) =>
        new(definition, DzMethodResolver.BuildResourceMethods(definition), files);

    private DzArchive BuildArchive()
    {
        var folderNames = new List<string> { "" };
        var folderIndexes = new Dictionary<string, ushort>(StringComparer.Ordinal) { [""] = 0 };
        var fileNames = new List<string>();
        var chunks = new List<DzChunkInfo>();

        foreach (var file in files)
        {
            var folder = DzPath.ToArchiveFormat(Path.GetDirectoryName(file.RelativePath) ?? "");
            if (!folderIndexes.TryGetValue(folder, out var folderIndex))
            {
                folderIndex = checked((ushort)folderNames.Count);
                folderIndexes[folder] = folderIndex;
                folderNames.Add(folder);
            }

            chunks.Add(new DzChunkInfo
            {
                FolderIndex = folderIndex,
                FileIndex = checked((ushort)fileNames.Count),
                ChunkIndex = checked((ushort)chunks.Count),
                Method = DzMethodResolver.Resolve(definition, file.RelativePath, fileMethods),
                IsReferenced = true,
            });
            fileNames.Add(Path.GetFileName(file.RelativePath));
        }

        return new DzArchive
        {
            FileNames = [.. fileNames],
            FolderNames = [.. folderNames],
            ArchiveNames = [null],
            Chunks = [.. chunks],
            ArchiveCount = 1,
        };
    }
}
