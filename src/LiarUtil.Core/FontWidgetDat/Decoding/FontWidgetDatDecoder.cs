using LiarUtil.Core.Core.Binary;
using LiarUtil.Core.FontWidgetDat.Binary;
using LiarUtil.Core.FontWidgetDat.Models;

namespace LiarUtil.Core.FontWidgetDat.Decoding;

internal sealed class FontWidgetDatDecoder(byte[] data)
{
    private readonly BufferReader _reader = new(data) { ErrorFactory = static message => new FontWidgetDatException(message) };

    public FontWidgetDatFile Decode()
    {
        if (data.Length < FontWidgetDatFormat.HeaderSize)
        {
            throw new FontWidgetDatException(LiarUtil.Core.Strings.FileLengthInsufficient);
        }

        var magic = _reader.ReadInt32();
        if (magic != FontWidgetDatFormat.Magic)
        {
            throw new FontWidgetDatException(LiarUtil.Core.Strings.NotSupportedFontWidgetDATFile);
        }

        var file = new FontWidgetDatFile
        {
            Header = new FontWidgetDatHeader
            {
                PointSize = _reader.ReadInt32(),
                Unknown = _reader.ReadInt32(),
                LayerCount = _reader.ReadUInt16(),
            },
        };

        for (var index = 0; index < file.Header.LayerCount; index++)
        {
            file.Layers.Add(ReadLayer());
        }

        ReadTrailer();
        return file;
    }

    private FontWidgetDatLayer ReadLayer()
    {
        var layer = new FontWidgetDatLayer
        {
            Name = FontWidgetDatStrings.Read(_reader, LiarUtil.Core.Strings.LayerName),
            Reserved32 = _reader.ReadInt32(),
        };
        var glyphSlots = _reader.ReadUInt16();
        layer.Reserved16 = _reader.ReadUInt16();

        for (var index = 0; index < FontWidgetDatFormat.ReservedHeaderWordCount; index++)
        {
            layer.ReservedHeader32.Add(_reader.ReadInt32());
        }

        layer.ReservedBeforeGlyphTable16 = _reader.ReadUInt16();
        if (glyphSlots == 0)
        {
            throw new FontWidgetDatException(LiarUtil.Core.Strings.LayerHasNoGlyphSlots);
        }

        for (var index = 0; index < glyphSlots - 1; index++)
        {
            layer.Glyphs.Add(ReadGlyph());
        }

        var kerningCount = _reader.ReadUInt16();
        for (var index = 0; index < kerningCount; index++)
        {
            layer.Kerning.Add(ReadKerning());
        }

        ReadLayerStyle(layer);
        BindGlyphKerning(layer);
        return layer;
    }

    private FontWidgetDatGlyph ReadGlyph()
    {
        var codePoint = _reader.ReadUInt16();
        return new FontWidgetDatGlyph
        {
            CodePoint = codePoint,
            Character = FontWidgetDatStrings.Character(codePoint),
            AtlasMarker = _reader.ReadUInt16(),
            ImageRectX = _reader.ReadInt32(),
            ImageRectY = _reader.ReadInt32(),
            ImageRectWidth = _reader.ReadInt32(),
            ImageRectHeight = _reader.ReadInt32(),
            ImageOffsetX = _reader.ReadInt32(),
            ImageOffsetY = _reader.ReadInt32(),
            Width = _reader.ReadInt32(),
            Order = _reader.ReadInt32(),
        };
    }

    private FontWidgetDatKerning ReadKerning()
    {
        var leftCodePoint = _reader.ReadUInt16();
        var rightCodePoint = _reader.ReadUInt16();
        return new FontWidgetDatKerning
        {
            LeftCodePoint = leftCodePoint,
            Left = FontWidgetDatStrings.Character(leftCodePoint),
            RightCodePoint = rightCodePoint,
            Right = FontWidgetDatStrings.Character(rightCodePoint),
            Offset = _reader.ReadInt16(),
            Unknown16 = _reader.ReadUInt16(),
        };
    }

    private void ReadLayerStyle(FontWidgetDatLayer layer)
    {
        layer.Multiply = ReadColor();
        layer.Add = ReadColor();
        layer.ImageFile = FontWidgetDatStrings.Read(_reader, LiarUtil.Core.Strings.LayerImageName);
        layer.DrawMode = _reader.ReadInt32();
        layer.OffsetX = _reader.ReadInt32();
        layer.OffsetY = _reader.ReadInt32();
        layer.Spacing = _reader.ReadInt32();
        layer.MinimumPointSize = _reader.ReadInt32();
        layer.MaximumPointSize = _reader.ReadInt32();
        layer.PointSize = _reader.ReadInt32();
        layer.Ascent = _reader.ReadInt32();
        layer.AscentPadding = _reader.ReadInt32();
        layer.Height = _reader.ReadInt32();
        layer.DefaultHeight = _reader.ReadInt32();
        layer.LineSpacingOffset = _reader.ReadInt32();
        layer.BaseOrder = _reader.ReadInt32();
        layer.Initialized = _reader.ReadBoolean();
    }

    private FontWidgetDatColor ReadColor() => new()
    {
        Red = _reader.ReadInt32(),
        Green = _reader.ReadInt32(),
        Blue = _reader.ReadInt32(),
        Alpha = _reader.ReadInt32(),
    };

    private static void BindGlyphKerning(FontWidgetDatLayer layer)
    {
        foreach (var glyph in layer.Glyphs)
        {
            glyph.Kerning.Clear();
            foreach (var pair in layer.Kerning)
            {
                if (pair.LeftCodePoint == glyph.CodePoint)
                {
                    glyph.Kerning.Add(pair);
                }
            }
        }
    }

    private void ReadTrailer()
    {
        if (_reader.Remaining < FontWidgetDatFormat.TrailerSize)
        {
            throw new FontWidgetDatException(LiarUtil.Core.Strings.FileTrailerIncomplete);
        }

        var trailer = _reader.ReadSpan(FontWidgetDatFormat.TrailerSize);
        if (!trailer.SequenceEqual(FontWidgetDatFormat.Trailer))
        {
            throw new FontWidgetDatException(LiarUtil.Core.Strings.FileTrailerMarkerInvalid);
        }

        if (_reader.Remaining != 0)
        {
            throw new FontWidgetDatException(LiarUtil.Core.Strings.ExtraDataExistsAfterEndOfFile);
        }
    }
}
