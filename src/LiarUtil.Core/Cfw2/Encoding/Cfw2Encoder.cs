using LiarUtil.Core.Core.Binary;
using LiarUtil.Core.Cfw2.Models;

namespace LiarUtil.Core.Cfw2.Encoding;

internal sealed class Cfw2Encoder
{
    private readonly BufferWriter _writer = new();

    public byte[] Encode(Cfw2FontWidget widget)
    {
        WriteHeader();
        WriteFontWidget(widget);
        return _writer.ToArray();
    }

    private void WriteHeader()
    {
        _writer.WriteZeros(Cfw2Format.HeaderSize);
    }

    private void WriteFontWidget(Cfw2FontWidget widget)
    {
        _writer.WriteInt32(widget.Ascent);
        _writer.WriteInt32(widget.AscentPadding);
        _writer.WriteInt32(widget.Height);
        _writer.WriteInt32(widget.LineSpacingOffset);
        _writer.WriteBoolean(widget.Initialized);
        _writer.WriteInt32(widget.DefaultPointSize);
        WriteList(widget.Characters, WriteCharacterItem);
        WriteList(widget.Layers, WriteFontLayer);
        Cfw2Strings.Write(_writer, widget.SourceFile);
        Cfw2Strings.Write(_writer, widget.ErrorHeader);
        _writer.WriteInt32(widget.PointSize);
        WriteList(widget.Tags, item => Cfw2Strings.Write(_writer, item));
        _writer.WriteDouble(widget.Scale);
        _writer.WriteBoolean(widget.ForceScaledImageWhite);
        _writer.WriteBoolean(widget.ActivateAllLayers);
    }

    private void WriteCharacterItem(Cfw2CharacterItem item)
    {
        _writer.WriteUInt16(item.Index);
        _writer.WriteUInt16(item.Value);
    }

    private void WriteFontKerning(Cfw2FontKerning item)
    {
        _writer.WriteUInt16(item.Offset);
        _writer.WriteUInt16(item.Index);
    }

    private void WriteFontCharacter(Cfw2FontCharacter item)
    {
        _writer.WriteUInt16(item.Index);
        _writer.WriteInt32(item.ImageRect.X);
        _writer.WriteInt32(item.ImageRect.Y);
        _writer.WriteInt32(item.ImageRect.Width);
        _writer.WriteInt32(item.ImageRect.Height);
        _writer.WriteInt32(item.ImageOffset.X);
        _writer.WriteInt32(item.ImageOffset.Y);
        _writer.WriteUInt16(item.KerningCount);
        _writer.WriteUInt16(item.KerningFirst);
        _writer.WriteInt32(item.Width);
        _writer.WriteInt32(item.Order);
    }

    private void WriteFontLayer(Cfw2FontLayer layer)
    {
        Cfw2Strings.Write(_writer, layer.Name);
        WriteList(layer.RequiredTags, item => Cfw2Strings.Write(_writer, item));
        WriteList(layer.ExcludedTags, item => Cfw2Strings.Write(_writer, item));
        WriteList(layer.Kernings, WriteFontKerning);
        WriteList(layer.Characters, WriteFontCharacter);
        WriteColor(layer.MultiplyColor);
        WriteColor(layer.AddColor);
        Cfw2Strings.Write(_writer, layer.ImageFile);
        _writer.WriteInt32(layer.DrawMode);
        WritePoint(layer.Offset);
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
    }

    private void WriteColor(Cfw2Color color)
    {
        _writer.WriteInt32(color.Red);
        _writer.WriteInt32(color.Green);
        _writer.WriteInt32(color.Blue);
        _writer.WriteInt32(color.Alpha);
    }

    private void WritePoint(Cfw2Point point)
    {
        _writer.WriteInt32(point.X);
        _writer.WriteInt32(point.Y);
    }

    private void WriteList<T>(IReadOnlyList<T> items, Action<T> write)
    {
        _writer.WriteUInt32((uint)items.Count);
        foreach (var item in items)
        {
            write(item);
        }
    }
}
