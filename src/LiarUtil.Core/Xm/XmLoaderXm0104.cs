namespace LiarUtil.Core.Xm;

internal static partial class XmLoaderXm0104
{
    private const long MaxTotalSampleFrames = 32L << 20;

    private const long MaxPatternSlots = 8L << 20;

    internal static XmPrescan Prescan(ReadOnlySpan<byte> data)
    {
        var r = new XmReader(data);
        var p = new XmPrescan
        {
            Format = XmFormat.Xm0104,
            PotLength = r.U16(64),
            NumChannels = r.U16(68),
            NumPatterns = r.U16(70),
            NumInstruments = r.U16(72),
        };

        if (p.NumChannels > XmConst.MaxChannels)
        {
            throw new XmException(LiarUtil.Core.Strings.ModuleChannelCountExceedsLimit);
        }

        if (p.NumPatterns > XmConst.MaxPatterns)
        {
            throw new XmException(LiarUtil.Core.Strings.ModulePatternCountExceedsLimit);
        }

        if (p.NumInstruments > XmConst.MaxInstruments)
        {
            throw new XmException(LiarUtil.Core.Strings.ModuleInstrumentCountExceedsLimit);
        }

        Span<byte> pot = stackalloc byte[XmConst.PatternOrderTableLength];
        for (var i = 0; i < pot.Length; i++)
        {
            pot[i] = r.U8(80 + i);
        }

        long offset = 60 + r.U32(60);
        for (var i = 0; i < p.NumPatterns; i++)
        {
            var off = XmLoader.Idx(offset);
            int numRows = r.U16(off + 5);
            var packedSize = r.U16(off + 7);
            if (packedSize == 0 && numRows != XmConst.EmptyPatternNumRows)
            {
                numRows = XmConst.EmptyPatternNumRows;
            }

            if (numRows > XmConst.MaxRowsPerPattern)
            {
                throw new XmException(LiarUtil.Core.Strings.PatternRowCountExceedsLimit);
            }

            p.NumRows += numRows;
            if ((long)p.NumRows * p.NumChannels > MaxPatternSlots)
            {
                throw new XmException(LiarUtil.Core.Strings.TotalPatternDataExceedsLimit);
            }
            offset += r.U32(off) + packedSize;
        }

        if (p.PotLength > XmConst.PatternOrderTableLength)
        {
            p.PotLength = XmConst.PatternOrderTableLength;
        }

        for (var i = 0; i < p.PotLength; i++)
        {
            if (pot[i] < p.NumPatterns)
            {
                continue;
            }

            if (p.NumPatterns >= XmConst.MaxPatterns)
            {
                throw new XmException(LiarUtil.Core.Strings.CannotInsertEmptyPatternForInvalidPattern);
            }

            p.NumRows += XmConst.EmptyPatternNumRows;
            p.NumPatterns += 1;
            break;
        }

        for (var i = 0; i < p.NumInstruments; i++)
        {
            var off = XmLoader.Idx(offset);
            var instHeaderSize = r.U32(off);
            int numSamples = 27 < instHeaderSize ? r.U8(off + 27) : 0;
            long instSamplesBytes = 0;
            p.NumSamples += numSamples;
            offset += instHeaderSize;

            for (var j = 0; j < numSamples; j++)
            {
                var soff = XmLoader.Idx(offset);
                long sampleLength = r.U32(soff);
                var sampleBytes = sampleLength;
                long loopStart = r.U32(soff + 4);
                long loopLength = r.U32(soff + 8);
                int flags = r.U8(soff + 14);
                sampleLength = XmLoader.TrimSampleLength(sampleLength, loopStart, loopLength, flags);
                if ((flags & XmConst.SampleFlag16Bit) != 0)
                {
                    sampleLength /= 2;
                }

                var max = XmConst.MaxSampleLength;
                if ((flags & XmConst.SampleFlagPingPong) != 0)
                {
                    max /= 2;
                }

                if (sampleLength > max)
                {
                    throw new XmException(LiarUtil.Core.Strings.SampleLengthExceedsLimit);
                }

                var total = (long)p.SamplesDataLength + sampleLength;
                if (total > MaxTotalSampleFrames)
                {
                    throw new XmException(LiarUtil.Core.Strings.TotalSampleDataExceedsLimit);
                }

                p.SamplesDataLength = (int)total;
                instSamplesBytes += sampleBytes;
                offset += XmConst.SampleHeaderSize;
            }

            offset += instSamplesBytes;
        }

        return p;
    }

