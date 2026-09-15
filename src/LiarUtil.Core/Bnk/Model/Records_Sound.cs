namespace LiarUtil.Core.Bnk.Model;

internal sealed class SoundPlaybackSetting
{
    public RandomizableValue<long> Loop = new();
    public RandomizableValue<double> InitialDelay = new();
}

internal sealed class Sound
{
    public long Identifier = 0L;
    public long Parent = 0L;
    public AudioSourceSetting Source = new();
    public SoundPlaybackSetting PlaybackSetting = new();
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

internal sealed class SoundPlaylistContainerPlaylistItem
{
    public long Item = 0L;
    public long Weight = 0L;
}

internal sealed class SoundPlaylistContainerPlaybackSetting
{
    public SoundPlaylistContainerScope Scope = default;
    public AudioPlayType Type = default;
    public AudioPlayTypeSetting TypeSetting = new();
    public AudioPlayMode Mode = default;
    public AudioPlayModeSetting ModeSetting = new();
    public List<SoundPlaylistContainerPlaylistItem> Playlist = [];
    public RandomizableValue<double> InitialDelay = new();
}

internal sealed class SoundPlaylistContainer
{
    public long Identifier = 0L;
    public long Parent = 0L;
    public List<long> Child = [];
    public SoundPlaylistContainerPlaybackSetting PlaybackSetting = new();
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

internal sealed class SoundSwitchContainerObjectAttributeItem
{
    public long Identifier = 0L;
    public bool PlayFirstOnly = false;
    public bool ContinueToPlayAcrossSwitch = false;
    public long U1 = 0L;
    public long FadeOutTime = 0L;
    public long FadeInTime = 0L;
}

internal sealed class SoundSwitchContainerObjectAssignItem
{
    public long Item = 0L;
    public List<long> Object = [];
}

internal sealed class SoundSwitchContainerPlaybackSetting
{
    public AudioPlayMode Mode = default;
    public AudioSwitcherSetting Switcher = new();
    public List<SoundSwitchContainerObjectAttributeItem> ObjectAttribute = [];
    public List<SoundSwitchContainerObjectAssignItem> ObjectAssign = [];
    public RandomizableValue<double> InitialDelay = new();
}

internal sealed class SoundSwitchContainer
{
    public long Identifier = 0L;
    public long Parent = 0L;
    public List<long> Child = [];
    public SoundSwitchContainerPlaybackSetting PlaybackSetting = new();
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

internal sealed class SoundBlendContainerTrackChildItem
{
    public long Identifier = 0L;
    public List<CoordinatePoint> Point = [];
}

internal sealed class SoundBlendContainerTrackItem
{
    public long Identifier = 0L;
    public RealTimeParameterControlSetting RealTimeParameterControl = new();
    public Parameter CrossFade = new();
    public List<SoundBlendContainerTrackChildItem> Child = [];
}

internal sealed class SoundBlendContainerPlaybackSetting
{
    public List<SoundBlendContainerTrackItem> Track = [];
    public RandomizableValue<double> InitialDelay = new();
    public AudioPlayMode Mode = default;
}

internal sealed class SoundBlendContainer
{
    public long Identifier = 0L;
    public long Parent = 0L;
    public List<long> Child = [];
    public SoundBlendContainerPlaybackSetting PlaybackSetting = new();
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
