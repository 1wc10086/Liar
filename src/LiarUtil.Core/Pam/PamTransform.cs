using System.Text.Json.Serialization;

namespace LiarUtil.Core.Pam;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(PamTranslateTransform), "translate")]
[JsonDerivedType(typeof(PamRotateTransform), "rotate")]
[JsonDerivedType(typeof(PamMatrixTransform), "matrix")]
public abstract class PamTransform
{
    public float X { get; set; }

    public float Y { get; set; }
}

public sealed class PamTranslateTransform : PamTransform;

public sealed class PamRotateTransform : PamTransform
{
    public float Angle { get; set; }
}

public sealed class PamMatrixTransform : PamTransform
{
    public float A { get; set; }

    public float B { get; set; }

    public float C { get; set; }

    public float D { get; set; }
}
