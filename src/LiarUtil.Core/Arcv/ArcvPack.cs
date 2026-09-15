using System.Globalization;
using LiarUtil.Core.Core.Binary;

namespace LiarUtil.Core.Arcv;

internal static class ArcvPack
{
    public static void Pack(ArcvPackOptions options)
    {
        var resources = LoadResources(options.InputFolder);
        using var output = new FileStream(options.OutputPath, FileMode.Create, FileAccess.Write, FileShare.None);
        WriteHeader(output, resources.Count);
        output.SetLength(ArcvFormat.HeaderSize + (long)resources.Count * ArcvFormat.EntrySize);
        output.Position = output.Length;

        var entries = new List<ArcvEntry>(resources.Count);
        foreach (var resource in resources)
        {
            Align(output);
            var offset = checked((int)output.Position);
            output.Write(resource.Content);
            entries.Add(new ArcvEntry(offset, resource.Content.Length, resource.Checksum));
        }

        WriteTotalLength(output, checked((int)output.Length));
        output.Position = ArcvFormat.HeaderSize;
        WriteEntries(output, entries);
    }

    private static void WriteHeader(Stream output, int count)
    {
        var writer = new BufferWriter(ArcvFormat.HeaderSize);
        writer.WriteBytes(ArcvFormat.MagicBytes);
        writer.WriteInt32(count);
        writer.WriteInt32(0);
        output.Write(writer.Span);
    }

    private static void WriteTotalLength(Stream output, int length)
    {
        var position = output.Position;
        output.Position = sizeof(int) * 2;
        var writer = new BufferWriter(sizeof(int));
        writer.WriteInt32(length);
        output.Write(writer.Span);
        output.Position = position;
    }

    private static void WriteEntries(Stream output, IReadOnlyList<ArcvEntry> entries)
    {
        var writer = new BufferWriter(entries.Count * ArcvFormat.EntrySize);
        foreach (var entry in entries)
        {
            WriteEntry(writer, entry);
        }

        output.Write(writer.Span);
    }

    private static void WriteEntry(BufferWriter writer, ArcvEntry entry)
    {
        writer.WriteInt32(entry.Offset);
        writer.WriteInt32(entry.Size);
        writer.WriteUInt32(entry.Checksum);
    }

    private static void Align(Stream output)
    {
        var remainder = (int)(output.Position % ArcvFormat.Alignment);
        if (remainder == 0)
        {
            return;
        }

        for (var index = 0; index < ArcvFormat.Alignment - remainder; index++)
        {
            output.WriteByte(ArcvFormat.AlignmentPadding);
        }
    }

    private static List<ArcvResource> LoadResources(string inputFolder)
    {
        var resources = new List<ArcvResource>();
        foreach (var path in Directory.EnumerateFiles(inputFolder, "*", SearchOption.AllDirectories))
        {
            if (!long.TryParse(Path.GetFileNameWithoutExtension(path), NumberStyles.Integer, CultureInfo.InvariantCulture, out var checksum))
            {
                continue;
            }

            resources.Add(new ArcvResource(unchecked((uint)checksum), checksum, File.ReadAllBytes(path)));
        }

        resources.Sort((left, right) => left.SortKey.CompareTo(right.SortKey));
        return resources;
    }
}

internal sealed record ArcvResource(uint Checksum, long SortKey, byte[] Content);
