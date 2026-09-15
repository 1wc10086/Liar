namespace LiarUtil.Core.Bnk.Codec;

using LiarUtil.Core.Bnk.Model;

internal static partial class BankCodecImpl
{
    public static void Section(BankContext c, ref SoundPlaylistContainerScope scopeValue, ref AudioPlayType playTypeValue, AudioPlayTypeSetting playTypeSettingValue, ref AudioPlayMode playModeValue, AudioPlayModeSetting playModeSettingValue)
    {
        if (c.Version.AtLeast(72))
        {
            c.S16(ref playModeSettingValue.Continuous.Loop.Value);
        }
        if (c.Version.AtLeast(88))
        {
            c.S16(ref playModeSettingValue.Continuous.Loop.MinimumValue);
        }
        if (c.Version.AtLeast(88))
        {
            c.S16(ref playModeSettingValue.Continuous.Loop.MaximumValue);
        }
        if (c.Version.AtLeast(72))
        {
            c.F32(ref playModeSettingValue.Continuous.TransitionDuration.Value);
        }
        if (c.Version.AtLeast(72))
        {
            c.F32(ref playModeSettingValue.Continuous.TransitionDuration.MinimumValue);
        }
        if (c.Version.AtLeast(72))
        {
            c.F32(ref playModeSettingValue.Continuous.TransitionDuration.MaximumValue);
        }
        if (c.Version.AtLeast(72))
        {
            c.U16(ref playTypeSettingValue.Random.AvoidRepeat);
        }
        if (c.Version.AtLeast(72))
        {
            var bits94 = c.Bits8();
            bits94.Enum(ref playModeSettingValue.Continuous.TransitionType, AudioPlayModeContinuousTransitionTypeWire.Instance);
            bits94.Done();
        }
        if (c.Version.AtLeast(72))
        {
            var bits95 = c.Bits8();
            bits95.Enum(ref playTypeSettingValue.Random.Type, AudioPlayTypeRandomTypeWire.Instance);
            bits95.Done();
        }
        if (c.Version.AtLeast(72))
        {
            var bits96 = c.Bits8();
            bits96.Enum(ref playTypeValue, AudioPlayTypeWire.Instance);
            bits96.Done();
        }
        if (c.Version.In(72, 112))
        {
            var bits97 = c.Bits8();
            bits97.Const(false);
            bits97.Done();
        }
        if (c.Version.In(72, 112))
        {
            var bits98 = c.Bits8();
            bits98.Bit(ref playModeSettingValue.Continuous.AlwaysResetPlaylist);
            bits98.Done();
        }
        if (c.Version.In(72, 112))
        {
            var bits99 = c.Bits8();
            bits99.Enum(ref playTypeSettingValue.Sequence.AtEndOfPlaylist, AudioPlayTypeSequenceAtEndOfPlaylistWire.Instance);
            bits99.Done();
        }
        if (c.Version.In(72, 112))
        {
            var bits100 = c.Bits8();
            bits100.Enum(ref playModeValue, AudioPlayModeWire.Instance);
            bits100.Done();
        }
        if (c.Version.In(72, 112))
        {
            var bits101 = c.Bits8();
            bits101.Enum(ref scopeValue, SoundPlaylistContainerScopeWire.Instance);
            bits101.Done();
        }
        if (c.Version.AtLeast(112))
        {
            var bits102 = c.Bits8();
            bits102.Const(false);
            bits102.Bit(ref playModeSettingValue.Continuous.AlwaysResetPlaylist);
            bits102.Enum(ref playTypeSettingValue.Sequence.AtEndOfPlaylist, AudioPlayTypeSequenceAtEndOfPlaylistWire.Instance);
            bits102.Enum(ref playModeValue, AudioPlayModeWire.Instance);
            bits102.Enum(ref scopeValue, SoundPlaylistContainerScopeWire.Instance);
            bits102.Done();
        }
    }

