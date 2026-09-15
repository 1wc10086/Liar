namespace LiarUtil.Core.Cfw2.Models;

public sealed class Cfw2FontLayer
{
    public string Name { get; set; } = "";

    public List<string> RequiredTags { get; set; } = [];

    public List<string> ExcludedTags { get; set; } = [];

    public List<Cfw2FontKerning> Kernings { get; set; } = [];

    public List<Cfw2FontCharacter> Characters { get; set; } = [];

    public Cfw2Color MultiplyColor { get; set; } = new();

    public Cfw2Color AddColor { get; set; } = new();

    public string ImageFile { get; set; } = "";

    public int DrawMode { get; set; }

    public Cfw2Point Offset { get; set; } = new();

    public int Spacing { get; set; }

    public int MinimumPointSize { get; set; }

    public int MaximumPointSize { get; set; }

    public int PointSize { get; set; }

    public int Ascent { get; set; }

    public int AscentPadding { get; set; }

    public int Height { get; set; }

    public int DefaultHeight { get; set; }

    public int LineSpacingOffset { get; set; }

    public int BaseOrder { get; set; }
}
