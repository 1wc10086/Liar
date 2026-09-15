namespace LiarUtil.Core.Xm;

internal sealed partial class XmContext
{
    internal XmModule ModuleRef => _module;

    internal int CurrentTempo => _currentTempo;

    internal byte CurrentTickByte => unchecked((byte)_currentTick);

    internal int CurrentTick => _currentTick;

    private void Tick()
    {
        if (_currentTick >= _currentTempo)
        {
            _currentTick = 0;
            _extraRowsDone++;
        }

        if (_currentTick == 0 && (_extraRows == 0 || _extraRowsDone > _extraRows))
        {
            _extraRows = 0;
            _extraRowsDone = 0;
            Row();
        }

        for (var i = 0; i < _channels.Length; i++)
        {
            var ch = _channels[i];
            XmEnvelopes.TickEnvelopes(ch);

            if (_currentTick != 0 || _extraRowsDone != 0)
            {
                TickEffects(ch);
            }

            if (ch.Period == 0)
            {
                continue;
            }

            var stepNumerator = ((ulong)XmFrequency.Frequency(_module, ch) * (ulong)XmConst.SampleMicrosteps)
                + (uint)(SampleRate / 2);
            ch.Step = (uint)(stepNumerator / (uint)SampleRate);

            var baseVolume = ch.Volume - ch.VolumeOffset;
            if (baseVolume < 0)
            {
                baseVolume = 0;
            }
            else if (baseVolume > XmConst.MaxVolume)
            {
                baseVolume = XmConst.MaxVolume;
            }

            baseVolume *= ch.VolumeEnvelopeVolume;
            baseVolume *= ch.FadeoutVolume;
            baseVolume /= 4;
            baseVolume *= _globalVolume;
            var volume = baseVolume / (float)int.MaxValue;

            var panning = unchecked((byte)ch.Panning);
            var baseDelta = (ch.BasePanning - (XmConst.MaxPanning / 2))
                * ((XmConst.MaxPanning / 2) - Math.Abs(panning - (XmConst.MaxPanning / 2)))
                / (XmConst.MaxPanning / 2);
            panning = unchecked((byte)(panning + baseDelta));

            var envDelta = (ch.PanningEnvelopePanning - (XmConst.MaxEnvelopeValue / 2))
                * ((XmConst.MaxPanning / 2) - Math.Abs(panning - (XmConst.MaxPanning / 2)))
                / (XmConst.MaxEnvelopeValue / 2);
            panning = unchecked((byte)(panning + envDelta));

            ch.TargetLeft = volume * MathF.Sqrt((XmConst.MaxPanning - panning) / (float)XmConst.MaxPanning);
            ch.TargetRight = volume * MathF.Sqrt(panning / (float)XmConst.MaxPanning);
        }

        _currentTick++;

        var bpm = _currentBpm <= 0 ? 1 : _currentBpm;
        var samplesInTick = (uint)SampleRate * (uint)(10 * (XmConst.TickSubsamples / 4));
        samplesInTick /= (uint)bpm;
        _remainingSamplesInTick = unchecked(_remainingSamplesInTick + samplesInTick);
    }

    private void Row()
    {
        if (_positionJump || _patternBreak)
        {
            if (_positionJump)
            {
                if (!_loopJump && _jumpDest <= _currentTableIndex)
                {
                    _finished = true;
                    return;
                }

                _currentTableIndex = _jumpDest;
            }
            else
            {
                _currentTableIndex++;
            }

            _patternBreak = false;
            _positionJump = false;
            _loopJump = false;
            _currentRow = _jumpRow;
            _jumpRow = 0;
        }

        if (_currentTableIndex < 0 || _currentTableIndex >= _module.Length)
        {
            _finished = true;
            return;
        }

        var pattern = _module.Patterns[_module.PatternTable[_currentTableIndex]];
        var baseSlot = _module.NumChannels * (pattern.RowsIndex + _currentRow);

        for (var i = 0; i < _channels.Length; i++)
        {
            var ch = _channels[i];
            var slotIndex = baseSlot + i;
            if (slotIndex < 0 || slotIndex >= _slots.Length)
            {
                continue;
            }

            ch.Current = _slots[slotIndex];

            if (ch.Current.EffectType != XmConst.EffectDelayNote)
            {
                HandlePatternSlot(ch);
            }

            if (ch.ShouldResetArpeggio)
            {
                XmModulation.PitchSlide(ch, 0, PitchSlideBehaviour.Wraparound);
                ch.ShouldResetArpeggio = false;
                ch.ArpNoteOffset = 0;
            }

            if (ch.ShouldResetVibrato && !SlotHasVibrato(ch.Current))
            {
                ch.VibratoOffset = 0;
            }
        }

        _currentRow++;
        if (!_positionJump && !_patternBreak
            && (_currentRow >= pattern.NumRows || _currentRow == 0))
        {
            _currentRow = _jumpRow;
            _jumpRow = 0;
            _currentTableIndex++;
            if (_currentTableIndex >= _module.Length)
            {
                _finished = true;
            }
        }

        _renderedRows++;
        if (_renderedRows > MaxRenderedRows)
        {
            _finished = true;
        }
    }

