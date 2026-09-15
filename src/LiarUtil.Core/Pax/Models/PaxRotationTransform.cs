namespace LiarUtil.Core.Pax.Models;

public sealed class PaxRotationTransform : PaxTransform
{
    public PaxRotation? InitialValue { get; set; }

    public List<PaxRotationKeyframe> Frames { get; set; } = [];

    internal override int FrameCount => Frames.Count;

    internal override PaxRawValue InitialRaw() => ToRaw(InitialValue ?? FirstValue());

    internal override IReadOnlyList<PaxRawFrame> StaticRawFrames()
    {
        var frames = new List<PaxRawFrame>(Frames.Count);
        foreach (var frame in Frames)
        {
            var raw = ToRaw(frame.Value);
            frames.Add(new PaxRawFrame(frame.Frame, raw.First, raw.Second));
        }
        return frames;
    }

    internal override IReadOnlyList<PaxRawKeyframe> KeyedRawFrames(int sourceDuration, int compositionDuration)
    {
        var frames = new List<PaxRawKeyframe>(Math.Max(Frames.Count - 1, 0));
        for (var index = 1; index < Frames.Count; index++)
        {
            var frame = Frames[index];
            var raw = ToRaw(frame.Value);
            frames.Add(new PaxRawKeyframe(
                Marker(frame.Marker),
                SourceFrame(frame.Frame, frame.SourceFrame, sourceDuration, compositionDuration),
                frame.Frame,
                raw.First,
                raw.Second));
        }
        return frames;
    }

    internal override void Fill(PaxRawValue initial, IReadOnlyList<PaxRawFrame> staticFrames, IReadOnlyList<PaxRawKeyframe> keyedFrames)
    {
        InitialValue = FromRaw(initial);
        Frames.Clear();
        if (staticFrames.Count == 0)
        {
            Frames.Add(new PaxRotationKeyframe { Frame = 0, Value = InitialValue });
        }
        foreach (var frame in staticFrames)
        {
            Frames.Add(new PaxRotationKeyframe { Frame = frame.Frame, Value = FromRaw(new PaxRawValue(frame.First, frame.Second)) });
        }
        foreach (var frame in keyedFrames)
        {
            Frames.Add(new PaxRotationKeyframe
            {
                Marker = frame.Marker,
                SourceFrame = frame.SourceFrame,
                Frame = frame.Frame,
                Value = FromRaw(new PaxRawValue(frame.First, frame.Second)),
            });
        }
    }

    private static PaxRawValue ToRaw(PaxRotation? value) => value is null ? default : new PaxRawValue(value.Degrees, 0);

    private static PaxRotation FromRaw(PaxRawValue raw) => new() { Degrees = raw.First };

    private PaxRotation FirstValue()
    {
        if (Frames.Count == 0)
        {
            throw new PaxException(LiarUtil.Core.Strings.PAXTransformHasNoKeyframes);
        }
        return Frames[0].Value ?? throw new PaxException(LiarUtil.Core.Strings.PAXTransformKeyframeMissingValues);
    }
}
