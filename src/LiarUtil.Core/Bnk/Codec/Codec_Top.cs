namespace LiarUtil.Core.Bnk.Codec;

using LiarUtil.Core.Bnk.Model;

internal static partial class BankCodecImpl
{
    public static void LoadProperty(BankContext c, CommonPropertyMap<AudioCommonPropertyType> map, BusVoiceSetting value)
    {
        if (c.Reading)
        {
            if (c.Version.AtLeast(72))
            {
                value.Volume.Value = CommonProperty.Float(map, AudioCommonPropertyType.VoiceVolume, 0.0d);
            }
            if (c.Version.AtLeast(72))
            {
                value.Pitch.Value = CommonProperty.Float(map, AudioCommonPropertyType.VoicePitch, 0.0d);
            }
            if (c.Version.AtLeast(72))
            {
                value.LowPassFilter.Value = CommonProperty.Float(map, AudioCommonPropertyType.VoiceLowPassFilter, 0.0d);
            }
            if (c.Version.AtLeast(112))
            {
                value.HighPassFilter.Value = CommonProperty.Float(map, AudioCommonPropertyType.VoiceHighPassFilter, 0.0d);
            }
        }
        else
        {
            if (c.Version.AtLeast(72))
            {
                CommonProperty.AddFloat(map, AudioCommonPropertyType.VoiceVolume, value.Volume.Value);
            }
            if (c.Version.AtLeast(72))
            {
                CommonProperty.AddFloat(map, AudioCommonPropertyType.VoicePitch, value.Pitch.Value);
            }
            if (c.Version.AtLeast(72))
            {
                CommonProperty.AddFloat(map, AudioCommonPropertyType.VoiceLowPassFilter, value.LowPassFilter.Value);
            }
            if (c.Version.AtLeast(112))
            {
                CommonProperty.AddFloat(map, AudioCommonPropertyType.VoiceHighPassFilter, value.HighPassFilter.Value);
            }
        }
    }

    public static void LoadProperty(BankContext c, CommonPropertyMap<AudioCommonPropertyType> map, AudioVoice value)
    {
        if (c.Reading)
        {
            if (c.Version.AtLeast(72))
            {
                CommonProperty.RandomizableFloat(map, AudioCommonPropertyType.VoiceVolume, value.Volume, 0.0d);
            }
            if (c.Version.AtLeast(72))
            {
                CommonProperty.RandomizableFloat(map, AudioCommonPropertyType.VoicePitch, value.Pitch, 0.0d);
            }
            if (c.Version.AtLeast(72))
            {
                CommonProperty.RandomizableFloat(map, AudioCommonPropertyType.VoiceLowPassFilter, value.LowPassFilter, 0.0d);
            }
            if (c.Version.AtLeast(112))
            {
                CommonProperty.RandomizableFloat(map, AudioCommonPropertyType.VoiceHighPassFilter, value.HighPassFilter, 0.0d);
            }
        }
        else
        {
            if (c.Version.AtLeast(72))
            {
                CommonProperty.AddRandomizableFloat(map, AudioCommonPropertyType.VoiceVolume, value.Volume);
            }
            if (c.Version.AtLeast(72))
            {
                CommonProperty.AddRandomizableFloat(map, AudioCommonPropertyType.VoicePitch, value.Pitch);
            }
            if (c.Version.AtLeast(72))
            {
                CommonProperty.AddRandomizableFloat(map, AudioCommonPropertyType.VoiceLowPassFilter, value.LowPassFilter);
            }
            if (c.Version.AtLeast(112))
            {
                CommonProperty.AddRandomizableFloat(map, AudioCommonPropertyType.VoiceHighPassFilter, value.HighPassFilter);
            }
        }
    }

    public static void LoadProperty(BankContext c, CommonPropertyMap<AudioCommonPropertyType> map, BusVoiceVolumeGainSetting value)
    {
        if (c.Reading)
        {
            if (c.Version.AtLeast(125))
            {
                value.MakeUp.Value = CommonProperty.Float(map, AudioCommonPropertyType.VoiceVolumeMakeUpGain, 0.0d);
            }
        }
        else
        {
            if (c.Version.AtLeast(125))
            {
                CommonProperty.AddFloat(map, AudioCommonPropertyType.VoiceVolumeMakeUpGain, value.MakeUp.Value);
            }
        }
    }

