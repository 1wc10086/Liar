namespace LiarUtil.Core.Rsb.Resources;

internal sealed class RsbResourceDefinitionTexture
{
    public ushort Type { get; set; }

    public ushort Flags { get; set; }

    public ushort X { get; set; }

    public ushort Y { get; set; }

    public ushort AnchorX { get; set; }

    public ushort AnchorY { get; set; }

    public ushort AnchorWidth { get; set; }

    public ushort AnchorHeight { get; set; }

    public ushort Rows { get; set; } = 1;

    public ushort Cols { get; set; } = 1;

    public string Parent { get; set; } = "";
}