    internal static void Load(ReadOnlySpan<byte> data, XmModule module, XmPrescan prescan)
    {
        var r = new XmReader(data);
        var offset = LoadModuleHeader(r, module);

        if (module.NumChannels != prescan.NumChannels || module.NumInstruments != prescan.NumInstruments)
        {
            throw new XmException(LiarUtil.Core.Strings.ModuleHeaderInconsistentWithActualStructure);
        }

        Array.Fill(module.DefaultChannelPanning, (byte)(XmConst.MaxPanning / 2), 0, module.NumChannels);

        for (var i = 0; i < module.NumPatterns; i++)
        {
            offset = LoadPattern(r, module, i, offset);
        }

        var hasInvalidPattern = false;
        for (var i = 0; i < module.Length; i++)
        {
            if (module.PatternTable[i] >= module.NumPatterns)
            {
                hasInvalidPattern = true;
                break;
            }
        }

        if (hasInvalidPattern)
        {
            for (var i = 0; i < module.Length; i++)
            {
                if (module.PatternTable[i] < module.NumPatterns)
                {
                    continue;
                }

                module.PatternTable[i] = (byte)module.NumPatterns;
            }

            module.Patterns[module.NumPatterns] = new XmPattern(module.NumRows, XmConst.EmptyPatternNumRows);
            module.NumPatterns += 1;
            module.NumRows += XmConst.EmptyPatternNumRows;
        }

        for (var i = 0; i < module.NumInstruments; i++)
        {
            offset = LoadInstrument(r, module, i, offset);
        }
    }

    private static int LoadModuleHeader(XmReader r, XmModule module)
    {
        module.Name = r.Text(17, 20, XmConst.ModuleNameLength - 1);
        module.TrackerName = r.Text(38, 20, XmConst.TrackerNameLength - 1);

        const int offset = 60;
        var headerSize = r.U32(offset);

        module.Length = r.U16(offset + 4);
        if (module.Length > XmConst.PatternOrderTableLength)
        {
            module.Length = XmConst.PatternOrderTableLength;
        }

        int restart = r.U16(offset + 6);
        if (restart >= module.Length)
        {
            restart = 0;
        }

        module.RestartPosition = restart;
        module.NumChannels = r.U8(offset + 8);
        module.NumPatterns = r.U16(offset + 10);
        module.NumInstruments = r.U8(offset + 12);

        var flags = r.U16(offset + 14);
        module.AmigaFrequencies = (flags & 1) == 0;

        int tempo = r.U16(offset + 16);
        if (tempo >= XmConst.MinBpm)
        {
            tempo = XmConst.MinBpm - 1;
        }

        module.DefaultTempo = tempo;

        int bpm = r.U16(offset + 18);
        if (bpm > XmConst.MaxBpm)
        {
            bpm = XmConst.MaxBpm;
        }

        module.DefaultBpm = bpm;
        module.DefaultGlobalVolume = XmConst.MaxVolume;

        for (var i = 0; i < XmConst.PatternOrderTableLength; i++)
        {
            module.PatternTable[i] = r.U8(offset + 20 + i);
        }

        return XmLoader.Idx((long)offset + headerSize);
    }

