using System.Text.Json.Serialization;
using LiarUtil.Core.Reanim.Json;
using LiarUtil.Core.PopCap;

namespace LiarUtil.Core.Reanim;

[JsonConverter(typeof(ReanimTransformJsonConverter))]
public sealed class ReanimTransform
{
    public float? X { get; set; }

    public float? Y { get; set; }

    public float? SkewX { get; set; }

    public float? SkewY { get; set; }

    public float? ScaleX { get; set; }

    public float? ScaleY { get; set; }

    public float? Frame { get; set; }

    public float? Alpha { get; set; }

    public ImageReference? Image { get; set; }

    public string? ImageResource { get; set; }

    public ImageReference? Image2 { get; set; }

    public string? Image2Resource { get; set; }

    public string? Font { get; set; }

    public string? Text { get; set; }

    internal bool IsEmpty =>
        X is null && Y is null && SkewX is null && SkewY is null && ScaleX is null && ScaleY is null &&
        Frame is null && Alpha is null && Image is null && ImageResource is null && Image2 is null &&
        Image2Resource is null && Font is null && Text is null;
}
