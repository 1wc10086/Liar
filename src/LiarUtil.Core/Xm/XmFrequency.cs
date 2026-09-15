namespace LiarUtil.Core.Xm;

internal static class XmFrequency
{
    internal static ushort LinearPeriod(int note) => unchecked((ushort)(7680 - (note * 4)));

    internal static ushort AmigaPeriod(int note)
    {
        var value = 32f * 856f * Exp2(note / (-12f * 16f));
        return ToUShort(value);
    }

    internal static ushort Period(XmModule module, int note) =>
        module.AmigaFrequencies ? AmigaPeriod(note) : LinearPeriod(note);

    internal static uint LinearFrequency(ushort period, int arpNoteOffset)
    {
        var p = unchecked((ushort)(period - (arpNoteOffset * 64)));
        if (arpNoteOffset != 0 && p < 1540)
        {
            p = 1540;
        }

        return ToUInt(8363f * Exp2((4608f - p) / 768f));
    }

    internal static uint AmigaFrequency(ushort period, int arpNoteOffset)
    {
        var p = (float)period;
        if (arpNoteOffset != 0)
        {
            p *= Exp2(arpNoteOffset / -12f);
            if (p < 107f)
            {
                p = 107f;
            }
        }

        return ToUInt(4f * 7093789.2f / (p * 2f));
    }

    internal static uint Frequency(XmModule module, XmChannel ch)
    {
        var period = unchecked((ushort)(ch.Period - ch.VibratoOffset - ch.AutovibratoOffset));
        return module.AmigaFrequencies
            ? AmigaFrequency(period, ch.ArpNoteOffset)
            : LinearFrequency(period, ch.ArpNoteOffset);
    }

    internal static void RoundLinearPeriodToSemitone(XmChannel ch)
    {
        var finetune = ch.Finetune * 4;
        var newPeriod = unchecked((ushort)(((ch.Period + finetune + 32) & 0xFFC0) - finetune));
        ch.GlissandoControlError = unchecked((sbyte)(ch.Period - newPeriod));
        ch.Period = newPeriod;
    }

    internal static void RoundPeriodToSemitone(XmModule module, XmChannel ch)
    {
        XmModulation.PitchSlide(ch, 0, PitchSlideBehaviour.Wraparound);
        if (!module.AmigaFrequencies)
        {
            RoundLinearPeriodToSemitone(ch);
        }
    }

    private static float Exp2(float value) => MathF.Pow(2f, value);

    private static ushort ToUShort(float value)
    {
        if (!(value > 0f))
        {
            return 0;
        }

        return value >= 65535f ? ushort.MaxValue : (ushort)value;
    }

    private static uint ToUInt(float value)
    {
        if (!(value > 0f))
        {
            return 0;
        }

        return value >= 4294967295f ? uint.MaxValue : (uint)value;
    }
}
