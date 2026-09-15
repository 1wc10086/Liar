using LiarUtil.Core.Core.Binary;
using LiarUtil.Core.PopCap;
using LiarUtil.Core.Core.Xml;

namespace LiarUtil.Core.Particles.Binary;

internal static partial class ParticleBinaryCodec
{
    private static ParticleFile ReadBinary(byte[] input, ParticlePlatform platform)
    {
        var profile = ParticleBinaryProfiles.Get(platform);
        var data = Decompress(input, profile);
        var reader = new BufferReader(data, profile.BigEndian) { ErrorFactory = BinaryErrors.Factory<ArgumentOutOfRangeException>() };

        int count;
        if (profile.RecordSize == 0x2B0)
        {
            reader.Position = 12;
            count = reader.ReadInt32();
            reader.Position = 20;
        }
        else
        {
            reader.Position = 8;
            count = reader.ReadInt32();
            reader.Position = 12;
        }

        if (count < 0)
        {
            throw new InvalidDataException(string.Format(LiarUtil.Core.Strings.ParticleEmitterCountInvalid0, count));
        }

        if (reader.ReadInt32() != profile.RecordSize)
        {
            throw new InvalidDataException(LiarUtil.Core.Strings.ParticleFileHeaderInvalid);
        }

        var file = new ParticleFile { Emitters = new List<ParticleEmitter>(count) };
        var fieldCounts = new int[count];
        var systemFieldCounts = new int[count];
        for (var i = 0; i < count; i++)
        {
            var emitter = new ParticleEmitter();
            var (fieldCount, systemFieldCount) = ReadRecord(reader, emitter, profile);
            fieldCounts[i] = fieldCount;
            systemFieldCounts[i] = systemFieldCount;
            file.Emitters.Add(emitter);
        }

        for (var i = 0; i < count; i++)
        {
            ReadBody(reader, file.Emitters[i], profile, fieldCounts[i], systemFieldCounts[i]);
        }

        return file;
    }

    private static (int FieldCount, int SystemFieldCount) ReadRecord(
        BufferReader reader,
        ParticleEmitter emitter,
        ParticleBinaryProfile profile)
    {
        reader.Skip(profile.RecordSize == 0x2B0 ? 8 : 4);

        var columns = reader.ReadInt32();
        var rows = reader.ReadInt32();
        var frames = reader.ReadInt32();
        var animated = reader.ReadInt32();
        var flags = reader.ReadInt32();
        var emitterType = reader.ReadInt32();

        ParticleImage? image = null;
        if (columns != 0)
        {
            image ??= new ParticleImage();
            image.Columns = columns;
        }

        if (rows != 0)
        {
            image ??= new ParticleImage();
            image.Rows = rows;
        }

        if (frames != 1)
        {
            image ??= new ParticleImage();
            image.Frames = frames;
        }

        if (animated != 0)
        {
            image ??= new ParticleImage();
            image.Animated = animated;
        }

        emitter.Image = image;
        emitter.Flags = flags == 0 ? null : (ParticleFlags)flags;
        emitter.EmitterType = emitterType == 1 ? null : (ParticleEmitterType)emitterType;

        int fieldCount;
        int systemFieldCount;
        if (profile.RecordSize == 0x2B0)
        {
            reader.Skip(376);
            fieldCount = reader.ReadInt32();
            reader.Skip(12);
            systemFieldCount = reader.ReadInt32();
            reader.Skip(260);
        }
        else
        {
            reader.Skip(188);
            fieldCount = reader.ReadInt32();
            reader.Skip(4);
            systemFieldCount = reader.ReadInt32();
            reader.Skip(128);
        }

        return (fieldCount, systemFieldCount);
    }

