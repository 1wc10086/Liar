namespace LiarUtil.Core.Xm;

internal static class XmLoader
{
    internal static XmModule Load(ReadOnlySpan<byte> data)
    {
        var prescan = Prescan(data);
        var module = new XmModule
        {
            NumChannels = prescan.NumChannels,
            NumInstruments = prescan.NumInstruments,
            NumSamples = 0,
            NumPatterns = prescan.NumPatterns,
            SamplesDataLength = 0,
            Patterns = new XmPattern[prescan.NumPatterns],
            PatternSlots = new XmPatternSlot[prescan.NumRows * prescan.NumChannels],
            Instruments = new XmInstrument[prescan.NumInstruments],
            Samples = new XmSample[prescan.NumSamples],
            SamplesData = new short[prescan.SamplesDataLength],
        };

        for (var i = 0; i < module.Instruments.Length; i++)
        {
            module.Instruments[i] = new XmInstrument();
        }

        for (var i = 0; i < module.Samples.Length; i++)
        {
            module.Samples[i] = new XmSample();
        }

        XmLoaderXm0104.Load(data, module, prescan);
        FixupCommon(module);

        if (module.NumRows != prescan.NumRows
            || module.NumSamples != prescan.NumSamples
            || module.SamplesDataLength != prescan.SamplesDataLength)
        {
            throw new XmException(LiarUtil.Core.Strings.XMModuleStructureInconsistent);
        }

        return module;
    }

    internal static XmPrescan Prescan(ReadOnlySpan<byte> data)
    {
        if (!IsXm0104(data))
        {
            throw new XmException(LiarUtil.Core.Strings.NotSupportedXMModule);
        }

        return XmLoaderXm0104.Prescan(data);
    }

    private static bool IsXm0104(ReadOnlySpan<byte> data) =>
        data.Length >= 60
        && data[..17].SequenceEqual("Extended Module: "u8)
        && data[37] == 0x1A
        && data[58] == 0x04
        && data[59] == 0x01;

    private static void FixupCommon(XmModule module)
    {
        var slots = module.PatternSlots;
        for (var i = 0; i < slots.Length; i++)
        {
            var slot = slots[i];

            if (slot.EffectType == XmConst.EffectJumpToOrder && slot.EffectParam >= module.Length)
            {
                slot.EffectParam = 0;
            }

            if ((slot.EffectType == XmConst.EffectSetVolume
                 || slot.EffectType == XmConst.EffectSetGlobalVolume)
                && slot.EffectParam > XmConst.MaxVolume)
            {
                slot.EffectParam = XmConst.MaxVolume;
            }

            if (slot.EffectType == XmConst.EffectCutNote && slot.EffectParam == 0)
            {
                slot.EffectType = XmConst.EffectSetVolume;
            }

            if (slot.EffectType == XmConst.EffectDelayNote && slot.EffectParam == 0)
            {
                slot.EffectType = 0;
            }

            if (slot.EffectType == XmConst.EffectRetriggerNote && slot.EffectParam == 0)
            {
                if (slot.Note == 0)
                {
                    slot.Note = XmConst.NoteRetrigger;
                }

                slot.EffectType = 0;
            }

            if ((slot.EffectType == XmConst.EffectSetTempo || slot.EffectType == XmConst.EffectSetBpm)
                && slot.EffectParam == 0)
            {
                slot.EffectType = XmConst.EffectJumpToOrder;
                slot.EffectParam = (byte)module.RestartPosition;
            }

            if ((slot.VolumeColumn >> 4) == XmConst.VolumeEffectVibratoSpeed
                && (slot.VolumeColumn & 0xF) == 0)
            {
                slot.VolumeColumn = 0;
            }

            slots[i] = slot;
        }
    }

    internal static int Idx(long offset) => offset >= int.MaxValue
        ? int.MaxValue
        : offset <= 0 ? 0 : (int)offset;

    internal static long TrimSampleLength(long length, long loopStart, long loopLength, int flags)
    {
        if ((flags & (XmConst.SampleFlagPingPong | XmConst.SampleFlagForward)) == 0)
        {
            return length;
        }

        if (loopStart > length)
        {
            return length;
        }

        return loopStart + (loopStart + loopLength > length ? 0 : loopLength);
    }
}