    public static void LoadProperty(BankContext c, CommonPropertyMap<AudioCommonPropertyType> map, AudioVoiceVolumeGainSetting value)
    {
        if (c.Reading)
        {
            if (c.Version.In(88, 112))
            {
                value.MakeUp.Value = CommonProperty.Float(map, AudioCommonPropertyType.VoiceVolumeMakeUpGain, 0.0d);
            }
            if (c.Version.AtLeast(112))
            {
                CommonProperty.RandomizableFloat(map, AudioCommonPropertyType.VoiceVolumeMakeUpGain, value.MakeUp, 0.0d);
            }
        }
        else
        {
            if (c.Version.In(88, 112))
            {
                CommonProperty.AddFloat(map, AudioCommonPropertyType.VoiceVolumeMakeUpGain, value.MakeUp.Value);
            }
            if (c.Version.AtLeast(112))
            {
                CommonProperty.AddRandomizableFloat(map, AudioCommonPropertyType.VoiceVolumeMakeUpGain, value.MakeUp);
            }
        }
    }

    public static void LoadProperty(BankContext c, CommonPropertyMap<AudioCommonPropertyType> map, BusBusSetting value)
    {
        if (c.Reading)
        {
            if (c.Version.AtLeast(72))
            {
                value.Volume.Value = CommonProperty.Float(map, AudioCommonPropertyType.BusVolume, 0.0d);
            }
        }
        else
        {
            if (c.Version.AtLeast(72))
            {
                CommonProperty.AddFloat(map, AudioCommonPropertyType.BusVolume, value.Volume.Value);
            }
        }
    }

    public static void LoadProperty(BankContext c, CommonPropertyMap<AudioCommonPropertyType> map, BusOutputBusSetting value)
    {
        if (c.Reading)
        {
            if (c.Version.AtLeast(128))
            {
                value.Volume.Value = CommonProperty.Float(map, AudioCommonPropertyType.OutputBusVolume, 0.0d);
            }
            if (c.Version.AtLeast(128))
            {
                value.LowPassFilter.Value = CommonProperty.Float(map, AudioCommonPropertyType.OutputBusLowPassFilter, 0.0d);
            }
            if (c.Version.AtLeast(128))
            {
                value.HighPassFilter.Value = CommonProperty.Float(map, AudioCommonPropertyType.OutputBusHighPassFilter, 0.0d);
            }
        }
        else
        {
            if (c.Version.AtLeast(128))
            {
                CommonProperty.AddFloat(map, AudioCommonPropertyType.OutputBusVolume, value.Volume.Value);
            }
            if (c.Version.AtLeast(128))
            {
                CommonProperty.AddFloat(map, AudioCommonPropertyType.OutputBusLowPassFilter, value.LowPassFilter.Value);
            }
            if (c.Version.AtLeast(128))
            {
                CommonProperty.AddFloat(map, AudioCommonPropertyType.OutputBusHighPassFilter, value.HighPassFilter.Value);
            }
        }
    }

    public static void LoadProperty(BankContext c, CommonPropertyMap<AudioCommonPropertyType> map, AudioOutputBusSetting value)
    {
        if (c.Reading)
        {
            if (c.Version.AtLeast(72))
            {
                value.Volume.Value = CommonProperty.Float(map, AudioCommonPropertyType.OutputBusVolume, 0.0d);
            }
            if (c.Version.AtLeast(72))
            {
                value.LowPassFilter.Value = CommonProperty.Float(map, AudioCommonPropertyType.OutputBusLowPassFilter, 0.0d);
            }
            if (c.Version.AtLeast(112))
            {
                value.HighPassFilter.Value = CommonProperty.Float(map, AudioCommonPropertyType.OutputBusHighPassFilter, 0.0d);
            }
        }
        else
        {
            if (c.Version.AtLeast(72))
            {
                CommonProperty.AddFloat(map, AudioCommonPropertyType.OutputBusVolume, value.Volume.Value);
            }
            if (c.Version.AtLeast(72))
            {
                CommonProperty.AddFloat(map, AudioCommonPropertyType.OutputBusLowPassFilter, value.LowPassFilter.Value);
            }
            if (c.Version.AtLeast(112))
            {
                CommonProperty.AddFloat(map, AudioCommonPropertyType.OutputBusHighPassFilter, value.HighPassFilter.Value);
            }
        }
    }

