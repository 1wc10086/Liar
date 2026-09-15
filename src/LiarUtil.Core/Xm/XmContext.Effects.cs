namespace LiarUtil.Core.Xm;

internal sealed partial class XmContext
{
    private void ApplyImmediateEffect(XmChannel ch, XmPatternSlot slot)
    {
        switch (slot.EffectType)
        {
            case XmConst.EffectSetVolume:
                XmModulation.ResetVolumeOffset(ch);
                ch.Volume = slot.EffectParam;
                break;

            case XmConst.EffectFineVolumeSlideUp:
                if (slot.EffectParam != 0)
                {
                    ch.FineVolumeSlideUpParam = unchecked((byte)(slot.EffectParam << 4));
                }

                XmModulation.ResetVolumeOffset(ch);
                XmModulation.ParamSlide(ref ch.Volume, ch.FineVolumeSlideUpParam, XmConst.MaxVolume);
                break;

            case XmConst.EffectFineVolumeSlideDown:
                if (slot.EffectParam != 0)
                {
                    ch.FineVolumeSlideDownParam = slot.EffectParam;
                }

                XmModulation.ResetVolumeOffset(ch);
                XmModulation.ParamSlide(ref ch.Volume, ch.FineVolumeSlideDownParam, XmConst.MaxVolume);
                break;

            case XmConst.EffectS3mVolumeSlide:
                if ((ch.EffectParam >> 4) == 0xF
                    || (ch.EffectParam & 0xF) == 0xF
                    || _module.FastS3mVolumeSlides)
                {
                    XmModulation.VolumeSlideS3m(ch);
                }

                break;

            case XmConst.EffectSetPanning:
                ch.Panning = slot.EffectParam;
                break;

            case XmConst.EffectSetChannelPanning:
                ch.BasePanning = slot.EffectParam;
                break;

            case XmConst.EffectJumpToOrder:
                JumpToOrder(slot.EffectParam);
                break;

            case XmConst.EffectPatternBreak:
                BreakToRow(slot.EffectParam);
                break;

            case XmConst.EffectSetTempo:
                _currentTempo = slot.EffectParam;
                break;

            case XmConst.EffectSetBpm:
                _currentBpm = slot.EffectParam;
                break;

            case XmConst.EffectSetGlobalVolume:
                _globalVolume = slot.EffectParam;
                break;

            case XmConst.EffectSetEnvelopePosition:
                ch.VolumeEnvelopeFrameCount = slot.EffectParam;
                ch.PanningEnvelopeFrameCount = slot.EffectParam;
                break;

            case XmConst.EffectMultiRetrigNote:
                XmModulation.UpdateEffectMemoryXy(ref ch.MultiRetrigParam, slot.EffectParam);
                XmModulation.MultiRetrigNote(this, ch, ch.MultiRetrigParam);
                break;

            case XmConst.EffectS3mMultiRetrigNote:
                XmModulation.MultiRetrigNote(this, ch, ch.EffectParam);
                break;

            case XmConst.EffectS3mTremor:
                XmModulation.Tremor(ch, ch.EffectParam);
                break;

            case XmConst.EffectExtraFinePortamentoUp:
                if ((slot.EffectParam & 0x0F) != 0)
                {
                    ch.ExtraFinePortamentoUpParam = slot.EffectParam;
                }

                XmModulation.PitchSlide(ch, -ch.ExtraFinePortamentoUpParam, PitchSlideBehaviour.Clamp);
                break;

            case XmConst.EffectExtraFinePortamentoDown:
                if (slot.EffectParam != 0)
                {
                    ch.ExtraFinePortamentoDownParam = slot.EffectParam;
                }

                XmModulation.PitchSlide(ch, ch.ExtraFinePortamentoDownParam, PitchSlideBehaviour.Wraparound);
                break;

            case XmConst.EffectFinePortamentoUp:
                if (slot.EffectParam != 0)
                {
                    ch.FinePortamentoUpParam = 4 * slot.EffectParam;
                }

                XmModulation.PitchSlide(ch, -ch.FinePortamentoUpParam, PitchSlideBehaviour.Clamp);
                break;

            case XmConst.EffectFinePortamentoDown:
                if (slot.EffectParam != 0)
                {
                    ch.FinePortamentoDownParam = 4 * slot.EffectParam;
                }

                XmModulation.PitchSlide(ch, ch.FinePortamentoDownParam, PitchSlideBehaviour.Wraparound);
                break;

            case XmConst.EffectS3mPortamentoUp:
                switch (ch.EffectParam >> 4)
                {
                    case 0xE:
                        XmModulation.PitchSlide(ch, -(ch.EffectParam & 0xF), PitchSlideBehaviour.Cut);
                        break;

                    case 0xF:
                        XmModulation.PitchSlide(ch, -(ch.EffectParam & 0xF) * 4, PitchSlideBehaviour.Cut);
                        break;
                }

                break;

            case XmConst.EffectS3mPortamentoDown:
                switch (ch.EffectParam >> 4)
                {
                    case 0xE:
                        XmModulation.PitchSlide(ch, ch.EffectParam & 0xF, PitchSlideBehaviour.Cut);
                        break;

                    case 0xF:
                        XmModulation.PitchSlide(ch, (ch.EffectParam & 0xF) * 4, PitchSlideBehaviour.Cut);
                        break;
                }

                break;

            case XmConst.EffectSetGlissandoControl:
                ch.GlissandoControlParam = slot.EffectParam;
                break;

            case XmConst.EffectSetVibratoControl:
                ch.VibratoControlParam = slot.EffectParam;
                break;

            case XmConst.EffectSetTremoloControl:
                ch.TremoloControlParam = slot.EffectParam;
                break;

            case XmConst.EffectRowLoop:
                ch.PatternLoopOrigin = _currentRow;
                DoPatternLoop(ch, slot.EffectParam);
                break;

            case XmConst.EffectPatternLoop:
                if (slot.EffectParam != 0)
                {
                    DoPatternLoop(ch, slot.EffectParam);
                }
                else
                {
                    ch.PatternLoopOrigin = _currentRow;
                    _jumpRow = ch.PatternLoopOrigin;
                }

                break;

            case XmConst.EffectDelayPattern:
                _extraRows = slot.EffectParam;
                break;
        }
    }

