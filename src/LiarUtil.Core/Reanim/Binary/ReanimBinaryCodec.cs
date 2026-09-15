using LiarUtil.Core.Core.Binary;
using LiarUtil.Core.Core.Compression;
using LiarUtil.Core.PopCap;

namespace LiarUtil.Core.Reanim.Binary;

internal static class ReanimBinaryCodec
{
    private const float Missing = -10000f;
    private const float WpMissing = -99999f;

    private static readonly byte[] WpMagic = [0x58, 0x4E, 0x42, 0x6D, 0x05, 0x00];

    private static readonly byte[] WpInfo =
    [
        0x01, 0x1E, 0x53, 0x65, 0x78, 0x79, 0x2E, 0x54, 0x6F, 0x64, 0x4C, 0x69, 0x62, 0x2E, 0x52, 0x65,
        0x61, 0x6E, 0x69, 0x6D, 0x52, 0x65, 0x61, 0x64, 0x65, 0x72, 0x2C, 0x20, 0x4C, 0x41, 0x57, 0x4E,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x01,
    ];

    public static ReanimFile Decode(byte[] data, ReanimPlatform platform, ImageIdMap? images = null) =>
        ReanimLayouts.IsWp(platform) ? ReadWp(data) : ReadBinary(data, platform, images);

    public static byte[] Encode(ReanimFile reanim, ReanimPlatform platform, bool compress = true) =>
        ReanimLayouts.IsWp(platform) ? WriteWp(reanim) : WriteBinary(reanim, platform, compress);

    private static ReanimFile ReadBinary(byte[] input, ReanimPlatform platform, ImageIdMap? images)
    {
        var layout = ReanimLayouts.Get(platform);
        var data = layout.UnwrapOnDecode
            ? CompressedBlob.Unwrap(input, layout.CompressionHeaderBytes, layout.BigEndian)
            : input;
        var reader = new BufferReader(data, layout.BigEndian);
        reader.Position = layout.LeadingInt32Count * sizeof(int);
        var trackCount = reader.ReadInt32();
        if (trackCount < 0 || trackCount > reader.Remaining)
        {
            throw new InvalidDataException(string.Format(LiarUtil.Core.Strings.TrackCountInvalid0, trackCount));
        }

        var reanim = new ReanimFile { Fps = reader.ReadSingle() };
        reader.Skip(layout.MarkerPrefixInts * sizeof(int));
        reader.ExpectInt32(layout.Marker);

        var transformCounts = new int[trackCount];
        for (var index = 0; index < trackCount; index++)
        {
            reader.Skip(layout.TrackInfoPrefixBytes);
            transformCounts[index] = reader.ReadInt32();
            reader.Skip(layout.TrackInfoSuffixBytes);
            if (transformCounts[index] < 0 || transformCounts[index] > reader.Remaining)
            {
                throw new InvalidDataException(string.Format(LiarUtil.Core.Strings.TransformCountInvalid0, transformCounts[index]));
            }
        }

        for (var index = 0; index < trackCount; index++)
        {
            var track = new ReanimTrack
            {
                Name = reader.ReadStringByInt32Head(),
                Transforms = new List<ReanimTransform>(transformCounts[index]),
            };
            reader.ExpectInt32(layout.TransformStride);
            var transformCount = transformCounts[index];
            for (var transformIndex = 0; transformIndex < transformCount; transformIndex++)
            {
                track.Transforms.Add(ReadTransform(reader, layout.TransformStride - (8 * sizeof(float))));
            }
            for (var transformIndex = 0; transformIndex < transformCount; transformIndex++)
            {
                ReadStrings(reader, track.Transforms[transformIndex], layout, images);
            }
            reanim.Tracks.Add(track);
        }

        if (reader.Position != reader.Length)
        {
            throw new InvalidDataException(LiarUtil.Core.Strings.ReanimDataLengthMismatch);
        }
        return reanim;
    }

