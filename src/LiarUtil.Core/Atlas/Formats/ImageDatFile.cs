using LiarUtil.Core.Core.Binary;
using LiarUtil.Core.Core.IO;

namespace LiarUtil.Core.Atlas.Formats;

internal static class ImageDatFile
{
    public static List<ImageDatEntry> Read(string path)
    {
        var reader = CreateReader(FileIO.ReadAllBytes(path));
        var count = reader.ReadInt32();
        if (count < 0)
        {
            throw new AtlasException(LiarUtil.Core.Strings.AtlasimagemapDatEntryCountInvalid);
        }

        var entries = new List<ImageDatEntry>(count);
        for (var i = 0; i < count; i++)
        {
            if (reader.ReadInt32() != 4)
            {
                throw new AtlasException(LiarUtil.Core.Strings.AtlasimagemapDatFixedMarkerInvalid);
            }

            var entry = new ImageDatEntry
            {
                Id = reader.ReadStringOrEmptyByUInt16Head(),
            };
            reader.ReadStringOrEmptyByUInt16Head();
            reader.ReadStringOrEmptyByUInt16Head();
            reader.Skip(1);
            entry.XOffset = reader.Position;
            entry.X = reader.ReadInt32();
            entry.Y = reader.ReadInt32();
            entry.Width = reader.ReadInt32();
            entry.Height = reader.ReadInt32();
            reader.Skip(32);
            entry.Parent = reader.ReadStringOrEmptyByUInt16Head();
            reader.Skip(4);
            entries.Add(entry);
        }

        return entries;
    }

    public static void Update(string path, string itemName, IReadOnlyDictionary<string, AtlasSubImage> subImages)
    {
        var data = FileIO.ReadAllBytes(path);
        var writer = new BufferWriter(data.Length);
        writer.SetLength(data.Length);
        writer.WriteBytesAt(0, data);
        foreach (var entry in Read(path))
        {
            if (!string.Equals(entry.Parent, itemName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!subImages.TryGetValue(entry.Id.ToLowerInvariant(), out var subImage))
            {
                throw new AtlasException(string.Format(LiarUtil.Core.Strings.MissingInputImage0, entry.Id));
            }

            writer.WriteInt32At(entry.XOffset, subImage.X);
            writer.WriteInt32At(entry.XOffset + 4, subImage.Y);
            writer.WriteInt32At(entry.XOffset + 8, subImage.Width);
            writer.WriteInt32At(entry.XOffset + 12, subImage.Height);
        }

        FileIO.WriteAllBytes(path, writer.ToArray());
    }

    private static BufferReader CreateReader(byte[] data) =>
        new(data)
        {
            ErrorFactory = static message => new AtlasException("atlasimagemap.dat " + message),
        };
}
