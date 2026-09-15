namespace LiarUtil.Core.Cfw2.Models;

public sealed class Cfw2FontCharacter
{
    public Cfw2Unicode Index { get; set; }

    public Cfw2Rectangle ImageRect { get; set; } = new();

    public Cfw2Point ImageOffset { get; set; } = new();

    public ushort KerningFirst { get; set; }

    public ushort KerningCount { get; set; }

    public int Width { get; set; }

    public int Order { get; set; }
}
