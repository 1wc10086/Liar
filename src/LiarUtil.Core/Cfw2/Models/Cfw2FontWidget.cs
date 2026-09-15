namespace LiarUtil.Core.Cfw2.Models;

public sealed class Cfw2FontWidget
{
    public int Ascent { get; set; }

    public int AscentPadding { get; set; }

    public int Height { get; set; }

    public int LineSpacingOffset { get; set; }

    public bool Initialized { get; set; }

    public int DefaultPointSize { get; set; }

    public List<Cfw2CharacterItem> Characters { get; set; } = [];

    public List<Cfw2FontLayer> Layers { get; set; } = [];

    public string SourceFile { get; set; } = "";

    public string ErrorHeader { get; set; } = "";

    public int PointSize { get; set; }

    public List<string> Tags { get; set; } = [];

    public double Scale { get; set; }

    public bool ForceScaledImageWhite { get; set; }

    public bool ActivateAllLayers { get; set; }
}
