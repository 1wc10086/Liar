namespace LiarUtil.Core.Pax.Models;

public sealed class PaxOpacityTransform : PaxTransform
{
    public double? InitialValue { get; set; }

    public List<PaxNumberKeyframe> Frames { get; set; } = [];

    internal override int FrameCount => Frames.Count;

    internal override PaxRawValue InitialRaw() => new((InitialValue ?? FirstValue()) * 100, 0);

    internal override IReadOnlyList<PaxRawFrame> StaticRawFrames()
    {
        var frames = new List<PaxRawFrame>(Frames.Count);
        foreach (var frame in Frames)
        {
            frames.Add(new PaxRawFrame(frame.Frame, frame.Value * 100, 0));
        }
        return frames;
    }

    internal override IReadOnlyList<PaxRawKeyframe> KeyedRawFrames(int sourceDuration, int compositionDuration)
    {
        var frames = new List<PaxRawKeyframe>(Math.Max(Frames.Count - 1, 0));
        for (var index = 1; index < Frames.Count; index++)
        {
            var frame = Frames[index];
            frames.Add(new PaxRawKeyframe(
                Marker(frame.Marker),
                SourceFrame(frame.Frame, frame.SourceFrame, sourceDuration, compositionDuration),
                frame.Frame,
                frame.Value * 100,
                0));
        }
        return frames;
    }

    internal override void Fill(PaxRawValue initial, IReadOnlyList<PaxRawFrame> staticFrames, IReadOnlyList<PaxRawKeyframe> keyedFrames)
    {
        InitialValue = initial.First / 100;
        Frames.Clear();
        if (staticFrames.Count == 0)
        {
            Frames.Add(new PaxNumberKeyframe { Frame = 0, Value = InitialValue.Value });
        }
        foreach (var frame in staticFrames)
        {
            Frames.Add(new PaxNumberKeyframe { Frame = frame.Frame, Value = frame.First / 100 });
        }
        foreach (var frame in keyedFrames)
        {
            Frames.Add(new PaxNumberKeyframe
            {
                Marker = frame.Marker,
                SourceFrame = frame.SourceFrame,
                Frame = frame.Frame,
                Value = frame.First / 100,
            });
        }
    }

    private double FirstValue()
    {
        if (Frames.Count == 0)
        {
            throw new PaxException(LiarUtil.Core.Strings.PAXTransformHasNoKeyframes);
        }
        return Frames[0].Value;
    }
}
