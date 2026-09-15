using LiarUtil.Core.Core.Binary;

namespace LiarUtil.Core.Pam;

public sealed class PamReader
{
    private const float TimeRate = 65536f;
    private const float SizeRate = 20f;
    private const float AngleRate = 1000f;
    private const float MatrixRate = 65536f;
    private const float MatrixExactRate = 20f * 65536f;
    private const float ColorRate = 255f;
    private const uint MagicMarker = 0xBAF01954;

    private readonly BufferReader _reader;

    public PamReader(byte[] data) => _reader = new BufferReader(data) { ErrorFactory = Error };

    public PamAnimation Decode()
    {
        if (ReadU32() != MagicMarker)
        {
            throw new InvalidDataException(LiarUtil.Core.Strings.PAMMagicNumberIncorrect);
        }
        var version = ReadU32();
        if (version is < 1 or > 6)
        {
            throw new InvalidDataException(string.Format(LiarUtil.Core.Strings.UnsupportedPAMVersion0, version));
        }
        return ReadAnimation((int)version);
    }

    private PamAnimation ReadAnimation(int version)
    {
        var animation = new PamAnimation
        {
            Version = version,
            FrameRate = ReadU8(),
            PositionX = ReadF16(SizeRate),
            PositionY = ReadF16(SizeRate),
            Width = ReadU16() / SizeRate,
            Height = ReadU16() / SizeRate,
        };
        var imageCount = ReadU16();
        for (var i = 0; i < imageCount; i++)
        {
            animation.Images.Add(ReadImage(version));
        }
        var spriteCount = ReadU16();
        for (var i = 0; i < spriteCount; i++)
        {
            animation.Sprites.Add(ReadSprite(version));
        }
        animation.MainSprite = version < 4 || ReadU8() != 0 ? ReadSprite(version) : null;
        return animation;
    }

    private PamImage ReadImage(int version)
    {
        var name = ReadString16();
        int? width = null;
        int? height = null;
        if (version >= 4)
        {
            width = ReadS16();
            height = ReadS16();
        }
        PamTransform transform = version < 2
            ? new PamRotateTransform { Angle = ReadF16(AngleRate) }
            : new PamMatrixTransform
            {
                A = ReadF32(MatrixExactRate),
                C = ReadF32(MatrixExactRate),
                B = ReadF32(MatrixExactRate),
                D = ReadF32(MatrixExactRate),
            };
        transform.X = ReadF16(SizeRate);
        transform.Y = ReadF16(SizeRate);
        return new PamImage { Name = name, Width = width, Height = height, Transform = transform };
    }

    private PamSprite ReadSprite(int version)
    {
        string? name = null;
        float? frameRate = null;
        string? description = null;
        if (version >= 4)
        {
            name = ReadString16();
            if (version >= 6)
            {
                description = ReadString16();
            }
            frameRate = ReadF32(TimeRate);
        }
        var frameCount = ReadU16();
        var frames = new List<PamFrame>(frameCount);
        var workAreaStart = 0;
        var workAreaDuration = 0;
        if (version >= 5)
        {
            workAreaStart = ReadS16();
            workAreaDuration = ReadS16();
        }
        for (var i = 0; i < frameCount; i++)
        {
            frames.Add(ReadFrame(version));
        }
        return new PamSprite
        {
            Name = name,
            Description = description,
            FrameRate = frameRate,
            WorkAreaStart = workAreaStart,
            WorkAreaDuration = workAreaDuration,
            Frames = frames,
        };
    }

    private PamFrame ReadFrame(int version)
    {
        var flags = ReadU8();
        return new PamFrame
        {
            Removes = (flags & 1) != 0 ? ReadVariantCountList(ReadLayerRemove) : [],
            Appends = (flags & 2) != 0 ? ReadVariantCountList(() => ReadLayerAppend(version)) : [],
            Changes = (flags & 4) != 0 ? ReadVariantCountList(ReadLayerChange) : [],
            Label = (flags & 8) != 0 ? ReadString16() : null,
            Stop = (flags & 16) != 0,
            Commands = (flags & 32) != 0 ? ReadCommandList() : [],
        };
    }

    private PamLayerRemove ReadLayerRemove()
    {
        var (index, _) = ReadVariantFlagU16U32(5);
        return new PamLayerRemove { Index = (int)index };
    }

