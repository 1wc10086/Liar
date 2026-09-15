namespace LiarUtil.Core.Xm;

internal sealed partial class XmContext
{
    internal int GenerateSamples(Span<float> output, int frames)
    {
        var produced = 0;
        for (var i = 0; i < frames; i++)
        {
            output[i * 2] = 0f;
            output[(i * 2) + 1] = 0f;
            Sample(output, i * 2, (i * 2) + 1);
            produced++;
            if (_finished)
            {
                break;
            }
        }

        return produced;
    }

    private void Sample(Span<float> output, int leftIndex, int rightIndex)
    {
        AdvanceTick();

        var left = 0f;
        var right = 0f;
        for (var i = 0; i < _channels.Length; i++)
        {
            NextOfChannel(_channels[i], ref left, ref right);
        }

        output[leftIndex] = left;
        output[rightIndex] = right;
    }

    private void NextOfChannel(XmChannel ch, ref float outLeft, ref float outRight)
    {
        var value = NextOfSample(ch) * XmConst.Amplification;

        outLeft += value * ch.ActualLeft;
        outRight += value * ch.ActualRight;

        ch.FrameCount++;
        SlideTowards(ref ch.ActualLeft, ch.TargetLeft, XmConst.RampingVolumeRamp);
        SlideTowards(ref ch.ActualRight, ch.TargetRight, XmConst.RampingVolumeRamp);
    }

    private static void SlideTowards(ref float value, float goal, float rate)
    {
        if (value > goal)
        {
            value -= rate;
            if (value < goal)
            {
                value = goal;
            }
        }
        else
        {
            value += rate;
            if (value > goal)
            {
                value = goal;
            }
        }
    }

    private float NextOfSample(XmChannel ch)
    {
        var sample = ch.Sample;

        if (ch.SampleOffsetInvalid || sample is null
            || (sample.LoopLength == 0
                && ch.SamplePosition >= unchecked((uint)sample.Length * (uint)XmConst.SampleMicrosteps)))
        {
            if (ch.FrameCount >= XmConst.RampingPoints)
            {
                return 0f;
            }

            return Lerp(ch.EndOfPreviousSample[ch.FrameCount], 0f,
                ch.FrameCount / (float)XmConst.RampingPoints);
        }

        if (sample.LoopLength != 0
            && ch.SamplePosition >= unchecked((uint)sample.Length * (uint)XmConst.SampleMicrosteps))
        {
            var off = unchecked((uint)(sample.Length - sample.LoopLength) * (uint)XmConst.SampleMicrosteps);
            ch.SamplePosition = unchecked(ch.SamplePosition - off);
            var modulo = sample.PingPong
                ? unchecked((uint)sample.LoopLength * (uint)XmConst.SampleMicrosteps * 2u)
                : unchecked((uint)sample.LoopLength * (uint)XmConst.SampleMicrosteps);
            if (modulo != 0)
            {
                ch.SamplePosition %= modulo;
            }

            ch.SamplePosition = unchecked(ch.SamplePosition + off);
        }

        var a = ch.SamplePosition / XmConst.SampleMicrosteps;
        uint b;
        var t = (ch.SamplePosition % XmConst.SampleMicrosteps) / (float)XmConst.SampleMicrosteps;
        var length = (uint)sample.Length;

        if (sample.LoopLength == 0)
        {
            b = a + 1 < length ? a + 1 : a;
        }
        else if (!sample.PingPong)
        {
            b = a + 1 == length ? (uint)(sample.Length - sample.LoopLength) : a + 1;
        }
        else if (a < length)
        {
            b = a + 1 == length ? a : a + 1;
        }
        else
        {
            a = (unchecked((uint)sample.Length * 2u) - 1u) - a;
            b = a == (uint)(sample.Length - sample.LoopLength) ? a : a - 1;
        }

        if (a >= length)
        {
            a = length - 1;
        }

        if (b >= length)
        {
            b = length - 1;
        }

        var u = SampleAt(sample, a);
        u = Lerp(u, SampleAt(sample, b), t);

        if (ch.FrameCount < XmConst.RampingPoints)
        {
            return Lerp(ch.EndOfPreviousSample[ch.FrameCount], u,
                ch.FrameCount / (float)XmConst.RampingPoints);
        }

        ch.SamplePosition = unchecked(ch.SamplePosition + ch.Step);
        return u;
    }

    private float SampleAt(XmSample sample, uint index)
    {
        var offset = sample.Index + (int)index;
        if ((uint)offset >= (uint)_module.SamplesDataLength)
        {
            return 0f;
        }

        return _module.SamplesData[offset] / 32768f;
    }

    private static float Lerp(float u, float v, float t) => u + (t * (v - u));
}
