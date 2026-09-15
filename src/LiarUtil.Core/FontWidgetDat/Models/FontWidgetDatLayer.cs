namespace LiarUtil.Core.FontWidgetDat.Models;

public sealed class FontWidgetDatLayer
{
    public string Name { get; set; } = "";

    public int Reserved32 { get; set; }

    public ushort Reserved16 { get; set; }

    public List<int> ReservedHeader32 { get; set; } = [];

    public ushort ReservedBeforeGlyphTable16 { get; set; }

    public List<FontWidgetDatGlyph> Glyphs { get; set; } = [];

    public List<FontWidgetDatKerning> Kerning { get; set; } = [];

    public FontWidgetDatColor Multiply { get; set; } = new();

    public FontWidgetDatColor Add { get; set; } = new();

    public string ImageFile { get; set; } = "";

    public int DrawMode { get; set; }

    public int OffsetX { get; set; }

    public int OffsetY { get; set; }

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

    public bool Initialized { get; set; }
}

public sealed class FontWidgetDatColor
{
    public int Red { get; set; }

    public int Green { get; set; }

    public int Blue { get; set; }

    public int Alpha { get; set; }
}