    private void DoPatternLoop(XmChannel ch, int param)
    {
        if (param == ch.PatternLoopCount)
        {
            ch.PatternLoopCount = 0;
            return;
        }

        ch.PatternLoopCount++;
        _positionJump = true;
        _jumpRow = ch.PatternLoopOrigin;
        _jumpDest = _currentTableIndex;
    }

    private void TickEffects(XmChannel ch)
    {
        var slot = ch.Current;

        switch (slot.VolumeColumn >> 4)
        {
            case XmConst.VolumeEffectSlideDown:
                XmModulation.ResetVolumeOffset(ch);
                XmModulation.ParamSlide(ref ch.Volume, slot.VolumeColumn & 0x0F, XmConst.MaxVolume);
                break;

            case XmConst.VolumeEffectSlideUp:
                XmModulation.ResetVolumeOffset(ch);
                XmModulation.ParamSlide(ref ch.Volume, unchecked((byte)(slot.VolumeColumn << 4)), XmConst.MaxVolume);
                break;

            case XmConst.VolumeEffectVibrato:
                XmModulation.UpdateEffectMemoryXy(ref ch.VibratoParam, slot.VolumeColumn & 0x0F);
                ch.ShouldResetVibrato = false;
                XmModulation.Vibrato(ch, slot.EffectType == XmConst.EffectFineVibrato ? 1 : 0);
                break;

            case XmConst.VolumeEffectPanningSlideLeft:
                XmModulation.ParamSlide(ref ch.Panning, slot.VolumeColumn & 0x0F, XmConst.MaxPanning - 1);
                break;

            case XmConst.VolumeEffectPanningSlideRight:
                XmModulation.ParamSlide(ref ch.Panning, unchecked((byte)(slot.VolumeColumn << 4)), XmConst.MaxPanning - 1);
                break;

            case XmConst.VolumeEffectTonePortamento:
                XmModulation.TonePortamento(this, ch);
                break;
        }

        switch (slot.EffectType)
        {
            case XmConst.EffectArpeggio:
                if (slot.EffectParam != 0)
                {
                    XmModulation.Arpeggio(this, ch, slot.EffectParam);
                }

                break;

            case XmConst.EffectS3mArpeggio:
                XmModulation.Arpeggio(this, ch, ch.EffectParam);
                break;

            case XmConst.EffectPortamentoUp:
                if (slot.EffectParam > 0)
                {
                    ch.PortamentoUpParam = slot.EffectParam;
                }

                XmModulation.PitchSlide(ch, -4 * ch.PortamentoUpParam, PitchSlideBehaviour.Clamp);
                break;

            case XmConst.EffectS3mPortamentoUp:
                if (ch.EffectParam < 0xE0)
                {
                    XmModulation.PitchSlide(ch, -4 * ch.EffectParam, PitchSlideBehaviour.Cut);
                }

                break;

            case XmConst.EffectPortamentoDown:
                if (slot.EffectParam > 0)
                {
                    ch.PortamentoDownParam = slot.EffectParam;
                }

                XmModulation.PitchSlide(ch, 4 * ch.PortamentoDownParam, PitchSlideBehaviour.Wraparound);
                break;

            case XmConst.EffectS3mPortamentoDown:
                if (ch.EffectParam < 0xE0)
                {
                    XmModulation.PitchSlide(ch, 4 * ch.EffectParam, PitchSlideBehaviour.Cut);
                }

                break;

            case XmConst.EffectTonePortamento:
                XmModulation.TonePortamento(this, ch);
                break;

            case XmConst.EffectTonePortamentoVolumeSlide:
                XmModulation.TonePortamento(this, ch);
                DoVolumeSlide(ch);
                break;

            case XmConst.EffectVibrato:
            case XmConst.EffectFineVibrato:
                XmModulation.UpdateEffectMemoryXy(ref ch.VibratoParam, slot.EffectParam);
                ch.ShouldResetVibrato = true;
                XmModulation.Vibrato(ch, slot.EffectType == XmConst.EffectFineVibrato ? 1 : 0);
                break;

            case XmConst.EffectVibratoVolumeSlide:
                ch.ShouldResetVibrato = true;
                XmModulation.Vibrato(ch, 0);
                DoVolumeSlide(ch);
                break;

            case XmConst.EffectVolumeSlide:
                DoVolumeSlide(ch);
                break;

            case XmConst.EffectS3mVolumeSlide:
            case XmConst.EffectS3mVibratoVolumeSlide:
            case XmConst.EffectS3mTonePortamentoVolumeSlide:
                DoS3mVolumeSlide(ch, slot.EffectType);
                break;

            case XmConst.EffectTremolo:
                XmModulation.UpdateEffectMemoryXy(ref ch.TremoloParam, slot.EffectParam);
                XmModulation.Tremolo(ch, ch.TremoloParam);
                break;

            case XmConst.EffectS3mTremolo:
                XmModulation.Tremolo(ch, ch.EffectParam);
                ch.VolumeOffset >>= 1;
                break;

            case XmConst.EffectGlobalVolumeSlide:
                if (slot.EffectParam > 0)
                {
                    ch.GlobalVolumeSlideParam = slot.EffectParam;
                }

                XmModulation.ParamSlide(ref _globalVolume, ch.GlobalVolumeSlideParam, XmConst.MaxVolume);
                break;

            case XmConst.EffectKeyOff:
                if (_currentTick == slot.EffectParam)
                {
                    KeyOff(ch);
                }

                break;

            case XmConst.EffectPanningSlide:
                if (slot.EffectParam > 0)
                {
                    ch.PanningSlideParam = slot.EffectParam;
                }

                XmModulation.ParamSlide(ref ch.Panning, ch.PanningSlideParam, XmConst.MaxPanning - 1);
                break;

            case XmConst.EffectMultiRetrigNote:
                XmModulation.MultiRetrigNote(this, ch, ch.MultiRetrigParam);
                break;

            case XmConst.EffectS3mMultiRetrigNote:
                XmModulation.MultiRetrigNote(this, ch, ch.EffectParam);
                break;

            case XmConst.EffectTremor:
                if (slot.EffectParam > 0)
                {
                    ch.TremorParam = slot.EffectParam;
                }

                XmModulation.Tremor(ch, ch.TremorParam);
                break;

            case XmConst.EffectS3mTremor:
                XmModulation.Tremor(ch, ch.EffectParam);
                break;

            case XmConst.EffectRetriggerNote:
                if (slot.EffectParam > 0 && (_currentTick % slot.EffectParam) == 0)
                {
                    TriggerInstrument(ch);
                    TriggerNote(ch);
                    XmEnvelopes.TickEnvelopes(ch);
                }

                break;

            case XmConst.EffectCutNote:
                if (_currentTick == slot.EffectParam)
                {
                    CutNote(ch);
                }

                break;

            case XmConst.EffectDelayNote:
                if (_currentTick == slot.EffectParam)
                {
                    HandlePatternSlot(ch);
                    TriggerInstrument(ch);
                    if ((slot.Note & 128) == 0)
                    {
                        TriggerNote(ch);
                    }

                    XmEnvelopes.TickEnvelopes(ch);
                }

                break;
        }
    }

    private void DoVolumeSlide(XmChannel ch)
    {
        if (ch.Current.EffectParam > 0)
        {
            ch.VolumeSlideParam = ch.Current.EffectParam;
        }

        XmModulation.ResetVolumeOffset(ch);
        XmModulation.ParamSlide(ref ch.Volume, ch.VolumeSlideParam, XmConst.MaxVolume);
    }

    private void DoS3mVolumeSlide(XmChannel ch, int effectType)
    {
        var p = ch.EffectParam;
        if ((p >> 4) == 0 || (p & 0xF) == 0 || ((p >> 4) != 0xF && (p & 0xF) != 0xF))
        {
            if (effectType == XmConst.EffectS3mVibratoVolumeSlide)
            {
                ch.ShouldResetVibrato = true;
                XmModulation.Vibrato(ch, 0);
            }
            else if (effectType == XmConst.EffectS3mTonePortamentoVolumeSlide)
            {
                XmModulation.TonePortamento(this, ch);
            }

            XmModulation.VolumeSlideS3m(ch);
        }
    }
}
