namespace LiarUtil.Core.Particles;

internal static class ParticleNames
{
    public static readonly string[] EmitterTypes =
    [
        "Circle", "Box", "BoxPath", "CirclePath", "CircleEvenSpacing",
    ];

    public static readonly string[] Curves =
    [
        "Constant", "Linear", "EaseIn", "EaseOut", "EaseInOut", "EaseInOutWeak", "FastInOut",
        "FastInOutWeak", "WeakFastInOut", "Bounce", "BounceFastMiddle", "BounceSlowMiddle", "SinWave",
        "EaseSinWave",
    ];

    public static readonly string[] FieldTypes =
    [
        "Invalid", "Friction", "Acceleration", "Attractor", "MaxVelocity", "Velocity", "Position",
        "SystemPosition", "GroundConstraint", "Shake", "Circle", "Away",
    ];

    public static readonly string[] Flags =
    [
        "RandomLaunchSpin", "AlignLaunchSpin", "AlignToPixel", "SystemLoops", "ParticleLoops",
        "ParticlesDontFollow", "RandomStartTime", "DieIfOverloaded", "Additive", "FullScreen",
        "SoftwareOnly", "HardwareOnly",
    ];
}