    public static void LoadProperty(BankContext c, CommonPropertyMap<AudioCommonPropertyType> map, AudioAuxiliarySendSetting value)
    {
        if (c.Reading)
        {
            if (c.Version.AtLeast(72))
            {
                value.GameDefined.Volume.Value = CommonProperty.Float(map, AudioCommonPropertyType.GameDefinedAuxiliarySendVolume, 0.0d);
            }
            if (c.Version.AtLeast(128))
            {
                value.GameDefined.LowPassFilter.Value = CommonProperty.Float(map, AudioCommonPropertyType.GameDefinedAuxiliarySendLowPassFilter, 0.0d);
            }
            if (c.Version.AtLeast(128))
            {
                value.GameDefined.HighPassFilter.Value = CommonProperty.Float(map, AudioCommonPropertyType.GameDefinedAuxiliarySendHighPassFilter, 0.0d);
            }
            if (c.Version.AtLeast(72))
            {
                value.UserDefined.Item1.Volume.Value = CommonProperty.Float(map, AudioCommonPropertyType.UserDefinedAuxiliarySendVolume0, 0.0d);
            }
            if (c.Version.AtLeast(128))
            {
                value.UserDefined.Item1.LowPassFilter.Value = CommonProperty.Float(map, AudioCommonPropertyType.UserDefinedAuxiliarySendLowPassFilter0, 0.0d);
            }
            if (c.Version.AtLeast(128))
            {
                value.UserDefined.Item1.HighPassFilter.Value = CommonProperty.Float(map, AudioCommonPropertyType.UserDefinedAuxiliarySendHighPassFilter0, 0.0d);
            }
            if (c.Version.AtLeast(72))
            {
                value.UserDefined.Item2.Volume.Value = CommonProperty.Float(map, AudioCommonPropertyType.UserDefinedAuxiliarySendVolume1, 0.0d);
            }
            if (c.Version.AtLeast(128))
            {
                value.UserDefined.Item2.LowPassFilter.Value = CommonProperty.Float(map, AudioCommonPropertyType.UserDefinedAuxiliarySendLowPassFilter1, 0.0d);
            }
            if (c.Version.AtLeast(128))
            {
                value.UserDefined.Item2.HighPassFilter.Value = CommonProperty.Float(map, AudioCommonPropertyType.UserDefinedAuxiliarySendHighPassFilter1, 0.0d);
            }
            if (c.Version.AtLeast(72))
            {
                value.UserDefined.Item3.Volume.Value = CommonProperty.Float(map, AudioCommonPropertyType.UserDefinedAuxiliarySendVolume2, 0.0d);
            }
            if (c.Version.AtLeast(128))
            {
                value.UserDefined.Item3.LowPassFilter.Value = CommonProperty.Float(map, AudioCommonPropertyType.UserDefinedAuxiliarySendLowPassFilter2, 0.0d);
            }
            if (c.Version.AtLeast(128))
            {
                value.UserDefined.Item3.HighPassFilter.Value = CommonProperty.Float(map, AudioCommonPropertyType.UserDefinedAuxiliarySendHighPassFilter2, 0.0d);
            }
            if (c.Version.AtLeast(72))
            {
                value.UserDefined.Item4.Volume.Value = CommonProperty.Float(map, AudioCommonPropertyType.UserDefinedAuxiliarySendVolume3, 0.0d);
            }
            if (c.Version.AtLeast(128))
            {
                value.UserDefined.Item4.LowPassFilter.Value = CommonProperty.Float(map, AudioCommonPropertyType.UserDefinedAuxiliarySendLowPassFilter3, 0.0d);
            }
            if (c.Version.AtLeast(128))
            {
                value.UserDefined.Item4.HighPassFilter.Value = CommonProperty.Float(map, AudioCommonPropertyType.UserDefinedAuxiliarySendHighPassFilter3, 0.0d);
            }
            if (c.Version.AtLeast(135))
            {
                value.EarlyReflection.Volume.Value = CommonProperty.Float(map, AudioCommonPropertyType.EarlyReflectionAuxiliarySendVolume, 0.0d);
            }
        }
        else
        {
            if (c.Version.AtLeast(72))
            {
                CommonProperty.AddFloat(map, AudioCommonPropertyType.GameDefinedAuxiliarySendVolume, value.GameDefined.Volume.Value);
            }
            if (c.Version.AtLeast(128))
            {
                CommonProperty.AddFloat(map, AudioCommonPropertyType.GameDefinedAuxiliarySendLowPassFilter, value.GameDefined.LowPassFilter.Value);
            }
            if (c.Version.AtLeast(128))
            {
                CommonProperty.AddFloat(map, AudioCommonPropertyType.GameDefinedAuxiliarySendHighPassFilter, value.GameDefined.HighPassFilter.Value);
            }
            if (c.Version.AtLeast(72))
            {
                CommonProperty.AddFloat(map, AudioCommonPropertyType.UserDefinedAuxiliarySendVolume0, value.UserDefined.Item1.Volume.Value);
            }
            if (c.Version.AtLeast(128))
            {
                CommonProperty.AddFloat(map, AudioCommonPropertyType.UserDefinedAuxiliarySendLowPassFilter0, value.UserDefined.Item1.LowPassFilter.Value);
            }
            if (c.Version.AtLeast(128))
            {
                CommonProperty.AddFloat(map, AudioCommonPropertyType.UserDefinedAuxiliarySendHighPassFilter0, value.UserDefined.Item1.HighPassFilter.Value);
            }
            if (c.Version.AtLeast(72))
            {
                CommonProperty.AddFloat(map, AudioCommonPropertyType.UserDefinedAuxiliarySendVolume1, value.UserDefined.Item2.Volume.Value);
            }
            if (c.Version.AtLeast(128))
            {
                CommonProperty.AddFloat(map, AudioCommonPropertyType.UserDefinedAuxiliarySendLowPassFilter1, value.UserDefined.Item2.LowPassFilter.Value);
            }
            if (c.Version.AtLeast(128))
            {
                CommonProperty.AddFloat(map, AudioCommonPropertyType.UserDefinedAuxiliarySendHighPassFilter1, value.UserDefined.Item2.HighPassFilter.Value);
            }
            if (c.Version.AtLeast(72))
            {
                CommonProperty.AddFloat(map, AudioCommonPropertyType.UserDefinedAuxiliarySendVolume2, value.UserDefined.Item3.Volume.Value);
            }
            if (c.Version.AtLeast(128))
            {
                CommonProperty.AddFloat(map, AudioCommonPropertyType.UserDefinedAuxiliarySendLowPassFilter2, value.UserDefined.Item3.LowPassFilter.Value);
            }
            if (c.Version.AtLeast(128))
            {
                CommonProperty.AddFloat(map, AudioCommonPropertyType.UserDefinedAuxiliarySendHighPassFilter2, value.UserDefined.Item3.HighPassFilter.Value);
            }
            if (c.Version.AtLeast(72))
            {
                CommonProperty.AddFloat(map, AudioCommonPropertyType.UserDefinedAuxiliarySendVolume3, value.UserDefined.Item4.Volume.Value);
            }
            if (c.Version.AtLeast(128))
            {
                CommonProperty.AddFloat(map, AudioCommonPropertyType.UserDefinedAuxiliarySendLowPassFilter3, value.UserDefined.Item4.LowPassFilter.Value);
            }
            if (c.Version.AtLeast(128))
            {
                CommonProperty.AddFloat(map, AudioCommonPropertyType.UserDefinedAuxiliarySendHighPassFilter3, value.UserDefined.Item4.HighPassFilter.Value);
            }
            if (c.Version.AtLeast(135))
            {
                CommonProperty.AddFloat(map, AudioCommonPropertyType.EarlyReflectionAuxiliarySendVolume, value.EarlyReflection.Volume.Value);
            }
        }
    }

