namespace LiarUtil.Core.FontWidgetDat.Models;

public sealed class FontWidgetDatFile
{
    public FontWidgetDatHeader Header { get; set; } = new();

    public List<FontWidgetDatLayer> Layers { get; set; } = [];
}

public sealed class FontWidgetDatHeader
{
    public int PointSize { get; set; }

    public int Unknown { get; set; }

    public ushort LayerCount { get; set; }
}
