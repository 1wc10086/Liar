using LiarUtil.Core.Core.Binary;
using LiarUtil.Core.Core.Compression;
using LiarUtil.Core.PopCap;

namespace LiarUtil.Core.Trail.Binary;

internal static class TrailBinaryCodec
{
    private static readonly byte[] WpMagic = [0x58, 0x4E, 0x42, 0x6D, 0x05, 0x00];

    private static readonly byte[] WpInfo =
    [
        0x01, 0x1D, 0x53, 0x65, 0x78, 0x79, 0x2E, 0x54, 0x6F, 0x64, 0x4C, 0x69, 0x62, 0x2E, 0x54, 0x72,
        0x61, 0x69, 0x6C, 0x52, 0x65, 0x61, 0x64, 0x65, 0x72, 0x2C, 0x20, 0x4C, 0x41, 0x57, 0x4E, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x01,
    ];

    public static TrailFile Decode(byte[] data, TrailPlatform platform, ImageIdMap? images = null) =>
        TrailLayouts.IsWp(platform) ? ReadWp(data) : ReadBinary(data, platform, images);

    public static byte[] Encode(TrailFile trail, TrailPlatform platform, bool compress = true) =>
        TrailLayouts.IsWp(platform) ? WriteWp(trail) : WriteBinary(trail, platform, compress);

    private static TrailFile ReadBinary(byte[] input, TrailPlatform platform, ImageIdMap? images)
    {
        var layout = TrailLayouts.Get(platform);
        var data = layout.UnwrapOnDecode
            ? CompressedBlob.Unwrap(input, layout.CompressionHeaderBytes, layout.BigEndian)
            : input;
        var reader = new BufferReader(data, layout.BigEndian)
        {
            Position = layout.LeadingInt32Count * sizeof(int),
        };

        var trail = new TrailFile
        {
            MaxPoints = ReadOptionalInt32(reader),
            MinPointDistance = ReadOptionalSingle(reader),
        };
        trail.Loops = (reader.ReadInt32() & 1) != 0;
        if (layout.HeaderPaddingBytes > 0)
        {
            reader.Skip(layout.HeaderPaddingBytes);
        }
        trail.Image = layout.IntegerImage
            ? ReadImageId(reader, images)
            : ImageReference.FromName(reader.ReadStringByInt32Head());
        if (layout.HasImageResource)
        {
            trail.ImageResource = reader.ReadStringByInt32Head();
        }

        foreach (var name in layout.TrackOrder)
        {
            trail.Tracks.Set(name, ReadNodes(reader));
        }

        if (reader.Position != reader.Length)
        {
            throw new InvalidDataException(LiarUtil.Core.Strings.TrailDataLengthMismatch);
        }
        return trail;
    }

    private static byte[] WriteBinary(TrailFile trail, TrailPlatform platform, bool compress)
    {
        var layout = TrailLayouts.Get(platform);
        var writer = new BufferWriter(layout.BigEndian);
        writer.WriteInt32(layout.Magic);
        for (var index = 1; index < layout.LeadingInt32Count; index++)
        {
            writer.WriteInt32(0);
        }
        writer.WriteInt32(trail.MaxPoints ?? 2);
        writer.WriteSingle(trail.MinPointDistance ?? 1f);
        writer.WriteInt32(trail.Loops ? 1 : 0);
        if (layout.HeaderPaddingBytes > 0)
        {
            writer.WriteZeros(layout.HeaderPaddingBytes);
        }
        if (layout.IntegerImage)
        {
            writer.WriteInt32(ImageReference.ResolveId(trail.Image));
        }
        else
        {
            writer.WriteStringByInt32Head(ImageReference.ResolveName(trail.Image));
            if (layout.HasImageResource)
            {
                writer.WriteStringByInt32Head(trail.ImageResource);
            }
        }

        foreach (var name in layout.TrackOrder)
        {
            WriteNodes(writer, trail.Tracks.Get(name));
        }

        var body = writer.ToArray();
        return layout.WrapOnEncode
            ? CompressedBlob.Wrap(body, layout.CompressionHeaderBytes, layout.BigEndian, compress)
            : body;
    }

    private static TrailFile ReadWp(byte[] data)
    {
        var reader = new BufferReader(data);
        reader.ExpectBytes(WpMagic);
        _ = reader.ReadInt32();
        reader.ExpectBytes(WpInfo);
        var trail = new TrailFile
        {
            Image = ImageReference.FromName(reader.ReadStringByVarInt32Head()),
        };
        trail.MaxPoints = ReadOptionalInt32(reader);
        trail.MinPointDistance = ReadOptionalDouble(reader);
        trail.Loops = (reader.ReadInt32() & 1) != 0;
        foreach (var name in TrailLayouts.WpTrackOrder)
        {
            trail.Tracks.Set(name, ReadWpNodes(reader));
        }
        if (reader.Position != data.Length)
        {
            throw new InvalidDataException(LiarUtil.Core.Strings.TrailDataLengthMismatch);
        }
        return trail;
    }

