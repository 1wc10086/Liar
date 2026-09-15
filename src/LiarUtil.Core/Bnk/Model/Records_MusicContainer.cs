namespace LiarUtil.Core.Bnk.Model;

internal sealed class ActorMixerPlaybackSetting
{
}

internal sealed class ActorMixer
{
    public long Identifier = 0L;
    public long Parent = 0L;
    public List<long> Child = [];
    public ActorMixerPlaybackSetting PlaybackSetting = new();
    public AudioVoice Voice = new();
    public AudioOutputBusSetting OutputBus = new();
    public AudioAuxiliarySendSetting AuxiliarySend = new();
    public AudioEffectSetting Effect = new();
    public AudioPositioningSetting Positioning = new();
    public RealTimeParameterControlSetting RealTimeParameterControl = new();
    public StateSetting State = new();
    public AudioPlaybackLimitSetting PlaybackLimit = new();
    public AudioVirtualVoiceSetting VirtualVoice = new();
    public AudioPlaybackPrioritySetting PlaybackPriority = new();
    public AudioMotionSetting Motion = new();
    public bool OverrideGameDefinedAuxiliarySend = false;
    public bool OverrideUserDefinedAuxiliarySend = false;
    public bool OverrideEffect = false;
    public bool OverridePositioning = false;
    public bool OverridePlaybackLimit = false;
    public bool OverrideVirtualVoice = false;
    public bool OverridePlaybackPriority = false;
    public AudioVoiceVolumeGainSetting VoiceVolumeGain = new();
    public AudioHdrSetting Hdr = new();
    public bool OverrideVoiceVolumeLoudnessNormalization = false;
    public bool OverrideHdrEnvelopeTracking = false;
    public SoundMidiSetting Midi = new();
    public AudioMixerSetting Mixer = new();
    public bool OverrideMidiEvent = false;
    public bool OverrideMidiNoteTracking = false;
    public bool OverrideMixer = false;
    public bool OverrideEarlyReflectionAuxiliarySend = false;
    public AudioMetadataSetting Metadata = new();
    public bool OverrideMetadata = false;
}

internal sealed class MusicTrackClipCurveItem
{
    public long Index = 0L;
    public MusicTrackClipCurveItemType Type = default;
    public List<CoordinatePoint> Point = [];
}

internal sealed class MusicTrackClipItem
{
    public long U1 = 0L;
    public long Source = 0L;
    public double Offset = 0d;
    public double Begin = 0d;
    public double End = 0d;
    public double Duration = 0d;
    public long Event = 0L;
}

internal sealed class MusicTrackClip
{
    public long U1 = 0L;
    public List<MusicTrackClipItem> Item = [];
    public List<MusicTrackClipCurveItem> Curve = [];
}

internal sealed class MusicTrackPlaybackSetting
{
    public MusicTrackClip Clip = new();
    public MusicTrackTrackType Type = default;
    public AudioSwitcherSetting Switcher = new();
    public MusicTrackTransitionSetting Transition = new();
}

internal sealed class MusicTrackStream
{
    public long LookAheadTime = 0L;
}

internal sealed class MusicTrack
{
    public long Identifier = 0L;
    public long Parent = 0L;
    public List<AudioSourceSetting> Source = [];
    public MusicTrackPlaybackSetting PlaybackSetting = new();
    public MusicTrackStream Stream = new();
    public AudioVoice Voice = new();
    public AudioOutputBusSetting OutputBus = new();
    public AudioAuxiliarySendSetting AuxiliarySend = new();
    public AudioEffectSetting Effect = new();
    public AudioPositioningSetting Positioning = new();
    public RealTimeParameterControlSetting RealTimeParameterControl = new();
    public StateSetting State = new();
    public AudioPlaybackLimitSetting PlaybackLimit = new();
    public AudioVirtualVoiceSetting VirtualVoice = new();
    public AudioPlaybackPrioritySetting PlaybackPriority = new();
    public AudioMotionSetting Motion = new();
    public bool OverrideGameDefinedAuxiliarySend = false;
    public bool OverrideUserDefinedAuxiliarySend = false;
    public bool OverrideEffect = false;
    public bool OverridePositioning = false;
    public bool OverridePlaybackLimit = false;
    public bool OverrideVirtualVoice = false;
    public bool OverridePlaybackPriority = false;
    public AudioVoiceVolumeGainSetting VoiceVolumeGain = new();
    public AudioHdrSetting Hdr = new();
    public bool OverrideVoiceVolumeLoudnessNormalization = false;
    public bool OverrideHdrEnvelopeTracking = false;
    public MusicMidiSetting Midi = new();
    public AudioMixerSetting Mixer = new();
    public bool OverrideMidiTarget = false;
    public bool OverrideMidiClipTempo = false;
    public bool OverrideMixer = false;
    public bool OverrideEarlyReflectionAuxiliarySend = false;
    public AudioMetadataSetting Metadata = new();
    public bool OverrideMetadata = false;
}

