using LiarUtil.Core.Core.Binary;
using LiarUtil.Core.Core.IO;

namespace LiarUtil.Core.Xpr;

internal sealed record XprPackOptions
{
    public required string InputFolder { get; init; }

    public required string OutputPath { get; init; }
}

internal static class XprPack
{
    public static void Pack(XprPackOptions options)
    {
        var info = XprDefinitionStore.Load(options.InputFolder);
        if (info.XprDataOffset < XprFormat.HeaderSize)
        {
            throw new XprException(LiarUtil.Core.Strings.XprDataOffsetInDefinitionJsonInvalid);
        }

        var resourceFolder = Path.Combine(options.InputFolder, XprFormat.ResourceFolderName);
        var records = info.RecordFiles;
        var count = records.Count;
        var poolSize = 0;
        var paths = new byte[count][];
        for (var i = 0; i < count; i++)
        {
            paths[i] = TextCodec.Encode(records[i].Path, TextFormat.Latin1);
            poolSize = checked(poolSize + paths[i].Length + 1);
        }

        var headerSize = XprFormat.Align(checked(8 + (count * XprFormat.EntrySize) + poolSize), XprFormat.FileAlignment);
        var offsets = new int[count];
        var sizes = new int[count];
        var position = headerSize;
        for (var i = 0; i < count; i++)
        {
            if (info.XprDataFileAlign)
            {
                position = XprFormat.Align(position, XprFormat.FileAlignment);
            }

            offsets[i] = position;
            sizes[i] = checked((int)new FileInfo(Resolve(resourceFolder, records[i].Path)).Length);
            position = checked(position + sizes[i]);
        }

        var dataSize = XprFormat.Align(position, XprFormat.ArchiveAlignment);
        var region = new byte[dataSize];
        var countWriter = new BufferWriter(sizeof(uint)) { Order = ByteOrder.Big };
        countWriter.WriteUInt32(checked((uint)count));
        countWriter.Span.CopyTo(region);
        var pathOffset = 8 + (count * XprFormat.EntrySize);
        for (var i = 0; i < count; i++)
        {
            new XprFileEntry(XprText.StringToType(records[i].Type), (uint)offsets[i], (uint)sizes[i], (uint)pathOffset)
                .Write(region.AsSpan(4 + (i * XprFormat.EntrySize), XprFormat.EntrySize));
            paths[i].CopyTo(region.AsSpan(pathOffset));
            pathOffset = checked(pathOffset + paths[i].Length + 1);
        }

        for (var i = 0; i < count; i++)
        {
            using var source = File.OpenRead(Resolve(resourceFolder, records[i].Path));
            source.ReadExactly(region, offsets[i], sizes[i]);
        }

        FileIO.EnsureDirectory(options.OutputPath);
        var header = new BufferWriter(XprFormat.HeaderSize) { Order = ByteOrder.Big };
        header.WriteUInt32(XprFormat.Signature);
        header.WriteUInt32(checked((uint)dataSize));
        header.WriteUInt32(0);
        using var stream = new FileStream(options.OutputPath, FileMode.Create, FileAccess.Write, FileShare.None);
        stream.SetLength(checked((long)info.XprDataOffset + dataSize));
        stream.Write(header.Span);
        stream.Position = info.XprDataOffset;
        stream.Write(region);
    }

    private static string Resolve(string resourceFolder, string recordPath)
    {
        var path = Path.Combine(resourceFolder, XprText.ToNativePath(recordPath));
        return File.Exists(path) ? path : throw new XprException(string.Format(LiarUtil.Core.Strings.XPRResourceNotFound0, recordPath));
    }
}
