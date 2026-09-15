using LiarUtil.Core.Core.Binary;

namespace LiarUtil.Core.Pam;

public sealed class PamWriter
{
    private const float TimeRate = 65536f;
    private const float SizeRate = 20f;
    private const float AngleRate = 1000f;
    private const float MatrixRate = 65536f;
    private const float MatrixExactRate = 20f * 65536f;
    private const float ColorRate = 255f;
    private const uint MagicMarker = 0xBAF01954;

    private readonly BufferWriter _writer = new();

    public byte[] Encode(PamAnimation animation, int version)
    {
        _writer.WriteUInt32(MagicMarker);
        _writer.WriteUInt32((uint)version);
        WriteAnimation(animation, version);
        return _writer.ToArray();
    }

    private void WriteAnimation(PamAnimation animation, int version)
    {
        _writer.WriteUInt8((byte)animation.FrameRate);
        WriteS16F(animation.PositionX, SizeRate);
        WriteS16F(animation.PositionY, SizeRate);
        WriteU16F(animation.Width);
        WriteU16F(animation.Height);
        _writer.WriteUInt16((ushort)animation.Images.Count);
        foreach (var image in animation.Images)
        {
            WriteImage(image, version);
        }
        _writer.WriteUInt16((ushort)animation.Sprites.Count);
        foreach (var sprite in animation.Sprites)
        {
            WriteSprite(sprite, version);
        }
        if (version < 4)
        {
            WriteSprite(animation.MainSprite ?? throw new InvalidDataException(LiarUtil.Core.Strings.PAMMainSpriteMissing), version);
        }
        else
        {
            _writer.WriteUInt8(animation.MainSprite is null ? (byte)0 : (byte)1);
            if (animation.MainSprite is { } sprite)
            {
                WriteSprite(sprite, version);
            }
        }
    }

    private void WriteImage(PamImage image, int version)
    {
        WriteString16(image.Name);
        if (version >= 4)
        {
            _writer.WriteInt16((short)(image.Width ?? -1));
            _writer.WriteInt16((short)(image.Height ?? -1));
        }
        switch (image.Transform)
        {
            case PamRotateTransform rotate when version < 2:
                WriteS16F(rotate.Angle, AngleRate);
                break;
            case PamRotateTransform rotate:
                WriteRotateMatrix(rotate.Angle);
                break;
            case PamMatrixTransform matrix when version >= 2:
                WriteS32F(matrix.A, MatrixExactRate);
                WriteS32F(matrix.C, MatrixExactRate);
                WriteS32F(matrix.B, MatrixExactRate);
                WriteS32F(matrix.D, MatrixExactRate);
                break;
            default:
                throw new InvalidDataException(LiarUtil.Core.Strings.PAMImageTransformTypeDoesNotMatchVersion);
        }
        WriteS16F(image.Transform.X, SizeRate);
        WriteS16F(image.Transform.Y, SizeRate);
    }

    private void WriteSprite(PamSprite sprite, int version)
    {
        if (version >= 4)
        {
            WriteString16(sprite.Name ?? "");
            if (version >= 6)
            {
                WriteString16(sprite.Description ?? "");
            }
            WriteS32F(sprite.FrameRate ?? 0f, TimeRate);
        }
        _writer.WriteUInt16((ushort)sprite.Frames.Count);
        if (version >= 5)
        {
            _writer.WriteInt16((short)sprite.WorkAreaStart);
            _writer.WriteInt16((short)sprite.WorkAreaDuration);
        }
        foreach (var frame in sprite.Frames)
        {
            WriteFrame(frame, version);
        }
    }

    private void WriteFrame(PamFrame frame, int version)
    {
        var flags = 0;
        if (frame.Removes.Count > 0)
        {
            flags |= 1;
        }
        if (frame.Appends.Count > 0)
        {
            flags |= 2;
        }
        if (frame.Changes.Count > 0)
        {
            flags |= 4;
        }
        if (frame.Label is not null)
        {
            flags |= 8;
        }
        if (frame.Stop)
        {
            flags |= 16;
        }
        if (frame.Commands.Count > 0)
        {
            flags |= 32;
        }
        _writer.WriteUInt8((byte)flags);
        if ((flags & 1) != 0)
        {
            WriteVariantU8U16(frame.Removes.Count);
            foreach (var remove in frame.Removes)
            {
                WriteLayerRemove(remove);
            }
        }
        if ((flags & 2) != 0)
        {
            WriteVariantU8U16(frame.Appends.Count);
            foreach (var append in frame.Appends)
            {
                WriteLayerAppend(append, version);
            }
        }
        if ((flags & 4) != 0)
        {
            WriteVariantU8U16(frame.Changes.Count);
            foreach (var change in frame.Changes)
            {
                WriteLayerChange(change);
            }
        }
        if ((flags & 8) != 0)
        {
            WriteString16(frame.Label!);
        }
        if ((flags & 32) != 0)
        {
            _writer.WriteUInt8((byte)frame.Commands.Count);
            foreach (var command in frame.Commands)
            {
                WriteString16(command.Command);
                WriteString16(command.Argument);
            }
        }
    }

    private void WriteLayerRemove(PamLayerRemove remove) => WriteVariantFlagU16U32(remove.Index, 0);