internal sealed class MusicSegmentCueItem
{
    public long Name = 0L;
    public double Time = 0d;
}

internal sealed class MusicSegmentCue
{
    public List<MusicSegmentCueItem> Item = [];
}

internal sealed class MusicSegmentPlaybackSetting
{
    public double Duration = 0d;
    public MusicSegmentCue Cue = new();
    public RegularValue<double> Speed = new();
}

internal sealed class MusicSegment
{
    public long Identifier = 0L;
    public long Parent = 0L;
    public List<long> Child = [];
    public MusicSegmentPlaybackSetting PlaybackSetting = new();
    public AudioTimeSetting TimeSetting = new();
    public AudioVoice Voice = new();
    public AudioOutputBusSetting OutputBus = new();
    public AudioAuxiliarySendSetting AuxiliarySend = new();
    public AudioEffectSetting Effect = new();
    public AudioPositioningSetting Positioning = new();
    public RealTimeParameterControlSetting RealTimeParameterControl = new();
    public StateSetting State = new();
    public MusicStingerSetting Stinger = new();
    public AudioPlaybackLimitSetting PlaybackLimit = new();
    public AudioVirtualVoiceSetting VirtualVoice = new();
    public AudioPlaybackPrioritySetting PlaybackPriority = new();
    public AudioMotionSetting Motion = new();
    public bool OverrideTimeSetting = false;
    public bool OverrideGameDefinedAuxiliarySend = false;
    public bool OverrideUserDefinedAuxiliarySend = false;
    public bool OverrideEffect = false;
    public bool OverridePositioning = false;
    public bool OverridePlaybackLimit = false;
    public bool OverrideVirtualVoice = false;
    public bool OverridePlaybackPriority = false;
    public AudioVoiceVolumeGainSetting VoiceVolumeGain = new();
    public AudioHdrSetting Hdr = new();
    public bool OverrideVoiceVolumeLoudnessNormalization = false;
    public bool OverrideHdrEnvelopeTracking = false;
    public MusicMidiSetting Midi = new();
    public AudioMixerSetting Mixer = new();
    public bool OverrideMidiTarget = false;
    public bool OverrideMidiClipTempo = false;
    public bool OverrideMixer = false;
    public bool OverrideEarlyReflectionAuxiliarySend = false;
    public AudioMetadataSetting Metadata = new();
    public bool OverrideMetadata = false;
}

internal sealed class MusicPlaylistContainerPlaylistItem
{
    public long U1 = 0L;
    public bool Group = false;
    public long ChildCount = 0L;
    public long Item = 0L;
    public AudioPlayType PlayType = default;
    public AudioPlayMode PlayMode = default;
    public AudioPlayTypeRandomSetting RandomSetting = new();
    public long Weight = 0L;
    public long Loop = 0L;
}

internal sealed class MusicPlaylistContainerPlaybackSetting
{
    public List<MusicPlaylistContainerPlaylistItem> Playlist = [];
    public RegularValue<double> Speed = new();
}

