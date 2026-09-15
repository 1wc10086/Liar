namespace LiarUtil.Core.Pax.Models;

public sealed class PaxTransformSet
{
    public PaxAnchorPointTransform? AnchorPoint { get; set; }

    public PaxPositionTransform? Position { get; set; }

    public PaxScaleTransform? Scale { get; set; }

    public PaxRotationTransform? Rotation { get; set; }

    public PaxOpacityTransform? Opacity { get; set; }
}
