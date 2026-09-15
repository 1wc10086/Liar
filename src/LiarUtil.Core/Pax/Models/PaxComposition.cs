namespace LiarUtil.Core.Pax.Models;

public sealed class PaxComposition
{
    public int Offset { get; set; }

    public string Name { get; set; } = "";

    public string? NameEncodedText { get; set; }

    public int? NameEncodedLength { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public int LayerCount { get; set; }

    public int Duration { get; set; }

    public List<PaxLayer> Layers { get; set; } = [];
}