    private static byte[] WriteBinary(ReanimFile reanim, ReanimPlatform platform, bool compress)
    {
        var layout = ReanimLayouts.Get(platform);
        var writer = new BufferWriter(layout.BigEndian);
        writer.WriteInt32(layout.Magic);
        for (var index = 1; index < layout.LeadingInt32Count; index++)
        {
            writer.WriteInt32(0);
        }
        writer.WriteInt32(reanim.Tracks.Count);
        writer.WriteSingle(reanim.Fps);
        writer.WriteZeros(layout.MarkerPrefixInts * sizeof(int));
        writer.WriteInt32(layout.Marker);
        foreach (var track in reanim.Tracks)
        {
            writer.WriteZeros(layout.TrackInfoPrefixBytes);
            writer.WriteInt32(track.Transforms.Count);
            writer.WriteZeros(layout.TrackInfoSuffixBytes);
        }
        var transformPadding = layout.TransformStride - (8 * sizeof(float));
        foreach (var track in reanim.Tracks)
        {
            writer.WriteStringByInt32Head(track.Name);
            writer.WriteInt32(layout.TransformStride);
            foreach (var transform in track.Transforms)
            {
                writer.WriteSingle(transform.X ?? Missing);
                writer.WriteSingle(transform.Y ?? Missing);
                writer.WriteSingle(transform.SkewX ?? Missing);
                writer.WriteSingle(transform.SkewY ?? Missing);
                writer.WriteSingle(transform.ScaleX ?? Missing);
                writer.WriteSingle(transform.ScaleY ?? Missing);
                writer.WriteSingle(transform.Frame ?? Missing);
                writer.WriteSingle(transform.Alpha ?? Missing);
                if (transformPadding > 0)
                {
                    writer.WriteZeros(transformPadding);
                }
            }
            foreach (var transform in track.Transforms)
            {
                WriteStrings(writer, transform, layout);
            }
        }
        var body = writer.ToArray();
        return layout.WrapOnEncode
            ? CompressedBlob.Wrap(body, layout.CompressionHeaderBytes, layout.BigEndian, compress)
            : body;
    }

    private static ReanimTransform ReadTransform(BufferReader reader, int padding)
    {
        var transform = new ReanimTransform
        {
            X = ReadOptional(reader),
            Y = ReadOptional(reader),
            SkewX = ReadOptional(reader),
            SkewY = ReadOptional(reader),
            ScaleX = ReadOptional(reader),
            ScaleY = ReadOptional(reader),
            Frame = ReadOptional(reader),
            Alpha = ReadOptional(reader),
        };
        if (padding > 0)
        {
            reader.Skip(padding);
        }
        return transform;
    }

    private static void WriteStrings(BufferWriter writer, ReanimTransform transform, ReanimLayout layout)
    {
        foreach (var field in layout.StringFields)
        {
            switch (field)
            {
                case ReanimStringField.Image:
                    writer.WriteStringByInt32Head(ImageReference.ResolveName(transform.Image));
                    break;
                case ReanimStringField.ImageId:
                    writer.WriteInt32(ImageReference.ResolveId(transform.Image));
                    break;
                case ReanimStringField.ImageResource:
                    writer.WriteStringByInt32Head(transform.ImageResource);
                    break;
                case ReanimStringField.Image2:
                    writer.WriteStringByInt32Head(ImageReference.ResolveName(transform.Image2));
                    break;
                case ReanimStringField.Image2Resource:
                    writer.WriteStringByInt32Head(transform.Image2Resource);
                    break;
                case ReanimStringField.Font:
                    writer.WriteStringByInt32Head(transform.Font);
                    break;
                default:
                    writer.WriteStringByInt32Head(transform.Text);
                    break;
            }
        }
    }

    private static void ReadStrings(BufferReader reader, ReanimTransform transform, ReanimLayout layout, ImageIdMap? images)
    {
        foreach (var field in layout.StringFields)
        {
            switch (field)
            {
                case ReanimStringField.Image:
                    transform.Image = ImageReference.FromName(reader.ReadStringByInt32Head());
                    break;
                case ReanimStringField.ImageId:
                    transform.Image = ImageReference.FromId(reader.ReadInt32(), images);
                    break;
                case ReanimStringField.ImageResource:
                    transform.ImageResource = reader.ReadStringByInt32Head();
                    break;
                case ReanimStringField.Image2:
                    transform.Image2 = ImageReference.FromName(reader.ReadStringByInt32Head());
                    break;
                case ReanimStringField.Image2Resource:
                    transform.Image2Resource = reader.ReadStringByInt32Head();
                    break;
                case ReanimStringField.Font:
                    transform.Font = reader.ReadStringByInt32Head();
                    break;
                default:
                    transform.Text = reader.ReadStringByInt32Head();
                    break;
            }
        }
    }

