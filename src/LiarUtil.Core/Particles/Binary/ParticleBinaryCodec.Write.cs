using System.Globalization;
using LiarUtil.Core.Core.Binary;

namespace LiarUtil.Core.Particles.Binary;

internal static partial class ParticleBinaryCodec
{
    private static byte[] WriteBinary(ParticleFile file, ParticleBinaryProfile profile)
    {
        var writer = new BufferWriter(profile.BigEndian);
        if (profile.RecordSize == 0x2B0)
        {
            writer.WriteInt32(Phone64Magic);
            writer.WriteInt32(0);
            writer.WriteInt32(0);
            writer.WriteInt32(file.Emitters.Count);
            writer.WriteInt32(0);
            writer.WriteInt32(0x2B0);
        }
        else
        {
            writer.WriteInt32(profile.Platform == ParticlePlatform.Tv ? 0 : BodyMagic);
            writer.WriteInt32(0);
            writer.WriteInt32(file.Emitters.Count);
            writer.WriteInt32(0x164);
        }

        foreach (var emitter in file.Emitters)
        {
            WriteRecord(writer, emitter, profile);
        }

        foreach (var emitter in file.Emitters)
        {
            WriteBody(writer, emitter, profile);
        }

        return writer.ToArray();
    }

    private static void WriteRecord(BufferWriter writer, ParticleEmitter emitter, ParticleBinaryProfile profile)
    {
        var image = emitter.Image;
        writer.WriteInt32(0);
        if (profile.RecordSize == 0x2B0)
        {
            writer.WriteInt32(0);
        }

        writer.WriteInt32(image?.Columns ?? 0);
        writer.WriteInt32(image?.Rows ?? 0);
        writer.WriteInt32(image?.Frames ?? 1);
        writer.WriteInt32(image?.Animated ?? 0);
        writer.WriteInt32((int)(emitter.Flags ?? ParticleFlags.None));
        writer.WriteInt32(emitter.EmitterType is { } type ? (int)type : 1);

        if (profile.RecordSize == 0x2B0)
        {
            writer.WriteZeros(376);
            writer.WriteInt32(emitter.Fields?.Count ?? 0);
            writer.WriteZeros(12);
            writer.WriteInt32(emitter.SystemFields?.Count ?? 0);
            writer.WriteZeros(260);
        }
        else
        {
            writer.WriteZeros(188);
            writer.WriteInt32(emitter.Fields?.Count ?? 0);
            writer.WriteZeros(4);
            writer.WriteInt32(emitter.SystemFields?.Count ?? 0);
            writer.WriteZeros(128);
        }
    }

    private static void WriteBody(BufferWriter writer, ParticleEmitter emitter, ParticleBinaryProfile profile)
    {
        var system = emitter.System;
        var particle = emitter.Particle;

        if (profile.IntegerImage)
        {
            writer.WriteInt32(ResolveImageId(emitter.Image));
        }
        else
        {
            writer.WriteStringByInt32Head(ParticleImageHelper.ResolveName(emitter.Image));
        }

        if (profile.HasImagePath)
        {
            writer.WriteStringByInt32Head(emitter.Image?.Resource);
        }

        writer.WriteStringByInt32Head(emitter.Name);
        WriteTrackNodes(writer, system?.Duration);
        writer.WriteStringByInt32Head(emitter.OnDuration);
        WriteTrackNodes(writer, system?.CrossFadeDuration);
        WriteTrackNodes(writer, system?.SpawnRate);
        WriteTrackNodes(writer, system?.SpawnMinActive);
        WriteTrackNodes(writer, system?.SpawnMaxActive);
        WriteTrackNodes(writer, system?.SpawnMaxLaunched);
        WriteTrackNodes(writer, system?.Radius);
        WriteTrackNodes(writer, system?.OffsetX);
        WriteTrackNodes(writer, system?.OffsetY);
        WriteTrackNodes(writer, system?.BoxX);
        WriteTrackNodes(writer, system?.BoxY);
        WriteTrackNodes(writer, system?.Path);
        WriteTrackNodes(writer, system?.SkewX);
        WriteTrackNodes(writer, system?.SkewY);
        WriteTrackNodes(writer, particle?.Duration);
        WriteTrackNodes(writer, system?.Red);
        WriteTrackNodes(writer, system?.Green);
        WriteTrackNodes(writer, system?.Blue);
        WriteTrackNodes(writer, system?.Alpha);
        WriteTrackNodes(writer, system?.Brightness);
        WriteTrackNodes(writer, particle?.LaunchSpeed);
        WriteTrackNodes(writer, particle?.LaunchAngle);
        WriteFields(writer, emitter.Fields, profile.FieldMarker, profile.FieldTypePaddingBytes);
        WriteFields(writer, emitter.SystemFields, profile.FieldMarker, profile.FieldTypePaddingBytes);
        WriteTrackNodes(writer, particle?.Red);
        WriteTrackNodes(writer, particle?.Green);
        WriteTrackNodes(writer, particle?.Blue);
        WriteTrackNodes(writer, particle?.Alpha);
        WriteTrackNodes(writer, particle?.Brightness);
        WriteTrackNodes(writer, particle?.SpinAngle);
        WriteTrackNodes(writer, particle?.SpinSpeed);
        WriteTrackNodes(writer, particle?.Scale);
        WriteTrackNodes(writer, particle?.Stretch);
        WriteTrackNodes(writer, particle?.CollisionReflect);
        WriteTrackNodes(writer, particle?.CollisionSpin);
        WriteTrackNodes(writer, particle?.ClipTop);
        WriteTrackNodes(writer, particle?.ClipBottom);
        WriteTrackNodes(writer, particle?.ClipLeft);
        WriteTrackNodes(writer, particle?.ClipRight);
        WriteTrackNodes(writer, particle?.AnimationRate);
    }

