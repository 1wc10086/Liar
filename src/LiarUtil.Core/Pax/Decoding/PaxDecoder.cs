using LiarUtil.Core.Core.Binary;
using LiarUtil.Core.Pax.Binary;
using LiarUtil.Core.Pax.Models;

namespace LiarUtil.Core.Pax.Decoding;

internal sealed class PaxDecoder(byte[] data)
{
    private readonly BufferReader _reader = new(data) { ErrorFactory = static message => new PaxException(message) };

    public PaxFile Decode()
    {
        var file = new PaxFile { Version = _reader.ReadInt32() };
        var footageById = ReadFootages(file);

        while (_reader.Remaining > 0)
        {
            var offset = _reader.Position;
            var header = _reader.ReadInt32();
            if (header != PaxFormat.CompositionHeader)
            {
                file.Terminator = new PaxTerminator
                {
                    Offset = offset,
                    Tag = header,
                    Name = header == PaxFormat.EndOfPopFx ? PaxTerminatorKind.EndOfPopFx : PaxTerminatorKind.Unknown,
                };
                break;
            }

            file.Compositions.Add(ReadComposition(file.Version, footageById, offset));
        }

        if (_reader.Remaining != 0)
        {
            throw new PaxException(LiarUtil.Core.Strings.PAXFileHasExtraDataAtEnd);
        }

        BindCompositionIndices(file);
        return file;
    }

    private Dictionary<int, PaxFootage> ReadFootages(PaxFile file)
    {
        var count = PaxRecords.ReadCount(_reader, "footage count");
        var footages = new Dictionary<int, PaxFootage>(count);
        for (var index = 0; index < count; index++)
        {
            var offset = _reader.Position;
            var shortName = PaxRecords.ReadString(_reader, "footage short name");
            var id = _reader.ReadInt32();
            var fullName = PaxRecords.ReadString(_reader, "footage full name");
            var width = _reader.ReadInt32();
            var height = _reader.ReadInt32();
            var footage = new PaxFootage
            {
                Offset = offset,
                ShortName = shortName,
                Id = id,
                FullName = fullName,
                Width = width,
                Height = height,
            };
            file.Footages.Add(footage);
            footages[id] = footage;
        }

        return footages;
    }

    private PaxComposition ReadComposition(int version, Dictionary<int, PaxFootage> footageById, int offset)
    {
        var nameLength = PaxRecords.ReadCount(_reader, "composition name length");
        var nameEncodedText = ReadText(nameLength, "composition name");
        var width = _reader.ReadInt32();
        var height = _reader.ReadInt32();
        var layerCount = PaxRecords.ReadCount(_reader, "composition layer count");
        var duration = _reader.ReadInt32();
        var composition = new PaxComposition
        {
            Offset = offset,
            Name = PaxText.Display(nameEncodedText),
            NameEncodedText = nameEncodedText,
            NameEncodedLength = nameLength,
            Width = width,
            Height = height,
            LayerCount = layerCount,
            Duration = duration,
        };

        for (var index = 0; index < layerCount; index++)
        {
            var layer = ReadLayer(version, duration, index);
            layer.Footage = layer.Type == PaxLayerType.Footage && layer.FootageId is { } id && footageById.TryGetValue(id, out var footage)
                ? footage
                : null;
            PaxDefaults.Apply(layer, width, height);
            composition.Layers.Add(layer);
        }

        return composition;
    }

    private PaxLayer ReadLayer(int version, int compositionDuration, int index)
    {
        if (_reader.ReadInt32() != PaxFormat.LayerHeader)
        {
            throw new PaxException(LiarUtil.Core.Strings.ExpectedPAXLayerHeader);
        }

        if (!_reader.ReadBoolean())
        {
            return new PaxLayer { Index = index };
        }

        var isFootage = _reader.ReadBoolean();
        var footageId = _reader.ReadInt32();
        var folderLength = PaxRecords.ReadCount(_reader, "layer folder length");
        var folderEncodedText = ReadText(folderLength, "layer folder");
        var nameLength = PaxRecords.ReadCount(_reader, "layer name length");
        var nameEncodedText = ReadText(nameLength, "layer name");
        var startFrame = _reader.ReadInt32();
        var sourceDuration = _reader.ReadInt32();
        var duration = _reader.ReadInt32();
        var offset = _reader.ReadInt32();
        if (sourceDuration <= 0)
        {
            throw new PaxException(LiarUtil.Core.Strings.InvalidPAXLayerSourceLength);
        }

        var layer = new PaxLayer
        {
            Index = index,
            Enabled = true,
            Type = isFootage ? PaxLayerType.Footage : PaxLayerType.Composition,
            FootageId = footageId,
            Folder = PaxText.Display(folderEncodedText),
            FolderEncodedText = folderEncodedText,
            FolderEncodedLength = folderLength,
            Name = PaxText.Display(nameEncodedText),
            NameEncodedText = nameEncodedText,
            NameEncodedLength = nameLength,
            StartFrame = startFrame,
            SourceDuration = sourceDuration,
            Duration = duration,
            Offset = offset,
            Additive = version >= 5 && _reader.ReadBoolean(),
            Transforms = new PaxTransformSet(),
        };

        for (;;)
        {
            var transformTag = _reader.ReadInt32();
            if (transformTag == PaxFormat.EndOfLayer)
            {
                break;
            }

            if (!PaxTransformAccess.TryKind(transformTag, out var kind))
            {
                throw new PaxException(string.Format(LiarUtil.Core.Strings.UnknownPAXTransformMarker0, transformTag));
            }

            PaxTransformAccess.Set(layer.Transforms, kind, ReadTransform(kind, transformTag, sourceDuration, duration));
        }

        return layer;
    }