    private static int LoadPattern(XmReader r, XmModule module, int index, int offset)
    {
        int packedSize = r.U16(offset + 7);
        int numRows = r.U16(offset + 5);
        var rowsIndex = module.NumRows;
        module.Patterns[index] = new XmPattern(rowsIndex, numRows);
        module.NumRows += numRows;

        offset += (int)r.U32(offset);

        if (packedSize == 0)
        {
            module.NumRows -= numRows;
            module.Patterns[index] = new XmPattern(rowsIndex, XmConst.EmptyPatternNumRows);
            module.NumRows += XmConst.EmptyPatternNumRows;
            return offset;
        }

        var slots = module.PatternSlots;
        var slotBase = rowsIndex * module.NumChannels;
        var slotCount = numRows * module.NumChannels;
        var limited = r.WithBound(XmLoader.Idx((long)offset + packedSize));

        var j = 0;
        var k = 0;
        while (j < packedSize && k < slotCount)
        {
            var slotIndex = slotBase + k;
            if (slotIndex >= slots.Length)
            {
                break;
            }

            var note = limited.U8(offset + j);
            var slot = slots[slotIndex];

            if ((note & 0x80) != 0)
            {
                j++;
                if ((note & 0x01) != 0)
                {
                    slot.Note = limited.U8(offset + j);
                    j++;
                }

                if ((note & 0x02) != 0)
                {
                    slot.Instrument = limited.U8(offset + j);
                    j++;
                }

                if ((note & 0x04) != 0)
                {
                    slot.VolumeColumn = limited.U8(offset + j);
                    j++;
                }

                if ((note & 0x08) != 0)
                {
                    slot.EffectType = limited.U8(offset + j);
                    j++;
                }

                if ((note & 0x10) != 0)
                {
                    slot.EffectParam = limited.U8(offset + j);
                    j++;
                }
            }
            else
            {
                slot.Note = note;
                slot.Instrument = limited.U8(offset + j + 1);
                slot.VolumeColumn = limited.U8(offset + j + 2);
                slot.EffectType = limited.U8(offset + j + 3);
                slot.EffectParam = limited.U8(offset + j + 4);
                j += 5;
            }

            if (slot.Note == 97)
            {
                slot.Note = XmConst.NoteKeyOff;
            }

            if (slot.EffectType > 0x0F)
            {
                switch (slot.EffectType)
                {
                    case 16:
                        slot.EffectType = XmConst.EffectSetGlobalVolume;
                        break;

                    case 17:
                        slot.EffectType = XmConst.EffectGlobalVolumeSlide;
                        break;

                    case 20:
                        if (slot.EffectParam == 0)
                        {
                            slot.Note = XmConst.NoteKeyOff;
                            slot.EffectType = 0;
                        }
                        else
                        {
                            slot.EffectType = XmConst.EffectKeyOff;
                        }

                        break;

                    case 21:
                        slot.EffectType = XmConst.EffectSetEnvelopePosition;
                        break;

                    case 25:
                        slot.EffectType = XmConst.EffectPanningSlide;
                        break;

                    case 27:
                        slot.EffectType = XmConst.EffectMultiRetrigNote;
                        break;

                    case 29:
                        slot.EffectType = XmConst.EffectTremor;
                        break;

                    case 33:
                        switch (slot.EffectParam >> 4)
                        {
                            case 1:
                                slot.EffectType = XmConst.EffectExtraFinePortamentoUp;
                                slot.EffectParam &= 0x0F;
                                break;

                            case 2:
                                slot.EffectType = XmConst.EffectExtraFinePortamentoDown;
                                slot.EffectParam &= 0x0F;
                                break;

                            default:
                                slot.EffectType = 0;
                                slot.EffectParam = 0;
                                break;
                        }

                        break;

                    default:
                        slot.EffectType = 0;
                        slot.EffectParam = 0;
                        break;
                }
            }
            else
            {
                RemapEffect(ref slot);
                if ((slot.EffectType == XmConst.EffectSetVibratoControl
                     || slot.EffectType == XmConst.EffectSetTremoloControl)
                    && (slot.EffectParam & 127) == XmConst.RandomWaveform)
                {
                    slot.EffectParam = (byte)((slot.EffectParam & 128) | XmConst.SquareWaveform);
                }
            }

            slots[slotIndex] = slot;
            k++;
        }

        return XmLoader.Idx((long)offset + packedSize);
    }