    public static void Section(BankContext c, List<SoundPlaylistContainerPlaylistItem> playlistValue)
    {
        if (c.Version.AtLeast(72))
        {
            c.List(playlistValue, SizeKind.U16,
    static (BankContext ctx24, ref SoundPlaylistContainerPlaylistItem item24) =>
            {
                if (ctx24.Version.AtLeast(72))
                {
                    ctx24.Id(ref item24.Item);
                }
                if (ctx24.Version.AtLeast(72))
                {
                    ctx24.U32(ref item24.Weight);
                }
            }
            );
        }
    }

    public static void Section(BankContext c, List<SoundSwitchContainerObjectAttributeItem> objectAttributeValue)
    {
        if (c.Version.AtLeast(72))
        {
            c.List(objectAttributeValue, SizeKind.U32,
    static (BankContext ctx25, ref SoundSwitchContainerObjectAttributeItem item25) =>
            {
                if (ctx25.Version.AtLeast(72))
                {
                    ctx25.Id(ref item25.Identifier);
                }
                if (ctx25.Version.In(72, 112))
                {
                    var bits103 = ctx25.Bits8();
                    bits103.Bit(ref item25.PlayFirstOnly);
                    bits103.Done();
                }
                if (ctx25.Version.In(72, 112))
                {
                    var bits104 = ctx25.Bits8();
                    bits104.Bit(ref item25.ContinueToPlayAcrossSwitch);
                    bits104.Done();
                }
                if (ctx25.Version.AtLeast(112))
                {
                    var bits105 = ctx25.Bits8();
                    bits105.Bit(ref item25.PlayFirstOnly);
                    bits105.Bit(ref item25.ContinueToPlayAcrossSwitch);
                    bits105.Done();
                }
                if (ctx25.Version.In(72, 112))
                {
                    ctx25.U32(ref item25.U1);
                }
                if (ctx25.Version.AtLeast(112))
                {
                    ctx25.U8(ref item25.U1);
                }
                if (ctx25.Version.AtLeast(72))
                {
                    ctx25.U32(ref item25.FadeOutTime);
                }
                if (ctx25.Version.AtLeast(72))
                {
                    ctx25.U32(ref item25.FadeInTime);
                }
            }
            );
        }
    }

    public static void Section(BankContext c, List<SoundSwitchContainerObjectAssignItem> assignedObjectValue)
    {
        if (c.Version.AtLeast(72))
        {
            c.List(assignedObjectValue, SizeKind.U32,
    static (BankContext ctx26, ref SoundSwitchContainerObjectAssignItem item26) =>
            {
                if (ctx26.Version.AtLeast(72))
                {
                    ctx26.Id(ref item26.Item);
                }
                if (ctx26.Version.AtLeast(72))
                {
                    ctx26.List(item26.Object, SizeKind.U32,
    static (BankContext ctx27, ref long item27) =>
                    {
                        if (ctx27.Version.AtLeast(72))
                        {
                            ctx27.Id(ref item27);
                        }
                    }
                    );
                }
            }
            );
        }
    }

    public static void Section(BankContext c, List<SoundBlendContainerTrackItem> trackValue)
    {
        if (c.Version.AtLeast(72))
        {
            c.List(trackValue, SizeKind.U32,
    static (BankContext ctx28, ref SoundBlendContainerTrackItem item28) =>
            {
                if (ctx28.Version.AtLeast(72))
                {
                    ctx28.Id(ref item28.Identifier);
                }
                if (ctx28.Version.AtLeast(72))
                {
                    Section(ctx28, item28.RealTimeParameterControl);
                }
                if (ctx28.Version.AtLeast(72))
                {
                    ctx28.Id(ref item28.CrossFade.Identifier);
                }
                if (ctx28.Version.AtLeast(112))
                {
                    var bits106 = ctx28.Bits8();
                    bits106.Enum(ref item28.CrossFade.Category, ParameterCategoryWire.Instance);
                    bits106.Done();
                }
                if (ctx28.Version.AtLeast(72))
                {
                    ctx28.List(item28.Child, SizeKind.U32,
    static (BankContext ctx29, ref SoundBlendContainerTrackChildItem item29) =>
                    {
                        if (ctx29.Version.AtLeast(72))
                        {
                            ctx29.Id(ref item29.Identifier);
                        }
                        if (ctx29.Version.AtLeast(72))
                        {
                            ctx29.List(item29.Point, SizeKind.U32,
    static (BankContext ctx30, ref CoordinatePoint item30) =>
                            {
                                if (ctx30.Version.AtLeast(72))
                                {
                                    ctx30.F32(ref item30.Position.X);
                                }
                                if (ctx30.Version.AtLeast(72))
                                {
                                    ctx30.F32(ref item30.Position.Y);
                                }
                                if (ctx30.Version.AtLeast(72))
                                {
                                    var bits107 = ctx30.Bits32();
                                    bits107.Enum(ref item30.Curve, CurveWire.Instance);
                                    bits107.Done();
                                }
                            }
                            );
                        }
                    }
                    );
                }
            }
            );
        }
    }

