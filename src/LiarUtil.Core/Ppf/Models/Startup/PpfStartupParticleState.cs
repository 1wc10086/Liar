namespace LiarUtil.Core.Ppf.Models;

public sealed class PpfStartupParticleState
{
    public short ParticleDefIndex { get; set; }
    public float Ticks { get; set; }
    public float Life { get; set; }
    public float LifePercent { get; set; }
    public float Zoom { get; set; }
    public PpfDoublePoint Position { get; set; } = new();
    public PpfDoublePoint Velocity { get; set; } = new();
    public PpfDoublePoint EmittedPosition { get; set; } = new();
    public PpfDoublePoint OriginalPosition { get; set; } = new();
    public float OriginalEmitterAngle { get; set; }
    public float ImageAngle { get; set; }
    public short VariationFlags { get; set; }
    public List<float> Variations { get; set; } = [];
    public float SourceSizeXMultiplier { get; set; }
    public float SourceSizeYMultiplier { get; set; }
    public float GradientRandom { get; set; }
    public short AnimationFrameRandom { get; set; }
    public float ThicknessHitVariation { get; set; }
}