    private PamLayerAppend ReadLayerAppend(int version)
    {
        var (index, flags) = ReadVariantFlagU16U32(5);
        var resource = version < 6 ? ReadU8() : ReadVariantU8U16();
        return new PamLayerAppend
        {
            Index = (int)index,
            Resource = (int)resource,
            Sprite = (flags & 16) != 0,
            Additive = (flags & 8) != 0,
            PreloadFrame = (flags & 4) != 0 ? ReadS16() : 0,
            Name = (flags & 2) != 0 ? ReadString16() : null,
            TimeScale = (flags & 1) != 0 ? ReadF32(TimeRate) : 1f,
        };
    }

    private PamLayerChange ReadLayerChange()
    {
        var (index, flags) = ReadVariantFlagU16U32(6);
        var rotate = (flags & 16) != 0;
        var matrix = (flags & 4) != 0;
        PamTransform transform = !rotate && !matrix
            ? new PamTranslateTransform()
            : rotate
                ? new PamRotateTransform { Angle = ReadF16(AngleRate) }
                : new PamMatrixTransform
                {
                    A = ReadF32(MatrixRate),
                    C = ReadF32(MatrixRate),
                    B = ReadF32(MatrixRate),
                    D = ReadF32(MatrixRate),
                };
        transform.X = (flags & 2) != 0 ? ReadF32(SizeRate) : ReadF16(SizeRate);
        transform.Y = (flags & 2) != 0 ? ReadF32(SizeRate) : ReadF16(SizeRate);
        return new PamLayerChange
        {
            Index = (int)index,
            Transform = transform,
            SourceRectangle = (flags & 32) != 0 ? ReadRectangle() : null,
            Color = (flags & 8) != 0 ? ReadColor() : null,
            SpriteFrameNumber = (flags & 1) != 0 ? ReadS16() : null,
        };
    }

    private PamRectangle ReadRectangle() => new()
    {
        X = ReadF16(SizeRate),
        Y = ReadF16(SizeRate),
        Width = ReadF16(SizeRate),
        Height = ReadF16(SizeRate),
    };

    private PamColor ReadColor() => new()
    {
        Red = ReadU8() / ColorRate,
        Green = ReadU8() / ColorRate,
        Blue = ReadU8() / ColorRate,
        Alpha = ReadU8() / ColorRate,
    };

    private List<PamCommand> ReadCommandList()
    {
        var count = ReadU8();
        var list = new List<PamCommand>(count);
        for (var i = 0; i < count; i++)
        {
            list.Add(new PamCommand { Command = ReadString16(), Argument = ReadString16() });
        }
        return list;
    }

    private List<T> ReadVariantCountList<T>(Func<T> readElement)
    {
        var count = ReadVariantU8U16();
        var list = new List<T>((int)count);
        for (var i = 0; i < count; i++)
        {
            list.Add(readElement());
        }
        return list;
    }

    private long ReadVariantU8U16()
    {
        var shortValue = ReadU8();
        return shortValue != byte.MaxValue ? shortValue : ReadU16();
    }

    private long ReadVariantU16U32()
    {
        var shortValue = ReadU16();
        return shortValue != ushort.MaxValue ? shortValue : ReadU32();
    }

    private (long Value, int Flags) ReadVariantFlagU16U32(int flagCount)
    {
        var valueBitCount = 16 - flagCount;
        var valueMaximum = (1 << valueBitCount) - 1;
        var combined = ReadU16();
        var value = combined & valueMaximum;
        var flags = combined >> valueBitCount;
        return (value != valueMaximum ? value : ReadU32(), flags);
    }

    private float ReadF16(float rate) => ReadS16() / rate;

    private float ReadF32(float rate) => ReadS32() / rate;

    private byte ReadU8() => _reader.ReadUInt8();

    private ushort ReadU16() => _reader.ReadUInt16();

    private uint ReadU32() => _reader.ReadUInt32();

    private short ReadS16() => _reader.ReadInt16();

    private int ReadS32() => _reader.ReadInt32();

    private string ReadString16() => _reader.ReadString(_reader.ReadUInt16()) ?? "";

    private static Exception Error(string message) => new InvalidDataException(message);
}
