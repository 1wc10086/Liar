namespace LiarUtil.Core.Xm;

internal static partial class XmLoaderXm0104
{
    private static int LoadInstrument(XmReader r, XmModule module, int index, int offset)
    {
        var instr = module.Instruments[index];
        instr.Name = r.Text(offset + 4, 22, XmConst.InstrumentNameLength - 1);

        var headerSize = r.U32(offset);
        var headerBound = XmLoader.Idx((long)offset + headerSize);
        var hr = r.WithBound(Math.Max(headerBound, offset));

        int numSamples = 27 < headerSize ? hr.U8(offset + 27) : 0;
        if (numSamples == 0)
        {
            Array.Fill(instr.SampleOfNotes, ushort.MaxValue);
            return XmLoader.Idx((long)offset + headerSize);
        }

        for (var j = 0; j < XmConst.MaxNote; j++)
        {
            int s = hr.U8(offset + 33 + j);
            instr.SampleOfNotes[j] = s >= numSamples
                ? ushort.MaxValue
                : (ushort)(s + module.NumSamples);
        }

        LoadEnvelopePoints(hr, instr.VolumeEnvelope, offset + 129);
        instr.VolumeEnvelope.NumPoints = hr.U8(offset + 225);
        instr.VolumeEnvelope.SustainPoint = hr.U8(offset + 227);
        instr.VolumeEnvelope.LoopStartPoint = hr.U8(offset + 228);
        instr.VolumeEnvelope.LoopEndPoint = hr.U8(offset + 229);
        FixEnvelope(instr.VolumeEnvelope, hr.U8(offset + 233));

        LoadEnvelopePoints(hr, instr.PanningEnvelope, offset + 177);
        instr.PanningEnvelope.NumPoints = hr.U8(offset + 226);
        instr.PanningEnvelope.SustainPoint = hr.U8(offset + 230);
        instr.PanningEnvelope.LoopStartPoint = hr.U8(offset + 231);
        instr.PanningEnvelope.LoopEndPoint = hr.U8(offset + 232);
        FixEnvelope(instr.PanningEnvelope, hr.U8(offset + 234));

        instr.VibratoType = hr.U8(offset + 235) switch
        {
            0 => XmConst.SineWaveform,
            1 => XmConst.SquareWaveform,
            2 => XmConst.RampDownWaveform,
            _ => XmConst.RampUpWaveform,
        };

        instr.VibratoSweep = hr.U8(offset + 236);
        instr.VibratoDepth = hr.U8(offset + 237);
        instr.VibratoRate = hr.U8(offset + 238);
        instr.VolumeFadeout = hr.U16(offset + 239);

        offset = XmLoader.Idx((long)offset + headerSize);

        var samplesIndex = module.NumSamples;
        module.NumSamples += numSamples;

        for (var i = 0; i < numSamples; i++)
        {
            var target = samplesIndex + i;
            if (target >= module.Samples.Length)
            {
                throw new XmException(LiarUtil.Core.Strings.SampleIndexOutOfRange);
            }

            offset = LoadSampleHeader(r, module.Samples[target], offset);
        }

        for (var i = 0; i < numSamples; i++)
        {
            var target = samplesIndex + i;
            if (target >= module.Samples.Length)
            {
                throw new XmException(LiarUtil.Core.Strings.SampleIndexOutOfRange);
            }

            offset = LoadSampleData(r, module, module.Samples[target], offset);
        }

        return offset;
    }

    private static void LoadEnvelopePoints(XmReader r, XmEnvelope env, int baseOffset)
    {
        for (var i = 0; i < XmConst.MaxEnvelopePoints; i++)
        {
            env.Points[i].Frame = r.U16(baseOffset + (4 * i));
            int value = r.U16(baseOffset + (4 * i) + 2);
            if (value > XmConst.MaxEnvelopeValue)
            {
                value = XmConst.MaxEnvelopeValue;
            }

            env.Points[i].Value = value;
        }
    }