    public static void LoadProperty(BankContext c, CommonPropertyMap<AudioCommonPropertyType> map, AudioPositioningSetting value)
    {
        if (c.Reading)
        {
            if (c.Version.AtLeast(72))
            {
                value.CenterPercent.Value = CommonProperty.Float(map, AudioCommonPropertyType.PositioningCenterPercent, 0.0d);
            }
            if (c.Version.AtLeast(72))
            {
                value.SpeakerPanning.Position.X = CommonProperty.Float(map, AudioCommonPropertyType.PositioningSpeakerPanningX, value.SpeakerPanning.Position.X);
            }
            if (c.Version.AtLeast(72))
            {
                value.SpeakerPanning.Position.Y = CommonProperty.Float(map, AudioCommonPropertyType.PositioningSpeakerPanningY, value.SpeakerPanning.Position.Y);
            }
            if (c.Version.AtLeast(140))
            {
                value.SpeakerPanning.Position.Z = CommonProperty.Float(map, AudioCommonPropertyType.PositioningSpeakerPanningZ, value.SpeakerPanning.Position.Z);
            }
            if (c.Version.AtLeast(132))
            {
                value.ListenerRouting.SpeakerPanningDivsionSpatializationMix.Value = CommonProperty.Float(map, AudioCommonPropertyType.PositioningListenerRoutingSpeakerPanningDivisionSpatializationMix, 100.0d);
            }
            if (c.Version.AtLeast(132))
            {
                value.ListenerRouting.Attenuation.Identifier = CommonProperty.Identifier(map, AudioCommonPropertyType.PositioningListenerRoutingAttenuationIdentifier, value.ListenerRouting.Attenuation.Identifier);
            }
        }
        else
        {
            if (c.Version.AtLeast(72))
            {
                CommonProperty.AddFloat(map, AudioCommonPropertyType.PositioningCenterPercent, value.CenterPercent.Value);
            }
            if (c.Version.AtLeast(72))
            {
                CommonProperty.AddFloat(map, AudioCommonPropertyType.PositioningSpeakerPanningX, value.SpeakerPanning.Position.X);
            }
            if (c.Version.AtLeast(72))
            {
                CommonProperty.AddFloat(map, AudioCommonPropertyType.PositioningSpeakerPanningY, value.SpeakerPanning.Position.Y);
            }
            if (c.Version.AtLeast(140))
            {
                CommonProperty.AddFloat(map, AudioCommonPropertyType.PositioningSpeakerPanningZ, value.SpeakerPanning.Position.Z);
            }
            if (c.Version.AtLeast(132))
            {
                CommonProperty.AddFloat(map, AudioCommonPropertyType.PositioningListenerRoutingSpeakerPanningDivisionSpatializationMix, value.ListenerRouting.SpeakerPanningDivsionSpatializationMix.Value);
            }
            if (c.Version.AtLeast(132))
            {
                CommonProperty.AddIdentifier(map, AudioCommonPropertyType.PositioningListenerRoutingAttenuationIdentifier, value.ListenerRouting.Attenuation.Identifier);
            }
        }
    }

