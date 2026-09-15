using LiarUtil.Core.Ppf.Models;

namespace LiarUtil.Core.Ppf.Encoding;

internal sealed partial class PpfEncoder
{
    private void WriteTexture(PpfTexture value)
    {
        PpfStrings.Write(_writer, value.Name);
        _writer.WriteInt16(value.Cell);
        _writer.WriteInt16(value.Row);
        _writer.WriteBoolean(value.Padded);
        PpfStrings.Write(_writer, value.Path);
    }

    private void WriteEmitter(PpfEmitter value)
    {
        _writer.WriteInt32(value.Unknown1);
        PpfStrings.Write(_writer, value.Name);
        _writer.WriteBoolean(value.KeepInOrder);
        _writer.WriteInt32(value.Unknown2);
        _writer.WriteBoolean(value.OldestInFront);
        WriteList(value.Particles, WriteEmitterParticle);
        _writer.WriteInt32(value.Unknown3);
        WriteEmitterValues(value.Values);
        _writer.WriteInt32(value.Unknown4);
        _writer.WriteInt32(value.Unknown5);
    }

    private void WriteEmitterParticle(PpfEmitterParticle value)
    {
        _writer.WriteInt32(value.Unknown1);
        _writer.WriteInt32(value.Unknown2);
        _writer.WriteInt32(value.Unknown3);
        _writer.WriteSingle(value.Unknown4);
        _writer.WriteInt32(value.Unknown5);
        _writer.WriteInt32(value.Unknown6);
        _writer.WriteInt32(value.Unknown7);
        _writer.WriteInt32(value.Unknown8);
        _writer.WriteInt32(value.Unknown9);
        _writer.WriteInt32(value.Unknown10);
        _writer.WriteInt32(value.Unknown11);
        _writer.WriteInt32(value.Unknown12);
        _writer.WriteInt32(value.Unknown13);
        _writer.WriteInt32(value.Unknown14);
        _writer.WriteInt32(value.Unknown15);
        _writer.WriteInt32(value.Unknown16);
        _writer.WriteBoolean(value.Instance);
        _writer.WriteBoolean(value.Single);
        _writer.WriteBoolean(value.PreserveColor);
        _writer.WriteBoolean(value.AttachToEmitter);
        _writer.WriteSingle(value.AttachValue);
        _writer.WriteBoolean(value.FlipHorizontal);
        _writer.WriteBoolean(value.FlipVertical);
        _writer.WriteBoolean(value.AnimationStartOnRandomFrame);
        _writer.WriteInt32(value.RepeatColor);
        _writer.WriteInt32(value.RepeatAlpha);
        _writer.WriteBoolean(value.LinkTransparencyToColor);
        PpfStrings.Write(_writer, value.Name);
        _writer.WriteBoolean(value.AngleAlignToMotion);
        _writer.WriteBoolean(value.AngleRandomAlign);
        _writer.WriteBoolean(value.AngleKeepAlignedToMotion);
        _writer.WriteInt32(value.AngleValue);
        _writer.WriteInt32(value.AngleAlignOffset);
        _writer.WriteInt32(value.AnimationSpeed);
        _writer.WriteBoolean(value.RandomGradientColor);
        _writer.WriteInt32(value.Unknown17);
        _writer.WriteInt32(value.Texture);
        WriteList(value.Colors, WriteColorPoint);
        WriteList(value.Alphas, WriteAlphaPoint);
        var values = value.Values;
        WriteValue1(values.Life);
        WriteValue1(values.Number);
        WriteValue1(values.SizeX);
        WriteValue1(values.Velocity);
        WriteValue1(values.Weight);
        WriteValue1(values.Spin);
        WriteValue1(values.MotionRand);
        WriteValue1(values.Bounce);
        WriteValue1(values.LifeVariation);
        WriteValue1(values.NumberVariation);
        WriteValue1(values.SizeXVariation);
        WriteValue1(values.VelocityVariation);
        WriteValue1(values.WeightVariation);
        WriteValue1(values.SpinVariation);
        WriteValue1(values.MotionRandVariation);
        WriteValue1(values.BounceVariation);
        WriteValue1(values.SizeXOverLife);
        WriteValue1(values.VelocityOverLife);
        WriteValue1(values.WeightOverLife);
        WriteValue1(values.SpinOverLife);
        WriteValue1(values.MotionRandOverLife);
        WriteValue1(values.BounceOverLife);
        WriteValue1(values.Visibility);
        WriteVector2(value.ReferencePointOffset);
        _writer.WriteInt32(value.Unknown18);
        _writer.WriteInt32(value.Unknown19);
        _writer.WriteBoolean(value.LockAspect);
        WriteValue1(values.SizeY);
        WriteValue1(values.SizeYVariation);
        WriteValue1(values.SizeYOverLife);
        _writer.WriteInt32(value.AngleRange);
        _writer.WriteInt32(value.AngleOffset);
        _writer.WriteBoolean(value.GetColorFromLayer);
        _writer.WriteBoolean(value.UpdateColorFromLayer);
        _writer.WriteBoolean(value.UseEmitterAngleAndRange);
        WriteValue1(values.EmissionAngle);
        WriteValue1(values.EmissionRange);
        _writer.WriteInt32(value.Unknown20);
        WriteValue1(value.Unknown21);
        _writer.WriteBoolean(value.UseKeyColorOnly);
        _writer.WriteBoolean(value.UpdateTransparencyFromLayer);
        _writer.WriteBoolean(value.UseNextColorKey);
        _writer.WriteInt32(value.NumberOfEachColor);
        _writer.WriteBoolean(value.GetTransparencyFromLayer);
    }
}
