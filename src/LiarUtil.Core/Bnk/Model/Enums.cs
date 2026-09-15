namespace LiarUtil.Core.Bnk.Model;

internal enum Curve : byte
{
    Constant,
    Linear,
    S,
    SInverted,
    Sine,
    SineReciprocal,
    Logarithmic1dot41,
    Logarithmic3dot0,
    Exponential1dot41,
    Exponential3dot0,
}

internal enum TimePoint : byte
{
    Immediate,
    NextGrid,
    NextBar,
    NextBeat,
    NextCue,
    CustomCue,
    EntryCue,
    ExitCue,
    LastExitPosition,
}

internal enum CoordinateMode : byte
{
    Linear,
    Scaled,
    Scaled3,
}

internal enum PropertyCategory : byte
{
    Unidirectional,
    Bidirectional,
    BidirectionalRanged,
    Boolean,
    Unknown6,
}

internal enum ParameterCategory : byte
{
    GameParameter,
    MidiParameter,
    Modulator,
}

internal enum AudioPlayType : byte
{
    Sequence,
    Random,
}

internal enum AudioPlayMode : byte
{
    Step,
    Continuous,
}

internal enum AudioPositioningSettingListenerRoutingPositionSourceMode : byte
{
    UserDefined,
    GameDefined,
    Emitter,
    EmitterWithAutomation,
    ListenerWithAutomation,
}

internal enum AudioPositioningSettingListenerRoutingSpatialization : byte
{
    None,
    Position,
    PositionAndOrientation,
}

internal enum AudioPositioningSettingSpeakerPanningMode : byte
{
    DirectAssignment,
    BalanceFade,
    Steering,
}

internal enum AudioPositioningSettingType : byte
{
    TwoDimension,
    ThreeDimension,
}

internal enum BusAutomaticDuckingSettingBusTarget : byte
{
    VoiceVolume,
    BusVolume,
}

internal enum MusicTransitionSettingJumpMode : byte
{
    Start,
    Specific,
    Next,
    LastPlayed,
}

internal enum MusicTransitionSettingSynchronizeMode : byte
{
    EntryCue,
    RandomCue,
    CustomCue,
    SameTimeAsPlayingSegment,
}

internal enum BusHdrSettingDynamicReleaseMode : byte
{
    Linear,
    Exponential,
}

internal enum SoundMidiSettingEventPlayOn : byte
{
    NoteOn,
    NoteOff,
}

internal enum MusicMidiSettingClipTempoSource : byte
{
    Hierarchy,
    File,
}

internal enum AudioPlaybackLimitSettingScope : byte
{
    PerGameObject,
    Globally,
}

internal enum AudioPlaybackLimitSettingWhenPriorityIsEqual : byte
{
    DiscardOldestInstance,
    DiscardNewestInstance,
}

internal enum AudioPlaybackLimitSettingWhenLimitIsReached : byte
{
    KillVoice,
    UseVirtualVoiceSetting,
}

internal enum AudioVirtualVoiceSettingBehavior : byte
{
    ContinueToPlay,
    KillVoice,
    SendToVirtualVoice,
    KillIfFiniteElseVirtual,
}

internal enum AudioVirtualVoiceSettingOnReturnToPhysical : byte
{
    PlayFromBeginning,
    PlayFromElapsedTime,
    Resume,
}

internal enum AudioAssociationSettingMode : byte
{
    BestMatch,
    Weighted,
}

internal enum AudioSourceType : byte
{
    Embedded,
    Streamed,
    StreamedPrefetched,
}

internal enum AudioPlayTypeRandomType : byte
{
    Standard,
    Shuffle,
}

internal enum AudioPlayTypeSequenceAtEndOfPlaylist : byte
{
    Restart,
    PlayInReserveOrder,
}

internal enum AudioPlayModeContinuousTransitionType : byte
{
    None,
    XfadeAmp,
    XfadePower,
    Delay,
    SampleAccurate,
    TriggerRate,
}

internal enum GameParameterBindToBuiltInParameterMode : byte
{
    None,
    Distance,
    Azimuth,
    Elevation,
    ObjectToListenerAngle,
    Obstruction,
    Occlusion,
    EmitterCone,
    ListenerCone,
    Diffraction,
}

internal enum GameParameterInterpolationMode : byte
{
    None,
    SlewRate,
    FilteringOverTime,
}

internal enum EventActionPropertyValueApplyMode : byte
{
    Absolute,
    Relative,
}

internal enum EventActionPropertySeekType : byte
{
    Time,
    Percent,
}

internal enum EventActionPropertyType : byte
{
    PlayAudio,
    StopAudio,
    PauseAudio,
    ResumeAudio,
    BreakAudio,
    SeekAudio,
    SetBusVolume,
    SetVoiceVolume,
    SetVoicePitch,
    SetVoiceLowPassFilter,
    SetMute,
    SetGameParameter,
    SetStateAvailability,
    ActivateState,
    ActivateSwitch,
    ActivateTrigger,
    SetBypassEffect,
    SetVoiceHighPassFilter,
    ReleaseEnvelope,
    PostEvent,
    ResetPlaylist,
}