    public static void LoadProperty(BankContext c, CommonPropertyMap<AudioCommonPropertyType> map, BusHdrSetting value)
    {
        if (c.Reading)
        {
            if (c.Version.AtLeast(88))
            {
                value.Dynamic.Threshold = CommonProperty.Float(map, AudioCommonPropertyType.HdrThreshold, value.Dynamic.Threshold);
            }
            if (c.Version.AtLeast(88))
            {
                value.Dynamic.Ratio = CommonProperty.Float(map, AudioCommonPropertyType.HdrRatio, value.Dynamic.Ratio);
            }
            if (c.Version.AtLeast(88))
            {
                value.Dynamic.ReleaseTime = CommonProperty.Float(map, AudioCommonPropertyType.HdrReleaseTime, value.Dynamic.ReleaseTime);
            }
            if (c.Version.AtLeast(88))
            {
                value.WindowTopOutputGameParameter.Identifier = CommonProperty.Identifier(map, AudioCommonPropertyType.HdrWindowTapOutputGameParameterIdentifier, value.WindowTopOutputGameParameter.Identifier);
            }
            if (c.Version.AtLeast(88))
            {
                value.WindowTopOutputGameParameter.Minimum = CommonProperty.Float(map, AudioCommonPropertyType.HdrWindowTapOutputGameParameterMinimum, value.WindowTopOutputGameParameter.Minimum);
            }
            if (c.Version.AtLeast(88))
            {
                value.WindowTopOutputGameParameter.Maximum = CommonProperty.Float(map, AudioCommonPropertyType.HdrWindowTapOutputGameParameterMaximum, value.WindowTopOutputGameParameter.Maximum);
            }
        }
        else
        {
            if (c.Version.AtLeast(88))
            {
                CommonProperty.AddFloat(map, AudioCommonPropertyType.HdrThreshold, value.Dynamic.Threshold);
            }
            if (c.Version.AtLeast(88))
            {
                CommonProperty.AddFloat(map, AudioCommonPropertyType.HdrRatio, value.Dynamic.Ratio);
            }
            if (c.Version.AtLeast(88))
            {
                CommonProperty.AddFloat(map, AudioCommonPropertyType.HdrReleaseTime, value.Dynamic.ReleaseTime);
            }
            if (c.Version.AtLeast(88))
            {
                CommonProperty.AddIdentifier(map, AudioCommonPropertyType.HdrWindowTapOutputGameParameterIdentifier, value.WindowTopOutputGameParameter.Identifier);
            }
            if (c.Version.AtLeast(88))
            {
                CommonProperty.AddFloat(map, AudioCommonPropertyType.HdrWindowTapOutputGameParameterMinimum, value.WindowTopOutputGameParameter.Minimum);
            }
            if (c.Version.AtLeast(88))
            {
                CommonProperty.AddFloat(map, AudioCommonPropertyType.HdrWindowTapOutputGameParameterMaximum, value.WindowTopOutputGameParameter.Maximum);
            }
        }
    }

