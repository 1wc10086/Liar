namespace LiarUtil.Core.Bnk.Codec;

using LiarUtil.Core.Bnk.Model;

internal static partial class BankCodecImpl
{
    public static void Section(BankContext c, MusicTrackClip clipValue)
    {
        if (c.Version.AtLeast(72))
        {
            c.List(clipValue.Item, SizeKind.U32,
    static (BankContext ctx18, ref MusicTrackClipItem item18) =>
            {
                if (ctx18.Version.AtLeast(72))
                {
                    ctx18.U32(ref item18.U1);
                }
                if (ctx18.Version.AtLeast(72))
                {
                    ctx18.Id(ref item18.Source);
                }
                if (ctx18.Version.AtLeast(140))
                {
                    ctx18.Id(ref item18.Event);
                }
                if (ctx18.Version.AtLeast(72))
                {
                    ctx18.F64(ref item18.Offset);
                }
                if (ctx18.Version.AtLeast(72))
                {
                    ctx18.F64(ref item18.Begin);
                }
                if (ctx18.Version.AtLeast(72))
                {
                    ctx18.F64(ref item18.End);
                }
                if (ctx18.Version.AtLeast(72))
                {
                    ctx18.F64(ref item18.Duration);
                }
            }
            );
        }
        if (c.Version.AtLeast(72))
        {
            if (!(clipValue.Item.Count == 0))
            {
                c.U32(ref clipValue.U1);
            }
        }
        if (c.Version.AtLeast(72))
        {
            c.List(clipValue.Curve, SizeKind.U32,
    static (BankContext ctx19, ref MusicTrackClipCurveItem item19) =>
            {
                if (ctx19.Version.AtLeast(72))
                {
                    ctx19.U32(ref item19.Index);
                }
                if (ctx19.Version.AtLeast(72))
                {
                    var bits74 = ctx19.Bits32();
                    bits74.Enum(ref item19.Type, MusicTrackClipCurveItemTypeWire.Instance);
                    bits74.Done();
                }
                if (ctx19.Version.AtLeast(72))
                {
                    ctx19.List(item19.Point, SizeKind.U32,
    static (BankContext ctx20, ref CoordinatePoint item20) =>
                    {
                        if (ctx20.Version.AtLeast(72))
                        {
                            ctx20.F32(ref item20.Position.X);
                        }
                        if (ctx20.Version.AtLeast(72))
                        {
                            ctx20.F32(ref item20.Position.Y);
                        }
                        if (ctx20.Version.AtLeast(72))
                        {
                            var bits75 = ctx20.Bits32();
                            bits75.Enum(ref item20.Curve, CurveWire.Instance);
                            bits75.Done();
                        }
                    }
                    );
                }
            }
            );
        }
    }

    public static void Section(BankContext c, MusicTrackTrackType trackTypeValue)
    {
        if (c.Version.In(72, 112))
        {
            var bits108 = c.Bits32();
            bits108.Enum(ref trackTypeValue, MusicTrackTrackTypeWire.Instance);
            bits108.Done();
        }
    }

    public static void Section(BankContext c, MusicTrackTrackType trackTypeValue, AudioSwitcherSetting switcherValue, MusicTrackTransitionSetting transitionValue)
    {
        if (c.Version.AtLeast(112))
        {
            var bits109 = c.Bits8();
            bits109.Enum(ref trackTypeValue, MusicTrackTrackTypeWire.Instance);
            bits109.Done();
        }
        if (c.Version.AtLeast(112))
        {
            if (trackTypeValue == MusicTrackTrackType.Switcher)
            {
                if (c.Version.AtLeast(112))
                {
                    Section(c, switcherValue);
                }
                if (c.Version.AtLeast(112))
                {
                    Section(c, transitionValue);
                }
            }
        }
    }

    public static void Section(BankContext c, MusicTrackStream streamValue)
    {
        if (c.Version.AtLeast(72))
        {
            c.U16(ref streamValue.LookAheadTime);
        }
    }