internal enum EventActionMode : byte
{
    None,
    One,
    All,
    AllExcept,
}

internal enum EventActionScope : byte
{
    Global,
    GameObject,
}

internal enum ModulatorScope : byte
{
    Voice,
    NoteOrEvent,
    GameObject,
    Global,
}

internal enum ModulatorTriggerOn : byte
{
    Play,
    NoteOff,
}

internal enum ModulatorWaveform : byte
{
    Sine,
    Triangle,
    Square,
    SawUp,
    SawDown,
    Random,
}

internal enum SoundPlaylistContainerScope : byte
{
    GameObject,
    Global,
}

internal enum MusicTrackTrackType : byte
{
    Normal,
    RandomStep,
    SequenceStep,
    Switcher,
}

internal enum MusicTrackClipCurveItemType : byte
{
    VoiceVolume,
    VoiceLowPassFilter,
    ClipFadeIn,
    ClipFadeOut,
    VoiceHighPassFilter,
}

internal enum HierarchyType : byte
{
    Unknown,
    StatefulPropertySetting,
    EventAction,
    Event,
    DialogueEvent,
    Attenuation,
    Effect,
    Source,
    AudioBus,
    AuxiliaryAudioBus,
    Sound,
    SoundPlaylistContainer,
    SoundSwitchContainer,
    SoundBlendContainer,
    ActorMixer,
    MusicTrack,
    MusicSegment,
    MusicPlaylistContainer,
    MusicSwitchContainer,
    LowFrequencyOscillatorModulator,
    EnvelopeModulator,
    AudioDevice,
    TimeModulator,
}

internal enum VoiceFilterBehavior : byte
{
    SumAllValue,
    UseHighestValue,
}

internal enum EventActionCommonPropertyType : byte
{
    Delay,
    FadeTime,
    Probability,
}

internal enum ModulatorCommonPropertyType : byte
{
    Scope,
    TriggerOn,
    Depth,
    Frequency,
    Waveform,
    Smoothing,
    PulseWidthModulation,
    Attack,
    InitialPhaseOffset,
    AttackTime,
    AttackCurve,
    DecayTime,
    SustainLevel,
    ReleaseTime,
    SustainTime,
    InitialDelay,
    Duration,
    Loop,
    PlaybackRate,
    StopPlayback,
}

internal enum AudioCommonPropertyType : byte
{
    BusVolume,
    OutputBusVolume,
    OutputBusLowPassFilter,
    VoiceVolume,
    VoicePitch,
    VoiceLowPassFilter,
    GameDefinedAuxiliarySendVolume,
    UserDefinedAuxiliarySendVolume0,
    UserDefinedAuxiliarySendVolume1,
    UserDefinedAuxiliarySendVolume2,
    UserDefinedAuxiliarySendVolume3,
    PositioningCenterPercent,
    PositioningSpeakerPanningX,
    PositioningSpeakerPanningY,
    PlaybackPriorityValue,
    PlaybackPriorityOffsetAtMaximumDistance,
    PlaybackLoop,
    MotionVolumeOffset,
    MotionLowPassFilter,
    VoiceVolumeMakeUpGain,
    HdrThreshold,
    HdrRatio,
    HdrReleaseTime,
    HdrWindowTapOutputGameParameterIdentifier,
    HdrWindowTapOutputGameParameterMinimum,
    HdrWindowTapOutputGameParameterMaximum,
    HdrEnvelopeTrackingActiveRange,
    PlaybackInitialDelay,
    OutputBusHighPassFilter,
    VoiceHighPassFilter,
    MidiNoteTrackingRootNote,
    MidiEventPlayOn,
    MidiTransformationTransposition,
    MidiTransformationVelocityOffset,
    MidiFilterKeyRangeMinimum,
    MidiFilterKeyRangeMaximum,
    MidiFilterVelocityMinimum,
    MidiFilterVelocityMaximum,
    MidiFilterChannel,
    MidiClipTempoSource,
    MidiTargetIdentifier,
    PlaybackSpeed,
    MixerIdentifier,
    GameDefinedAuxiliarySendLowPassFilter,
    GameDefinedAuxiliarySendHighPassFilter,
    UserDefinedAuxiliarySendLowPassFilter0,
    UserDefinedAuxiliarySendLowPassFilter1,
    UserDefinedAuxiliarySendLowPassFilter2,
    UserDefinedAuxiliarySendLowPassFilter3,
    UserDefinedAuxiliarySendHighPassFilter0,
    UserDefinedAuxiliarySendHighPassFilter1,
    UserDefinedAuxiliarySendHighPassFilter2,
    UserDefinedAuxiliarySendHighPassFilter3,
    PositioningListenerRoutingSpeakerPanningDivisionSpatializationMix,
    PositioningListenerRoutingAttenuationIdentifier,
    EarlyReflectionAuxiliarySendVolume,
    PositioningSpeakerPanningZ,
}
