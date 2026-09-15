namespace LiarUtil.Core.Cfu2.Models;

public sealed class Cfu2FontCharacter
{
    public Cfu2Unicode Index { get; set; }

    public Cfu2Rectangle ImageRect { get; set; } = new();

    public Cfu2Point ImageOffset { get; set; } = new();

    public ushort KerningCount { get; set; }

    public ushort KerningFirst { get; set; }

    public int Width { get; set; }
}
