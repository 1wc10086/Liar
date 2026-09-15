namespace LiarUtil.Core.FontWidgetDat.Models;

public sealed class FontWidgetDatGlyph
{
    public ushort CodePoint { get; set; }

    public string? Character { get; set; }

    public ushort AtlasMarker { get; set; }

    public int ImageRectX { get; set; }

    public int ImageRectY { get; set; }

    public int ImageRectWidth { get; set; }

    public int ImageRectHeight { get; set; }

    public int ImageOffsetX { get; set; }

    public int ImageOffsetY { get; set; }

    public int Width { get; set; }

    public int Order { get; set; }

    public List<FontWidgetDatKerning> Kerning { get; set; } = [];
}

public sealed class FontWidgetDatKerning
{
    public ushort LeftCodePoint { get; set; }

    public string? Left { get; set; }

    public ushort RightCodePoint { get; set; }

    public string? Right { get; set; }

    public short Offset { get; set; }

    public ushort Unknown16 { get; set; }
}