    private static void WriteTrackNodes(BufferWriter writer, List<ParticleTrackNode>? nodes)
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

    private static void WriteFields(
        BufferWriter writer,
        List<ParticleField>? fields,
        int marker,
        int typePaddingBytes)
    {
        writer.WriteInt32(marker);
        if (fields is null || fields.Count == 0)
        {
            return;
        }

        foreach (var field in fields)
        {
            writer.WriteInt32((int)(field.Type ?? ParticleFieldType.Invalid));
            writer.WriteZeros(typePaddingBytes);
        }

        foreach (var field in fields)
        {
            WriteTrackNodes(writer, field.X);
            WriteTrackNodes(writer, field.Y);
        }
    }

    private static byte[] WriteWp(ParticleFile file)
    {
        var writer = new BufferWriter(false);
        writer.WriteBytes(WpMagic);
        var sizePosition = writer.Position;
        writer.WriteInt32(0);
        writer.WriteBytes(WpInfo);
        writer.WriteInt32(file.Emitters.Count);
        foreach (var emitter in file.Emitters)
        {
            WriteWpEmitter(writer, emitter);
        }

        var length = writer.Length;
        writer.Position = sizePosition;
        writer.WriteInt32(length);
        return writer.ToArray();
    }

    private static void WriteWpEmitter(BufferWriter writer, ParticleEmitter emitter)
    {
        var image = emitter.Image;
        var system = emitter.System;
        var particle = emitter.Particle;

        writer.WriteStringByVarInt32Head(ParticleImageHelper.ResolveName(image));
        writer.WriteInt32(image?.Columns ?? 0);
        writer.WriteInt32(image?.Rows ?? 0);
        writer.WriteInt32(image?.Frames ?? 1);
        writer.WriteInt32(image?.Animated ?? 0);
        writer.WriteInt32((int)(emitter.Flags ?? ParticleFlags.None));
        writer.WriteInt32(emitter.EmitterType is { } type ? (int)type : 1);
        writer.WriteStringByVarInt32Head(emitter.Name);
        writer.WriteStringByVarInt32Head(emitter.OnDuration);

        WriteTrackNodes(writer, system?.Duration);
        WriteTrackNodes(writer, system?.CrossFadeDuration);
        WriteTrackNodes(writer, system?.SpawnRate);
        WriteTrackNodes(writer, system?.SpawnMinActive);
        WriteTrackNodes(writer, system?.SpawnMaxActive);
        WriteTrackNodes(writer, system?.SpawnMaxLaunched);
        WriteTrackNodes(writer, system?.Radius);
        WriteTrackNodes(writer, system?.OffsetX);
        WriteTrackNodes(writer, system?.OffsetY);
        WriteTrackNodes(writer, system?.BoxX);
        WriteTrackNodes(writer, system?.BoxY);
        WriteTrackNodes(writer, system?.SkewX);
        WriteTrackNodes(writer, system?.SkewY);
        WriteTrackNodes(writer, system?.Path);
        WriteTrackNodes(writer, particle?.Duration);
        WriteTrackNodes(writer, particle?.LaunchSpeed);
        WriteTrackNodes(writer, particle?.LaunchAngle);
        WriteTrackNodes(writer, system?.Red);
        WriteTrackNodes(writer, system?.Green);
        WriteTrackNodes(writer, system?.Blue);
        WriteTrackNodes(writer, system?.Alpha);
        WriteTrackNodes(writer, system?.Brightness);
        WriteWpFields(writer, emitter.Fields);
        WriteWpFields(writer, emitter.SystemFields);
        WriteTrackNodes(writer, particle?.Red);
        WriteTrackNodes(writer, particle?.Green);
        WriteTrackNodes(writer, particle?.Blue);
        WriteTrackNodes(writer, particle?.Alpha);
        WriteTrackNodes(writer, particle?.Brightness);
        WriteTrackNodes(writer, particle?.SpinAngle);
        WriteTrackNodes(writer, particle?.SpinSpeed);
        WriteTrackNodes(writer, particle?.Scale);
        WriteTrackNodes(writer, particle?.Stretch);
        WriteTrackNodes(writer, particle?.CollisionReflect);
        WriteTrackNodes(writer, particle?.CollisionSpin);
        WriteTrackNodes(writer, particle?.ClipTop);
        WriteTrackNodes(writer, particle?.ClipBottom);
        WriteTrackNodes(writer, particle?.ClipLeft);
        WriteTrackNodes(writer, particle?.ClipRight);
        WriteTrackNodes(writer, particle?.AnimationRate);
    }

    private static void WriteWpFields(BufferWriter writer, List<ParticleField>? fields)
    {
        if (fields is null || fields.Count == 0)
        {
            writer.WriteInt32(0);
            return;
        }

        writer.WriteInt32(fields.Count);
        foreach (var field in fields)
        {
            WriteTrackNodes(writer, field.X);
            WriteTrackNodes(writer, field.Y);
            writer.WriteInt32((int)(field.Type ?? ParticleFieldType.Invalid));
        }
    }

    private static int ResolveImageId(ParticleImage? image)
    {
        if (image is null)
        {
            return -1;
        }

        if (image.Id is { } id)
        {
            return id;
        }

        if (!string.IsNullOrEmpty(image.Name) &&
            int.TryParse(image.Name, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            return value;
        }

        return -1;
    }
}
