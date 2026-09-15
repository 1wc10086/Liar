namespace LiarUtil.Core.Pax.Models;

public abstract class PaxTransform
{
    public int Tag { get; set; }

    public bool Defaulted { get; set; }

    public int StaticFrameCount { get; set; }

    public int? FirstStaticFrame { get; set; }

    public int EncodedKeyedFrameCount { get; set; }

    public int KeyedFrameCount { get; set; }

    public int EncodedLoopType { get; set; }

    public int EncodedLoopFrame { get; set; }

    public PaxLoop? Loop { get; set; }

    internal abstract int FrameCount { get; }

    internal abstract PaxRawValue InitialRaw();

    internal abstract IReadOnlyList<PaxRawFrame> StaticRawFrames();

    internal abstract IReadOnlyList<PaxRawKeyframe> KeyedRawFrames(int sourceDuration, int compositionDuration);

    internal abstract void Fill(PaxRawValue initial, IReadOnlyList<PaxRawFrame> staticFrames, IReadOnlyList<PaxRawKeyframe> keyedFrames);

    protected static int Marker(int? marker) => marker ?? PaxFormat.KeyframeMarker;

    protected static int SourceFrame(int frame, int? sourceFrame, int sourceDuration, int compositionDuration) =>
        sourceFrame ?? (int)Math.Round(frame * (double)sourceDuration / compositionDuration, MidpointRounding.AwayFromZero);
}
