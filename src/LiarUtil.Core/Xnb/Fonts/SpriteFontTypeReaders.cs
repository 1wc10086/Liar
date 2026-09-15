namespace LiarUtil.Core.Xnb.Fonts;

internal static class SpriteFontTypeReaders
{
    public static IReadOnlyList<XnbTypeReader> Readers { get; } =
    [
        new("Microsoft.Xna.Framework.Content.SpriteFontReader, Microsoft.Xna.Framework.Graphics, Version=4.0.0.0, Culture=neutral, PublicKeyToken=842cf8be1de50553", 0),
        new("Microsoft.Xna.Framework.Content.Texture2DReader, Microsoft.Xna.Framework.Graphics, Version=4.0.0.0, Culture=neutral, PublicKeyToken=842cf8be1de50553", 0),
        new("Microsoft.Xna.Framework.Content.ListReader`1[[Microsoft.Xna.Framework.Rectangle, Microsoft.Xna.Framework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=842cf8be1de50553]]", 0),
        new("Microsoft.Xna.Framework.Content.RectangleReader", 0),
        new("Microsoft.Xna.Framework.Content.ListReader`1[[System.Char, mscorlib, Version=3.7.0.0, Culture=neutral, PublicKeyToken=969db8053d3322ac]]", 0),
        new("Microsoft.Xna.Framework.Content.CharReader", 0),
        new("Microsoft.Xna.Framework.Content.ListReader`1[[Microsoft.Xna.Framework.Vector3, Microsoft.Xna.Framework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=842cf8be1de50553]]", 0),
        new("Microsoft.Xna.Framework.Content.Vector3Reader", 0),
    ];
}
