using System.Text;
using LiarUtil.Core.Core.Binary;

namespace LiarUtil.Core.Xnb.Fonts;

internal static class SpriteFontReader
{
    private const int RectangleSize = 16;
    private const int KerningSize = 12;
    private const int MaxMipCount = 32;

    public static SpriteFontData Read(byte[] data, int offset, int length)
    {
        var reader = new BufferReader(data, offset, length) { ErrorFactory = XnbErrors.Throw };
        return new SpriteFontData
        {
            Texture = ReadTexture(reader),
            Glyphs = ReadRectangles(reader),
            Cropping = ReadRectangles(reader),
            Characters = ReadCharacters(reader),
            LineSpacing = reader.ReadInt32(),
            Spacing = reader.ReadSingle(),
            Kerning = ReadKernings(reader),
            DefaultCharacter = reader.ReadBoolean() ? ReadCharacter(reader) : null,
        };
    }

    private static FontTexture ReadTexture(BufferReader reader)
    {
        if (reader.ReadVarUInt32() is not SpriteFontFormat.TextureIndex)
        {
            throw new XnbException(LiarUtil.Core.Strings.XNBFontTextureObjectTypeMismatch);
        }

        var format = reader.ReadInt32();
        var width = reader.ReadInt32();
        var height = reader.ReadInt32();
        var mipCount = reader.ReadInt32();
        if (width <= 0 || height <= 0 || mipCount is <= 0 or > MaxMipCount)
        {
            throw new XnbException(string.Format(LiarUtil.Core.Strings.XNBFontTextureLayoutInvalid0X1, width, height, mipCount));
        }

        var mipmaps = new List<byte[]>(mipCount);
        for (var level = 0; level < mipCount; level++)
        {
            var size = reader.ReadInt32();
            if (size < 0 || size > reader.Remaining)
            {
                throw new XnbException(string.Format(LiarUtil.Core.Strings.XNBFontTextureDataLengthInvalid0, size));
            }

            mipmaps.Add(reader.ReadBytes(size));
        }

        return new FontTexture
        {
            Format = format,
            Width = width,
            Height = height,
            Mipmaps = mipmaps,
        };
    }

    private static List<FontRectangle> ReadRectangles(BufferReader reader)
    {
        if (reader.ReadVarUInt32() == 0)
        {
            return [];
        }

        var count = ReadCount(reader, RectangleSize);
        var rectangles = new List<FontRectangle>(count);
        for (var index = 0; index < count; index++)
        {
            rectangles.Add(new FontRectangle
            {
                X = reader.ReadInt32(),
                Y = reader.ReadInt32(),
                Width = reader.ReadInt32(),
                Height = reader.ReadInt32(),
            });
        }

        return rectangles;
    }

    private static List<char> ReadCharacters(BufferReader reader)
    {
        if (reader.ReadVarUInt32() == 0)
        {
            return [];
        }

        var count = ReadCount(reader, 1);
        var characters = new List<char>(count);
        for (var index = 0; index < count; index++)
        {
            characters.Add(ReadCharacter(reader));
        }

        return characters;
    }

    private static List<FontVector3> ReadKernings(BufferReader reader)
    {
        if (reader.ReadVarUInt32() == 0)
        {
            return [];
        }

        var count = ReadCount(reader, KerningSize);
        var kernings = new List<FontVector3>(count);
        for (var index = 0; index < count; index++)
        {
            kernings.Add(new FontVector3
            {
                X = reader.ReadSingle(),
                Y = reader.ReadSingle(),
                Z = reader.ReadSingle(),
            });
        }

        return kernings;
    }

    private static char ReadCharacter(BufferReader reader)
    {
        var first = reader.ReadUInt8();
        var size = first < 0x80 ? 1 : first < 0xE0 ? 2 : first < 0xF0 ? 3 : 4;
        Span<byte> buffer = stackalloc byte[4];
        buffer[0] = first;
        for (var index = 1; index < size; index++)
        {
            buffer[index] = reader.ReadUInt8();
        }

        var text = Encoding.UTF8.GetString(buffer[..size]);
        return text.Length > 0 ? text[0] : '\0';
    }

    private static int ReadCount(BufferReader reader, int itemSize)
    {
        var count = reader.ReadInt32();
        if (count < 0 || count > reader.Remaining / itemSize)
        {
            throw new XnbException(string.Format(LiarUtil.Core.Strings.XNBFontListLengthInvalid0, count));
        }

        return count;
    }
}