    internal void JumpToOrder(int dest)
    {
        _positionJump = true;
        _jumpDest = dest;
        _jumpRow = 0;
    }

    internal void BreakToRow(int row)
    {
        _patternBreak = true;
        _jumpRow = row;
    }

    internal int CurrentTableIndex => _currentTableIndex;

    internal int CurrentRowIndex => _currentRow;

    private static bool IsKeyOff(int note) => (note & 128) != 0;

    private static bool SlotHasVibrato(XmPatternSlot slot) =>
        slot.EffectType is XmConst.EffectVibrato
            or XmConst.EffectFineVibrato
            or XmConst.EffectVibratoVolumeSlide
            or XmConst.EffectS3mVibratoVolumeSlide
        || (slot.VolumeColumn >> 4) == XmConst.VolumeEffectVibrato;

    private static bool SlotHasTonePortamento(XmPatternSlot slot) =>
        slot.EffectType is XmConst.EffectTonePortamento
            or XmConst.EffectTonePortamentoVolumeSlide
            or XmConst.EffectS3mTonePortamentoVolumeSlide
        || (slot.VolumeColumn >> 4) == XmConst.VolumeEffectTonePortamento;

    internal void TriggerInstrument(XmChannel ch)
    {
        ch.Sustained = true;
        ch.VolumeEnvelopeFrameCount = 0;
        ch.PanningEnvelopeFrameCount = 0;
        ch.MultiRetrigTicks = 0;
        ch.TremorTicks = 0;
        ch.AutovibratoTicks = 0;
        XmModulation.ResetVolumeOffset(ch);

        if ((ch.VibratoControlParam & 128) == 0)
        {
            ch.VibratoTicks = 0;
        }

        if ((ch.TremoloControlParam & 128) == 0)
        {
            ch.TremoloTicks = 0;
        }
    }

    internal void TriggerNote(XmChannel ch)
    {
        if (ch.Sample is not null && ch.Period != 0)
        {
            for (var i = 0; i < XmConst.RampingPoints; i++)
            {
                ch.EndOfPreviousSample[i] = NextOfSample(ch);
            }
        }
        else
        {
            Array.Clear(ch.EndOfPreviousSample);
        }

        ch.FrameCount = 0;

        if (ch.NextInstrument == 0 || ch.NextInstrument > _module.NumInstruments)
        {
            ch.Instrument = null;
            ch.Sample = null;
            CutNote(ch);
            return;
        }

        ch.Instrument = _module.Instruments[ch.NextInstrument - 1];

        var noteIndex = ch.OrigNote - 1;
        if ((uint)noteIndex >= (uint)XmConst.MaxNote)
        {
            ch.Instrument = null;
            ch.Sample = null;
            CutNote(ch);
            return;
        }

        var sampleIndex = ch.Instrument.SampleOfNotes[noteIndex];
        if (sampleIndex >= _module.NumSamples)
        {
            ch.Instrument = null;
            ch.Sample = null;
            CutNote(ch);
            return;
        }

        var newSample = _module.Samples[sampleIndex];
        var note = ch.OrigNote + newSample.RelativeNote;
        if (note <= 0 || note >= 120)
        {
            return;
        }

        ch.Sample = newSample;

        if (ch.Current.Note == XmConst.NoteSwitch)
        {
            return;
        }

        ch.Finetune = ch.Current.EffectType == XmConst.EffectSetFinetune
            ? (sbyte)((ch.Current.EffectParam * 2) - 16)
            : ch.Sample.Finetune;

        ch.Period = XmFrequency.Period(_module, (16 * (note - 1)) + ch.Finetune);

        if (ch.Current.EffectType == XmConst.EffectSetSampleOffset)
        {
            if (ch.Current.EffectParam > 0)
            {
                ch.SampleOffsetParam = ch.Current.EffectParam;
            }

            ch.SamplePosition = unchecked((uint)ch.SampleOffsetParam * 256u * (uint)XmConst.SampleMicrosteps);
            ch.SampleOffsetInvalid = ch.SamplePosition
                >= unchecked((uint)ch.Sample.Length * (uint)XmConst.SampleMicrosteps);
        }
        else
        {
            ch.SamplePosition = 0;
            ch.SampleOffsetInvalid = false;
        }

        ch.GlissandoControlError = 0;
        ch.VibratoOffset = 0;
    }

