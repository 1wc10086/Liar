using LiarUtil.Core.Ppf.Models;

namespace LiarUtil.Core.Ppf.Encoding;

internal sealed partial class PpfEncoder
{
    private void WriteLayer(PpfLayer value)
    {
        PpfStrings.Write(_writer, value.Name);
        WriteList(value.Emitters, WriteLayerEmitter);
        WriteList(value.Deflectors, WriteLayerDeflector);
        WriteList(value.Blockers, WriteLayerBlocker);
        WriteValue2(value.Offset);
        WriteValue1(value.Angle);
        PpfStrings.Write(_writer, value.Unknown1);
        WriteFixedBytes(value.Unknown3, 32, LiarUtil.Core.Strings.LayerUnknownData3);
        WriteStringList(value.Unknown2);
        WriteFixedBytes(value.Unknown4, 36, LiarUtil.Core.Strings.LayerUnknownData4);
        WriteList(value.Forces, WriteLayerForce);
        WriteFixedBytes(value.Unknown5, 28, LiarUtil.Core.Strings.LayerUnknownData5);
    }

    private void WriteLayerEmitter(PpfLayerEmitter value)
    {
        _writer.WriteSingle(value.Unknown1);
        _writer.WriteSingle(value.Unknown2);
        _writer.WriteSingle(value.Unknown3);
        _writer.WriteSingle(value.Unknown4);
        _writer.WriteSingle(value.Unknown5);
        _writer.WriteSingle(value.Unknown6);
        _writer.WriteSingle(value.Unknown7);
        _writer.WriteSingle(value.Unknown8);
        _writer.WriteSingle(value.Unknown9);
        _writer.WriteSingle(value.Unknown10);
        _writer.WriteSingle(value.Unknown11);
        _writer.WriteSingle(value.Unknown12);
        _writer.WriteInt32(value.Unknown13);
        _writer.WriteInt32(value.Unknown14);
        _writer.WriteInt32(value.PreloadFrame);
        _writer.WriteInt32(value.Unknown15);
        PpfStrings.Write(_writer, value.Name);
        _writer.WriteInt32(value.Geom);
        _writer.WriteSingle(value.Unknown16);
        _writer.WriteSingle(value.Unknown17);
        _writer.WriteBoolean(value.Geom4If2);
        _writer.WriteBoolean(value.EmitIn);
        _writer.WriteBoolean(value.EmitOut);
        WriteColor(value.TintColor);
        _writer.WriteInt32(value.Unknown18);
        _writer.WriteInt32(value.EmitAtPoint.X);
        _writer.WriteInt32(value.Type);
        WriteValue2(value.Position);
        WriteList(value.Points, WriteValue2Simple);
        var values = value.Values;
        WriteValue1(values.Life);
        WriteValue1(values.Number);
        WriteValue1(values.SizeX);
        WriteValue1(values.Velocity);
        WriteValue1(values.Weight);
        WriteValue1(values.Spin);
        WriteValue1(values.MotionRand);
        WriteValue1(values.Bounce);
        WriteValue1(values.Zoom);
        WriteValue1(values.Visibility);
        WriteValue1(values.TintStrength);
        WriteValue1(values.EmissionAngle);
        WriteValue1(values.EmissionRange);
        WriteValue1(values.Active);
        WriteValue1(values.Angle);
        WriteValue1(values.XRadius);
        WriteValue1(values.YRadius);
        _writer.WriteInt32(value.EmitAtPoint.Y);
        _writer.WriteInt32(value.Unknown19);
        WriteValue1(values.SizeY);
        _writer.WriteInt32(value.Unknown20);
        WriteValue1(values.Unknown18);
        WriteStringList(value.MaskPath);
        _writer.WriteBoolean(value.Mask);
        PpfStrings.Write(_writer, value.MaskName);
        _writer.WriteInt32(value.Unknown21);
        _writer.WriteInt32(value.Unknown22);
        _writer.WriteBoolean(value.InvertMask);
        _writer.WriteInt32(value.Unknown23);
        _writer.WriteInt32(value.Unknown24);
        _writer.WriteBoolean(value.IsSuper);
        WriteList(value.Free, _writer.WriteInt16);
        _writer.WriteInt32(value.Unknown25);
        _writer.WriteSingle(value.Unknown26);
        _writer.WriteSingle(value.Unknown27);
    }

    private void WriteLayerDeflector(PpfLayerDeflector value)
    {
        PpfStrings.Write(_writer, value.Name);
        _writer.WriteInt32(value.Bounce);
        _writer.WriteInt32(value.Hit);
        _writer.WriteInt32(value.Thickness);
        _writer.WriteBoolean(value.Visible);
        WriteValue2(value.Position);
        WriteList(value.Points, WriteValue2Simple);
        WriteValue1(value.Active);
        WriteValue1(value.Angle);
    }

    private void WriteLayerBlocker(PpfLayerBlocker value)
    {
        PpfStrings.Write(_writer, value.Name);
        _writer.WriteInt32(value.Unknown1);
        _writer.WriteInt32(value.Unknown2);
        _writer.WriteInt32(value.Unknown3);
        _writer.WriteInt32(value.Unknown4);
        _writer.WriteInt32(value.Unknown5);
        WriteValue2(value.Position);
        WriteList(value.Points, WriteValue2Simple);
        WriteValue1(value.Active);
        WriteValue1(value.Angle);
    }

    private void WriteLayerForce(PpfLayerForce value)
    {
        PpfStrings.Write(_writer, value.Name);
        _writer.WriteBoolean(value.Visible);
        WriteValue2(value.Position);
        WriteValue1(value.Active);
        WriteValue1(value.Unknown1);
        WriteValue1(value.Strength);
        WriteValue1(value.Width);
        WriteValue1(value.Height);
        WriteValue1(value.Angle);
        WriteValue1(value.Direction);
    }
}