    public static void Section(BankContext c, Sound value)
    {
        c.Id(ref value.Identifier);
        if (c.Version.AtLeast(72))
        {
            Section(c, value.Source);
        }
        if (c.Version.AtLeast(72))
        {
            Section(c, value.Effect, ref value.OverrideEffect);
        }
        if (c.Version.AtLeast(140))
        {
            Section(c, value.Metadata, ref value.OverrideMetadata);
        }
        if (c.Version.In(112, 150))
        {
            Section(c, value.Mixer, ref value.OverrideMixer);
        }
        if (c.Version.AtLeast(72))
        {
            Section(c, value.OutputBus);
        }
        if (c.Version.AtLeast(72))
        {
            c.Id(ref value.Parent);
        }
        if (c.Version.In(72, 112))
        {
            Section(c, value.PlaybackPriority, ref value.OverridePlaybackPriority);
        }
        if (c.Version.AtLeast(112))
        {
            Section(c, value.Midi, value.PlaybackPriority, ref value.OverrideMidiEvent, ref value.OverrideMidiNoteTracking, ref value.OverridePlaybackPriority);
        }
        if (c.Version.AtLeast(72))
        {
            {
                var commonProperty = new CommonPropertyMap<AudioCommonPropertyType>();
                void ApplyCommonProperties()
                {
                if (c.Version.AtLeast(88))
                {
                    CommonProperty.ExchangeRandomizableFloat(c, commonProperty, AudioCommonPropertyType.PlaybackInitialDelay, value.PlaybackSetting.InitialDelay, 0.0d);
                }
                if (c.Version.AtLeast(72))
                {
                    CommonProperty.ExchangeRandomizableInt(c, commonProperty, AudioCommonPropertyType.PlaybackLoop, value.PlaybackSetting.Loop, 0);
                }
                if (c.Version.AtLeast(72))
                {
                    LoadProperty(c, commonProperty, value.Voice);
                }
                if (c.Version.AtLeast(88))
                {
                    LoadProperty(c, commonProperty, value.VoiceVolumeGain);
                }
                if (c.Version.AtLeast(72))
                {
                    LoadProperty(c, commonProperty, value.OutputBus);
                }
                if (c.Version.AtLeast(72))
                {
                    LoadProperty(c, commonProperty, value.AuxiliarySend);
                }
                if (c.Version.AtLeast(72))
                {
                    LoadProperty(c, commonProperty, value.Positioning);
                }
                if (c.Version.AtLeast(88))
                {
                    LoadProperty(c, commonProperty, value.Hdr);
                }
                if (c.Version.AtLeast(112))
                {
                    LoadProperty(c, commonProperty, value.Midi);
                }
                if (c.Version.AtLeast(72))
                {
                    LoadProperty(c, commonProperty, value.PlaybackLimit);
                }
                if (c.Version.AtLeast(72))
                {
                    LoadProperty(c, commonProperty, value.VirtualVoice);
                }
                if (c.Version.AtLeast(72))
                {
                    LoadProperty(c, commonProperty, value.PlaybackPriority);
                }
                if (c.Version.In(72, 128))
                {
                    LoadProperty(c, commonProperty, value.Motion);
                }
                if (c.Version.In(112, 150))
                {
                    LoadProperty(c, commonProperty, value.Mixer);
                }
                }
                if (!c.Reading)
                {
                    ApplyCommonProperties();
                }
                CommonProperty.Read(c, commonProperty, AudioCommonPropertyTypeTable.Instance, true);
                if (c.Reading)
                {
                    ApplyCommonProperties();
                }
            }

        }
        if (c.Version.AtLeast(72))
        {
            Section(c, value.Positioning, ref value.OverridePositioning);
        }
        if (c.Version.In(72, 135))
        {
            Section(c, value.AuxiliarySend, ref value.OverrideGameDefinedAuxiliarySend, ref value.OverrideUserDefinedAuxiliarySend);
        }
        if (c.Version.AtLeast(135))
        {
            Section(c, value.AuxiliarySend, ref value.OverrideGameDefinedAuxiliarySend, ref value.OverrideUserDefinedAuxiliarySend, ref value.OverrideEarlyReflectionAuxiliarySend);
        }
        if (c.Version.AtLeast(72))
        {
            Section(c, value.PlaybackLimit, value.VirtualVoice, ref value.OverridePlaybackLimit, ref value.OverrideVirtualVoice);
        }
        if (c.Version.AtLeast(88))
        {
            Section(c, value.VoiceVolumeGain, value.Hdr, ref value.OverrideVoiceVolumeLoudnessNormalization, ref value.OverrideHdrEnvelopeTracking);
        }
        if (c.Version.AtLeast(72))
        {
            Section(c, value.State);
        }
        if (c.Version.AtLeast(72))
        {
            Section(c, value.RealTimeParameterControl);
        }
    }