internal sealed class MusicPlaylistContainer
{
    public long Identifier = 0L;
    public long Parent = 0L;
    public List<long> Child = [];
    public MusicPlaylistContainerPlaybackSetting PlaybackSetting = new();
    public AudioTimeSetting TimeSetting = new();
    public AudioVoice Voice = new();
    public AudioOutputBusSetting OutputBus = new();
    public AudioAuxiliarySendSetting AuxiliarySend = new();
    public AudioEffectSetting Effect = new();
    public AudioPositioningSetting Positioning = new();
    public RealTimeParameterControlSetting RealTimeParameterControl = new();
    public StateSetting State = new();
    public MusicTransitionSetting Transition = new();
    public MusicStingerSetting Stinger = new();
    public AudioPlaybackLimitSetting PlaybackLimit = new();
    public AudioVirtualVoiceSetting VirtualVoice = new();
    public AudioPlaybackPrioritySetting PlaybackPriority = new();
    public AudioMotionSetting Motion = new();
    public bool OverrideTimeSetting = false;
    public bool OverrideGameDefinedAuxiliarySend = false;
    public bool OverrideUserDefinedAuxiliarySend = false;
    public bool OverrideEffect = false;
    public bool OverridePositioning = false;
    public bool OverridePlaybackLimit = false;
    public bool OverrideVirtualVoice = false;
    public bool OverridePlaybackPriority = false;
    public AudioVoiceVolumeGainSetting VoiceVolumeGain = new();
    public AudioHdrSetting Hdr = new();
    public bool OverrideVoiceVolumeLoudnessNormalization = false;
    public bool OverrideHdrEnvelopeTracking = false;
    public MusicMidiSetting Midi = new();
    public AudioMixerSetting Mixer = new();
    public bool OverrideMidiTarget = false;
    public bool OverrideMidiClipTempo = false;
    public bool OverrideMixer = false;
    public bool OverrideEarlyReflectionAuxiliarySend = false;
    public AudioMetadataSetting Metadata = new();
    public bool OverrideMetadata = false;
}

internal sealed class MusicSwitchContainerAssociationItem
{
    public long Item = 0L;
    public long Child = 0L;
}

internal sealed class MusicSwitchContainerPlaybackSetting
{
    public bool ContinuePlayingOnSwitchChange = false;
    public AudioSwitcherSetting Switcher = new();
    public AudioAssociationSetting Association = new();
    public RegularValue<double> Speed = new();
}

internal sealed class MusicSwitchContainer
{
    public long Identifier = 0L;
    public long Parent = 0L;
    public List<long> Child = [];
    public MusicSwitchContainerPlaybackSetting PlaybackSetting = new();
    public AudioTimeSetting TimeSetting = new();
    public AudioVoice Voice = new();
    public AudioOutputBusSetting OutputBus = new();
    public AudioAuxiliarySendSetting AuxiliarySend = new();
    public AudioEffectSetting Effect = new();
    public AudioPositioningSetting Positioning = new();
    public RealTimeParameterControlSetting RealTimeParameterControl = new();
    public StateSetting State = new();
    public MusicTransitionSetting Transition = new();
    public MusicStingerSetting Stinger = new();
    public AudioPlaybackLimitSetting PlaybackLimit = new();
    public AudioVirtualVoiceSetting VirtualVoice = new();
    public AudioPlaybackPrioritySetting PlaybackPriority = new();
    public AudioMotionSetting Motion = new();
    public bool OverrideTimeSetting = false;
    public bool OverrideGameDefinedAuxiliarySend = false;
    public bool OverrideUserDefinedAuxiliarySend = false;
    public bool OverrideEffect = false;
    public bool OverridePositioning = false;
    public bool OverridePlaybackLimit = false;
    public bool OverrideVirtualVoice = false;
    public bool OverridePlaybackPriority = false;
    public AudioVoiceVolumeGainSetting VoiceVolumeGain = new();
    public AudioHdrSetting Hdr = new();
    public bool OverrideVoiceVolumeLoudnessNormalization = false;
    public bool OverrideHdrEnvelopeTracking = false;
    public MusicMidiSetting Midi = new();
    public AudioMixerSetting Mixer = new();
    public bool OverrideMidiTarget = false;
    public bool OverrideMidiClipTempo = false;
    public bool OverrideMixer = false;
    public bool OverrideEarlyReflectionAuxiliarySend = false;
    public AudioMetadataSetting Metadata = new();
    public bool OverrideMetadata = false;
}