    private static void RemapEffect(ref XmPatternSlot slot)
    {
        if (slot.EffectType == 0x0E)
        {
            switch (slot.EffectParam >> 4)
            {
                case 0:
                case 0x0F:
                    slot.EffectType = 0;
                    slot.EffectParam = 0;
                    return;

                case 1:
                    slot.EffectType = XmConst.EffectFinePortamentoUp;
                    slot.EffectParam &= 0x0F;
                    return;

                case 2:
                    slot.EffectType = XmConst.EffectFinePortamentoDown;
                    slot.EffectParam &= 0x0F;
                    return;

                case 3:
                    slot.EffectType = XmConst.EffectSetGlissandoControl;
                    slot.EffectParam = (byte)(slot.EffectParam > 0 ? 1 : 0);
                    return;

                case 4:
                    slot.EffectType = XmConst.EffectSetVibratoControl;
                    FixupControlParam(ref slot);
                    return;

                case 5:
                    slot.EffectType = XmConst.EffectSetFinetune;
                    slot.EffectParam &= 0x0F;
                    return;

                case 6:
                    slot.EffectType = XmConst.EffectPatternLoop;
                    slot.EffectParam &= 0x0F;
                    return;

                case 7:
                    slot.EffectType = XmConst.EffectSetTremoloControl;
                    FixupControlParam(ref slot);
                    return;

                case 8:
                    slot.EffectType = XmConst.EffectSetChannelPanning;
                    slot.EffectParam = (byte)((slot.EffectParam & 0x0F) * 0x11);
                    return;

                case 9:
                    slot.EffectType = XmConst.EffectRetriggerNote;
                    slot.EffectParam &= 0x0F;
                    return;

                case 0x0A:
                    slot.EffectType = XmConst.EffectFineVolumeSlideUp;
                    slot.EffectParam &= 0x0F;
                    return;

                case 0x0B:
                    slot.EffectType = XmConst.EffectFineVolumeSlideDown;
                    slot.EffectParam &= 0x0F;
                    return;

                case 0x0C:
                    slot.EffectType = XmConst.EffectCutNote;
                    slot.EffectParam &= 0x0F;
                    return;

                case 0x0D:
                    slot.EffectType = XmConst.EffectDelayNote;
                    slot.EffectParam &= 0x0F;
                    return;

                default:
                    slot.EffectType = XmConst.EffectDelayPattern;
                    slot.EffectParam &= 0x0F;
                    return;
            }
        }

        switch (slot.EffectType)
        {
            case 0x00:
                slot.EffectType = XmConst.EffectArpeggio;
                return;

            case 0x01:
                slot.EffectType = XmConst.EffectPortamentoUp;
                return;

            case 0x02:
                slot.EffectType = XmConst.EffectPortamentoDown;
                return;

            case 0x03:
                slot.EffectType = XmConst.EffectTonePortamento;
                return;

            case 0x04:
                slot.EffectType = XmConst.EffectVibrato;
                return;

            case 0x05:
                slot.EffectType = XmConst.EffectTonePortamentoVolumeSlide;
                return;

            case 0x06:
                slot.EffectType = XmConst.EffectVibratoVolumeSlide;
                return;

            case 0x07:
                slot.EffectType = XmConst.EffectTremolo;
                return;

            case 0x08:
                slot.EffectType = XmConst.EffectSetPanning;
                return;

            case 0x09:
                slot.EffectType = XmConst.EffectSetSampleOffset;
                return;

            case 0x0A:
                slot.EffectType = XmConst.EffectVolumeSlide;
                return;

            case 0x0B:
                slot.EffectType = XmConst.EffectJumpToOrder;
                return;

            case 0x0C:
                slot.EffectType = XmConst.EffectSetVolume;
                return;

            case 0x0D:
                slot.EffectType = XmConst.EffectPatternBreak;
                slot.EffectParam = (byte)(slot.EffectParam - (6 * (slot.EffectParam >> 4)));
                return;

            default:
                slot.EffectType = slot.EffectParam < XmConst.MinBpm
                    ? (byte)XmConst.EffectSetTempo
                    : (byte)XmConst.EffectSetBpm;
                return;
        }
    }

    private static void FixupControlParam(ref XmPatternSlot slot)
    {
        var waveform = (slot.EffectParam & 3) switch
        {
            0 => XmConst.SineWaveform,
            1 => XmConst.RampDownWaveform,
            2 => XmConst.SquareWaveform,
            _ => XmConst.RandomWaveform,
        };

        slot.EffectParam = (byte)(waveform | ((slot.EffectParam & 4) != 0 ? 128 : 0));
    }
}