    public static void Section(BankContext c, SoundPlaylistContainer value)
    {
        c.Id(ref value.Identifier);
        if (c.Version.AtLeast(72))
        {
            Section(c, value.Effect, ref value.OverrideEffect);
        }
        if (c.Version.AtLeast(140))
        {
            Section(c, value.Metadata, ref value.OverrideMetadata);
        }
        if (c.Version.In(112, 150))
        {
            Section(c, value.Mixer, ref value.OverrideMixer);
        }
        if (c.Version.AtLeast(72))
        {
            Section(c, value.OutputBus);
        }
        if (c.Version.AtLeast(72))
        {
            c.Id(ref value.Parent);
        }
        if (c.Version.In(72, 112))
        {
            Section(c, value.PlaybackPriority, ref value.OverridePlaybackPriority);
        }
        if (c.Version.AtLeast(112))
        {
            Section(c, value.Midi, value.PlaybackPriority, ref value.OverrideMidiEvent, ref value.OverrideMidiNoteTracking, ref value.OverridePlaybackPriority);
        }
        if (c.Version.AtLeast(72))
        {
            {
                var commonProperty = new CommonPropertyMap<AudioCommonPropertyType>();
                void ApplyCommonProperties()
                {
                if (c.Version.AtLeast(88))
                {
                    CommonProperty.ExchangeRandomizableFloat(c, commonProperty, AudioCommonPropertyType.PlaybackInitialDelay, value.PlaybackSetting.InitialDelay, 0.0d);
                }
                if (c.Version.AtLeast(72))
                {
                    LoadProperty(c, commonProperty, value.Voice);
                }
                if (c.Version.AtLeast(88))
                {
                    LoadProperty(c, commonProperty, value.VoiceVolumeGain);
                }
                if (c.Version.AtLeast(72))
                {
                    LoadProperty(c, commonProperty, value.OutputBus);
                }
                if (c.Version.AtLeast(72))
                {
                    LoadProperty(c, commonProperty, value.AuxiliarySend);
                }
                if (c.Version.AtLeast(72))
                {
                    LoadProperty(c, commonProperty, value.Positioning);
                }
                if (c.Version.AtLeast(88))
                {
                    LoadProperty(c, commonProperty, value.Hdr);
                }
                if (c.Version.AtLeast(112))
                {
                    LoadProperty(c, commonProperty, value.Midi);
                }
                if (c.Version.AtLeast(72))
                {
                    LoadProperty(c, commonProperty, value.PlaybackLimit);
                }
                if (c.Version.AtLeast(72))
                {
                    LoadProperty(c, commonProperty, value.VirtualVoice);
                }
                if (c.Version.AtLeast(72))
                {
                    LoadProperty(c, commonProperty, value.PlaybackPriority);
                }
                if (c.Version.In(72, 128))
                {
                    LoadProperty(c, commonProperty, value.Motion);
                }
                if (c.Version.In(112, 150))
                {
                    LoadProperty(c, commonProperty, value.Mixer);
                }
                }
                if (!c.Reading)
                {
                    ApplyCommonProperties();
                }
                CommonProperty.Read(c, commonProperty, AudioCommonPropertyTypeTable.Instance, true);
                if (c.Reading)
                {
                    ApplyCommonProperties();
                }
            }

        }
        if (c.Version.AtLeast(72))
        {
            Section(c, value.Positioning, ref value.OverridePositioning);
        }
        if (c.Version.In(72, 135))
        {
            Section(c, value.AuxiliarySend, ref value.OverrideGameDefinedAuxiliarySend, ref value.OverrideUserDefinedAuxiliarySend);
        }
        if (c.Version.AtLeast(135))
        {
            Section(c, value.AuxiliarySend, ref value.OverrideGameDefinedAuxiliarySend, ref value.OverrideUserDefinedAuxiliarySend, ref value.OverrideEarlyReflectionAuxiliarySend);
        }
        if (c.Version.AtLeast(72))
        {
            Section(c, value.PlaybackLimit, value.VirtualVoice, ref value.OverridePlaybackLimit, ref value.OverrideVirtualVoice);
        }
        if (c.Version.AtLeast(88))
        {
            Section(c, value.VoiceVolumeGain, value.Hdr, ref value.OverrideVoiceVolumeLoudnessNormalization, ref value.OverrideHdrEnvelopeTracking);
        }
        if (c.Version.AtLeast(72))
        {
            Section(c, value.State);
        }
        if (c.Version.AtLeast(72))
        {
            Section(c, value.RealTimeParameterControl);
        }
        if (c.Version.AtLeast(72))
        {
            Section(c, ref value.PlaybackSetting.Scope, ref value.PlaybackSetting.Type, value.PlaybackSetting.TypeSetting, ref value.PlaybackSetting.Mode, value.PlaybackSetting.ModeSetting);
        }
        if (c.Version.AtLeast(72))
        {
            Section(c, value.Child);
        }
        if (c.Version.AtLeast(72))
        {
            Section(c, value.PlaybackSetting.Playlist);
        }
    }