    private static void FixEnvelope(XmEnvelope env, int flags)
    {
        if (env.NumPoints > XmConst.MaxEnvelopePoints)
        {
            env.NumPoints = XmConst.MaxEnvelopePoints;
        }

        var kill = (flags & XmConst.EnvelopeFlagEnabled) == 0 || env.NumPoints < 2;
        if (!kill)
        {
            for (var i = 1; i < env.NumPoints; i++)
            {
                if (env.Points[i - 1].Frame < env.Points[i].Frame)
                {
                    continue;
                }

                kill = true;
                break;
            }
        }

        if (kill)
        {
            env.ResetAll();
            return;
        }

        if (env.LoopStartPoint >= env.NumPoints)
        {
            env.LoopStartPoint = 0;
            env.LoopEndPoint = 0;
        }

        if (env.LoopEndPoint >= env.NumPoints || env.LoopEndPoint < env.LoopStartPoint)
        {
            env.LoopStartPoint = 0;
            env.LoopEndPoint = 0;
        }

        if (env.LoopStartPoint == env.LoopEndPoint || (flags & XmConst.EnvelopeFlagLoop) == 0)
        {
            env.LoopStartPoint = 0;
            env.LoopEndPoint = 0;
        }

        if (env.SustainPoint >= env.NumPoints)
        {
            env.SustainPoint = 128;
        }

        if ((flags & XmConst.EnvelopeFlagSustain) == 0)
        {
            env.SustainPoint = 128;
        }
    }

    private static int LoadSampleHeader(XmReader r, XmSample sample, int offset)
    {
        long length = r.U32(offset);
        sample.Index = XmLoader.Idx(length);

        long loopStart = r.U32(offset + 4);
        sample.LoopLength = XmLoader.Idx(r.U32(offset + 8));
        int flags = r.U8(offset + 14);

        if (loopStart > length)
        {
            loopStart = length;
        }

        if (loopStart + sample.LoopLength > length)
        {
            sample.LoopLength = 0;
        }

        length = XmLoader.TrimSampleLength(length, loopStart, sample.LoopLength, flags);

        int volume = r.U8(offset + 12);
        if (volume > XmConst.MaxVolume)
        {
            volume = XmConst.MaxVolume;
        }

        sample.Volume = volume;

        var finetune = (sbyte)r.U8(offset + 13);
        sample.Finetune = (sbyte)(((finetune - sbyte.MinValue) / 8) - 16);
        sample.PingPong = (flags & XmConst.SampleFlagPingPong) != 0;

        if ((flags & (XmConst.SampleFlagForward | XmConst.SampleFlagPingPong)) == 0)
        {
            sample.LoopLength = 0;
        }

        sample.Panning = r.U8(offset + 15);
        sample.RelativeNote = (sbyte)r.U8(offset + 16);
        sample.Name = r.Text(offset + 18, 22, XmConst.SampleNameLength - 1);
        sample.Is16Bit = (flags & XmConst.SampleFlag16Bit) != 0;

        if (sample.Is16Bit)
        {
            sample.LoopLength >>= 1;
            length >>= 1;
            sample.Index >>= 1;
        }

        sample.Length = XmLoader.Idx(length);
        return offset + XmConst.SampleHeaderSize;
    }

    private static int LoadSampleData(XmReader r, XmModule module, XmSample sample, int offset)
    {
        var length = sample.Length;
        if (length < 0 || module.SamplesDataLength + length > module.SamplesData.Length)
        {
            throw new XmException(LiarUtil.Core.Strings.SampleDataExceedsModuleBounds);
        }

        var destination = module.SamplesData;
        var outOffset = module.SamplesDataLength;

        var sourceBytes = sample.Index;
        if (sample.Is16Bit)
        {
            short value = 0;
            for (var k = 0; k < length; k++)
            {
                value = unchecked((short)(value + (short)r.U16(offset + (k << 1))));
                destination[outOffset + k] = value;
            }

            offset = XmLoader.Idx((long)offset + (sourceBytes * 2L));
        }
        else
        {
            sbyte value = 0;
            for (var k = 0; k < length; k++)
            {
                value = unchecked((sbyte)(value + (sbyte)r.U8(offset + k)));
                destination[outOffset + k] = (short)(value * 256);
            }

            offset = XmLoader.Idx((long)offset + sourceBytes);
        }

        sample.Index = module.SamplesDataLength;
        module.SamplesDataLength += length;
        return offset;
    }
}
