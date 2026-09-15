using System.Text.Json.Serialization;

namespace LiarUtil.Core.Pam.Xfl;

internal sealed class PamXflExtra
{
    [JsonPropertyName("version")]
    public int Version { get; set; } = PamXflConstants.DefaultVersion;

    [JsonPropertyName("frame_rate")]
    public int FrameRate { get; set; } = PamXflConstants.DefaultFrameRate;

    [JsonPropertyName("position")]
    public double[] Position { get; set; } = [0, 0];

    [JsonPropertyName("image")]
    public List<PamXflExtraImage> Image { get; set; } = [];

    [JsonPropertyName("sprite")]
    public List<PamXflExtraSprite> Sprite { get; set; } = [];

    [JsonPropertyName("main_sprite")]
    public PamXflExtraSprite? MainSprite { get; set; }
}

internal sealed class PamXflExtraImage
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("size")]
    public int[]? Size { get; set; }
}

internal sealed class PamXflExtraSprite
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("frame_rate")]
    public double FrameRate { get; set; }

    [JsonPropertyName("work_area")]
    public int[]? WorkArea { get; set; }
}