    private static ReanimFile ReadWp(byte[] data)
    {
        var reader = new BufferReader(data);
        reader.ExpectBytes(WpMagic);
        _ = reader.ReadInt32();
        reader.ExpectBytes(WpInfo);
        var reanim = new ReanimFile
        {
            DoScale = reader.ReadInt8(),
            Fps = reader.ReadSingle(),
        };
        var trackCount = reader.ReadInt32();
        if (trackCount < 0)
        {
            throw new InvalidDataException(string.Format(LiarUtil.Core.Strings.TrackCountInvalid0, trackCount));
        }
        for (var index = 0; index < trackCount; index++)
        {
            var track = new ReanimTrack { Name = reader.ReadUtf16ByInt32Head() };
            var transformCount = reader.ReadInt32();
            if (transformCount < 0)
            {
                throw new InvalidDataException(string.Format(LiarUtil.Core.Strings.TransformCountInvalid0, transformCount));
            }
            for (var transformIndex = 0; transformIndex < transformCount; transformIndex++)
            {
                var type = reader.ReadUInt8();
                if (type != 0)
                {
                    track.Transforms.Add(new ReanimTransform());
                    continue;
                }
                var transform = new ReanimTransform
                {
                    Font = reader.ReadUtf16ByInt32Head(),
                    Image = ImageReference.FromName(reader.ReadUtf16ByInt32Head()),
                    Text = reader.ReadUtf16ByInt32Head(),
                    Alpha = ReadWpOptional(reader),
                    Frame = ReadWpOptional(reader),
                    ScaleX = ReadWpOptional(reader),
                    ScaleY = ReadWpOptional(reader),
                    SkewX = ReadWpOptional(reader),
                    SkewY = ReadWpOptional(reader),
                    X = ReadWpOptional(reader),
                    Y = ReadWpOptional(reader),
                };
                track.Transforms.Add(transform);
            }
            reanim.Tracks.Add(track);
        }
        if (reader.Position != data.Length)
        {
            throw new InvalidDataException(LiarUtil.Core.Strings.ReanimDataLengthMismatch);
        }
        return reanim;
    }

    private static byte[] WriteWp(ReanimFile reanim)
    {
        var writer = new BufferWriter();
        writer.WriteBytes(WpMagic);
        var sizePosition = writer.Position;
        writer.WriteInt32(0);
        writer.WriteBytes(WpInfo);
        writer.WriteInt8(reanim.DoScale ?? 0);
        writer.WriteSingle(reanim.Fps);
        writer.WriteInt32(reanim.Tracks.Count);
        foreach (var track in reanim.Tracks)
        {
            writer.WriteUtf16ByInt32Head(track.Name);
            writer.WriteInt32(track.Transforms.Count);
            for (var index = 0; index < track.Transforms.Count; index++)
            {
                var transform = track.Transforms[index];
                var hidden = index != 0 && transform.IsEmpty;
                writer.WriteUInt8(hidden ? (byte)1 : (byte)0);
                if (hidden)
                {
                    continue;
                }
                writer.WriteUtf16ByInt32Head(transform.Font);
                writer.WriteUtf16ByInt32Head(ImageReference.ResolveName(transform.Image));
                writer.WriteUtf16ByInt32Head(transform.Text);
                writer.WriteSingle(transform.Alpha ?? WpMissing);
                writer.WriteSingle(transform.Frame ?? WpMissing);
                writer.WriteSingle(transform.ScaleX ?? WpMissing);
                writer.WriteSingle(transform.ScaleY ?? WpMissing);
                writer.WriteSingle(transform.SkewX ?? WpMissing);
                writer.WriteSingle(transform.SkewY ?? WpMissing);
                writer.WriteSingle(transform.X ?? WpMissing);
                writer.WriteSingle(transform.Y ?? WpMissing);
            }
        }
        var result = writer.ToArray();
        writer.WriteInt32At(sizePosition, result.Length);
        return writer.ToArray();
    }

    private static float? ReadOptional(BufferReader reader)
    {
        var value = reader.ReadSingle();
        return value == Missing ? null : value;
    }

    private static float? ReadWpOptional(BufferReader reader)
    {
        var value = reader.ReadSingle();
        return value == WpMissing ? null : value;
    }
}