    private static byte[] WriteWp(TrailFile trail)
    {
        var writer = new BufferWriter();
        writer.WriteBytes(WpMagic);
        var sizePosition = writer.Position;
        writer.WriteInt32(0);
        writer.WriteBytes(WpInfo);
        writer.WriteStringByVarInt32Head(ImageReference.ResolveName(trail.Image));
        writer.WriteInt32(trail.MaxPoints ?? 2);
        writer.WriteDouble(trail.MinPointDistance ?? 1f);
        writer.WriteInt32(trail.Loops ? 1 : 0);
        foreach (var name in TrailLayouts.WpTrackOrder)
        {
            WriteWpNodes(writer, trail.Tracks.Get(name));
        }
        var result = writer.ToArray();
        writer.WriteInt32At(sizePosition, result.Length);
        return writer.ToArray();
    }

    private static ImageReference? ReadImageId(BufferReader reader, ImageIdMap? images) =>
        ImageReference.FromId(reader.ReadInt32(), images);

    private static List<TrailTrackNode>? ReadNodes(BufferReader reader)
    {
        var count = reader.ReadInt32();
        if (count <= 0)
        {
            return null;
        }
        if (count > reader.Remaining / (5 * sizeof(float)))
        {
            throw new InvalidDataException(string.Format(LiarUtil.Core.Strings.TrailNodeCountInvalid0, count));
        }
        var nodes = new List<TrailTrackNode>(count);
        for (var index = 0; index < count; index++)
        {
            nodes.Add(new TrailTrackNode
            {
                Time = reader.ReadSingle(),
                Low = reader.ReadSingle(),
                High = reader.ReadSingle(),
                Curve = ReadCurve(reader),
                Distribution = ReadCurve(reader),
            });
        }
        return nodes;
    }

    private static List<TrailTrackNode>? ReadWpNodes(BufferReader reader)
    {
        var count = reader.ReadInt32();
        if (count <= 0)
        {
            return null;
        }
        if (count > reader.Remaining / (2 * sizeof(int) + 3 * sizeof(double)))
        {
            throw new InvalidDataException(string.Format(LiarUtil.Core.Strings.TrailNodeCountInvalid0, count));
        }
        var nodes = new List<TrailTrackNode>(count);
        for (var index = 0; index < count; index++)
        {
            nodes.Add(new TrailTrackNode
            {
                Curve = ReadCurve(reader),
                Distribution = ReadCurve(reader),
                High = (float)reader.ReadDouble(),
                Low = (float)reader.ReadDouble(),
                Time = (float)reader.ReadDouble(),
            });
        }
        return nodes;
    }

    private static void WriteNodes(BufferWriter writer, List<TrailTrackNode>? nodes)
    {
        if (nodes is null || nodes.Count == 0)
        {
            writer.WriteInt32(0);
            return;
        }
        writer.WriteInt32(nodes.Count);
        foreach (var node in nodes)
        {
            writer.WriteSingle(node.Time);
            writer.WriteSingle(node.Low);
            writer.WriteSingle(node.High);
            writer.WriteInt32((int)node.Curve);
            writer.WriteInt32((int)node.Distribution);
        }
    }

    private static void WriteWpNodes(BufferWriter writer, List<TrailTrackNode>? nodes)
    {
        if (nodes is null || nodes.Count == 0)
        {
            writer.WriteInt32(0);
            return;
        }
        writer.WriteInt32(nodes.Count);
        foreach (var node in nodes)
        {
            writer.WriteInt32((int)node.Curve);
            writer.WriteInt32((int)node.Distribution);
            writer.WriteDouble(node.High);
            writer.WriteDouble(node.Low);
            writer.WriteDouble(node.Time);
        }
    }

    private static int? ReadOptionalInt32(BufferReader reader)
    {
        var value = reader.ReadInt32();
        return value == 0 ? null : value;
    }

    private static float? ReadOptionalSingle(BufferReader reader)
    {
        var value = reader.ReadSingle();
        return value == 0f ? null : value;
    }

    private static float? ReadOptionalDouble(BufferReader reader)
    {
        var value = reader.ReadDouble();
        return value == 0 ? null : (float)value;
    }

    private static PopCurve ReadCurve(BufferReader reader) => (PopCurve)reader.ReadInt32();
}
