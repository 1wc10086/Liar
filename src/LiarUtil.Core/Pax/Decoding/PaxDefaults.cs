using LiarUtil.Core.Pax.Models;

namespace LiarUtil.Core.Pax.Decoding;

internal static class PaxDefaults
{
    public static void Apply(PaxLayer layer, int width, int height)
    {
        if (!layer.Enabled || layer.Transforms is not { } transforms)
        {
            return;
        }

        transforms.Opacity ??= Opacity();
        transforms.Rotation ??= Rotation();
        transforms.Scale ??= Point(new PaxScaleTransform(), 1, 1);
        transforms.Position ??= Point(new PaxPositionTransform(), width / 2.0, height / 2.0);
        if (layer.Type == PaxLayerType.Footage && transforms.AnchorPoint is null && layer.Footage is { } footage)
        {
            transforms.AnchorPoint = Point(new PaxAnchorPointTransform(), footage.Width / 2.0, footage.Height / 2.0);
        }
    }

    private static TTransform Point<TTransform>(TTransform transform, double x, double y)
        where TTransform : PaxPointTransform
    {
        var value = new PaxPoint { X = x, Y = y };
        transform.Defaulted = true;
        transform.InitialValue = value;
        transform.Frames = [new PaxPointKeyframe { Frame = 0, Value = value }];
        return transform;
    }

    private static PaxRotationTransform Rotation()
    {
        var value = new PaxRotation();
        return new PaxRotationTransform
        {
            Defaulted = true,
            InitialValue = value,
            Frames = [new PaxRotationKeyframe { Frame = 0, Value = value }],
        };
    }

    private static PaxOpacityTransform Opacity() => new()
    {
        Defaulted = true,
        InitialValue = 1,
        Frames = [new PaxNumberKeyframe { Frame = 0, Value = 1 }],
    };
}