    public static void Section(BankContext c, MusicSegmentCue cueValue)
    {
        if (c.Version.AtLeast(72))
        {
            c.List(cueValue.Item, SizeKind.U32,
    static (BankContext ctx31, ref MusicSegmentCueItem item31) =>
            {
                if (ctx31.Version.AtLeast(72))
                {
                    ctx31.Id(ref item31.Name);
                }
                if (ctx31.Version.AtLeast(72))
                {
                    ctx31.F64(ref item31.Time);
                }
                if (ctx31.Version.In(72, 140))
                {
                    ctx31.Const32(0u);
                }
                if (ctx31.Version.AtLeast(140))
                {
                    ctx31.Const8(0);
                }
            }
            );
        }
    }

    public static void Section(BankContext c, List<MusicPlaylistContainerPlaylistItem> playlistValue)
    {
        if (c.Version.AtLeast(72))
        {
            c.List(playlistValue, SizeKind.U32,
    static (BankContext ctx32, ref MusicPlaylistContainerPlaylistItem item32) =>
            {
                if (ctx32.Version.AtLeast(72))
                {
                    ctx32.Id(ref item32.Item);
                }
                if (ctx32.Version.AtLeast(72))
                {
                    ctx32.Id(ref item32.U1);
                }
                if (ctx32.Version.AtLeast(72))
                {
                    ctx32.U32(ref item32.ChildCount);
                }
                if (ctx32.Version.AtLeast(72))
                {
                    var bits110 = ctx32.Bits32();
                    bits110.Enum(ref item32.PlayMode, AudioPlayModeWire.Instance);
                    bits110.Enum(ref item32.PlayType, AudioPlayTypeWire.Instance);
                    bits110.Done(true);
                }
                if (ctx32.Version.AtLeast(72))
                {
                    ctx32.U16(ref item32.Loop);
                }
                if (ctx32.Version.AtLeast(112))
                {
                    ctx32.Const32(0u);
                }
                if (ctx32.Version.AtLeast(72))
                {
                    ctx32.U32(ref item32.Weight);
                }
                if (ctx32.Version.AtLeast(72))
                {
                    ctx32.U16(ref item32.RandomSetting.AvoidRepeat);
                }
                if (ctx32.Version.AtLeast(72))
                {
                    var bits111 = ctx32.Bits8();
                    bits111.Bit(ref item32.Group);
                    bits111.Done();
                }
                if (ctx32.Version.AtLeast(72))
                {
                    var bits112 = ctx32.Bits8();
                    bits112.Enum(ref item32.RandomSetting.Type, AudioPlayTypeRandomTypeWire.Instance);
                    bits112.Done();
                }
            }
            );
        }
    }

    public static void Section(BankContext c, List<MusicSwitchContainerAssociationItem> associationValue)
    {
        if (c.Version.In(72, 88))
        {
            c.List(associationValue, SizeKind.U32,
    static (BankContext ctx33, ref MusicSwitchContainerAssociationItem item33) =>
            {
                if (ctx33.Version.In(72, 88))
                {
                    ctx33.Id(ref item33.Item);
                }
                if (ctx33.Version.In(72, 88))
                {
                    ctx33.Id(ref item33.Child);
                }
            }
            );
        }
    }

