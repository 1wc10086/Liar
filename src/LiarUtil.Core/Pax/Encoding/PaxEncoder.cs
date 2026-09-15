using LiarUtil.Core.Core.Binary;
using LiarUtil.Core.Pax.Binary;
using LiarUtil.Core.Pax.Models;

namespace LiarUtil.Core.Pax.Encoding;

internal sealed class PaxEncoder
{
    private readonly BufferWriter _writer = new();

    public byte[] Encode(PaxFile file)
    {
        if (file.Version is < 1 or > 5)
        {
            throw new PaxException(LiarUtil.Core.Strings.UnsupportedPAXVersion);
        }

        RequireCount(file.Footages.Count, "footage count");
        RequireCount(file.Compositions.Count, "composition count");
        _writer.WriteInt32(file.Version);
        _writer.WriteInt32(file.Footages.Count);
        foreach (var footage in file.Footages)
        {
            WriteFootage(footage);
        }
        foreach (var composition in file.Compositions)
        {
            WriteComposition(file.Version, composition);
        }
        _writer.WriteInt32(PaxFormat.EndOfPopFx);
        return _writer.ToArray();
    }

    private void WriteFootage(PaxFootage footage)
    {
        PaxRecords.WriteString(_writer, footage.ShortName);
        _writer.WriteInt32(footage.Id);
        PaxRecords.WriteString(_writer, footage.FullName);
        _writer.WriteInt32(footage.Width);
        _writer.WriteInt32(footage.Height);
    }

    private void WriteComposition(int version, PaxComposition composition)
    {
        RequireCount(composition.Layers.Count, "composition layer count");
        var name = PaxText.Encoded(composition.Name, composition.NameEncodedText);
        _writer.WriteInt32(PaxFormat.CompositionHeader);
        PaxRecords.WriteSizedString(_writer, name, composition.NameEncodedLength, "composition name");
        _writer.WriteInt32(composition.Width);
        _writer.WriteInt32(composition.Height);
        _writer.WriteInt32(composition.Layers.Count);
        _writer.WriteInt32(composition.Duration);
        foreach (var layer in composition.Layers)
        {
            WriteLayer(version, composition.Duration, layer);
        }
    }

    private void WriteLayer(int version, int compositionDuration, PaxLayer layer)
    {
        _writer.WriteInt32(PaxFormat.LayerHeader);
        _writer.WriteBoolean(layer.Enabled);
        if (!layer.Enabled)
        {
            return;
        }

        if (layer.Type is not (PaxLayerType.Footage or PaxLayerType.Composition))
        {
            throw new PaxException(LiarUtil.Core.Strings.InvalidPAXLayerType);
        }

        var sourceDuration = Require(layer.SourceDuration, LiarUtil.Core.Strings.LayerSourceLength);
        if (sourceDuration <= 0)
        {
            throw new PaxException(LiarUtil.Core.Strings.InvalidPAXLayerSourceLength);
        }

        var name = PaxText.Encoded(layer.Name ?? "", layer.NameEncodedText);
        var folder = PaxText.Encoded(layer.Folder ?? "", layer.FolderEncodedText);
        _writer.WriteBoolean(layer.Type == PaxLayerType.Footage);
        _writer.WriteInt32(Require(layer.FootageId, LiarUtil.Core.Strings.LayerFootageId));
        PaxRecords.WriteSizedString(_writer, folder, layer.FolderEncodedLength, "layer folder");
        PaxRecords.WriteSizedString(_writer, name, layer.NameEncodedLength, "layer name");
        _writer.WriteInt32(Require(layer.StartFrame, LiarUtil.Core.Strings.LayerStartFrame));
        _writer.WriteInt32(sourceDuration);
        _writer.WriteInt32(Require(layer.Duration, LiarUtil.Core.Strings.LayerDuration));
        _writer.WriteInt32(Require(layer.Offset, LiarUtil.Core.Strings.LayerOffset));
        if (version >= 5)
        {
            _writer.WriteBoolean(layer.Additive == true);
        }

        if (layer.Transforms is { } transforms)
        {
            foreach (var kind in PaxTransformAccess.Order)
            {
                if (PaxTransformAccess.Get(transforms, kind) is { Defaulted: false } transform)
                {
                    WriteTransform(kind, transform, sourceDuration, compositionDuration);
                }
            }
        }

        _writer.WriteInt32(PaxFormat.EndOfLayer);
    }

    private void WriteTransform(PaxTransformKind kind, PaxTransform transform, int sourceDuration, int compositionDuration)
    {
        var name = PaxTransformAccess.Name(kind);
        if (transform.FrameCount == 0)
        {
            throw new PaxException(string.Format(LiarUtil.Core.Strings.InvalidPAX0Transform, name));
        }

        var initial = transform.InitialRaw();
        var staticCount = transform.StaticFrameCount;
        if (staticCount is < 0 or > PaxFormat.MaxRecords)
        {
            throw new PaxException(string.Format(LiarUtil.Core.Strings.InvalidPAX0StaticFrameCount, name));
        }
        if (staticCount > 0 && transform.FrameCount != staticCount)
        {
            throw new PaxException(string.Format(LiarUtil.Core.Strings.PAX0StaticFrameCountInconsistentWithFrame, name));
        }

        _writer.WriteInt32(transform.Tag == 0 ? PaxTransformAccess.Tag(kind) : transform.Tag);
        _writer.WriteDouble(initial.First);
        _writer.WriteDouble(initial.Second);
        _writer.WriteInt32(staticCount);
        _writer.WriteInt32(staticCount > 0 ? transform.EncodedKeyedFrameCount : transform.FrameCount - 1);
        _writer.WriteBoolean(transform.Loop is not null);
        _writer.WriteInt32(LoopType(transform));
        _writer.WriteInt32(LoopFrame(transform));
        if (staticCount > 0)
        {
            _writer.WriteInt32(Require(transform.FirstStaticFrame, name + " first static frame"));
            foreach (var frame in transform.StaticRawFrames())
            {
                _writer.WriteDouble(frame.First);
                _writer.WriteDouble(frame.Second);
            }
            return;
        }

        foreach (var frame in transform.KeyedRawFrames(sourceDuration, compositionDuration))
        {
            _writer.WriteInt32(frame.Marker);
            _writer.WriteInt32(frame.SourceFrame);
            _writer.WriteDouble(frame.First);
            _writer.WriteDouble(frame.Second);
        }
    }

    private static int LoopType(PaxTransform transform) => transform.Loop is { } loop
        ? loop.Type == PaxLoopType.Repeat ? PaxFormat.LoopRepeat : PaxFormat.LoopPingPong
        : transform.EncodedLoopType;

    private static int LoopFrame(PaxTransform transform) =>
        transform.Loop?.Frame ?? transform.EncodedLoopFrame;

    private static int Require(int? value, string field) =>
        value ?? throw new PaxException(string.Format(LiarUtil.Core.Strings.InvalidPAX0, field));

    private static void RequireCount(int value, string field)
    {
        if (value is < 0 or > PaxFormat.MaxRecords)
        {
            throw new PaxException(string.Format(LiarUtil.Core.Strings.InvalidPAX0, field));
        }
    }
}
