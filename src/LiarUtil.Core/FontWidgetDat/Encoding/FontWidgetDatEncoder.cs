using LiarUtil.Core.Core.Binary;
using LiarUtil.Core.FontWidgetDat.Binary;
using LiarUtil.Core.FontWidgetDat.Models;

namespace LiarUtil.Core.FontWidgetDat.Encoding;

internal sealed class FontWidgetDatEncoder
{
    private readonly BufferWriter _writer = new();

    public byte[] Encode(FontWidgetDatFile file)
    {
        if (file.Layers.Count > ushort.MaxValue)
        {
            throw new FontWidgetDatException(LiarUtil.Core.Strings.LayerCountExceedsLimit);
        }

        _writer.WriteInt32(FontWidgetDatFormat.Magic);
        _writer.WriteInt32(file.Header.PointSize);
        _writer.WriteInt32(file.Header.Unknown);
        _writer.WriteUInt16((ushort)file.Layers.Count);
        foreach (var layer in file.Layers)
        {
            WriteLayer(layer);
        }
        _writer.WriteBytes(FontWidgetDatFormat.Trailer);
        return _writer.ToArray();
    }

    private void WriteLayer(FontWidgetDatLayer layer)
    {
        if (layer.ReservedHeader32.Count != FontWidgetDatFormat.ReservedHeaderWordCount)
        {
            throw new FontWidgetDatException(LiarUtil.Core.Strings.ReservedHeader32MustContain8Values);
        }

        var glyphSlots = (ushort)(layer.Glyphs.Count + 1);
        if (layer.Kerning.Count > ushort.MaxValue)
        {
            throw new FontWidgetDatException(LiarUtil.Core.Strings.KerningPairCountExceedsLimit);
        }

        FontWidgetDatStrings.Write(_writer, layer.Name, LiarUtil.Core.Strings.LayerName);
        _writer.WriteInt32(layer.Reserved32);
        _writer.WriteUInt16(glyphSlots);
        _writer.WriteUInt16(layer.Reserved16);
        foreach (var value in layer.ReservedHeader32)
        {
            _writer.WriteInt32(value);
        }
        _writer.WriteUInt16(layer.ReservedBeforeGlyphTable16);
        foreach (var glyph in layer.Glyphs)
        {
            WriteGlyph(glyph);
        }
        _writer.WriteUInt16((ushort)layer.Kerning.Count);
        foreach (var pair in layer.Kerning)
        {
            WriteKerning(pair);
        }
        WriteLayerStyle(layer);
    }

    private void WriteGlyph(FontWidgetDatGlyph glyph)
    {
        _writer.WriteUInt16(glyph.CodePoint);
        _writer.WriteUInt16(glyph.AtlasMarker);
        _writer.WriteInt32(glyph.ImageRectX);
        _writer.WriteInt32(glyph.ImageRectY);
        _writer.WriteInt32(glyph.ImageRectWidth);
        _writer.WriteInt32(glyph.ImageRectHeight);
        _writer.WriteInt32(glyph.ImageOffsetX);
        _writer.WriteInt32(glyph.ImageOffsetY);
        _writer.WriteInt32(glyph.Width);
        _writer.WriteInt32(glyph.Order);
    }

    private void WriteKerning(FontWidgetDatKerning pair)
    {
        _writer.WriteUInt16(pair.LeftCodePoint);
        _writer.WriteUInt16(pair.RightCodePoint);
        _writer.WriteInt16(pair.Offset);
        _writer.WriteUInt16(pair.Unknown16);
    }

    private void WriteLayerStyle(FontWidgetDatLayer layer)
    {
        WriteColor(layer.Multiply);
        WriteColor(layer.Add);
        FontWidgetDatStrings.Write(_writer, layer.ImageFile, LiarUtil.Core.Strings.LayerImageName);
        _writer.WriteInt32(layer.DrawMode);
        _writer.WriteInt32(layer.OffsetX);
        _writer.WriteInt32(layer.OffsetY);
        _writer.WriteInt32(layer.Spacing);
        _writer.WriteInt32(layer.MinimumPointSize);
        _writer.WriteInt32(layer.MaximumPointSize);
        _writer.WriteInt32(layer.PointSize);
        _writer.WriteInt32(layer.Ascent);
        _writer.WriteInt32(layer.AscentPadding);
        _writer.WriteInt32(layer.Height);
        _writer.WriteInt32(layer.DefaultHeight);
        _writer.WriteInt32(layer.LineSpacingOffset);
        _writer.WriteInt32(layer.BaseOrder);
        _writer.WriteBoolean(layer.Initialized);
    }

    private void WriteColor(FontWidgetDatColor color)
    {
        _writer.WriteInt32(color.Red);
        _writer.WriteInt32(color.Green);
        _writer.WriteInt32(color.Blue);
        _writer.WriteInt32(color.Alpha);
    }
}