    public static void LoadProperty(BankContext c, CommonPropertyMap<AudioCommonPropertyType> map, AudioHdrSetting value)
    {
        if (c.Reading)
        {
            if (c.Version.AtLeast(88))
            {
                value.EnvelopeTracking.ActiveRange = CommonProperty.Float(map, AudioCommonPropertyType.HdrEnvelopeTrackingActiveRange, value.EnvelopeTracking.ActiveRange);
            }
        }
        else
        {
            if (c.Version.AtLeast(88))
            {
                CommonProperty.AddFloat(map, AudioCommonPropertyType.HdrEnvelopeTrackingActiveRange, value.EnvelopeTracking.ActiveRange);
            }
        }
    }

    public static void LoadProperty(BankContext c, CommonPropertyMap<AudioCommonPropertyType> map, SoundMidiSetting value)
    {
        if (c.Reading)
        {
            if (c.Version.AtLeast(112))
            {
                value.Event.PlayOn = SoundMidiSettingEventPlayOnWire.Instance.FromRaw(c.Version, CommonProperty.Enum(map, AudioCommonPropertyType.MidiEventPlayOn, (byte)SoundMidiSettingEventPlayOnWire.Instance.ToRaw(c.Version, value.Event.PlayOn)));
            }
            if (c.Version.AtLeast(112))
            {
                value.NoteTracking.RootNote = CommonProperty.Int(map, AudioCommonPropertyType.MidiNoteTrackingRootNote, value.NoteTracking.RootNote);
            }
            if (c.Version.AtLeast(112))
            {
                value.Transformation.Transposition.Value = CommonProperty.Int(map, AudioCommonPropertyType.MidiTransformationTransposition, 0);
            }
            if (c.Version.AtLeast(112))
            {
                value.Transformation.VelocityOffset.Value = CommonProperty.Int(map, AudioCommonPropertyType.MidiTransformationVelocityOffset, 0);
            }
            if (c.Version.AtLeast(112))
            {
                value.Filter.KeyRangeMinimum = CommonProperty.Int(map, AudioCommonPropertyType.MidiFilterKeyRangeMinimum, value.Filter.KeyRangeMinimum);
            }
            if (c.Version.AtLeast(112))
            {
                value.Filter.KeyRangeMaximum = CommonProperty.Int(map, AudioCommonPropertyType.MidiFilterKeyRangeMaximum, value.Filter.KeyRangeMaximum);
            }
            if (c.Version.AtLeast(112))
            {
                value.Filter.VelocityMinimum = CommonProperty.Int(map, AudioCommonPropertyType.MidiFilterVelocityMinimum, value.Filter.VelocityMinimum);
            }
            if (c.Version.AtLeast(112))
            {
                value.Filter.VelocityMaximum = CommonProperty.Int(map, AudioCommonPropertyType.MidiFilterVelocityMaximum, value.Filter.VelocityMaximum);
            }
            if (c.Version.AtLeast(112))
            {
                value.Filter.Channel = CommonProperty.Int(map, AudioCommonPropertyType.MidiFilterChannel, value.Filter.Channel);
            }
        }
        else
        {
            if (c.Version.AtLeast(112))
            {
                CommonProperty.AddEnum(map, AudioCommonPropertyType.MidiEventPlayOn, (byte)SoundMidiSettingEventPlayOnWire.Instance.ToRaw(c.Version, value.Event.PlayOn));
            }
            if (c.Version.AtLeast(112))
            {
                CommonProperty.AddInt(map, AudioCommonPropertyType.MidiNoteTrackingRootNote, value.NoteTracking.RootNote);
            }
            if (c.Version.AtLeast(112))
            {
                CommonProperty.AddInt(map, AudioCommonPropertyType.MidiTransformationTransposition, value.Transformation.Transposition.Value);
            }
            if (c.Version.AtLeast(112))
            {
                CommonProperty.AddInt(map, AudioCommonPropertyType.MidiTransformationVelocityOffset, value.Transformation.VelocityOffset.Value);
            }
            if (c.Version.AtLeast(112))
            {
                CommonProperty.AddInt(map, AudioCommonPropertyType.MidiFilterKeyRangeMinimum, value.Filter.KeyRangeMinimum);
            }
            if (c.Version.AtLeast(112))
            {
                CommonProperty.AddInt(map, AudioCommonPropertyType.MidiFilterKeyRangeMaximum, value.Filter.KeyRangeMaximum);
            }
            if (c.Version.AtLeast(112))
            {
                CommonProperty.AddInt(map, AudioCommonPropertyType.MidiFilterVelocityMinimum, value.Filter.VelocityMinimum);
            }
            if (c.Version.AtLeast(112))
            {
                CommonProperty.AddInt(map, AudioCommonPropertyType.MidiFilterVelocityMaximum, value.Filter.VelocityMaximum);
            }
            if (c.Version.AtLeast(112))
            {
                CommonProperty.AddInt(map, AudioCommonPropertyType.MidiFilterChannel, value.Filter.Channel);
            }
        }
    }