    private PaxTransform ReadTransform(PaxTransformKind kind, int tag, int sourceDuration, int compositionDuration)
    {
        var name = PaxTransformAccess.Name(kind);
        var initial = new PaxRawValue(_reader.ReadDouble(), _reader.ReadDouble());
        var staticFrameCount = PaxRecords.ReadCount(_reader, name + " static frame count");
        var encodedKeyedFrameCount = PaxRecords.ReadCount(_reader, name + " keyed frame count");
        var hasLoop = _reader.ReadBoolean();
        var loopType = _reader.ReadInt32();
        var loopFrame = _reader.ReadInt32();
        if (hasLoop && loopType is not (PaxFormat.LoopRepeat or PaxFormat.LoopPingPong))
        {
            throw new PaxException(string.Format(LiarUtil.Core.Strings.InvalidPAX0LoopType1, name, loopType));
        }

        var staticFrames = new List<PaxRawFrame>();
        var keyedFrames = new List<PaxRawKeyframe>();
        var firstStaticFrame = 0;
        var keyedFrameCount = encodedKeyedFrameCount;
        if (staticFrameCount > 0)
        {
            firstStaticFrame = _reader.ReadInt32();
            keyedFrameCount = 0;
            for (var index = 0; index < staticFrameCount; index++)
            {
                staticFrames.Add(new PaxRawFrame(firstStaticFrame + index, _reader.ReadDouble(), _reader.ReadDouble()));
            }
        }
        else
        {
            for (var index = 0; index < keyedFrameCount; index++)
            {
                var marker = _reader.ReadInt32();
                var sourceFrame = _reader.ReadInt32();
                keyedFrames.Add(new PaxRawKeyframe(
                    marker,
                    sourceFrame,
                    sourceFrame * (compositionDuration / sourceDuration),
                    _reader.ReadDouble(),
                    _reader.ReadDouble()));
            }
        }

        var transform = PaxTransformAccess.Create(kind);
        transform.Tag = tag;
        transform.StaticFrameCount = staticFrameCount;
        transform.FirstStaticFrame = staticFrameCount > 0 ? firstStaticFrame : null;
        transform.EncodedKeyedFrameCount = encodedKeyedFrameCount;
        transform.KeyedFrameCount = keyedFrameCount;
        transform.EncodedLoopType = loopType;
        transform.EncodedLoopFrame = loopFrame;
        transform.Loop = hasLoop
            ? new PaxLoop
            {
                Type = loopType == PaxFormat.LoopRepeat ? PaxLoopType.Repeat : PaxLoopType.PingPong,
                Frame = loopFrame,
            }
            : null;
        transform.Fill(initial, staticFrames, keyedFrames);
        return transform;
    }

    private string ReadText(int length, string field)
    {
        if (length > _reader.Remaining)
        {
            throw new PaxException(string.Format(LiarUtil.Core.Strings.PAX0LengthInvalid, field));
        }
        return PaxText.Decode(_reader.ReadSpan(length));
    }

    private static void BindCompositionIndices(PaxFile file)
    {
        var byName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < file.Compositions.Count; index++)
        {
            byName[file.Compositions[index].Name] = index;
        }

        foreach (var composition in file.Compositions)
        {
            foreach (var layer in composition.Layers)
            {
                if (layer.Enabled && layer.Type == PaxLayerType.Composition && layer.Name is { } name && byName.TryGetValue(name, out var index))
                {
                    layer.CompositionIndex = index;
                }
            }
        }
    }
}
