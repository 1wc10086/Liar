namespace LiarUtil.Core.Xm;

internal static class XmModulation
{
    internal static void ResetVolumeOffset(XmChannel ch) => ch.VolumeOffset = 0;

    internal static void PitchSlide(XmChannel ch, int periodOffset, PitchSlideBehaviour mode)
    {
        ch.Period = unchecked((ushort)(ch.Period + ch.GlissandoControlError));
        ch.GlissandoControlError = 0;
        ch.VibratoOffset = 0;

        var sum = ch.Period + periodOffset;
        switch (mode)
        {
            case PitchSlideBehaviour.Clamp:
                ch.Period = sum is < 0 or > 0xFFFF
                    ? (ushort)(periodOffset > 0 ? 32767 : 1)
                    : (ushort)sum;
                break;

            case PitchSlideBehaviour.Cut:
                if (sum is < 0 or > 0xFFFF)
                {
                    ch.Sample = null;
                }

                ch.Period = unchecked((ushort)sum);
                break;

            default:
                ch.Period = unchecked((ushort)sum);
                break;
        }
    }

    internal static void ParamSlide(ref int param, int rawValue, int max)
    {
        if ((rawValue & 0xF0) != 0)
        {
            var sum = param + (rawValue >> 4);
            param = sum > 0xFF || sum > max ? max : sum;
        }
        else
        {
            var diff = param - rawValue;
            param = diff < 0 ? 0 : diff;
        }
    }

    internal static void Vibrato(XmChannel ch, int fine)
    {
        PitchSlide(ch, 0, PitchSlideBehaviour.Wraparound);

        var div = fine != 0 ? 0x40 : 0x10;
        var wave = XmTables.Waveform(ch.VibratoControlParam, unchecked((byte)ch.VibratoTicks));
        ch.VibratoOffset = unchecked((sbyte)(wave * (ch.VibratoParam & 0x0F) / div));
        ch.VibratoTicks = unchecked((byte)(ch.VibratoTicks + ((ch.VibratoParam >> 4) << 2)));
    }

    internal static void Tremolo(XmChannel ch, int param)
    {
        var ticks = (byte)ch.TremoloTicks;
        if ((ch.TremoloControlParam & 127) == XmConst.RampDownWaveform)
        {
            if (ticks >= 0x80)
            {
                ticks = unchecked((byte)(0x80 - ticks));
            }

            if (ch.VibratoTicks >= 0x80)
            {
                ticks = unchecked((byte)(0x80 - ticks));
            }
        }

        var wave = XmTables.Waveform(ch.TremoloControlParam, ticks);
        ch.VolumeOffset = unchecked((sbyte)(wave * (param & 0x0F) * 4 / 128));
        ch.TremoloTicks = unchecked((byte)(ch.TremoloTicks + ((param >> 4) << 2)));
    }

    internal static void Tremor(XmChannel ch, int param)
    {
        var expired = ch.TremorTicks == 0;
        ch.TremorTicks = unchecked((byte)(ch.TremorTicks - 1));
        if (expired)
        {
            ch.TremorOn = !ch.TremorOn;
            ch.TremorTicks = ch.TremorOn ? param >> 4 : param & 0xF;
        }

        ch.VolumeOffset = ch.TremorOn ? 0 : XmConst.MaxVolume;
    }

    internal static void Arpeggio(XmContext ctx, XmChannel ch, int param)
    {
        var t = unchecked((byte)(ctx.CurrentTempo - ctx.CurrentTickByte));

        if (ctx.CurrentTickByte == 0 || t == 16 || (t < 16 && t % 3 == 0))
        {
            ch.ArpNoteOffset = 0;
            return;
        }

        ch.ShouldResetArpeggio = true;
        XmFrequency.RoundPeriodToSemitone(ctx.ModuleRef, ch);

        ch.ArpNoteOffset = t > 16 || t % 3 == 2 ? param & 0x0F : param >> 4;
    }

    internal static void TonePortamento(XmContext ctx, XmChannel ch)
    {
        if (ch.TonePortamentoTargetPeriod == 0 || ch.Period == 0)
        {
            return;
        }

        var incr = 4 * ch.TonePortamentoParam;
        var diff = ch.TonePortamentoTargetPeriod - ch.Period;
        if (diff > incr)
        {
            diff = incr;
        }

        if (diff < -incr)
        {
            diff = -incr;
        }

        PitchSlide(ch, unchecked((short)diff), PitchSlideBehaviour.Wraparound);

        if (ch.GlissandoControlParam != 0)
        {
            XmFrequency.RoundPeriodToSemitone(ctx.ModuleRef, ch);
        }
    }

    internal static void TonePortamentoTarget(XmContext ctx, XmChannel ch)
    {
        if (ch.Sample is null)
        {
            return;
        }

        var note = ch.Current.Note + ch.Sample.RelativeNote;
        if (note <= 0 || note >= 120)
        {
            return;
        }

        ch.TonePortamentoTargetPeriod = XmFrequency.Period(
            ctx.ModuleRef,
            (16 * (note - 1)) + ch.Finetune);
    }

    internal static void MultiRetrigNote(XmContext ctx, XmChannel ch, int param)
    {
        if (ch.Current.VolumeColumn != 0 && ctx.CurrentTickByte == 0)
        {
            return;
        }

        ch.MultiRetrigTicks++;
        if (ch.MultiRetrigTicks < (param & 0x0F))
        {
            return;
        }

        ch.MultiRetrigTicks = 0;
        ctx.TriggerNote(ch);

        if (ch.Current.VolumeColumn is >= 0x10 and <= 0x50)
        {
            return;
        }

        var x = param >> 4;
        var volume = ch.Volume;
        volume += XmTables.MultiRetrigAdd[x];
        volume = unchecked((byte)volume);
        volume -= XmTables.MultiRetrigAdd[x ^ 8];
        volume = unchecked((byte)volume);
        volume *= XmTables.MultiRetrigMultiply[x];
        volume = unchecked((byte)volume);
        volume /= XmTables.MultiRetrigMultiply[x ^ 8];
        volume = unchecked((byte)volume);

        ch.Volume = volume > 255 - 16 ? 0 : volume > XmConst.MaxVolume ? XmConst.MaxVolume : volume;
    }

    internal static void VolumeSlideS3m(XmChannel ch)
    {
        var x = ch.EffectParam >> 4;
        var y = ch.EffectParam & 0xF;
        var p = x == 0 || y == 0
            ? ch.EffectParam
            : y == 0xF ? x << 4 : y;

        ResetVolumeOffset(ch);
        ParamSlide(ref ch.Volume, p, XmConst.MaxVolume);
    }

    internal static void UpdateEffectMemoryXy(ref int memory, int value)
    {
        if ((value & 0x0F) != 0)
        {
            memory = (memory & 0xF0) | (value & 0x0F);
        }

        if ((value & 0xF0) != 0)
        {
            memory = (memory & 0x0F) | (value & 0xF0);
        }
    }
}
