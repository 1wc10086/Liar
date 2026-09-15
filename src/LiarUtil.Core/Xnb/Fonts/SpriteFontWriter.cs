using System.Text;
using LiarUtil.Core.Core.Binary;

namespace LiarUtil.Core.Xnb.Fonts;

internal static class SpriteFontWriter
{
    public static byte[] Write(SpriteFontData font)
    {
        var writer = new BufferWriter();
        writer.WriteVarUInt32(SpriteFontFormat.RootIndex);
        WriteTexture(writer, font.Texture);
        WriteRectangles(writer, SpriteFontFormat.GlyphIndex, font.Glyphs);
        WriteRectangles(writer, SpriteFontFormat.CroppingIndex, font.Cropping);
        WriteCharacters(writer, font.Characters);
        writer.WriteInt32(font.LineSpacing);
        writer.WriteSingle(font.Spacing);
        WriteKernings(writer, font.Kerning);
        writer.WriteBoolean(font.DefaultCharacter.HasValue);
        if (font.DefaultCharacter.HasValue)
        {
            WriteCharacter(writer, font.DefaultCharacter.Value);
        }

        return XnbWriter.Write(
            SpriteFontFormat.Platform,
            SpriteFontFormat.Version,
            XnbFlags.None,
            SpriteFontTypeReaders.Readers,
            writer.ToArray());
    }

    private static void WriteTexture(BufferWriter writer, FontTexture texture)
    {
        writer.WriteVarUInt32(SpriteFontFormat.TextureIndex);
        writer.WriteInt32(texture.Format);
        writer.WriteInt32(texture.Width);
        writer.WriteInt32(texture.Height);
        writer.WriteInt32(texture.Mipmaps.Count);
        foreach (var mipmap in texture.Mipmaps)
        {
            writer.WriteInt32(mipmap.Length);
            writer.WriteBytes(mipmap);
        }
    }

    private static void WriteRectangles(BufferWriter writer, uint objectIndex, List<FontRectangle> rectangles)
    {
        writer.WriteVarUInt32(objectIndex);
        writer.WriteInt32(rectangles.Count);
        foreach (var rectangle in rectangles)
        {
            writer.WriteInt32(rectangle.X);
            writer.WriteInt32(rectangle.Y);
            writer.WriteInt32(rectangle.Width);
            writer.WriteInt32(rectangle.Height);
        }
    }

    private static void WriteCharacters(BufferWriter writer, List<char> characters)
    {
        writer.WriteVarUInt32(SpriteFontFormat.CharacterIndex);
        writer.WriteInt32(characters.Count);
        foreach (var character in characters)
        {
            WriteCharacter(writer, character);
        }
    }

    private static void WriteKernings(BufferWriter writer, List<FontVector3> kernings)
    {
        writer.WriteVarUInt32(SpriteFontFormat.KerningIndex);
        writer.WriteInt32(kernings.Count);
        foreach (var kerning in kernings)
        {
            writer.WriteSingle(kerning.X);
            writer.WriteSingle(kerning.Y);
            writer.WriteSingle(kerning.Z);
        }
    }

    private static void WriteCharacter(BufferWriter writer, char value) =>
        writer.WriteBytes(Encoding.UTF8.GetBytes(value.ToString()));
}