    public static void LoadProperty(BankContext c, CommonPropertyMap<AudioCommonPropertyType> map, MusicMidiSetting value)
    {
        if (c.Reading)
        {
            if (c.Version.AtLeast(112))
            {
                value.Target.Identifier = CommonProperty.Identifier(map, AudioCommonPropertyType.MidiTargetIdentifier, value.Target.Identifier);
            }
            if (c.Version.AtLeast(112))
            {
                value.ClipTempo.Source = MusicMidiSettingClipTempoSourceWire.Instance.FromRaw(c.Version, CommonProperty.Enum(map, AudioCommonPropertyType.MidiClipTempoSource, (byte)MusicMidiSettingClipTempoSourceWire.Instance.ToRaw(c.Version, value.ClipTempo.Source)));
            }
        }
        else
        {
            if (c.Version.AtLeast(112))
            {
                CommonProperty.AddIdentifier(map, AudioCommonPropertyType.MidiTargetIdentifier, value.Target.Identifier);
            }
            if (c.Version.AtLeast(112))
            {
                CommonProperty.AddEnum(map, AudioCommonPropertyType.MidiClipTempoSource, (byte)MusicMidiSettingClipTempoSourceWire.Instance.ToRaw(c.Version, value.ClipTempo.Source));
            }
        }
    }

