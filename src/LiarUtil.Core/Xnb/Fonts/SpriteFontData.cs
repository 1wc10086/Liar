namespace LiarUtil.Core.Xnb.Fonts;

public sealed class SpriteFontData
{
    public FontTexture Texture { get; set; } = new();

    public List<FontRectangle> Glyphs { get; set; } = [];

    public List<FontRectangle> Cropping { get; set; } = [];

    public List<char> Characters { get; set; } = [];

    public int LineSpacing { get; set; }

    public float Spacing { get; set; }

    public List<FontVector3> Kerning { get; set; } = [];

    public char? DefaultCharacter { get; set; }
}
