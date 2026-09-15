using LiarUtil.Core.Core.IO;
using LiarUtil.Core.Dz.Buffers;
using LiarUtil.Core.Dz.Compression;
using LiarUtil.Core.Dz.Models;
using LiarUtil.Core.Dz.Packing;

namespace LiarUtil.Core.Dz.Binary;

internal static class DzArchiveWriter
{
    public static void Write(string outputPath, DzArchive archive, IReadOnlyList<DzResourceFile> files)
    {
        FileIO.EnsureDirectory(outputPath);
        using var stream = File.Create(outputPath);
        var writer = new DzStreamWriter(stream);
        WriteHeader(writer, archive);
        var infoOffset = writer.Position;
        writer.WriteZeros((long)archive.Chunks.Length * DzFormat.ChunkInfoSize);
        for (var i = 0; i < archive.Chunks.Length; i++)
        {
            WritePayload(stream, archive.Chunks[i], files[i]);
        }

        var end = writer.Position;
        writer.Position = infoOffset;
        WriteChunkInfo(writer, archive.Chunks);
        writer.Position = end;
    }

    private static void WriteHeader(DzStreamWriter writer, DzArchive archive)
    {
        writer.WriteBytes("DTRZ"u8);
        writer.WriteUInt16(checked((ushort)archive.FileNames.Length));
        writer.WriteUInt16(checked((ushort)archive.FolderNames.Length));
        writer.WriteByte(DzFormat.Version);
        foreach (var name in archive.FileNames)
        {
            writer.WriteName(name);
        }
        for (var i = 1; i < archive.FolderNames.Length; i++)
        {
            writer.WriteName(archive.FolderNames[i]);
        }

        for (ushort fileIndex = 0; fileIndex < archive.FileNames.Length; fileIndex++)
        {
            var fileChunks = archive.Chunks.Where(chunk => chunk.FileIndex == fileIndex).ToArray();
            writer.WriteUInt16(fileChunks.Length > 0 ? fileChunks[0].FolderIndex : (ushort)0);
            foreach (var chunk in fileChunks)
            {
                writer.WriteUInt16(chunk.ChunkIndex);
            }
            writer.WriteUInt16(DzFormat.EndOfChunks);
        }

        writer.WriteUInt16(archive.ArchiveCount);
        writer.WriteUInt16(checked((ushort)archive.Chunks.Length));
    }

    private static void WritePayload(Stream stream, DzChunkInfo chunk, DzResourceFile file)
    {
        var source = File.ReadAllBytes(file.LocalPath);
        var method = DzCompressor.Normalize(chunk.Method);
        chunk.Method = method;
        chunk.Offset = checked((uint)stream.Position);
        chunk.Size = checked((uint)source.Length);
        chunk.RecordedSize = checked((uint)source.Length);
        if ((method & DzCompressionMethod.Zero) != 0)
        {
            chunk.RecordedSize = 0;
            return;
        }

        if ((method & (DzCompressionMethod.Zlib | DzCompressionMethod.Bzip2 | DzCompressionMethod.Lzma)) != 0)
        {
            stream.Write(DzCompressor.Compress(source, method));
            return;
        }

        chunk.Method = method | DzCompressionMethod.Store;
        stream.Write(source);
    }

    private static void WriteChunkInfo(DzStreamWriter writer, IReadOnlyList<DzChunkInfo> chunks)
    {
        foreach (var chunk in chunks)
        {
            writer.WriteUInt32(chunk.Offset);
            writer.WriteUInt32(chunk.RecordedSize);
            writer.WriteUInt32(chunk.Size);
            writer.WriteUInt16((ushort)chunk.Method);
            writer.WriteUInt16(chunk.ArchiveIndex);
        }
    }
}