    private void WriteLayerAppend(PamLayerAppend append, int version)
    {
        var flags = 0;
        if (append.TimeScale != 1f)
        {
            flags |= 1;
        }
        if (append.Name is not null)
        {
            flags |= 2;
        }
        if (append.PreloadFrame != 0)
        {
            flags |= 4;
        }
        if (append.Additive)
        {
            flags |= 8;
        }
        if (append.Sprite)
        {
            flags |= 16;
        }
        WriteVariantFlagU16U32(append.Index, flags);
        if (version < 6)
        {
            _writer.WriteUInt8((byte)append.Resource);
        }
        else
        {
            WriteVariantU8U16(append.Resource);
        }
        if ((flags & 4) != 0)
        {
            _writer.WriteInt16((short)append.PreloadFrame);
        }
        if ((flags & 2) != 0)
        {
            WriteString16(append.Name!);
        }
        if ((flags & 1) != 0)
        {
            WriteS32F(append.TimeScale, TimeRate);
        }
    }

    private void WriteLayerChange(PamLayerChange change)
    {
        var flags = 2;
        if (change.SpriteFrameNumber is not null)
        {
            flags |= 1;
        }
        if (change.Color is not null)
        {
            flags |= 8;
        }
        if (change.SourceRectangle is not null)
        {
            flags |= 32;
        }
        switch (change.Transform)
        {
            case PamRotateTransform:
                flags |= 16;
                break;
            case PamMatrixTransform:
                flags |= 4;
                break;
            case PamTranslateTransform:
                break;
            default:
                throw new InvalidDataException(LiarUtil.Core.Strings.UnknownPAMTransformType);
        }
        WriteVariantFlagU16U32(change.Index, flags, 6);
        switch (change.Transform)
        {
            case PamRotateTransform rotate:
                WriteS16F(rotate.Angle, AngleRate);
                break;
            case PamMatrixTransform matrix:
                WriteS32F(matrix.A, MatrixRate);
                WriteS32F(matrix.C, MatrixRate);
                WriteS32F(matrix.B, MatrixRate);
                WriteS32F(matrix.D, MatrixRate);
                break;
        }
        WriteS32F(change.Transform.X, SizeRate);
        WriteS32F(change.Transform.Y, SizeRate);
        if (change.SourceRectangle is { } rectangle)
        {
            WriteS16F(rectangle.X, SizeRate);
            WriteS16F(rectangle.Y, SizeRate);
            WriteS16F(rectangle.Width, SizeRate);
            WriteS16F(rectangle.Height, SizeRate);
        }
        if (change.Color is { } color)
        {
            WriteU8F(color.Red, ColorRate);
            WriteU8F(color.Green, ColorRate);
            WriteU8F(color.Blue, ColorRate);
            WriteU8F(color.Alpha, ColorRate);
        }
        if (change.SpriteFrameNumber is { } spriteFrameNumber)
        {
            _writer.WriteInt16((short)spriteFrameNumber);
        }
    }

    private void WriteRotateMatrix(float angle)
    {
        var cos = MathF.Cos(angle);
        var sin = MathF.Sin(angle);
        WriteS32F(cos, MatrixExactRate);
        WriteS32F(-sin, MatrixExactRate);
        WriteS32F(sin, MatrixExactRate);
        WriteS32F(cos, MatrixExactRate);
    }

    private void WriteVariantU8U16(long value)
    {
        if (value < byte.MaxValue)
        {
            _writer.WriteUInt8((byte)value);
        }
        else
        {
            _writer.WriteUInt8(byte.MaxValue);
            _writer.WriteUInt16((ushort)value);
        }
    }

    private void WriteVariantU16U32(long value)
    {
        if (value < ushort.MaxValue)
        {
            _writer.WriteUInt16((ushort)value);
        }
        else
        {
            _writer.WriteUInt16(ushort.MaxValue);
            _writer.WriteUInt32((uint)value);
        }
    }

    private void WriteVariantFlagU16U32(long value, int flags, int flagCount = 5)
    {
        var valueBitCount = 16 - flagCount;
        var valueMaximum = (1 << valueBitCount) - 1;
        var shortValue = value < valueMaximum ? (int)value : valueMaximum;
        _writer.WriteUInt16((ushort)(flags << valueBitCount | shortValue));
        if (value >= valueMaximum)
        {
            _writer.WriteUInt32((uint)value);
        }
    }

    private void WriteS16F(float value, float rate) => _writer.WriteInt16((short)MathF.Round(value * rate, MidpointRounding.AwayFromZero));

    private void WriteS32F(float value, float rate) => _writer.WriteInt32((int)MathF.Round(value * rate, MidpointRounding.AwayFromZero));

    private void WriteU16F(float value) => _writer.WriteUInt16((ushort)MathF.Round(value * SizeRate, MidpointRounding.AwayFromZero));

    private void WriteU8F(float value, float rate) => _writer.WriteUInt8((byte)MathF.Round(value * rate, MidpointRounding.AwayFromZero));

    private void WriteString16(string value)
    {
        var bytes = TextCodec.Encode(value);
        _writer.WriteUInt16((ushort)bytes.Length);
        _writer.WriteBytes(bytes);
    }
}
