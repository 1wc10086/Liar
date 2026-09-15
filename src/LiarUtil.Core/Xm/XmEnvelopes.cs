namespace LiarUtil.Core.Xm;

internal static class XmEnvelopes
{
    internal static void TickEnvelopes(XmChannel ch)
    {
        var inst = ch.Instrument;
        if (inst is null)
        {
            return;
        }

        Autovibrato(ch, inst);

        if (!ch.Sustained)
        {
            ch.FadeoutVolume = ch.FadeoutVolume < inst.VolumeFadeout
                ? (ushort)0
                : (ushort)(ch.FadeoutVolume - inst.VolumeFadeout);
        }
        else
        {
            ch.FadeoutVolume = XmConst.MaxFadeoutVolume - 1;
        }

        ch.VolumeEnvelopeVolume = inst.VolumeEnvelope.NumPoints != 0
            ? TickEnvelope(ch, inst.VolumeEnvelope, ref ch.VolumeEnvelopeFrameCount)
            : XmConst.MaxEnvelopeValue;

        ch.PanningEnvelopePanning = inst.PanningEnvelope.NumPoints != 0
            ? TickEnvelope(ch, inst.PanningEnvelope, ref ch.PanningEnvelopeFrameCount)
            : XmConst.MaxEnvelopeValue / 2;
    }

    private static void Autovibrato(XmChannel ch, XmInstrument inst)
    {
        var step = unchecked((byte)(ch.AutovibratoTicks * inst.VibratoRate));
        var offset = unchecked((sbyte)((int)XmTables.Waveform(inst.VibratoType, step)
            * -inst.VibratoDepth / 128));

        if (ch.AutovibratoTicks < inst.VibratoSweep)
        {
            offset = unchecked((sbyte)(offset * ch.AutovibratoTicks / inst.VibratoSweep));
        }

        ch.AutovibratoOffset = offset;
        ch.AutovibratoTicks++;
    }

    private static int TickEnvelope(XmChannel ch, XmEnvelope env, ref ushort counter)
    {
        if (counter == env.Points[env.LoopEndPoint].Frame
            && (ch.Sustained || env.SustainPoint != env.LoopEndPoint))
        {
            counter = (ushort)env.Points[env.LoopStartPoint].Frame;
        }

        if (ch.Sustained
            && (env.SustainPoint & 128) == 0
            && counter == env.Points[env.SustainPoint].Frame)
        {
            return env.Points[env.SustainPoint].Value;
        }

        for (var j = env.NumPoints - 1; j > 0; j--)
        {
            if (counter < env.Points[j - 1].Frame)
            {
                continue;
            }

            var value = Lerp(env.Points[j - 1], env.Points[j], counter);
            counter++;
            return value;
        }

        return env.Points[env.NumPoints - 1].Value;
    }

    private static int Lerp(XmEnvelopePoint a, XmEnvelopePoint b, int pos)
    {
        if (pos >= b.Frame)
        {
            return b.Value;
        }

        var frames = b.Frame - a.Frame;
        if (frames <= 0)
        {
            return b.Value;
        }

        var val = b.Value * (pos - a.Frame) + a.Value * (b.Frame - pos);
        return val / frames;
    }
}
