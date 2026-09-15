namespace LiarUtil.Core.Atlas.Formats;

internal sealed class ImageDatEntry
{
    public string Id { get; set; } = "";

    public string Parent { get; set; } = "";

    public int X { get; set; }

    public int Y { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public int XOffset { get; set; }
}
