namespace LiarUtil.Core.Ppf.Models;

public sealed class PpfEmitterParticle
{
    public string Name { get; set; } = "";
    public int Texture { get; set; }
    public bool Instance { get; set; }
    public bool Single { get; set; }
    public bool FlipHorizontal { get; set; }
    public bool FlipVertical { get; set; }
    public bool LockAspect { get; set; }
    public int AnimationSpeed { get; set; }
    public bool AnimationStartOnRandomFrame { get; set; }
    public bool UseEmitterAngleAndRange { get; set; }
    public bool AttachToEmitter { get; set; }
    public float AttachValue { get; set; }
    public PpfVector2 ReferencePointOffset { get; set; } = new();
    public int AngleValue { get; set; }
    public int AngleRange { get; set; }
    public int AngleOffset { get; set; }
    public bool AngleRandomAlign { get; set; }
    public int AngleAlignOffset { get; set; }
    public bool AngleAlignToMotion { get; set; }
    public bool AngleKeepAlignedToMotion { get; set; }
    public int RepeatColor { get; set; }
    public bool RandomGradientColor { get; set; }
    public int NumberOfEachColor { get; set; }
    public bool UseKeyColorOnly { get; set; }
    public bool UseNextColorKey { get; set; }
    public bool GetColorFromLayer { get; set; }
    public bool UpdateColorFromLayer { get; set; }
    public List<PpfColorPoint> Colors { get; set; } = [];
    public int RepeatAlpha { get; set; }
    public bool PreserveColor { get; set; }
    public bool LinkTransparencyToColor { get; set; }
    public bool GetTransparencyFromLayer { get; set; }
    public bool UpdateTransparencyFromLayer { get; set; }
    public List<PpfAlphaPoint> Alphas { get; set; } = [];
    public PpfParticleValues Values { get; set; } = new();
    public int Unknown1 { get; set; }
    public int Unknown2 { get; set; }
    public int Unknown3 { get; set; }
    public float Unknown4 { get; set; }
    public int Unknown5 { get; set; }
    public int Unknown6 { get; set; }
    public int Unknown7 { get; set; }
    public int Unknown8 { get; set; }
    public int Unknown9 { get; set; }
    public int Unknown10 { get; set; }
    public int Unknown11 { get; set; }
    public int Unknown12 { get; set; }
    public int Unknown13 { get; set; }
    public int Unknown14 { get; set; }
    public int Unknown15 { get; set; }
    public int Unknown16 { get; set; }
    public int Unknown17 { get; set; }
    public int Unknown18 { get; set; }
    public int Unknown19 { get; set; }
    public int Unknown20 { get; set; }
    public PpfValue1 Unknown21 { get; set; } = new();
}