    public static void Section(BankContext c, SoundSwitchContainer value)
    {
        c.Id(ref value.Identifier);
        if (c.Version.AtLeast(72))
        {
            Section(c, value.Effect, ref value.OverrideEffect);
        }
        if (c.Version.AtLeast(140))
        {
            Section(c, value.Metadata, ref value.OverrideMetadata);
        }
        if (c.Version.In(112, 150))
        {
            Section(c, value.Mixer, ref value.OverrideMixer);
        }
        if (c.Version.AtLeast(72))
        {
            Section(c, value.OutputBus);
        }
        if (c.Version.AtLeast(72))
        {
            c.Id(ref value.Parent);
        }
        if (c.Version.In(72, 112))
        {
            Section(c, value.PlaybackPriority, ref value.OverridePlaybackPriority);
        }
        if (c.Version.AtLeast(112))
        {
            Section(c, value.Midi, value.PlaybackPriority, ref value.OverrideMidiEvent, ref value.OverrideMidiNoteTracking, ref value.OverridePlaybackPriority);
        }
        if (c.Version.AtLeast(72))
        {
            {
                var commonProperty = new CommonPropertyMap<AudioCommonPropertyType>();
                void ApplyCommonProperties()
                {
                if (c.Version.AtLeast(88))
                {
                    CommonProperty.ExchangeRandomizableFloat(c, commonProperty, AudioCommonPropertyType.PlaybackInitialDelay, value.PlaybackSetting.InitialDelay, 0.0d);
                }
                if (c.Version.AtLeast(72))
                {
                    LoadProperty(c, commonProperty, value.Voice);
                }
                if (c.Version.AtLeast(88))
                {
                    LoadProperty(c, commonProperty, value.VoiceVolumeGain);
                }
                if (c.Version.AtLeast(72))
                {
                    LoadProperty(c, commonProperty, value.OutputBus);
                }
                if (c.Version.AtLeast(72))
                {
                    LoadProperty(c, commonProperty, value.AuxiliarySend);
                }
                if (c.Version.AtLeast(72))
                {
                    LoadProperty(c, commonProperty, value.Positioning);
                }
                if (c.Version.AtLeast(88))
                {
                    LoadProperty(c, commonProperty, value.Hdr);
                }
                if (c.Version.AtLeast(112))
                {
                    LoadProperty(c, commonProperty, value.Midi);
                }
                if (c.Version.AtLeast(72))
                {
                    LoadProperty(c, commonProperty, value.PlaybackLimit);
                }
                if (c.Version.AtLeast(72))
                {
                    LoadProperty(c, commonProperty, value.VirtualVoice);
                }
                if (c.Version.AtLeast(72))
                {
                    LoadProperty(c, commonProperty, value.PlaybackPriority);
                }
                if (c.Version.In(72, 128))
                {
                    LoadProperty(c, commonProperty, value.Motion);
                }
                if (c.Version.In(112, 150))
                {
                    LoadProperty(c, commonProperty, value.Mixer);
                }
                }
                if (!c.Reading)
                {
                    ApplyCommonProperties();
                }
                CommonProperty.Read(c, commonProperty, AudioCommonPropertyTypeTable.Instance, true);
                if (c.Reading)
                {
                    ApplyCommonProperties();
                }
            }

        }
        if (c.Version.AtLeast(72))
        {
            Section(c, value.Positioning, ref value.OverridePositioning);
        }
        if (c.Version.In(72, 135))
        {
            Section(c, value.AuxiliarySend, ref value.OverrideGameDefinedAuxiliarySend, ref value.OverrideUserDefinedAuxiliarySend);
        }
        if (c.Version.AtLeast(135))
        {
            Section(c, value.AuxiliarySend, ref value.OverrideGameDefinedAuxiliarySend, ref value.OverrideUserDefinedAuxiliarySend, ref value.OverrideEarlyReflectionAuxiliarySend);
        }
        if (c.Version.AtLeast(72))
        {
            Section(c, value.PlaybackLimit, value.VirtualVoice, ref value.OverridePlaybackLimit, ref value.OverrideVirtualVoice);
        }
        if (c.Version.AtLeast(88))
        {
            Section(c, value.VoiceVolumeGain, value.Hdr, ref value.OverrideVoiceVolumeLoudnessNormalization, ref value.OverrideHdrEnvelopeTracking);
        }
        if (c.Version.AtLeast(72))
        {
            Section(c, value.State);
        }
        if (c.Version.AtLeast(72))
        {
            Section(c, value.RealTimeParameterControl);
        }
        if (c.Version.AtLeast(72))
        {
            Section(c, value.PlaybackSetting.Switcher);
        }
        if (c.Version.AtLeast(72))
        {
            Section(c, value.PlaybackSetting.Mode);
        }
        if (c.Version.AtLeast(72))
        {
            Section(c, value.Child);
        }
        if (c.Version.AtLeast(72))
        {
            Section(c, value.PlaybackSetting.ObjectAssign);
        }
        if (c.Version.AtLeast(72))
        {
            Section(c, value.PlaybackSetting.ObjectAttribute);
        }
    }

