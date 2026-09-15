using LiarUtil.Core.Ppf.Models;

namespace LiarUtil.Core.Ppf.Encoding;

internal sealed partial class PpfEncoder
{
    private void WriteEmitterValues(PpfEmitterValues values)
    {
        WriteValue1(values.FLife);
        WriteValue1(values.FNumber);
        WriteValue1(values.FVelocity);
        WriteValue1(values.FWeight);
        WriteValue1(values.FSpin);
        WriteValue1(values.FMotionRand);
        WriteValue1(values.FBounce);
        WriteValue1(values.FZoom);
        WriteValue1(values.Life);
        WriteValue1(values.Number);
        WriteValue1(values.SizeX);
        WriteValue1(values.SizeY);
        WriteValue1(values.Velocity);
        WriteValue1(values.Weight);
        WriteValue1(values.Spin);
        WriteValue1(values.MotionRand);
        WriteValue1(values.Bounce);
        WriteValue1(values.Zoom);
        WriteValue1(values.Visibility);
        WriteValue1(values.Unknown19);
        WriteValue1(values.TintStrength);
        WriteValue1(values.EmissionAngle);
        WriteValue1(values.EmissionRange);
        WriteValue1(values.FLifeVariation);
        WriteValue1(values.FNumberVariation);
        WriteValue1(values.FSizeXVariation);
        WriteValue1(values.FSizeYVariation);
        WriteValue1(values.FVelocityVariation);
        WriteValue1(values.FWeightVariation);
        WriteValue1(values.FSpinVariation);
        WriteValue1(values.FMotionRandVariation);
        WriteValue1(values.FBounceVariation);
        WriteValue1(values.FZoomVariation);
        WriteValue1(values.FNumberOverLife);
        WriteValue1(values.FSizeXOverLife);
        WriteValue1(values.FSizeYOverLife);
        WriteValue1(values.FVelocityOverLife);
        WriteValue1(values.FWeightOverLife);
        WriteValue1(values.FSpinOverLife);
        WriteValue1(values.FMotionRandOverLife);
        WriteValue1(values.FBounceOverLife);
        WriteValue1(values.FZoomOverLife);
    }

    private void WriteValue1(PpfValue1 value)
    {
        var points = value.Points;
        var count = points.Count;
        float? initialTime = null;
        float? initialValue = null;
        if (count > 0)
        {
            var first = points[0];
            if (first.Time == 0f)
            {
                initialTime = first.Time;
            }
            if (first.Value == 0f || first.Value == 1f || first.Value == 2f)
            {
                initialValue = first.Value;
            }
        }
        var flags = 0;
        if (count > 1 && value.Control)
        {
            flags |= 0b1;
        }
        if (initialTime.HasValue)
        {
            flags |= 0b10;
        }
        if (initialValue == 0f)
        {
            flags |= 0b100;
        }
        else if (initialValue == 1f)
        {
            flags |= 0b1000;
        }
        else if (initialValue == 2f)
        {
            flags |= 0b1100;
        }
        _writer.WriteByte((byte)((flags << 3) | Math.Min(count, 0b111)));
        if (count >= 0b111)
        {
            _writer.WriteInt16(checked((short)count));
        }
        for (var index = 0; index < count; index++)
        {
            var point = points[index];
            if (index != 0 || !initialTime.HasValue)
            {
                _writer.WriteSingle(point.Time);
            }
            if (index != 0 || !initialValue.HasValue)
            {
                _writer.WriteSingle(point.Value);
            }
            if (value.Control)
            {
                WriteControlValue(point.ControlValue);
            }
        }
    }

    private void WriteValue2(PpfValue2 value)
    {
        var points = value.Points;
        WriteCount(points.Count);
        if (points.Count > 1)
        {
            _writer.WriteBoolean(value.Control);
        }
        foreach (var point in points)
        {
            _writer.WriteInt32(point.Time);
            WriteVector2(point.Value);
            if (value.Control)
            {
                WriteControlValue(point.ControlValue);
            }
        }
    }

    private void WriteValue2Simple(PpfValue2 value)
    {
        _writer.WriteSingle(value.Unknown1);
        _writer.WriteSingle(value.Unknown2);
        WriteCount(value.Points.Count);
        foreach (var point in value.Points)
        {
            _writer.WriteInt32(point.Time);
            WriteVector2(point.Value);
        }
    }

    private void WriteColorPoint(PpfColorPoint value)
    {
        _writer.WriteByte(value.Value.Red);
        _writer.WriteByte(value.Value.Green);
        _writer.WriteByte(value.Value.Blue);
        _writer.WriteSingle(value.Time);
    }

    private void WriteAlphaPoint(PpfAlphaPoint value)
    {
        _writer.WriteByte(value.Value);
        _writer.WriteSingle(value.Time);
    }
}
