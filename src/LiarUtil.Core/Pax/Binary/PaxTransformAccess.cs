using LiarUtil.Core.Pax.Models;

namespace LiarUtil.Core.Pax.Binary;

internal static class PaxTransformAccess
{
    public static readonly PaxTransformKind[] Order =
    [
        PaxTransformKind.AnchorPoint,
        PaxTransformKind.Position,
        PaxTransformKind.Scale,
        PaxTransformKind.Rotation,
        PaxTransformKind.Opacity,
    ];

    public static string Name(PaxTransformKind kind) => kind switch
    {
        PaxTransformKind.AnchorPoint => "anchorPoint",
        PaxTransformKind.Position => "position",
        PaxTransformKind.Scale => "scale",
        PaxTransformKind.Rotation => "rotation",
        _ => "opacity",
    };

    public static int Tag(PaxTransformKind kind) => kind switch
    {
        PaxTransformKind.AnchorPoint => PaxFormat.AnchorPoint,
        PaxTransformKind.Position => PaxFormat.Position,
        PaxTransformKind.Scale => PaxFormat.Scale,
        PaxTransformKind.Rotation => PaxFormat.Rotation,
        _ => PaxFormat.Opacity,
    };

    public static bool TryKind(int tag, out PaxTransformKind kind)
    {
        switch (tag)
        {
            case PaxFormat.AnchorPoint:
                kind = PaxTransformKind.AnchorPoint;
                return true;
            case PaxFormat.Position:
                kind = PaxTransformKind.Position;
                return true;
            case PaxFormat.Scale:
                kind = PaxTransformKind.Scale;
                return true;
            case PaxFormat.Rotation:
                kind = PaxTransformKind.Rotation;
                return true;
            case PaxFormat.Opacity:
                kind = PaxTransformKind.Opacity;
                return true;
            default:
                kind = default;
                return false;
        }
    }

    public static PaxTransform Create(PaxTransformKind kind) => kind switch
    {
        PaxTransformKind.AnchorPoint => new PaxAnchorPointTransform(),
        PaxTransformKind.Position => new PaxPositionTransform(),
        PaxTransformKind.Scale => new PaxScaleTransform(),
        PaxTransformKind.Rotation => new PaxRotationTransform(),
        _ => new PaxOpacityTransform(),
    };

    public static PaxTransform? Get(PaxTransformSet set, PaxTransformKind kind) => kind switch
    {
        PaxTransformKind.AnchorPoint => set.AnchorPoint,
        PaxTransformKind.Position => set.Position,
        PaxTransformKind.Scale => set.Scale,
        PaxTransformKind.Rotation => set.Rotation,
        _ => set.Opacity,
    };

    public static void Set(PaxTransformSet set, PaxTransformKind kind, PaxTransform transform)
    {
        switch (kind)
        {
            case PaxTransformKind.AnchorPoint:
                set.AnchorPoint = (PaxAnchorPointTransform)transform;
                break;
            case PaxTransformKind.Position:
                set.Position = (PaxPositionTransform)transform;
                break;
            case PaxTransformKind.Scale:
                set.Scale = (PaxScaleTransform)transform;
                break;
            case PaxTransformKind.Rotation:
                set.Rotation = (PaxRotationTransform)transform;
                break;
            default:
                set.Opacity = (PaxOpacityTransform)transform;
                break;
        }
    }
}
