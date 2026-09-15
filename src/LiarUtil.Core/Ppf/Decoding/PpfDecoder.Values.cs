using LiarUtil.Core.Ppf.Models;

namespace LiarUtil.Core.Ppf.Decoding;

internal sealed partial class PpfDecoder
{
    private PpfEmitterValues ReadEmitterValues()
    {
        var values = new PpfEmitterValues();
        values.FLife = ReadValue1();
        values.FNumber = ReadValue1();
        values.FVelocity = ReadValue1();
        values.FWeight = ReadValue1();
        values.FSpin = ReadValue1();
        values.FMotionRand = ReadValue1();
        values.FBounce = ReadValue1();
        values.FZoom = ReadValue1();
        values.Life = ReadValue1();
        values.Number = ReadValue1();
        values.SizeX = ReadValue1();
        values.SizeY = ReadValue1();
        values.Velocity = ReadValue1();
        values.Weight = ReadValue1();
        values.Spin = ReadValue1();
        values.MotionRand = ReadValue1();
        values.Bounce = ReadValue1();
        values.Zoom = ReadValue1();
        values.Visibility = ReadValue1();
        values.Unknown19 = ReadValue1();
        values.TintStrength = ReadValue1();
        values.EmissionAngle = ReadValue1();
        values.EmissionRange = ReadValue1();
        values.FLifeVariation = ReadValue1();
        values.FNumberVariation = ReadValue1();
        values.FSizeXVariation = ReadValue1();
        values.FSizeYVariation = ReadValue1();
        values.FVelocityVariation = ReadValue1();
        values.FWeightVariation = ReadValue1();
        values.FSpinVariation = ReadValue1();
        values.FMotionRandVariation = ReadValue1();
        values.FBounceVariation = ReadValue1();
        values.FZoomVariation = ReadValue1();
        values.FNumberOverLife = ReadValue1();
        values.FSizeXOverLife = ReadValue1();
        values.FSizeYOverLife = ReadValue1();
        values.FVelocityOverLife = ReadValue1();
        values.FWeightOverLife = ReadValue1();
        values.FSpinOverLife = ReadValue1();
        values.FMotionRandOverLife = ReadValue1();
        values.FBounceOverLife = ReadValue1();
        values.FZoomOverLife = ReadValue1();
        return values;
    }

    private PpfValue1 ReadValue1()
    {
        var countWithFlags = _reader.ReadByte();
        var count = countWithFlags & 0b111;
        if (count == 0b111)
        {
            count = _reader.ReadInt16();
        }
        if (count < 0)
        {
            throw new InvalidDataException(string.Format(LiarUtil.Core.Strings.PPFValuePointCountInvalid0, count));
        }
        var flags = countWithFlags >> 3;
        var control = count > 1 && (flags & 0b1) != 0;
        float? initialTime = (flags & 0b10) != 0 ? 0f : null;
        float? initialValue = (flags & 0b1100) switch
        {
            0b0100 => 0f,
            0b1000 => 1f,
            0b1100 => 2f,
            _ => null,
        };
        var points = new List<PpfValue1Point>(count);
        for (var index = 0; index < count; index++)
        {
            var time = index == 0 && initialTime.HasValue ? initialTime.Value : _reader.ReadSingle();
            var value = index == 0 && initialValue.HasValue ? initialValue.Value : _reader.ReadSingle();
            var controlValue = control ? ReadControlValue() : new PpfControlValue();
            points.Add(new PpfValue1Point { Time = time, Value = value, ControlValue = controlValue });
        }
        return new PpfValue1 { Control = control, Points = points };
    }

    private PpfValue2 ReadValue2()
    {
        var count = _reader.ReadUInt16();
        var control = count > 1 && _reader.ReadBoolean();
        return new PpfValue2 { Control = control, Points = ReadValue2Points(count, control) };
    }

    private PpfValue2 ReadValue2Simple()
    {
        var unknown1 = _reader.ReadSingle();
        var unknown2 = _reader.ReadSingle();
        var count = _reader.ReadUInt16();
        return new PpfValue2
        {
            Unknown1 = unknown1,
            Unknown2 = unknown2,
            Points = ReadValue2Points(count, false),
        };
    }

    private List<PpfValue2Point> ReadValue2Points(int count, bool control)
    {
        var points = new List<PpfValue2Point>(count);
        for (var index = 0; index < count; index++)
        {
            points.Add(new PpfValue2Point
            {
                Time = _reader.ReadInt32(),
                Value = ReadVector2(),
                ControlValue = control ? ReadControlValue() : new PpfControlValue(),
            });
        }
        return points;
    }

    private PpfVector2 ReadVector2() => new()
    {
        X = _reader.ReadSingle(),
        Y = _reader.ReadSingle(),
    };

    private PpfControlValue ReadControlValue() => new()
    {
        Start = ReadVector2(),
        End = ReadVector2(),
    };

    private PpfColorPoint ReadColorPoint()
    {
        var value = ReadRgb();
        var time = _reader.ReadSingle();
        return new PpfColorPoint { Time = time, Value = value };
    }

    private PpfAlphaPoint ReadAlphaPoint()
    {
        var value = _reader.ReadByte();
        var time = _reader.ReadSingle();
        return new PpfAlphaPoint { Time = time, Value = value };
    }
}