    public static void Section(BankContext c, SoundBlendContainer value)
    {
        c.Id(ref value.Identifier);
        if (c.Version.AtLeast(72))
        {
            Section(c, value.Effect, ref value.OverrideEffect);
        }
        if (c.Version.AtLeast(140))
        {
            Section(c, value.Metadata, ref value.OverrideMetadata);
        }
        if (c.Version.In(112, 150))
        {
            Section(c, value.Mixer, ref value.OverrideMixer);
        }
        if (c.Version.AtLeast(72))
        {
            Section(c, value.OutputBus);
        }
        if (c.Version.AtLeast(72))
        {
            c.Id(ref value.Parent);
        }
        if (c.Version.In(72, 112))
        {
            Section(c, value.PlaybackPriority, ref value.OverridePlaybackPriority);
        }
        if (c.Version.AtLeast(112))
        {
            Section(c, value.Midi, value.PlaybackPriority, ref value.OverrideMidiEvent, ref value.OverrideMidiNoteTracking, ref value.OverridePlaybackPriority);
        }
        if (c.Version.AtLeast(72))
        {
            {
                var commonProperty = new CommonPropertyMap<AudioCommonPropertyType>();
                void ApplyCommonProperties()
                {
                if (c.Version.AtLeast(88))
                {
                    CommonProperty.ExchangeRandomizableFloat(c, commonProperty, AudioCommonPropertyType.PlaybackInitialDelay, value.PlaybackSetting.InitialDelay, 0.0d);
                }
                if (c.Version.AtLeast(72))
                {
                    LoadProperty(c, commonProperty, value.Voice);
                }
                if (c.Version.AtLeast(88))
                {
                    LoadProperty(c, commonProperty, value.VoiceVolumeGain);
                }
                if (c.Version.AtLeast(72))
                {
                    LoadProperty(c, commonProperty, value.OutputBus);
                }
                if (c.Version.AtLeast(72))
                {
                    LoadProperty(c, commonProperty, value.AuxiliarySend);
                }
                if (c.Version.AtLeast(72))
                {
                    LoadProperty(c, commonProperty, value.Positioning);
                }
                if (c.Version.AtLeast(88))
                {
                    LoadProperty(c, commonProperty, value.Hdr);
                }
                if (c.Version.AtLeast(112))
                {
                    LoadProperty(c, commonProperty, value.Midi);
                }
                if (c.Version.AtLeast(72))
                {
                    LoadProperty(c, commonProperty, value.PlaybackLimit);
                }
                if (c.Version.AtLeast(72))
                {
                    LoadProperty(c, commonProperty, value.VirtualVoice);
                }
                if (c.Version.AtLeast(72))
                {
                    LoadProperty(c, commonProperty, value.PlaybackPriority);
                }
                if (c.Version.In(72, 128))
                {
                    LoadProperty(c, commonProperty, value.Motion);
                }
                if (c.Version.In(112, 150))
                {
                    LoadProperty(c, commonProperty, value.Mixer);
                }
                }
                if (!c.Reading)
                {
                    ApplyCommonProperties();
                }
                CommonProperty.Read(c, commonProperty, AudioCommonPropertyTypeTable.Instance, true);
                if (c.Reading)
                {
                    ApplyCommonProperties();
                }
            }

        }
        if (c.Version.AtLeast(72))
        {
            Section(c, value.Positioning, ref value.OverridePositioning);
        }
        if (c.Version.In(72, 135))
        {
            Section(c, value.AuxiliarySend, ref value.OverrideGameDefinedAuxiliarySend, ref value.OverrideUserDefinedAuxiliarySend);
        }
        if (c.Version.AtLeast(135))
        {
            Section(c, value.AuxiliarySend, ref value.OverrideGameDefinedAuxiliarySend, ref value.OverrideUserDefinedAuxiliarySend, ref value.OverrideEarlyReflectionAuxiliarySend);
        }
        if (c.Version.AtLeast(72))
        {
            Section(c, value.PlaybackLimit, value.VirtualVoice, ref value.OverridePlaybackLimit, ref value.OverrideVirtualVoice);
        }
        if (c.Version.AtLeast(88))
        {
            Section(c, value.VoiceVolumeGain, value.Hdr, ref value.OverrideVoiceVolumeLoudnessNormalization, ref value.OverrideHdrEnvelopeTracking);
        }
        if (c.Version.AtLeast(72))
        {
            Section(c, value.State);
        }
        if (c.Version.AtLeast(72))
        {
            Section(c, value.RealTimeParameterControl);
        }
        if (c.Version.AtLeast(72))
        {
            Section(c, value.Child);
        }
        if (c.Version.AtLeast(72))
        {
            Section(c, value.PlaybackSetting.Track);
        }
        if (c.Version.AtLeast(120))
        {
            Section(c, value.PlaybackSetting.Mode);
        }
    }
}