    public static void LoadProperty(BankContext c, CommonPropertyMap<AudioCommonPropertyType> map, AudioPlaybackLimitSetting value)
    {
        if (c.Reading)
        {
        }
        else
        {
        }
    }

    public static void LoadProperty(BankContext c, CommonPropertyMap<AudioCommonPropertyType> map, AudioVirtualVoiceSetting value)
    {
        if (c.Reading)
        {
        }
        else
        {
        }
    }

    public static void LoadProperty(BankContext c, CommonPropertyMap<AudioCommonPropertyType> map, AudioPlaybackPrioritySetting value)
    {
        if (c.Reading)
        {
            if (c.Version.AtLeast(72))
            {
                value.Value.Value = CommonProperty.Float(map, AudioCommonPropertyType.PlaybackPriorityValue, 0.0d);
            }
            if (c.Version.AtLeast(72))
            {
                value.OffsetAtMaximumDistance.Value = CommonProperty.Float(map, AudioCommonPropertyType.PlaybackPriorityOffsetAtMaximumDistance, 0.0d);
            }
        }
        else
        {
            if (c.Version.AtLeast(72))
            {
                CommonProperty.AddFloat(map, AudioCommonPropertyType.PlaybackPriorityValue, value.Value.Value);
            }
            if (c.Version.AtLeast(72))
            {
                CommonProperty.AddFloat(map, AudioCommonPropertyType.PlaybackPriorityOffsetAtMaximumDistance, value.OffsetAtMaximumDistance.Value);
            }
        }
    }

    public static void LoadProperty(BankContext c, CommonPropertyMap<AudioCommonPropertyType> map, AudioMotionSetting value)
    {
        if (c.Reading)
        {
            if (c.Version.In(72, 128))
            {
                CommonProperty.RandomizableFloat(map, AudioCommonPropertyType.MotionLowPassFilter, value.LowPassFilter, 0.0d);
            }
            if (c.Version.In(72, 128))
            {
                CommonProperty.RandomizableFloat(map, AudioCommonPropertyType.MotionVolumeOffset, value.VolumeOffset, 0.0d);
            }
        }
        else
        {
            if (c.Version.In(72, 128))
            {
                CommonProperty.AddRandomizableFloat(map, AudioCommonPropertyType.MotionLowPassFilter, value.LowPassFilter);
            }
            if (c.Version.In(72, 128))
            {
                CommonProperty.AddRandomizableFloat(map, AudioCommonPropertyType.MotionVolumeOffset, value.VolumeOffset);
            }
        }
    }

    public static void LoadProperty(BankContext c, CommonPropertyMap<AudioCommonPropertyType> map, AudioMixerSetting value)
    {
        if (c.Reading)
        {
            if (c.Version.In(112, 150))
            {
                value.Identifier = CommonProperty.Identifier(map, AudioCommonPropertyType.MixerIdentifier, value.Identifier);
            }
        }
        else
        {
            if (c.Version.In(112, 150))
            {
                CommonProperty.AddIdentifier(map, AudioCommonPropertyType.MixerIdentifier, value.Identifier);
            }
        }
    }

    public static void Section(BankContext c, List<long> valueList)
    {
        c.List(valueList, SizeKind.U32, static (BankContext ctx, ref long item) => ctx.Id(ref item));
    }
}
