using LiarUtil.Core.Core.Binary;
using LiarUtil.Core.Cfw2.Models;

namespace LiarUtil.Core.Cfw2.Decoding;

internal sealed class Cfw2Decoder(byte[] data)
{
    private readonly BufferReader _reader = new(data) { ErrorFactory = Cfw2Strings.Error };

    public Cfw2FontWidget Decode()
    {
        if (data.Length < Cfw2Format.HeaderSize)
        {
            throw new InvalidDataException(LiarUtil.Core.Strings.CFW2DataLengthInsufficient);
        }
        _reader.Skip(Cfw2Format.HeaderSize);
        return ReadFontWidget();
    }

    private Cfw2FontWidget ReadFontWidget() => new()
    {
        Ascent = _reader.ReadInt32(),
        AscentPadding = _reader.ReadInt32(),
        Height = _reader.ReadInt32(),
        LineSpacingOffset = _reader.ReadInt32(),
        Initialized = _reader.ReadBoolean(),
        DefaultPointSize = _reader.ReadInt32(),
        Characters = ReadList(ReadCharacterItem),
        Layers = ReadList(ReadFontLayer),
        SourceFile = Cfw2Strings.Read(_reader),
        ErrorHeader = Cfw2Strings.Read(_reader),
        PointSize = _reader.ReadInt32(),
        Tags = ReadStringList(),
        Scale = _reader.ReadDouble(),
        ForceScaledImageWhite = _reader.ReadBoolean(),
        ActivateAllLayers = _reader.ReadBoolean(),
    };

    private Cfw2CharacterItem ReadCharacterItem()
    {
        var index = _reader.ReadUInt16();
        var value = _reader.ReadUInt16();
        return new() { Index = index, Value = value };
    }

    private Cfw2FontKerning ReadFontKerning()
    {
        var offset = _reader.ReadUInt16();
        var index = _reader.ReadUInt16();
        return new() { Index = index, Offset = offset };
    }

    private Cfw2FontCharacter ReadFontCharacter()
    {
        var index = _reader.ReadUInt16();
        var imageRect = new Cfw2Rectangle
        {
            X = _reader.ReadInt32(),
            Y = _reader.ReadInt32(),
            Width = _reader.ReadInt32(),
            Height = _reader.ReadInt32(),
        };
        var imageOffset = new Cfw2Point
        {
            X = _reader.ReadInt32(),
            Y = _reader.ReadInt32(),
        };
        var kerningCount = _reader.ReadUInt16();
        var kerningFirst = _reader.ReadUInt16();
        var width = _reader.ReadInt32();
        var order = _reader.ReadInt32();
        return new()
        {
            Index = index,
            ImageRect = imageRect,
            ImageOffset = imageOffset,
            KerningFirst = kerningFirst,
            KerningCount = kerningCount,
            Width = width,
            Order = order,
        };
    }

    private Cfw2FontLayer ReadFontLayer() => new()
    {
        Name = Cfw2Strings.Read(_reader),
        RequiredTags = ReadStringList(),
        ExcludedTags = ReadStringList(),
        Kernings = ReadList(ReadFontKerning),
        Characters = ReadList(ReadFontCharacter),
        MultiplyColor = ReadColor(),
        AddColor = ReadColor(),
        ImageFile = Cfw2Strings.Read(_reader),
        DrawMode = _reader.ReadInt32(),
        Offset = ReadPoint(),
        Spacing = _reader.ReadInt32(),
        MinimumPointSize = _reader.ReadInt32(),
        MaximumPointSize = _reader.ReadInt32(),
        PointSize = _reader.ReadInt32(),
        Ascent = _reader.ReadInt32(),
        AscentPadding = _reader.ReadInt32(),
        Height = _reader.ReadInt32(),
        DefaultHeight = _reader.ReadInt32(),
        LineSpacingOffset = _reader.ReadInt32(),
        BaseOrder = _reader.ReadInt32(),
    };

    private Cfw2Color ReadColor() => new()
    {
        Red = _reader.ReadInt32(),
        Green = _reader.ReadInt32(),
        Blue = _reader.ReadInt32(),
        Alpha = _reader.ReadInt32(),
    };

    private Cfw2Point ReadPoint() => new()
    {
        X = _reader.ReadInt32(),
        Y = _reader.ReadInt32(),
    };

    private List<string> ReadStringList() => ReadList(() => Cfw2Strings.Read(_reader));

    private List<T> ReadList<T>(Func<T> read)
    {
        var count = ReadCount();
        var items = new List<T>(count);
        for (var index = 0; index < count; index++)
        {
            items.Add(read());
        }
        return items;
    }

    private int ReadCount()
    {
        var count = _reader.ReadUInt32();
        if (count > (uint)_reader.Remaining)
        {
            throw new InvalidDataException(string.Format(LiarUtil.Core.Strings.CFW2ListCountOutOfRange0, count));
        }
        return (int)count;
    }
}
