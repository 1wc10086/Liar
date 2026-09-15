using LiarUtil.Core.Core.Binary;
using LiarUtil.Core.Core.IO;
using LiarUtil.Core.Pak.Binary;
using LiarUtil.Core.Pak.Definitions;
using LiarUtil.Core.Pak.Models;

namespace LiarUtil.Core.Pak.Packing;

internal sealed class PakArchiveWriter(PakDefinition definition, IReadOnlyList<PakResourceFile> files)
{
    private readonly List<PakEntry> _entries =
    [
        .. files.Select(file => new PakEntry
        {
            Name = PakPath.ToArchiveFormat(file.RelativePath, definition.WindowsPathSeparate),
            FileTime = PakCompressionResolver.ResolveFileTime(definition, file.RelativePath),
        }),
    ];

    public void Write(string outputPath)
    {
        FileIO.EnsureDirectory(outputPath);
        var temporary = $"{outputPath}.tmp";
        try
        {
            using (var stream = File.Create(temporary))
            {
                WriteArchive(stream);
            }

            Transform(temporary, outputPath);
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }
    }

    private void WriteArchive(Stream stream)
    {
        var archive = new PakArchive { Entries = _entries, HasCompressionSize = definition.ZlibCompress };
        var writer = new BufferWriter();
        PakFileTable.Write(writer, archive);
        WritePayloads(writer);
        writer.Position = 0;
        PakFileTable.Write(writer, archive);
        stream.Write(writer.Span);
    }

    private void WritePayloads(BufferWriter writer)
    {
        for (var index = 0; index < files.Count; index++)
        {
            WriteEntryPayload(writer, _entries[index], files[index]);
        }
    }

    private void WriteEntryPayload(BufferWriter writer, PakEntry entry, PakResourceFile file)
    {
        if (!definition.PcEncrypted)
        {
            PakPadding.Write(writer, AlignmentFor(file.RelativePath));
        }

        var body = File.ReadAllBytes(file.LocalPath);
        if (body.Length > int.MaxValue)
        {
            throw new PakException(string.Format(LiarUtil.Core.Strings.FileTooLarge0, file.LocalPath));
        }

        if (body.Length > 0 &&
            definition.ZlibCompress &&
            PakCompressionResolver.Resolve(definition, file.RelativePath) == PakCompression.Zlib)
        {
            var compressed = PakZlib.Compress(body);
            if (compressed.Length > 0)
            {
                writer.WriteBytes(compressed);
                entry.UncompressedSize = body.Length;
                entry.CompressedSize = compressed.Length;
                return;
            }
        }

        writer.WriteBytes(body);
        entry.UncompressedSize = 0;
        entry.CompressedSize = body.Length;
    }

    private int AlignmentFor(string relativePath) =>
        definition.Xbox360PtxAlign && string.Equals(Path.GetExtension(relativePath), ".ptx", StringComparison.OrdinalIgnoreCase)
            ? PakBinaryLayout.Xbox360Alignment
            : PakBinaryLayout.ToConsoleAlignment;

    private void Transform(string sourcePath, string outputPath)
    {
        if (definition.PcEncrypted)
        {
            PakXor.Transform(sourcePath, outputPath);
            return;
        }

        if (!definition.XmemCompress)
        {
            File.Move(sourcePath, outputPath, true);
            return;
        }

        FileIO.WriteAllBytes(outputPath, XmemPack.Compress(File.ReadAllBytes(sourcePath)));
    }
}
