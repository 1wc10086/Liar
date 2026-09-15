using LiarUtil.Core.Core.Binary;
using LiarUtil.Core.Cfu2.Models;

namespace LiarUtil.Core.Cfu2.Decoding;

internal sealed class Cfu2Decoder(byte[] data)
{
    private readonly BufferReader _reader = new(data) { ErrorFactory = Cfu2Strings.Error };

    public Cfu2FontWidget Decode()
    {
        if (data.Length < Cfu2Format.PrefixSize + 8)
        {
            throw new InvalidDataException(LiarUtil.Core.Strings.CFU2DataLengthInsufficient);
        }

        var header = _reader.ReadBytes(Cfu2Format.PrefixSize);
        if (_reader.ReadUInt32() != Cfu2Format.Magic)
        {
            throw new InvalidDataException(LiarUtil.Core.Strings.CFU2MarkerMismatch);
        }

        if (_reader.ReadInt32() != Cfu2Format.Version)
        {
            throw new InvalidDataException(LiarUtil.Core.Strings.UnsupportedCFU2Version);
        }

        return ReadFontWidget(header);
    }

    private Cfu2FontWidget ReadFontWidget(byte[] header) => new()
    {
        Header = header,
        Ascent = _reader.ReadInt32(),
        AscentPadding = _reader.ReadInt32(),
        Height = _reader.ReadInt32(),
        LineSpacingOffset = _reader.ReadInt32(),
        Initialized = _reader.ReadBoolean(),
        DefaultPointSize = _reader.ReadInt32(),
        Characters = ReadList(ReadCharacterItem),
        Layers = ReadList(ReadFontLayer),
        SourceFile = Cfu2Strings.Read(_reader),
        ErrorHeader = Cfu2Strings.Read(_reader),
        PointSize = _reader.ReadInt32(),
        Tags = ReadStringList(),
        Scale = _reader.ReadDouble(),
        ForceScaledImageWhite = _reader.ReadBoolean(),
    };

    private Cfu2CharacterItem ReadCharacterItem()
    {
        var index = _reader.ReadUInt16();
        var value = _reader.ReadUInt16();
        return new()
        {
            Index = index,
            Value = value,
        };
    }

    private Cfu2FontKerning ReadFontKerning()
    {
        var offset = _reader.ReadUInt16();
        var index = _reader.ReadUInt16();
        return new()
        {
            Index = index,
            Offset = offset,
        };
    }

    private Cfu2FontCharacter ReadFontCharacter()
    {
        var index = _reader.ReadInt32();
        if (index is < 0 or > ushort.MaxValue)
        {
            throw new InvalidDataException(string.Format(LiarUtil.Core.Strings.CFU2CharacterIndexOutOfRange0, index));
        }

        var imageRect = new Cfu2Rectangle
        {
            X = _reader.ReadInt16(),
            Y = _reader.ReadInt16(),
            Width = _reader.ReadInt16(),
            Height = _reader.ReadInt16(),
        };
        var imageOffset = new Cfu2Point
        {
            X = _reader.ReadInt16(),
            Y = _reader.ReadInt16(),
        };
        var kerningCount = _reader.ReadUInt16();
        var kerningFirst = _reader.ReadUInt16();
        var width = _reader.ReadInt32();
        return new()
        {
            Index = new Cfu2Unicode((ushort)index),
            ImageRect = imageRect,
            ImageOffset = imageOffset,
            KerningCount = kerningCount,
            KerningFirst = kerningFirst,
            Width = width,
        };
    }

    private Cfu2FontLayer ReadFontLayer()
    {
        var name = Cfu2Strings.Read(_reader);
        var requiredTags = ReadStringList();
        var excludedTags = ReadStringList();
        var kernings = ReadList(ReadFontKerning);
        var characters = ReadList(ReadFontCharacter);
        var multiplyColor = ReadColor();
        var addColor = ReadColor();
        var imageFile = Cfu2Strings.Read(_reader);
        var unknown = _reader.ReadInt32();
        var drawMode = _reader.ReadInt32();
        var offset = ReadPoint();
        return new()
        {
            Name = name,
            RequiredTags = requiredTags,
            ExcludedTags = excludedTags,
            Kernings = kernings,
            Characters = characters,
            MultiplyColor = multiplyColor,
            AddColor = addColor,
            ImageFile = imageFile,
            Unknown = unknown,
            DrawMode = drawMode,
            Offset = offset,
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
    }

    private Cfu2Color ReadColor() => new()
    {
        Red = _reader.ReadInt32(),
        Green = _reader.ReadInt32(),
        Blue = _reader.ReadInt32(),
        Alpha = _reader.ReadInt32(),
    };

    private Cfu2Point ReadPoint() => new()
    {
        X = _reader.ReadInt32(),
        Y = _reader.ReadInt32(),
    };

    private List<string> ReadStringList() => ReadList(() => Cfu2Strings.Read(_reader));

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
            throw new InvalidDataException(string.Format(LiarUtil.Core.Strings.CFU2ListCountOutOfRange0, count));
        }
        return (int)count;
    }
}