    public static void Section(BankContext c, ActorMixer value)
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
    }

    public static void Section(BankContext c, MusicTrack value)
    {
        c.Id(ref value.Identifier);
        if (c.Version.AtLeast(112))
        {
            Section(c, value.Midi, ref value.OverrideMidiTarget, ref value.OverrideMidiClipTempo);
        }
        if (c.Version.AtLeast(72))
        {
            Section(c, value.Source);
        }
        if (c.Version.AtLeast(72))
        {
            Section(c, value.PlaybackSetting.Clip);
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
        if (c.Version.AtLeast(72))
        {
            Section(c, value.PlaybackPriority, ref value.OverridePlaybackPriority);
        }
        if (c.Version.AtLeast(72))
        {
            {
                var commonProperty = new CommonPropertyMap<AudioCommonPropertyType>();
                void ApplyCommonProperties()
                {
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
        if (c.Version.In(72, 112))
        {
            Section(c, value.PlaybackSetting.Type);
        }
        if (c.Version.AtLeast(112))
        {
            Section(c, value.PlaybackSetting.Type, value.PlaybackSetting.Switcher, value.PlaybackSetting.Transition);
        }
        if (c.Version.AtLeast(72))
        {
            Section(c, value.Stream);
        }
        if (c.Version.AtLeast(72))
        {
            c.Const16(0);
        }
    }

    public static void Section(BankContext c, MusicSegment value)
    {
        c.Id(ref value.Identifier);
        if (c.Version.AtLeast(112))
        {
            Section(c, value.Midi, ref value.OverrideMidiTarget, ref value.OverrideMidiClipTempo);
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
        if (c.Version.AtLeast(72))
        {
            Section(c, value.PlaybackPriority, ref value.OverridePlaybackPriority);
        }
        if (c.Version.AtLeast(72))
        {
            {
                var commonProperty = new CommonPropertyMap<AudioCommonPropertyType>();
                void ApplyCommonProperties()
                {
                if (c.Version.AtLeast(112))
                {
                    CommonProperty.ExchangeRegularFloat(c, commonProperty, AudioCommonPropertyType.PlaybackSpeed, value.PlaybackSetting.Speed, 1.0d);
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
            Section(c, value.TimeSetting, ref value.OverrideTimeSetting);
        }
        if (c.Version.AtLeast(72))
        {
            Section(c, value.Stinger);
        }
        if (c.Version.AtLeast(72))
        {
            c.F64(ref value.PlaybackSetting.Duration);
        }
        if (c.Version.AtLeast(72))
        {
            Section(c, value.PlaybackSetting.Cue);
        }
    }

    public static void Section(BankContext c, MusicPlaylistContainer value)
    {
        c.Id(ref value.Identifier);
        if (c.Version.AtLeast(112))
        {
            Section(c, value.Midi, ref value.OverrideMidiTarget, ref value.OverrideMidiClipTempo);
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
        if (c.Version.AtLeast(72))
        {
            Section(c, value.PlaybackPriority, ref value.OverridePlaybackPriority);
        }
        if (c.Version.AtLeast(72))
        {
            {
                var commonProperty = new CommonPropertyMap<AudioCommonPropertyType>();
                void ApplyCommonProperties()
                {
                if (c.Version.AtLeast(112))
                {
                    CommonProperty.ExchangeRegularFloat(c, commonProperty, AudioCommonPropertyType.PlaybackSpeed, value.PlaybackSetting.Speed, 1.0d);
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
            Section(c, value.TimeSetting, ref value.OverrideTimeSetting);
        }
        if (c.Version.AtLeast(72))
        {
            Section(c, value.Stinger);
        }
        if (c.Version.AtLeast(72))
        {
            Section(c, value.Transition);
        }
        if (c.Version.AtLeast(72))
        {
            Section(c, value.PlaybackSetting.Playlist);
        }
    }

    public static void Section(BankContext c, MusicSwitchContainer value)
    {
        c.Id(ref value.Identifier);
        if (c.Version.AtLeast(112))
        {
            Section(c, value.Midi, ref value.OverrideMidiTarget, ref value.OverrideMidiClipTempo);
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
        if (c.Version.AtLeast(72))
        {
            Section(c, value.PlaybackPriority, ref value.OverridePlaybackPriority);
        }
        if (c.Version.AtLeast(72))
        {
            {
                var commonProperty = new CommonPropertyMap<AudioCommonPropertyType>();
                void ApplyCommonProperties()
                {
                if (c.Version.AtLeast(112))
                {
                    CommonProperty.ExchangeRegularFloat(c, commonProperty, AudioCommonPropertyType.PlaybackSpeed, value.PlaybackSetting.Speed, 1.0d);
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
            Section(c, value.TimeSetting, ref value.OverrideTimeSetting);
        }
        if (c.Version.AtLeast(72))
        {
            Section(c, value.Stinger);
        }
        if (c.Version.AtLeast(72))
        {
            Section(c, value.Transition);
        }
        if (c.Version.In(72, 88))
        {
            Section(c, value.PlaybackSetting.Switcher);
        }
        if (c.Version.AtLeast(72))
        {
            var bits125 = c.Bits8();
            bits125.Bit(ref value.PlaybackSetting.ContinuePlayingOnSwitchChange);
            bits125.Done();
        }
        if (c.Version.AtLeast(72))
        {
            Section(c, value.PlaybackSetting.Association);
        }
    }
}
