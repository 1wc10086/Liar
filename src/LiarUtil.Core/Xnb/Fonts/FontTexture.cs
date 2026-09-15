namespace LiarUtil.Core.Xnb.Fonts;

public sealed class FontTexture
{
    public int Format { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public List<byte[]> Mipmaps { get; set; } = [];
}
