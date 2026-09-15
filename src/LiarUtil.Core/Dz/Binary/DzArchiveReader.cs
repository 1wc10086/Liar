using LiarUtil.Core.Dz.Buffers;
using LiarUtil.Core.Dz.Models;

namespace LiarUtil.Core.Dz.Binary;

internal static class DzArchiveReader
{
    private const int MagicLength = 4;

    public static DzArchive Read(Stream stream)
    {
        var reader = new DzStreamReader(stream);
        if (!reader.ReadBytes(MagicLength).AsSpan().SequenceEqual("DTRZ"u8))
        {
            throw new DzException(LiarUtil.Core.Strings.NotValidDzFile);
        }

        var fileNameCount = reader.ReadUInt16();
        var folderNameCount = reader.ReadUInt16();
        if (reader.ReadByte() != DzFormat.Version)
        {
            throw new DzException(LiarUtil.Core.Strings.UnsupportedDzVersion);
        }

        var fileNames = new string[fileNameCount];
        for (var i = 0; i < fileNameCount; i++)
        {
            fileNames[i] = reader.ReadName();
        }

        var folderNames = new string[folderNameCount < 1 ? 1 : folderNameCount];
        folderNames[0] = "";
        for (var i = 1; i < folderNameCount; i++)
        {
            folderNames[i] = reader.ReadName();
        }

        var placements = ReadPlacements(reader, fileNameCount);
        var archiveCount = reader.ReadUInt16();
        var chunkCount = reader.ReadUInt16();
        var chunks = ReadChunkInfo(reader, chunkCount, archiveCount);
        var archiveNames = ReadArchiveNames(reader, archiveCount);
        var result = Merge(placements, chunks);
        ResolvePayloadSizes(result, archiveCount);
        return new DzArchive
        {
            FileNames = fileNames,
            FolderNames = folderNames,
            ArchiveNames = archiveNames,
            Chunks = result,
            ArchiveCount = archiveCount,
        };
    }

    private static IReadOnlyList<DzChunkPlacement> ReadPlacements(DzStreamReader reader, ushort fileNameCount)
    {
        var placements = new List<DzChunkPlacement>();
        for (ushort fileIndex = 0; fileIndex < fileNameCount; fileIndex++)
        {
            var folderIndex = reader.ReadUInt16();
            var multiIndex = 0;
            while (true)
            {
                var chunkIndex = reader.ReadUInt16();
                if (chunkIndex == DzFormat.EndOfChunks)
                {
                    break;
                }

                placements.Add(new DzChunkPlacement(fileIndex, folderIndex, chunkIndex, multiIndex++));
            }
        }

        return placements;
    }

    private static DzChunkInfo[] ReadChunkInfo(DzStreamReader reader, ushort chunkCount, ushort archiveCount)
    {
        var chunks = new DzChunkInfo[chunkCount];
        for (var i = 0; i < chunkCount; i++)
        {
            var offset = reader.ReadUInt32();
            var legacySize = reader.ReadUInt32();
            var size = reader.ReadUInt32();
            var method = (DzCompressionMethod)reader.ReadUInt16();
            var archiveIndex = reader.ReadUInt16();
            if (archiveIndex >= archiveCount)
            {
                throw new DzException(string.Format(LiarUtil.Core.Strings.ResourceBlockArchiveIndexOutOfRange0, archiveIndex));
            }

            chunks[i] = new DzChunkInfo
            {
                ChunkIndex = checked((ushort)i),
                Offset = offset,
                RecordedSize = legacySize,
                Size = size,
                Method = method,
                ArchiveIndex = archiveIndex,
            };
        }

        return chunks;
    }

    private static string?[] ReadArchiveNames(DzStreamReader reader, ushort archiveCount)
    {
        var names = new string?[archiveCount];
        for (var i = 1; i < archiveCount; i++)
        {
            names[i] = reader.ReadName();
        }

        return names;
    }

    private static DzChunkInfo[] Merge(IReadOnlyList<DzChunkPlacement> placements, DzChunkInfo[] chunks)
    {
        foreach (var placement in placements)
        {
            if (placement.ChunkIndex >= chunks.Length)
            {
                throw new DzException(string.Format(LiarUtil.Core.Strings.ResourceBlockIndexOutOfRange01, placement.ChunkIndex, chunks.Length));
            }

            var chunk = chunks[placement.ChunkIndex];
            chunk.FileIndex = placement.FileIndex;
            chunk.FolderIndex = placement.FolderIndex;
            chunk.MultiIndex = placement.MultiIndex;
            chunk.IsReferenced = true;
        }

        return chunks;
    }

    private static void ResolvePayloadSizes(DzChunkInfo[] chunks, ushort archiveCount)
    {
        for (ushort archiveIndex = 0; archiveIndex < archiveCount; archiveIndex++)
        {
            var group = chunks
                .Where(chunk => chunk.ArchiveIndex == archiveIndex)
                .OrderBy(chunk => chunk.Offset)
                .ToArray();
            for (var i = 0; i < group.Length - 1; i++)
            {
                group[i].PayloadSize = checked((int)(group[i + 1].Offset - group[i].Offset));
            }
        }
    }
}
