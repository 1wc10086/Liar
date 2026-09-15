using LiarUtil.Core.Core.Binary;
using LiarUtil.Core.Cfu2.Models;

namespace LiarUtil.Core.Cfu2.Encoding;

internal sealed class Cfu2Encoder
{
    private readonly BufferWriter _writer = new();

    public byte[] Encode(Cfu2FontWidget widget)
    {
        WritePrefix(widget.Header);
        WriteFontWidget(widget);
        return _writer.ToArray();
    }

    private void WritePrefix(byte[] header)
    {
        if (header.Length == Cfu2Format.PrefixSize)
        {
            _writer.WriteBytes(header);
        }
        else
        {
            _writer.WriteZeros(Cfu2Format.PrefixSize);
        }

        _writer.WriteUInt32(Cfu2Format.Magic);
        _writer.WriteInt32(Cfu2Format.Version);
    }

    private void WriteFontWidget(Cfu2FontWidget widget)
    {
        _writer.WriteInt32(widget.Ascent);
        _writer.WriteInt32(widget.AscentPadding);
        _writer.WriteInt32(widget.Height);
        _writer.WriteInt32(widget.LineSpacingOffset);
        _writer.WriteBoolean(widget.Initialized);
        _writer.WriteInt32(widget.DefaultPointSize);
        WriteList(widget.Characters, WriteCharacterItem);
        WriteList(widget.Layers, WriteFontLayer);
        Cfu2Strings.Write(_writer, widget.SourceFile);
        Cfu2Strings.Write(_writer, widget.ErrorHeader);
        _writer.WriteInt32(widget.PointSize);
        WriteList(widget.Tags, item => Cfu2Strings.Write(_writer, item));
        _writer.WriteDouble(widget.Scale);
        _writer.WriteBoolean(widget.ForceScaledImageWhite);
    }

    private void WriteCharacterItem(Cfu2CharacterItem item)
    {
        _writer.WriteUInt16(item.Index);
        _writer.WriteUInt16(item.Value);
    }

    private void WriteFontKerning(Cfu2FontKerning item)
    {
        _writer.WriteUInt16(item.Offset);
        _writer.WriteUInt16(item.Index);
    }

    private void WriteFontCharacter(Cfu2FontCharacter item)
    {
        _writer.WriteInt32(item.Index);
        _writer.WriteInt16(item.ImageRect.X);
        _writer.WriteInt16(item.ImageRect.Y);
        _writer.WriteInt16(item.ImageRect.Width);
        _writer.WriteInt16(item.ImageRect.Height);
        _writer.WriteInt16(ToInt16(item.ImageOffset.X));
        _writer.WriteInt16(ToInt16(item.ImageOffset.Y));
        _writer.WriteUInt16(item.KerningCount);
        _writer.WriteUInt16(item.KerningFirst);
        _writer.WriteInt32(item.Width);
    }

    private static short ToInt16(int value) => value is >= short.MinValue and <= short.MaxValue
        ? (short)value
        : throw new InvalidDataException(string.Format(LiarUtil.Core.Strings.CFU2ValueOutOfInt16Range0, value));

    private void WriteFontLayer(Cfu2FontLayer layer)
    {
        Cfu2Strings.Write(_writer, layer.Name);
        WriteList(layer.RequiredTags, item => Cfu2Strings.Write(_writer, item));
        WriteList(layer.ExcludedTags, item => Cfu2Strings.Write(_writer, item));
        WriteList(layer.Kernings, WriteFontKerning);
        WriteList(layer.Characters, WriteFontCharacter);
        WriteColor(layer.MultiplyColor);
        WriteColor(layer.AddColor);
        Cfu2Strings.Write(_writer, layer.ImageFile);
        _writer.WriteInt32(layer.Unknown);
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

    private void WriteColor(Cfu2Color color)
    {
        _writer.WriteInt32(color.Red);
        _writer.WriteInt32(color.Green);
        _writer.WriteInt32(color.Blue);
        _writer.WriteInt32(color.Alpha);
    }

    private void WritePoint(Cfu2Point point)
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