    private void CutNote(XmChannel ch) => ch.Volume = 0;

    internal void KeyOff(XmChannel ch)
    {
        ch.Sustained = false;
        if (ch.Instrument is null || ch.Instrument.VolumeEnvelope.NumPoints == 0)
        {
            CutNote(ch);
        }
    }

    private void HandlePatternSlot(XmChannel ch)
    {
        var slot = ch.Current;

        if (slot.Instrument != 0)
        {
            ch.NextInstrument = slot.Instrument;
        }

        if (!IsKeyOff(slot.Note))
        {
            if (slot.Note != 0)
            {
                if (slot.Note <= XmConst.MaxNote)
                {
                    if (SlotHasTonePortamento(slot))
                    {
                        XmModulation.TonePortamentoTarget(this, ch);
                    }
                    else
                    {
                        ch.OrigNote = slot.Note;
                        TriggerNote(ch);
                    }
                }
                else
                {
                    TriggerNote(ch);
                }
            }
        }
        else
        {
            KeyOff(ch);
        }

        if (slot.Instrument != 0)
        {
            if (ch.Sample is not null)
            {
                ch.Volume = ch.Sample.Volume;
                ch.Panning = ch.Sample.Panning;
            }

            if (!IsKeyOff(slot.Note))
            {
                TriggerInstrument(ch);
            }
        }

        if (slot.VolumeColumn is >= 0x10 and <= 0x50)
        {
            XmModulation.ResetVolumeOffset(ch);
            ch.Volume = slot.VolumeColumn - 0x10;
        }

        if ((slot.VolumeColumn >> 4) == XmConst.VolumeEffectSetPanning)
        {
            ch.Panning = unchecked((byte)(slot.VolumeColumn << 4));
        }

        if ((slot.VolumeColumn >> 4) == XmConst.VolumeEffectTonePortamento)
        {
            if ((slot.VolumeColumn & 0x0F) != 0)
            {
                ch.TonePortamentoParam = unchecked((byte)(slot.VolumeColumn << 4));
            }
        }
        else if (slot.EffectType == XmConst.EffectTonePortamento && slot.EffectParam > 0)
        {
            ch.TonePortamentoParam = slot.EffectParam;
        }

        if (slot.EffectParam != 0)
        {
            ch.EffectParam = slot.EffectParam;
        }

        if (_currentTick == 0)
        {
            switch (slot.VolumeColumn >> 4)
            {
                case XmConst.VolumeEffectFineSlideDown:
                    XmModulation.ResetVolumeOffset(ch);
                    XmModulation.ParamSlide(ref ch.Volume, slot.VolumeColumn & 0x0F, XmConst.MaxVolume);
                    break;

                case XmConst.VolumeEffectFineSlideUp:
                    XmModulation.ResetVolumeOffset(ch);
                    XmModulation.ParamSlide(ref ch.Volume, unchecked((byte)(slot.VolumeColumn << 4)), XmConst.MaxVolume);
                    break;

                case XmConst.VolumeEffectVibratoSpeed:
                    XmModulation.UpdateEffectMemoryXy(ref ch.VibratoParam, unchecked((byte)(slot.VolumeColumn << 4)));
                    break;
            }
        }

        ApplyImmediateEffect(ch, slot);
    }
}