    private static void ReadBody(
        BufferReader reader,
        ParticleEmitter emitter,
        ParticleBinaryProfile profile,
        int fieldCount,
        int systemFieldCount)
    {
        var system = new ParticleSystemTracks();
        var particle = new ParticleTracks();

        if (profile.IntegerImage)
        {
            var id = reader.ReadInt32();
            if (id != -1)
            {
                emitter.Image ??= new ParticleImage();
                emitter.Image.Id = id;
            }
        }
        else
        {
            var imageName = reader.ReadStringOrEmptyByInt32Head();
            if (!string.IsNullOrEmpty(imageName))
            {
                emitter.Image ??= new ParticleImage();
                emitter.Image.Name = imageName;
            }
        }

        if (profile.HasImagePath)
        {
            var resource = reader.ReadStringOrEmptyByInt32Head();
            if (!string.IsNullOrEmpty(resource))
            {
                emitter.Image ??= new ParticleImage();
                emitter.Image.Resource = resource;
            }
        }

        emitter.Name = XmlText.EmptyToNull(reader.ReadStringOrEmptyByInt32Head());

        system.Duration = ReadTrackNodes(reader);
        emitter.OnDuration = XmlText.EmptyToNull(reader.ReadStringOrEmptyByInt32Head());
        system.CrossFadeDuration = ReadTrackNodes(reader);
        system.SpawnRate = ReadTrackNodes(reader);
        system.SpawnMinActive = ReadTrackNodes(reader);
        system.SpawnMaxActive = ReadTrackNodes(reader);
        system.SpawnMaxLaunched = ReadTrackNodes(reader);
        system.Radius = ReadTrackNodes(reader);
        system.OffsetX = ReadTrackNodes(reader);
        system.OffsetY = ReadTrackNodes(reader);
        system.BoxX = ReadTrackNodes(reader);
        system.BoxY = ReadTrackNodes(reader);
        system.Path = ReadTrackNodes(reader);
        system.SkewX = ReadTrackNodes(reader);
        system.SkewY = ReadTrackNodes(reader);
        particle.Duration = ReadTrackNodes(reader);
        system.Red = ReadTrackNodes(reader);
        system.Green = ReadTrackNodes(reader);
        system.Blue = ReadTrackNodes(reader);
        system.Alpha = ReadTrackNodes(reader);
        system.Brightness = ReadTrackNodes(reader);
        particle.LaunchSpeed = ReadTrackNodes(reader);
        particle.LaunchAngle = ReadTrackNodes(reader);

        emitter.Fields = ReadFields(reader, fieldCount, profile.FieldTypePaddingBytes);
        emitter.SystemFields = ReadFields(reader, systemFieldCount, profile.FieldTypePaddingBytes);

        particle.Red = ReadTrackNodes(reader);
        particle.Green = ReadTrackNodes(reader);
        particle.Blue = ReadTrackNodes(reader);
        particle.Alpha = ReadTrackNodes(reader);
        particle.Brightness = ReadTrackNodes(reader);
        particle.SpinAngle = ReadTrackNodes(reader);
        particle.SpinSpeed = ReadTrackNodes(reader);
        particle.Scale = ReadTrackNodes(reader);
        particle.Stretch = ReadTrackNodes(reader);
        particle.CollisionReflect = ReadTrackNodes(reader);
        particle.CollisionSpin = ReadTrackNodes(reader);
        particle.ClipTop = ReadTrackNodes(reader);
        particle.ClipBottom = ReadTrackNodes(reader);
        particle.ClipLeft = ReadTrackNodes(reader);
        particle.ClipRight = ReadTrackNodes(reader);
        particle.AnimationRate = ReadTrackNodes(reader);

        emitter.System = system;
        emitter.Particle = particle;
    }

    private static List<ParticleTrackNode>? ReadTrackNodes(BufferReader reader)
    {
        var count = reader.ReadInt32();
        if (count <= 0)
        {
            return null;
        }

        var nodes = new List<ParticleTrackNode>(count);
        for (var i = 0; i < count; i++)
        {
            nodes.Add(new ParticleTrackNode
            {
                Time = reader.ReadSingle(),
                Low = reader.ReadSingle(),
                High = reader.ReadSingle(),
                Curve = (PopCurve)reader.ReadInt32(),
                Distribution = (PopCurve)reader.ReadInt32(),
            });
        }

        return nodes;
    }

    private static List<ParticleField>? ReadFields(BufferReader reader, int count, int typePaddingBytes)
    {
        reader.Skip(sizeof(int));
        if (count <= 0)
        {
            return null;
        }

        var fields = new List<ParticleField>(count);
        for (var i = 0; i < count; i++)
        {
            var type = reader.ReadInt32();
            reader.Skip(typePaddingBytes);
            fields.Add(new ParticleField { Type = type == 0 ? null : (ParticleFieldType)type });
        }

        foreach (var field in fields)
        {
            field.X = ReadTrackNodes(reader);
            field.Y = ReadTrackNodes(reader);
        }

        return fields;
    }

    private static ParticleFile ReadWp(byte[] data)
    {
        var reader = new BufferReader(data, false) { ErrorFactory = BinaryErrors.Factory<ArgumentOutOfRangeException>() };
        if (!reader.ReadSpan(WpMagic.Length).SequenceEqual(WpMagic))
        {
            throw new InvalidDataException(LiarUtil.Core.Strings.WPParticleFileHeaderInvalid);
        }

        reader.Skip(sizeof(int));
        if (!reader.ReadSpan(WpInfo.Length).SequenceEqual(WpInfo))
        {
            throw new InvalidDataException(LiarUtil.Core.Strings.WPParticleFileInfoInvalid);
        }

        var count = reader.ReadInt32();
        if (count < 0)
        {
            throw new InvalidDataException(string.Format(LiarUtil.Core.Strings.ParticleEmitterCountInvalid0, count));
        }

        var file = new ParticleFile { Emitters = new List<ParticleEmitter>(count) };
        for (var i = 0; i < count; i++)
        {
            file.Emitters.Add(ReadWpEmitter(reader));
        }

        return file;
    }

    private static ParticleEmitter ReadWpEmitter(BufferReader reader)
    {
        var emitter = new ParticleEmitter();
        var system = new ParticleSystemTracks();
        var particle = new ParticleTracks();

        var imageName = reader.ReadStringOrEmptyByVarInt32Head();
        var columns = reader.ReadInt32();
        var rows = reader.ReadInt32();
        var frames = reader.ReadInt32();
        var animated = reader.ReadInt32();
        var flags = reader.ReadInt32();
        var emitterType = reader.ReadInt32();

        ParticleImage? image = null;
        if (!string.IsNullOrEmpty(imageName))
        {
            image ??= new ParticleImage();
            image.Name = imageName;
        }

        if (columns != 0)
        {
            image ??= new ParticleImage();
            image.Columns = columns;
        }

        if (rows != 0)
        {
            image ??= new ParticleImage();
            image.Rows = rows;
        }

        if (frames != 1)
        {
            image ??= new ParticleImage();
            image.Frames = frames;
        }

        if (animated != 0)
        {
            image ??= new ParticleImage();
            image.Animated = animated;
        }

        emitter.Image = image;
        emitter.Flags = flags == 0 ? null : (ParticleFlags)flags;
        emitter.EmitterType = emitterType == 1 ? null : (ParticleEmitterType)emitterType;
        emitter.Name = XmlText.EmptyToNull(reader.ReadStringOrEmptyByVarInt32Head());
        emitter.OnDuration = XmlText.EmptyToNull(reader.ReadStringOrEmptyByVarInt32Head());

        system.Duration = ReadTrackNodes(reader);
        system.CrossFadeDuration = ReadTrackNodes(reader);
        system.SpawnRate = ReadTrackNodes(reader);
        system.SpawnMinActive = ReadTrackNodes(reader);
        system.SpawnMaxActive = ReadTrackNodes(reader);
        system.SpawnMaxLaunched = ReadTrackNodes(reader);
        system.Radius = ReadTrackNodes(reader);
        system.OffsetX = ReadTrackNodes(reader);
        system.OffsetY = ReadTrackNodes(reader);
        system.BoxX = ReadTrackNodes(reader);
        system.BoxY = ReadTrackNodes(reader);
        system.SkewX = ReadTrackNodes(reader);
        system.SkewY = ReadTrackNodes(reader);
        system.Path = ReadTrackNodes(reader);
        particle.Duration = ReadTrackNodes(reader);
        particle.LaunchSpeed = ReadTrackNodes(reader);
        particle.LaunchAngle = ReadTrackNodes(reader);
        system.Red = ReadTrackNodes(reader);
        system.Green = ReadTrackNodes(reader);
        system.Blue = ReadTrackNodes(reader);
        system.Alpha = ReadTrackNodes(reader);
        system.Brightness = ReadTrackNodes(reader);

        emitter.Fields = ReadWpFields(reader);
        emitter.SystemFields = ReadWpFields(reader);

        particle.Red = ReadTrackNodes(reader);
        particle.Green = ReadTrackNodes(reader);
        particle.Blue = ReadTrackNodes(reader);
        particle.Alpha = ReadTrackNodes(reader);
        particle.Brightness = ReadTrackNodes(reader);
        particle.SpinAngle = ReadTrackNodes(reader);
        particle.SpinSpeed = ReadTrackNodes(reader);
        particle.Scale = ReadTrackNodes(reader);
        particle.Stretch = ReadTrackNodes(reader);
        particle.CollisionReflect = ReadTrackNodes(reader);
        particle.CollisionSpin = ReadTrackNodes(reader);
        particle.ClipTop = ReadTrackNodes(reader);
        particle.ClipBottom = ReadTrackNodes(reader);
        particle.ClipLeft = ReadTrackNodes(reader);
        particle.ClipRight = ReadTrackNodes(reader);
        particle.AnimationRate = ReadTrackNodes(reader);

        emitter.System = system;
        emitter.Particle = particle;
        return emitter;
    }

    private static List<ParticleField>? ReadWpFields(BufferReader reader)
    {
        var count = reader.ReadInt32();
        if (count <= 0)
        {
            return null;
        }

        var fields = new List<ParticleField>(count);
        for (var i = 0; i < count; i++)
        {
            var field = new ParticleField
            {
                X = ReadTrackNodes(reader),
                Y = ReadTrackNodes(reader),
            };
            var type = reader.ReadInt32();
            field.Type = type == 0 ? null : (ParticleFieldType)type;
            fields.Add(field);
        }

        return fields;
    }
}
