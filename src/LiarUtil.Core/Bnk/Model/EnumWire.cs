namespace LiarUtil.Core.Bnk.Model;

internal sealed class CurveWire : IWireEnum<Curve>
{
    public static readonly CurveWire Instance = new();

    public int Width(BankVersion version) => 4;
    public Curve FromRaw(BankVersion version, int raw) =>
        raw switch
        {
            0 => Curve.Logarithmic3dot0,
            1 => Curve.Sine,
            2 => Curve.Logarithmic1dot41,
            3 => Curve.SInverted,
            4 => Curve.Linear,
            5 => Curve.S,
            6 => Curve.Exponential1dot41,
            7 => Curve.SineReciprocal,
            8 => Curve.Exponential3dot0,
            9 => Curve.Constant,
            _ => default,
        };
    public int ToRaw(BankVersion version, Curve value) =>
        value switch
        {
            Curve.Logarithmic3dot0 => 0,
            Curve.Sine => 1,
            Curve.Logarithmic1dot41 => 2,
            Curve.SInverted => 3,
            Curve.Linear => 4,
            Curve.S => 5,
            Curve.Exponential1dot41 => 6,
            Curve.SineReciprocal => 7,
            Curve.Exponential3dot0 => 8,
            Curve.Constant => 9,
            _ => 0,
        };
}

internal sealed class TimePointWire : IWireEnum<TimePoint>
{
    public static readonly TimePointWire Instance = new();

    public int Width(BankVersion version) => version.Number >= 140 ? 4 : 3;
    public TimePoint FromRaw(BankVersion version, int raw) =>
        version.Number >= 140 ? raw switch
        {
            0 => TimePoint.Immediate,
            1 => TimePoint.NextGrid,
            2 => TimePoint.NextBar,
            3 => TimePoint.NextBeat,
            4 => TimePoint.NextCue,
            5 => TimePoint.CustomCue,
            6 => TimePoint.EntryCue,
            7 => TimePoint.ExitCue,
            9 => TimePoint.LastExitPosition,
            _ => default,
        } : raw switch
        {
            0 => TimePoint.Immediate,
            1 => TimePoint.NextGrid,
            2 => TimePoint.NextBar,
            3 => TimePoint.NextBeat,
            4 => TimePoint.NextCue,
            5 => TimePoint.CustomCue,
            6 => TimePoint.EntryCue,
            7 => TimePoint.ExitCue,
            _ => default,
        };
    public int ToRaw(BankVersion version, TimePoint value) =>
        version.Number >= 140 ? value switch
        {
            TimePoint.Immediate => 0,
            TimePoint.NextGrid => 1,
            TimePoint.NextBar => 2,
            TimePoint.NextBeat => 3,
            TimePoint.NextCue => 4,
            TimePoint.CustomCue => 5,
            TimePoint.EntryCue => 6,
            TimePoint.ExitCue => 7,
            TimePoint.LastExitPosition => 9,
            _ => 0,
        } : value switch
        {
            TimePoint.Immediate => 0,
            TimePoint.NextGrid => 1,
            TimePoint.NextBar => 2,
            TimePoint.NextBeat => 3,
            TimePoint.NextCue => 4,
            TimePoint.CustomCue => 5,
            TimePoint.EntryCue => 6,
            TimePoint.ExitCue => 7,
            _ => 0,
        };
}

internal sealed class CoordinateModeWire : IWireEnum<CoordinateMode>
{
    public static readonly CoordinateModeWire Instance = new();

    public int Width(BankVersion version) => 2;
    public CoordinateMode FromRaw(BankVersion version, int raw) =>
        raw switch
        {
            0 => CoordinateMode.Linear,
            2 => CoordinateMode.Scaled,
            3 => CoordinateMode.Scaled3,
            _ => default,
        };
    public int ToRaw(BankVersion version, CoordinateMode value) =>
        value switch
        {
            CoordinateMode.Linear => 0,
            CoordinateMode.Scaled => 2,
            CoordinateMode.Scaled3 => 3,
            _ => 0,
        };
}

internal sealed class PropertyCategoryWire : IWireEnum<PropertyCategory>
{
    public static readonly PropertyCategoryWire Instance = new();

    public int Width(BankVersion version) => version.Number >= 128 ? 3 : 2;
    public PropertyCategory FromRaw(BankVersion version, int raw) =>
        version.Number >= 145 ? raw switch
        {
            1 => PropertyCategory.Unidirectional,
            2 => PropertyCategory.Bidirectional,
            3 => PropertyCategory.BidirectionalRanged,
            4 => PropertyCategory.Unknown6,
            6 => PropertyCategory.Boolean,
            _ => default,
        } : version.Number >= 128 ? raw switch
        {
            1 => PropertyCategory.Unidirectional,
            2 => PropertyCategory.Bidirectional,
            3 => PropertyCategory.BidirectionalRanged,
            4 => PropertyCategory.Boolean,
            _ => default,
        } : raw switch
        {
            0 => PropertyCategory.Unidirectional,
            1 => PropertyCategory.Bidirectional,
            2 => PropertyCategory.BidirectionalRanged,
            3 => PropertyCategory.Boolean,
            _ => default,
        };
    public int ToRaw(BankVersion version, PropertyCategory value) =>
        version.Number >= 145 ? value switch
        {
            PropertyCategory.Unidirectional => 1,
            PropertyCategory.Bidirectional => 2,
            PropertyCategory.BidirectionalRanged => 3,
            PropertyCategory.Unknown6 => 4,
            PropertyCategory.Boolean => 6,
            _ => 0,
        } : version.Number >= 128 ? value switch
        {
            PropertyCategory.Unidirectional => 1,
            PropertyCategory.Bidirectional => 2,
            PropertyCategory.BidirectionalRanged => 3,
            PropertyCategory.Boolean => 4,
            _ => 0,
        } : value switch
        {
            PropertyCategory.Unidirectional => 0,
            PropertyCategory.Bidirectional => 1,
            PropertyCategory.BidirectionalRanged => 2,
            PropertyCategory.Boolean => 3,
            _ => 0,
        };
}

internal sealed class ParameterCategoryWire : IWireEnum<ParameterCategory>
{
    public static readonly ParameterCategoryWire Instance = new();

    public int Width(BankVersion version) => version.Number >= 145 ? 3 : 2;
    public ParameterCategory FromRaw(BankVersion version, int raw) =>
        version.Number >= 145 ? raw switch
        {
            0 => ParameterCategory.GameParameter,
            1 => ParameterCategory.MidiParameter,
            4 => ParameterCategory.Modulator,
            _ => default,
        } : raw switch
        {
            0 => ParameterCategory.GameParameter,
            1 => ParameterCategory.MidiParameter,
            2 => ParameterCategory.Modulator,
            _ => default,
        };
    public int ToRaw(BankVersion version, ParameterCategory value) =>
        version.Number >= 145 ? value switch
        {
            ParameterCategory.GameParameter => 0,
            ParameterCategory.MidiParameter => 1,
            ParameterCategory.Modulator => 4,
            _ => 0,
        } : value switch
        {
            ParameterCategory.GameParameter => 0,
            ParameterCategory.MidiParameter => 1,
            ParameterCategory.Modulator => 2,
            _ => 0,
        };
}

internal sealed class AudioPlayTypeWire : IWireEnum<AudioPlayType>
{
    public static readonly AudioPlayTypeWire Instance = new();

    public int Width(BankVersion version) => 1;
    public AudioPlayType FromRaw(BankVersion version, int raw) =>
        raw switch
        {
            0 => AudioPlayType.Sequence,
            1 => AudioPlayType.Random,
            _ => default,
        };
    public int ToRaw(BankVersion version, AudioPlayType value) =>
        value switch
        {
            AudioPlayType.Sequence => 0,
            AudioPlayType.Random => 1,
            _ => 0,
        };
}

internal sealed class AudioPlayModeWire : IWireEnum<AudioPlayMode>
{
    public static readonly AudioPlayModeWire Instance = new();

    public int Width(BankVersion version) => 1;
    public AudioPlayMode FromRaw(BankVersion version, int raw) =>
        raw switch
        {
            0 => AudioPlayMode.Step,
            1 => AudioPlayMode.Continuous,
            _ => default,
        };
    public int ToRaw(BankVersion version, AudioPlayMode value) =>
        value switch
        {
            AudioPlayMode.Step => 0,
            AudioPlayMode.Continuous => 1,
            _ => 0,
        };
}

internal sealed class AudioPositioningSettingListenerRoutingPositionSourceModeWire : IWireEnum<AudioPositioningSettingListenerRoutingPositionSourceMode>
{
    public static readonly AudioPositioningSettingListenerRoutingPositionSourceModeWire Instance = new();

    public int Width(BankVersion version) => 2;
    public AudioPositioningSettingListenerRoutingPositionSourceMode FromRaw(BankVersion version, int raw) =>
        version.Number >= 132 ? raw switch
        {
            0 => AudioPositioningSettingListenerRoutingPositionSourceMode.Emitter,
            1 => AudioPositioningSettingListenerRoutingPositionSourceMode.EmitterWithAutomation,
            2 => AudioPositioningSettingListenerRoutingPositionSourceMode.ListenerWithAutomation,
            _ => default,
        } : raw switch
        {
            0 => AudioPositioningSettingListenerRoutingPositionSourceMode.UserDefined,
            1 => AudioPositioningSettingListenerRoutingPositionSourceMode.GameDefined,
            _ => default,
        };
    public int ToRaw(BankVersion version, AudioPositioningSettingListenerRoutingPositionSourceMode value) =>
        version.Number >= 132 ? value switch
        {
            AudioPositioningSettingListenerRoutingPositionSourceMode.Emitter => 0,
            AudioPositioningSettingListenerRoutingPositionSourceMode.EmitterWithAutomation => 1,
            AudioPositioningSettingListenerRoutingPositionSourceMode.ListenerWithAutomation => 2,
            _ => 0,
        } : value switch
        {
            AudioPositioningSettingListenerRoutingPositionSourceMode.UserDefined => 0,
            AudioPositioningSettingListenerRoutingPositionSourceMode.GameDefined => 1,
            _ => 0,
        };
}

internal sealed class AudioPositioningSettingListenerRoutingSpatializationWire : IWireEnum<AudioPositioningSettingListenerRoutingSpatialization>
{
    public static readonly AudioPositioningSettingListenerRoutingSpatializationWire Instance = new();

    public int Width(BankVersion version) => version.Number >= 128 ? 3 : 1;
    public AudioPositioningSettingListenerRoutingSpatialization FromRaw(BankVersion version, int raw) =>
        raw switch
        {
            0 => AudioPositioningSettingListenerRoutingSpatialization.None,
            1 => AudioPositioningSettingListenerRoutingSpatialization.Position,
            2 => AudioPositioningSettingListenerRoutingSpatialization.PositionAndOrientation,
            _ => default,
        };
    public int ToRaw(BankVersion version, AudioPositioningSettingListenerRoutingSpatialization value) =>
        value switch
        {
            AudioPositioningSettingListenerRoutingSpatialization.None => 0,
            AudioPositioningSettingListenerRoutingSpatialization.Position => 1,
            AudioPositioningSettingListenerRoutingSpatialization.PositionAndOrientation => 2,
            _ => 0,
        };
}

internal sealed class AudioPositioningSettingSpeakerPanningModeWire : IWireEnum<AudioPositioningSettingSpeakerPanningMode>
{
    public static readonly AudioPositioningSettingSpeakerPanningModeWire Instance = new();

    public int Width(BankVersion version) => 2;
    public AudioPositioningSettingSpeakerPanningMode FromRaw(BankVersion version, int raw) =>
        version.Number >= 140 ? raw switch
        {
            0 => AudioPositioningSettingSpeakerPanningMode.DirectAssignment,
            1 => AudioPositioningSettingSpeakerPanningMode.BalanceFade,
            2 => AudioPositioningSettingSpeakerPanningMode.Steering,
            _ => default,
        } : raw switch
        {
            0 => AudioPositioningSettingSpeakerPanningMode.DirectAssignment,
            1 => AudioPositioningSettingSpeakerPanningMode.BalanceFade,
            _ => default,
        };
    public int ToRaw(BankVersion version, AudioPositioningSettingSpeakerPanningMode value) =>
        version.Number >= 140 ? value switch
        {
            AudioPositioningSettingSpeakerPanningMode.DirectAssignment => 0,
            AudioPositioningSettingSpeakerPanningMode.BalanceFade => 1,
            AudioPositioningSettingSpeakerPanningMode.Steering => 2,
            _ => 0,
        } : value switch
        {
            AudioPositioningSettingSpeakerPanningMode.DirectAssignment => 0,
            AudioPositioningSettingSpeakerPanningMode.BalanceFade => 1,
            _ => 0,
        };
}

internal sealed class AudioPositioningSettingTypeWire : IWireEnum<AudioPositioningSettingType>
{
    public static readonly AudioPositioningSettingTypeWire Instance = new();

    public int Width(BankVersion version) => 1;
    public AudioPositioningSettingType FromRaw(BankVersion version, int raw) =>
        raw switch
        {
            0 => AudioPositioningSettingType.TwoDimension,
            1 => AudioPositioningSettingType.ThreeDimension,
            _ => default,
        };
    public int ToRaw(BankVersion version, AudioPositioningSettingType value) =>
        value switch
        {
            AudioPositioningSettingType.TwoDimension => 0,
            AudioPositioningSettingType.ThreeDimension => 1,
            _ => 0,
        };
}

internal sealed class BusAutomaticDuckingSettingBusTargetWire : IWireEnum<BusAutomaticDuckingSettingBusTarget>
{
    public static readonly BusAutomaticDuckingSettingBusTargetWire Instance = new();

    public int Width(BankVersion version) => 3;
    public BusAutomaticDuckingSettingBusTarget FromRaw(BankVersion version, int raw) =>
        version.Number >= 112 ? raw switch
        {
            0 => BusAutomaticDuckingSettingBusTarget.VoiceVolume,
            5 => BusAutomaticDuckingSettingBusTarget.BusVolume,
            _ => default,
        } : raw switch
        {
            0 => BusAutomaticDuckingSettingBusTarget.VoiceVolume,
            4 => BusAutomaticDuckingSettingBusTarget.BusVolume,
            _ => default,
        };
    public int ToRaw(BankVersion version, BusAutomaticDuckingSettingBusTarget value) =>
        version.Number >= 112 ? value switch
        {
            BusAutomaticDuckingSettingBusTarget.VoiceVolume => 0,
            BusAutomaticDuckingSettingBusTarget.BusVolume => 5,
            _ => 0,
        } : value switch
        {
            BusAutomaticDuckingSettingBusTarget.VoiceVolume => 0,
            BusAutomaticDuckingSettingBusTarget.BusVolume => 4,
            _ => 0,
        };
}

internal sealed class MusicTransitionSettingJumpModeWire : IWireEnum<MusicTransitionSettingJumpMode>
{
    public static readonly MusicTransitionSettingJumpModeWire Instance = new();

    public int Width(BankVersion version) => 2;
    public MusicTransitionSettingJumpMode FromRaw(BankVersion version, int raw) =>
        raw switch
        {
            0 => MusicTransitionSettingJumpMode.Start,
            1 => MusicTransitionSettingJumpMode.Specific,
            2 => MusicTransitionSettingJumpMode.LastPlayed,
            3 => MusicTransitionSettingJumpMode.Next,
            _ => default,
        };
    public int ToRaw(BankVersion version, MusicTransitionSettingJumpMode value) =>
        value switch
        {
            MusicTransitionSettingJumpMode.Start => 0,
            MusicTransitionSettingJumpMode.Specific => 1,
            MusicTransitionSettingJumpMode.LastPlayed => 2,
            MusicTransitionSettingJumpMode.Next => 3,
            _ => 0,
        };
}

internal sealed class MusicTransitionSettingSynchronizeModeWire : IWireEnum<MusicTransitionSettingSynchronizeMode>
{
    public static readonly MusicTransitionSettingSynchronizeModeWire Instance = new();

    public int Width(BankVersion version) => 2;
    public MusicTransitionSettingSynchronizeMode FromRaw(BankVersion version, int raw) =>
        raw switch
        {
            0 => MusicTransitionSettingSynchronizeMode.EntryCue,
            1 => MusicTransitionSettingSynchronizeMode.SameTimeAsPlayingSegment,
            2 => MusicTransitionSettingSynchronizeMode.RandomCue,
            3 => MusicTransitionSettingSynchronizeMode.CustomCue,
            _ => default,
        };
    public int ToRaw(BankVersion version, MusicTransitionSettingSynchronizeMode value) =>
        value switch
        {
            MusicTransitionSettingSynchronizeMode.EntryCue => 0,
            MusicTransitionSettingSynchronizeMode.SameTimeAsPlayingSegment => 1,
            MusicTransitionSettingSynchronizeMode.RandomCue => 2,
            MusicTransitionSettingSynchronizeMode.CustomCue => 3,
            _ => 0,
        };
}

internal sealed class BusHdrSettingDynamicReleaseModeWire : IWireEnum<BusHdrSettingDynamicReleaseMode>
{
    public static readonly BusHdrSettingDynamicReleaseModeWire Instance = new();

    public int Width(BankVersion version) => 1;
    public BusHdrSettingDynamicReleaseMode FromRaw(BankVersion version, int raw) =>
        raw switch
        {
            0 => BusHdrSettingDynamicReleaseMode.Linear,
            1 => BusHdrSettingDynamicReleaseMode.Exponential,
            _ => default,
        };
    public int ToRaw(BankVersion version, BusHdrSettingDynamicReleaseMode value) =>
        value switch
        {
            BusHdrSettingDynamicReleaseMode.Linear => 0,
            BusHdrSettingDynamicReleaseMode.Exponential => 1,
            _ => 0,
        };
}

internal sealed class SoundMidiSettingEventPlayOnWire : IWireEnum<SoundMidiSettingEventPlayOn>
{
    public static readonly SoundMidiSettingEventPlayOnWire Instance = new();

    public int Width(BankVersion version) => 2;
    public SoundMidiSettingEventPlayOn FromRaw(BankVersion version, int raw) =>
        raw switch
        {
            0 => SoundMidiSettingEventPlayOn.NoteOn,
            2 => SoundMidiSettingEventPlayOn.NoteOff,
            _ => default,
        };
    public int ToRaw(BankVersion version, SoundMidiSettingEventPlayOn value) =>
        value switch
        {
            SoundMidiSettingEventPlayOn.NoteOn => 0,
            SoundMidiSettingEventPlayOn.NoteOff => 2,
            _ => 0,
        };
}

internal sealed class MusicMidiSettingClipTempoSourceWire : IWireEnum<MusicMidiSettingClipTempoSource>
{
    public static readonly MusicMidiSettingClipTempoSourceWire Instance = new();

    public int Width(BankVersion version) => 1;
    public MusicMidiSettingClipTempoSource FromRaw(BankVersion version, int raw) =>
        raw switch
        {
            0 => MusicMidiSettingClipTempoSource.Hierarchy,
            1 => MusicMidiSettingClipTempoSource.File,
            _ => default,
        };
    public int ToRaw(BankVersion version, MusicMidiSettingClipTempoSource value) =>
        value switch
        {
            MusicMidiSettingClipTempoSource.Hierarchy => 0,
            MusicMidiSettingClipTempoSource.File => 1,
            _ => 0,
        };
}

internal sealed class AudioPlaybackLimitSettingScopeWire : IWireEnum<AudioPlaybackLimitSettingScope>
{
    public static readonly AudioPlaybackLimitSettingScopeWire Instance = new();

    public int Width(BankVersion version) => 1;
    public AudioPlaybackLimitSettingScope FromRaw(BankVersion version, int raw) =>
        raw switch
        {
            0 => AudioPlaybackLimitSettingScope.PerGameObject,
            1 => AudioPlaybackLimitSettingScope.Globally,
            _ => default,
        };
    public int ToRaw(BankVersion version, AudioPlaybackLimitSettingScope value) =>
        value switch
        {
            AudioPlaybackLimitSettingScope.PerGameObject => 0,
            AudioPlaybackLimitSettingScope.Globally => 1,
            _ => 0,
        };
}

internal sealed class AudioPlaybackLimitSettingWhenPriorityIsEqualWire : IWireEnum<AudioPlaybackLimitSettingWhenPriorityIsEqual>
{
    public static readonly AudioPlaybackLimitSettingWhenPriorityIsEqualWire Instance = new();

    public int Width(BankVersion version) => 1;
    public AudioPlaybackLimitSettingWhenPriorityIsEqual FromRaw(BankVersion version, int raw) =>
        raw switch
        {
            0 => AudioPlaybackLimitSettingWhenPriorityIsEqual.DiscardOldestInstance,
            1 => AudioPlaybackLimitSettingWhenPriorityIsEqual.DiscardNewestInstance,
            _ => default,
        };
    public int ToRaw(BankVersion version, AudioPlaybackLimitSettingWhenPriorityIsEqual value) =>
        value switch
        {
            AudioPlaybackLimitSettingWhenPriorityIsEqual.DiscardOldestInstance => 0,
            AudioPlaybackLimitSettingWhenPriorityIsEqual.DiscardNewestInstance => 1,
            _ => 0,
        };
}

internal sealed class AudioPlaybackLimitSettingWhenLimitIsReachedWire : IWireEnum<AudioPlaybackLimitSettingWhenLimitIsReached>
{
    public static readonly AudioPlaybackLimitSettingWhenLimitIsReachedWire Instance = new();

    public int Width(BankVersion version) => 1;
    public AudioPlaybackLimitSettingWhenLimitIsReached FromRaw(BankVersion version, int raw) =>
        raw switch
        {
            0 => AudioPlaybackLimitSettingWhenLimitIsReached.KillVoice,
            1 => AudioPlaybackLimitSettingWhenLimitIsReached.UseVirtualVoiceSetting,
            _ => default,
        };
    public int ToRaw(BankVersion version, AudioPlaybackLimitSettingWhenLimitIsReached value) =>
        value switch
        {
            AudioPlaybackLimitSettingWhenLimitIsReached.KillVoice => 0,
            AudioPlaybackLimitSettingWhenLimitIsReached.UseVirtualVoiceSetting => 1,
            _ => 0,
        };
}

internal sealed class AudioVirtualVoiceSettingBehaviorWire : IWireEnum<AudioVirtualVoiceSettingBehavior>
{
    public static readonly AudioVirtualVoiceSettingBehaviorWire Instance = new();

    public int Width(BankVersion version) => 2;
    public AudioVirtualVoiceSettingBehavior FromRaw(BankVersion version, int raw) =>
        version.Number >= 140 ? raw switch
        {
            0 => AudioVirtualVoiceSettingBehavior.ContinueToPlay,
            1 => AudioVirtualVoiceSettingBehavior.KillVoice,
            2 => AudioVirtualVoiceSettingBehavior.SendToVirtualVoice,
            3 => AudioVirtualVoiceSettingBehavior.KillIfFiniteElseVirtual,
            _ => default,
        } : raw switch
        {
            0 => AudioVirtualVoiceSettingBehavior.ContinueToPlay,
            1 => AudioVirtualVoiceSettingBehavior.KillVoice,
            2 => AudioVirtualVoiceSettingBehavior.SendToVirtualVoice,
            _ => default,
        };
    public int ToRaw(BankVersion version, AudioVirtualVoiceSettingBehavior value) =>
        version.Number >= 140 ? value switch
        {
            AudioVirtualVoiceSettingBehavior.ContinueToPlay => 0,
            AudioVirtualVoiceSettingBehavior.KillVoice => 1,
            AudioVirtualVoiceSettingBehavior.SendToVirtualVoice => 2,
            AudioVirtualVoiceSettingBehavior.KillIfFiniteElseVirtual => 3,
            _ => 0,
        } : value switch
        {
            AudioVirtualVoiceSettingBehavior.ContinueToPlay => 0,
            AudioVirtualVoiceSettingBehavior.KillVoice => 1,
            AudioVirtualVoiceSettingBehavior.SendToVirtualVoice => 2,
            _ => 0,
        };
}

internal sealed class AudioVirtualVoiceSettingOnReturnToPhysicalWire : IWireEnum<AudioVirtualVoiceSettingOnReturnToPhysical>
{
    public static readonly AudioVirtualVoiceSettingOnReturnToPhysicalWire Instance = new();

    public int Width(BankVersion version) => 2;
    public AudioVirtualVoiceSettingOnReturnToPhysical FromRaw(BankVersion version, int raw) =>
        raw switch
        {
            0 => AudioVirtualVoiceSettingOnReturnToPhysical.PlayFromBeginning,
            1 => AudioVirtualVoiceSettingOnReturnToPhysical.PlayFromElapsedTime,
            2 => AudioVirtualVoiceSettingOnReturnToPhysical.Resume,
            _ => default,
        };
    public int ToRaw(BankVersion version, AudioVirtualVoiceSettingOnReturnToPhysical value) =>
        value switch
        {
            AudioVirtualVoiceSettingOnReturnToPhysical.PlayFromBeginning => 0,
            AudioVirtualVoiceSettingOnReturnToPhysical.PlayFromElapsedTime => 1,
            AudioVirtualVoiceSettingOnReturnToPhysical.Resume => 2,
            _ => 0,
        };
}

internal sealed class AudioAssociationSettingModeWire : IWireEnum<AudioAssociationSettingMode>
{
    public static readonly AudioAssociationSettingModeWire Instance = new();

    public int Width(BankVersion version) => 1;
    public AudioAssociationSettingMode FromRaw(BankVersion version, int raw) =>
        raw switch
        {
            0 => AudioAssociationSettingMode.BestMatch,
            1 => AudioAssociationSettingMode.Weighted,
            _ => default,
        };
    public int ToRaw(BankVersion version, AudioAssociationSettingMode value) =>
        value switch
        {
            AudioAssociationSettingMode.BestMatch => 0,
            AudioAssociationSettingMode.Weighted => 1,
            _ => 0,
        };
}

internal sealed class AudioSourceTypeWire : IWireEnum<AudioSourceType>
{
    public static readonly AudioSourceTypeWire Instance = new();

    public int Width(BankVersion version) => 2;
    public AudioSourceType FromRaw(BankVersion version, int raw) =>
        version.Number >= 112 ? raw switch
        {
            0 => AudioSourceType.Embedded,
            1 => AudioSourceType.StreamedPrefetched,
            2 => AudioSourceType.Streamed,
            _ => default,
        } : raw switch
        {
            0 => AudioSourceType.Embedded,
            1 => AudioSourceType.Streamed,
            2 => AudioSourceType.StreamedPrefetched,
            _ => default,
        };
    public int ToRaw(BankVersion version, AudioSourceType value) =>
        version.Number >= 112 ? value switch
        {
            AudioSourceType.Embedded => 0,
            AudioSourceType.StreamedPrefetched => 1,
            AudioSourceType.Streamed => 2,
            _ => 0,
        } : value switch
        {
            AudioSourceType.Embedded => 0,
            AudioSourceType.Streamed => 1,
            AudioSourceType.StreamedPrefetched => 2,
            _ => 0,
        };
}

internal sealed class AudioPlayTypeRandomTypeWire : IWireEnum<AudioPlayTypeRandomType>
{
    public static readonly AudioPlayTypeRandomTypeWire Instance = new();

    public int Width(BankVersion version) => 1;
    public AudioPlayTypeRandomType FromRaw(BankVersion version, int raw) =>
        raw switch
        {
            0 => AudioPlayTypeRandomType.Standard,
            1 => AudioPlayTypeRandomType.Shuffle,
            _ => default,
        };
    public int ToRaw(BankVersion version, AudioPlayTypeRandomType value) =>
        value switch
        {
            AudioPlayTypeRandomType.Standard => 0,
            AudioPlayTypeRandomType.Shuffle => 1,
            _ => 0,
        };
}

internal sealed class AudioPlayTypeSequenceAtEndOfPlaylistWire : IWireEnum<AudioPlayTypeSequenceAtEndOfPlaylist>
{
    public static readonly AudioPlayTypeSequenceAtEndOfPlaylistWire Instance = new();

    public int Width(BankVersion version) => 1;
    public AudioPlayTypeSequenceAtEndOfPlaylist FromRaw(BankVersion version, int raw) =>
        raw switch
        {
            0 => AudioPlayTypeSequenceAtEndOfPlaylist.Restart,
            1 => AudioPlayTypeSequenceAtEndOfPlaylist.PlayInReserveOrder,
            _ => default,
        };
    public int ToRaw(BankVersion version, AudioPlayTypeSequenceAtEndOfPlaylist value) =>
        value switch
        {
            AudioPlayTypeSequenceAtEndOfPlaylist.Restart => 0,
            AudioPlayTypeSequenceAtEndOfPlaylist.PlayInReserveOrder => 1,
            _ => 0,
        };
}

internal sealed class AudioPlayModeContinuousTransitionTypeWire : IWireEnum<AudioPlayModeContinuousTransitionType>
{
    public static readonly AudioPlayModeContinuousTransitionTypeWire Instance = new();

    public int Width(BankVersion version) => 3;
    public AudioPlayModeContinuousTransitionType FromRaw(BankVersion version, int raw) =>
        raw switch
        {
            0 => AudioPlayModeContinuousTransitionType.None,
            1 => AudioPlayModeContinuousTransitionType.XfadeAmp,
            2 => AudioPlayModeContinuousTransitionType.XfadePower,
            3 => AudioPlayModeContinuousTransitionType.Delay,
            4 => AudioPlayModeContinuousTransitionType.SampleAccurate,
            5 => AudioPlayModeContinuousTransitionType.TriggerRate,
            _ => default,
        };
    public int ToRaw(BankVersion version, AudioPlayModeContinuousTransitionType value) =>
        value switch
        {
            AudioPlayModeContinuousTransitionType.None => 0,
            AudioPlayModeContinuousTransitionType.XfadeAmp => 1,
            AudioPlayModeContinuousTransitionType.XfadePower => 2,
            AudioPlayModeContinuousTransitionType.Delay => 3,
            AudioPlayModeContinuousTransitionType.SampleAccurate => 4,
            AudioPlayModeContinuousTransitionType.TriggerRate => 5,
            _ => 0,
        };
}

internal sealed class GameParameterBindToBuiltInParameterModeWire : IWireEnum<GameParameterBindToBuiltInParameterMode>
{
    public static readonly GameParameterBindToBuiltInParameterModeWire Instance = new();

    public int Width(BankVersion version) => 3;
    public GameParameterBindToBuiltInParameterMode FromRaw(BankVersion version, int raw) =>
        version.Number >= 128 ? raw switch
        {
            0 => GameParameterBindToBuiltInParameterMode.None,
            1 => GameParameterBindToBuiltInParameterMode.Distance,
            2 => GameParameterBindToBuiltInParameterMode.Azimuth,
            3 => GameParameterBindToBuiltInParameterMode.Elevation,
            4 => GameParameterBindToBuiltInParameterMode.EmitterCone,
            5 => GameParameterBindToBuiltInParameterMode.Obstruction,
            6 => GameParameterBindToBuiltInParameterMode.Occlusion,
            7 => GameParameterBindToBuiltInParameterMode.ListenerCone,
            8 => GameParameterBindToBuiltInParameterMode.Diffraction,
            _ => default,
        } : raw switch
        {
            0 => GameParameterBindToBuiltInParameterMode.None,
            1 => GameParameterBindToBuiltInParameterMode.Distance,
            2 => GameParameterBindToBuiltInParameterMode.Azimuth,
            3 => GameParameterBindToBuiltInParameterMode.Elevation,
            4 => GameParameterBindToBuiltInParameterMode.ObjectToListenerAngle,
            5 => GameParameterBindToBuiltInParameterMode.Obstruction,
            6 => GameParameterBindToBuiltInParameterMode.Occlusion,
            _ => default,
        };
    public int ToRaw(BankVersion version, GameParameterBindToBuiltInParameterMode value) =>
        version.Number >= 128 ? value switch
        {
            GameParameterBindToBuiltInParameterMode.None => 0,
            GameParameterBindToBuiltInParameterMode.Distance => 1,
            GameParameterBindToBuiltInParameterMode.Azimuth => 2,
            GameParameterBindToBuiltInParameterMode.Elevation => 3,
            GameParameterBindToBuiltInParameterMode.EmitterCone => 4,
            GameParameterBindToBuiltInParameterMode.Obstruction => 5,
            GameParameterBindToBuiltInParameterMode.Occlusion => 6,
            GameParameterBindToBuiltInParameterMode.ListenerCone => 7,
            GameParameterBindToBuiltInParameterMode.Diffraction => 8,
            _ => 0,
        } : value switch
        {
            GameParameterBindToBuiltInParameterMode.None => 0,
            GameParameterBindToBuiltInParameterMode.Distance => 1,
            GameParameterBindToBuiltInParameterMode.Azimuth => 2,
            GameParameterBindToBuiltInParameterMode.Elevation => 3,
            GameParameterBindToBuiltInParameterMode.ObjectToListenerAngle => 4,
            GameParameterBindToBuiltInParameterMode.Obstruction => 5,
            GameParameterBindToBuiltInParameterMode.Occlusion => 6,
            _ => 0,
        };
}

internal sealed class GameParameterInterpolationModeWire : IWireEnum<GameParameterInterpolationMode>
{
    public static readonly GameParameterInterpolationModeWire Instance = new();

    public int Width(BankVersion version) => 2;
    public GameParameterInterpolationMode FromRaw(BankVersion version, int raw) =>
        raw switch
        {
            0 => GameParameterInterpolationMode.None,
            1 => GameParameterInterpolationMode.SlewRate,
            2 => GameParameterInterpolationMode.FilteringOverTime,
            _ => default,
        };
    public int ToRaw(BankVersion version, GameParameterInterpolationMode value) =>
        value switch
        {
            GameParameterInterpolationMode.None => 0,
            GameParameterInterpolationMode.SlewRate => 1,
            GameParameterInterpolationMode.FilteringOverTime => 2,
            _ => 0,
        };
}

internal sealed class EventActionPropertyValueApplyModeWire : IWireEnum<EventActionPropertyValueApplyMode>
{
    public static readonly EventActionPropertyValueApplyModeWire Instance = new();

    public int Width(BankVersion version) => 8;
    public EventActionPropertyValueApplyMode FromRaw(BankVersion version, int raw) =>
        raw switch
        {
            0 => EventActionPropertyValueApplyMode.Absolute,
            1 => EventActionPropertyValueApplyMode.Relative,
            _ => default,
        };
    public int ToRaw(BankVersion version, EventActionPropertyValueApplyMode value) =>
        value switch
        {
            EventActionPropertyValueApplyMode.Absolute => 0,
            EventActionPropertyValueApplyMode.Relative => 1,
            _ => 0,
        };
}

internal sealed class EventActionPropertySeekTypeWire : IWireEnum<EventActionPropertySeekType>
{
    public static readonly EventActionPropertySeekTypeWire Instance = new();

    public int Width(BankVersion version) => 8;
    public EventActionPropertySeekType FromRaw(BankVersion version, int raw) =>
        raw switch
        {
            0 => EventActionPropertySeekType.Time,
            1 => EventActionPropertySeekType.Percent,
            _ => default,
        };
    public int ToRaw(BankVersion version, EventActionPropertySeekType value) =>
        value switch
        {
            EventActionPropertySeekType.Time => 0,
            EventActionPropertySeekType.Percent => 1,
            _ => 0,
        };
}

internal sealed class EventActionPropertyTypeWire : IWireEnum<EventActionPropertyType>
{
    public static readonly EventActionPropertyTypeWire Instance = new();

    public int Width(BankVersion version) => 8;
    public EventActionPropertyType FromRaw(BankVersion version, int raw) =>
        raw switch
        {
            0 => EventActionPropertyType.PlayAudio,
            1 => EventActionPropertyType.StopAudio,
            2 => EventActionPropertyType.PauseAudio,
            3 => EventActionPropertyType.ResumeAudio,
            4 => EventActionPropertyType.BreakAudio,
            5 => EventActionPropertyType.SeekAudio,
            6 => EventActionPropertyType.SetBusVolume,
            7 => EventActionPropertyType.SetVoiceVolume,
            8 => EventActionPropertyType.SetVoicePitch,
            9 => EventActionPropertyType.SetVoiceLowPassFilter,
            10 => EventActionPropertyType.SetMute,
            11 => EventActionPropertyType.SetGameParameter,
            12 => EventActionPropertyType.SetStateAvailability,
            13 => EventActionPropertyType.ActivateState,
            14 => EventActionPropertyType.ActivateSwitch,
            15 => EventActionPropertyType.ActivateTrigger,
            16 => EventActionPropertyType.SetBypassEffect,
            17 => EventActionPropertyType.SetVoiceHighPassFilter,
            18 => EventActionPropertyType.ReleaseEnvelope,
            19 => EventActionPropertyType.PostEvent,
            20 => EventActionPropertyType.ResetPlaylist,
            _ => default,
        };
    public int ToRaw(BankVersion version, EventActionPropertyType value) =>
        value switch
        {
            EventActionPropertyType.PlayAudio => 0,
            EventActionPropertyType.StopAudio => 1,
            EventActionPropertyType.PauseAudio => 2,
            EventActionPropertyType.ResumeAudio => 3,
            EventActionPropertyType.BreakAudio => 4,
            EventActionPropertyType.SeekAudio => 5,
            EventActionPropertyType.SetBusVolume => 6,
            EventActionPropertyType.SetVoiceVolume => 7,
            EventActionPropertyType.SetVoicePitch => 8,
            EventActionPropertyType.SetVoiceLowPassFilter => 9,
            EventActionPropertyType.SetMute => 10,
            EventActionPropertyType.SetGameParameter => 11,
            EventActionPropertyType.SetStateAvailability => 12,
            EventActionPropertyType.ActivateState => 13,
            EventActionPropertyType.ActivateSwitch => 14,
            EventActionPropertyType.ActivateTrigger => 15,
            EventActionPropertyType.SetBypassEffect => 16,
            EventActionPropertyType.SetVoiceHighPassFilter => 17,
            EventActionPropertyType.ReleaseEnvelope => 18,
            EventActionPropertyType.PostEvent => 19,
            EventActionPropertyType.ResetPlaylist => 20,
            _ => 0,
        };
}

internal sealed class EventActionModeWire : IWireEnum<EventActionMode>
{
    public static readonly EventActionModeWire Instance = new();

    public int Width(BankVersion version) => version.Number >= 125 ? 2 : 3;
    public EventActionMode FromRaw(BankVersion version, int raw) =>
        version.Number >= 125 ? raw switch
        {
            0 => EventActionMode.None,
            1 => EventActionMode.One,
            2 => EventActionMode.All,
            _ => default,
        } : raw switch
        {
            0 => EventActionMode.None,
            1 => EventActionMode.One,
            2 => EventActionMode.All,
            4 => EventActionMode.AllExcept,
            _ => default,
        };
    public int ToRaw(BankVersion version, EventActionMode value) =>
        version.Number >= 125 ? value switch
        {
            EventActionMode.None => 0,
            EventActionMode.One => 1,
            EventActionMode.All => 2,
            _ => 0,
        } : value switch
        {
            EventActionMode.None => 0,
            EventActionMode.One => 1,
            EventActionMode.All => 2,
            EventActionMode.AllExcept => 4,
            _ => 0,
        };
}

internal sealed class EventActionScopeWire : IWireEnum<EventActionScope>
{
    public static readonly EventActionScopeWire Instance = new();

    public int Width(BankVersion version) => 1;
    public EventActionScope FromRaw(BankVersion version, int raw) =>
        raw switch
        {
            0 => EventActionScope.Global,
            1 => EventActionScope.GameObject,
            _ => default,
        };
    public int ToRaw(BankVersion version, EventActionScope value) =>
        value switch
        {
            EventActionScope.Global => 0,
            EventActionScope.GameObject => 1,
            _ => 0,
        };
}

internal sealed class ModulatorScopeWire : IWireEnum<ModulatorScope>
{
    public static readonly ModulatorScopeWire Instance = new();

    public int Width(BankVersion version) => 2;
    public ModulatorScope FromRaw(BankVersion version, int raw) =>
        raw switch
        {
            0 => ModulatorScope.Voice,
            1 => ModulatorScope.NoteOrEvent,
            2 => ModulatorScope.GameObject,
            3 => ModulatorScope.Global,
            _ => default,
        };
    public int ToRaw(BankVersion version, ModulatorScope value) =>
        value switch
        {
            ModulatorScope.Voice => 0,
            ModulatorScope.NoteOrEvent => 1,
            ModulatorScope.GameObject => 2,
            ModulatorScope.Global => 3,
            _ => 0,
        };
}

internal sealed class ModulatorTriggerOnWire : IWireEnum<ModulatorTriggerOn>
{
    public static readonly ModulatorTriggerOnWire Instance = new();

    public int Width(BankVersion version) => 2;
    public ModulatorTriggerOn FromRaw(BankVersion version, int raw) =>
        raw switch
        {
            0 => ModulatorTriggerOn.Play,
            2 => ModulatorTriggerOn.NoteOff,
            _ => default,
        };
    public int ToRaw(BankVersion version, ModulatorTriggerOn value) =>
        value switch
        {
            ModulatorTriggerOn.Play => 0,
            ModulatorTriggerOn.NoteOff => 2,
            _ => 0,
        };
}

internal sealed class ModulatorWaveformWire : IWireEnum<ModulatorWaveform>
{
    public static readonly ModulatorWaveformWire Instance = new();

    public int Width(BankVersion version) => 3;
    public ModulatorWaveform FromRaw(BankVersion version, int raw) =>
        version.Number >= 125 ? raw switch
        {
            0 => ModulatorWaveform.Sine,
            1 => ModulatorWaveform.Triangle,
            2 => ModulatorWaveform.Square,
            3 => ModulatorWaveform.SawUp,
            4 => ModulatorWaveform.SawDown,
            5 => ModulatorWaveform.Random,
            _ => default,
        } : raw switch
        {
            0 => ModulatorWaveform.Sine,
            1 => ModulatorWaveform.Triangle,
            2 => ModulatorWaveform.Square,
            3 => ModulatorWaveform.SawUp,
            4 => ModulatorWaveform.SawDown,
            _ => default,
        };
    public int ToRaw(BankVersion version, ModulatorWaveform value) =>
        version.Number >= 125 ? value switch
        {
            ModulatorWaveform.Sine => 0,
            ModulatorWaveform.Triangle => 1,
            ModulatorWaveform.Square => 2,
            ModulatorWaveform.SawUp => 3,
            ModulatorWaveform.SawDown => 4,
            ModulatorWaveform.Random => 5,
            _ => 0,
        } : value switch
        {
            ModulatorWaveform.Sine => 0,
            ModulatorWaveform.Triangle => 1,
            ModulatorWaveform.Square => 2,
            ModulatorWaveform.SawUp => 3,
            ModulatorWaveform.SawDown => 4,
            _ => 0,
        };
}

internal sealed class SoundPlaylistContainerScopeWire : IWireEnum<SoundPlaylistContainerScope>
{
    public static readonly SoundPlaylistContainerScopeWire Instance = new();

    public int Width(BankVersion version) => 1;
    public SoundPlaylistContainerScope FromRaw(BankVersion version, int raw) =>
        raw switch
        {
            0 => SoundPlaylistContainerScope.GameObject,
            1 => SoundPlaylistContainerScope.Global,
            _ => default,
        };
    public int ToRaw(BankVersion version, SoundPlaylistContainerScope value) =>
        value switch
        {
            SoundPlaylistContainerScope.GameObject => 0,
            SoundPlaylistContainerScope.Global => 1,
            _ => 0,
        };
}

internal sealed class MusicTrackTrackTypeWire : IWireEnum<MusicTrackTrackType>
{
    public static readonly MusicTrackTrackTypeWire Instance = new();

    public int Width(BankVersion version) => 2;
    public MusicTrackTrackType FromRaw(BankVersion version, int raw) =>
        version.Number >= 112 ? raw switch
        {
            0 => MusicTrackTrackType.Normal,
            1 => MusicTrackTrackType.RandomStep,
            2 => MusicTrackTrackType.SequenceStep,
            3 => MusicTrackTrackType.Switcher,
            _ => default,
        } : raw switch
        {
            0 => MusicTrackTrackType.Normal,
            1 => MusicTrackTrackType.RandomStep,
            2 => MusicTrackTrackType.SequenceStep,
            _ => default,
        };
    public int ToRaw(BankVersion version, MusicTrackTrackType value) =>
        version.Number >= 112 ? value switch
        {
            MusicTrackTrackType.Normal => 0,
            MusicTrackTrackType.RandomStep => 1,
            MusicTrackTrackType.SequenceStep => 2,
            MusicTrackTrackType.Switcher => 3,
            _ => 0,
        } : value switch
        {
            MusicTrackTrackType.Normal => 0,
            MusicTrackTrackType.RandomStep => 1,
            MusicTrackTrackType.SequenceStep => 2,
            _ => 0,
        };
}

internal sealed class MusicTrackClipCurveItemTypeWire : IWireEnum<MusicTrackClipCurveItemType>
{
    public static readonly MusicTrackClipCurveItemTypeWire Instance = new();

    public int Width(BankVersion version) => 3;
    public MusicTrackClipCurveItemType FromRaw(BankVersion version, int raw) =>
        version.Number >= 112 ? raw switch
        {
            0 => MusicTrackClipCurveItemType.VoiceVolume,
            1 => MusicTrackClipCurveItemType.VoiceLowPassFilter,
            2 => MusicTrackClipCurveItemType.VoiceHighPassFilter,
            3 => MusicTrackClipCurveItemType.ClipFadeIn,
            4 => MusicTrackClipCurveItemType.ClipFadeOut,
            _ => default,
        } : raw switch
        {
            0 => MusicTrackClipCurveItemType.VoiceVolume,
            1 => MusicTrackClipCurveItemType.VoiceLowPassFilter,
            2 => MusicTrackClipCurveItemType.ClipFadeIn,
            3 => MusicTrackClipCurveItemType.ClipFadeOut,
            _ => default,
        };
    public int ToRaw(BankVersion version, MusicTrackClipCurveItemType value) =>
        version.Number >= 112 ? value switch
        {
            MusicTrackClipCurveItemType.VoiceVolume => 0,
            MusicTrackClipCurveItemType.VoiceLowPassFilter => 1,
            MusicTrackClipCurveItemType.VoiceHighPassFilter => 2,
            MusicTrackClipCurveItemType.ClipFadeIn => 3,
            MusicTrackClipCurveItemType.ClipFadeOut => 4,
            _ => 0,
        } : value switch
        {
            MusicTrackClipCurveItemType.VoiceVolume => 0,
            MusicTrackClipCurveItemType.VoiceLowPassFilter => 1,
            MusicTrackClipCurveItemType.ClipFadeIn => 2,
            MusicTrackClipCurveItemType.ClipFadeOut => 3,
            _ => 0,
        };
}

internal sealed class HierarchyTypeWire : IWireEnum<HierarchyType>
{
    public static readonly HierarchyTypeWire Instance = new();

    public int Width(BankVersion version) => 8;
    public HierarchyType FromRaw(BankVersion version, int raw) =>
        version.Number >= 132 ? raw switch
        {
            1 => HierarchyType.StatefulPropertySetting,
            2 => HierarchyType.Sound,
            3 => HierarchyType.EventAction,
            4 => HierarchyType.Event,
            5 => HierarchyType.SoundPlaylistContainer,
            6 => HierarchyType.SoundSwitchContainer,
            7 => HierarchyType.ActorMixer,
            8 => HierarchyType.AudioBus,
            9 => HierarchyType.SoundBlendContainer,
            10 => HierarchyType.MusicSegment,
            11 => HierarchyType.MusicTrack,
            12 => HierarchyType.MusicSwitchContainer,
            13 => HierarchyType.MusicPlaylistContainer,
            14 => HierarchyType.Attenuation,
            15 => HierarchyType.DialogueEvent,
            16 => HierarchyType.Effect,
            17 => HierarchyType.Source,
            18 => HierarchyType.AuxiliaryAudioBus,
            19 => HierarchyType.LowFrequencyOscillatorModulator,
            20 => HierarchyType.EnvelopeModulator,
            21 => HierarchyType.AudioDevice,
            22 => HierarchyType.TimeModulator,
            255 => HierarchyType.Unknown,
            _ => default,
        } : version.Number >= 128 ? raw switch
        {
            1 => HierarchyType.StatefulPropertySetting,
            2 => HierarchyType.Sound,
            3 => HierarchyType.EventAction,
            4 => HierarchyType.Event,
            5 => HierarchyType.SoundPlaylistContainer,
            6 => HierarchyType.SoundSwitchContainer,
            7 => HierarchyType.ActorMixer,
            8 => HierarchyType.AudioBus,
            9 => HierarchyType.SoundBlendContainer,
            10 => HierarchyType.MusicSegment,
            11 => HierarchyType.MusicTrack,
            12 => HierarchyType.MusicSwitchContainer,
            13 => HierarchyType.MusicPlaylistContainer,
            14 => HierarchyType.Attenuation,
            15 => HierarchyType.DialogueEvent,
            16 => HierarchyType.Effect,
            17 => HierarchyType.Source,
            18 => HierarchyType.AuxiliaryAudioBus,
            19 => HierarchyType.LowFrequencyOscillatorModulator,
            20 => HierarchyType.EnvelopeModulator,
            21 => HierarchyType.AudioDevice,
            255 => HierarchyType.Unknown,
            _ => default,
        } : version.Number >= 112 ? raw switch
        {
            1 => HierarchyType.StatefulPropertySetting,
            2 => HierarchyType.Sound,
            3 => HierarchyType.EventAction,
            4 => HierarchyType.Event,
            5 => HierarchyType.SoundPlaylistContainer,
            6 => HierarchyType.SoundSwitchContainer,
            7 => HierarchyType.ActorMixer,
            8 => HierarchyType.AudioBus,
            9 => HierarchyType.SoundBlendContainer,
            10 => HierarchyType.MusicSegment,
            11 => HierarchyType.MusicTrack,
            12 => HierarchyType.MusicSwitchContainer,
            13 => HierarchyType.MusicPlaylistContainer,
            14 => HierarchyType.Attenuation,
            15 => HierarchyType.DialogueEvent,
            18 => HierarchyType.Effect,
            19 => HierarchyType.Source,
            20 => HierarchyType.AuxiliaryAudioBus,
            21 => HierarchyType.LowFrequencyOscillatorModulator,
            22 => HierarchyType.EnvelopeModulator,
            255 => HierarchyType.Unknown,
            _ => default,
        } : raw switch
        {
            1 => HierarchyType.StatefulPropertySetting,
            2 => HierarchyType.Sound,
            3 => HierarchyType.EventAction,
            4 => HierarchyType.Event,
            5 => HierarchyType.SoundPlaylistContainer,
            6 => HierarchyType.SoundSwitchContainer,
            7 => HierarchyType.ActorMixer,
            8 => HierarchyType.AudioBus,
            9 => HierarchyType.SoundBlendContainer,
            10 => HierarchyType.MusicSegment,
            11 => HierarchyType.MusicTrack,
            12 => HierarchyType.MusicSwitchContainer,
            13 => HierarchyType.MusicPlaylistContainer,
            14 => HierarchyType.Attenuation,
            15 => HierarchyType.DialogueEvent,
            18 => HierarchyType.Effect,
            19 => HierarchyType.Source,
            20 => HierarchyType.AuxiliaryAudioBus,
            255 => HierarchyType.Unknown,
            _ => default,
        };
    public int ToRaw(BankVersion version, HierarchyType value) =>
        version.Number >= 132 ? value switch
        {
            HierarchyType.StatefulPropertySetting => 1,
            HierarchyType.Sound => 2,
            HierarchyType.EventAction => 3,
            HierarchyType.Event => 4,
            HierarchyType.SoundPlaylistContainer => 5,
            HierarchyType.SoundSwitchContainer => 6,
            HierarchyType.ActorMixer => 7,
            HierarchyType.AudioBus => 8,
            HierarchyType.SoundBlendContainer => 9,
            HierarchyType.MusicSegment => 10,
            HierarchyType.MusicTrack => 11,
            HierarchyType.MusicSwitchContainer => 12,
            HierarchyType.MusicPlaylistContainer => 13,
            HierarchyType.Attenuation => 14,
            HierarchyType.DialogueEvent => 15,
            HierarchyType.Effect => 16,
            HierarchyType.Source => 17,
            HierarchyType.AuxiliaryAudioBus => 18,
            HierarchyType.LowFrequencyOscillatorModulator => 19,
            HierarchyType.EnvelopeModulator => 20,
            HierarchyType.AudioDevice => 21,
            HierarchyType.TimeModulator => 22,
            HierarchyType.Unknown => 255,
            _ => 0,
        } : version.Number >= 128 ? value switch
        {
            HierarchyType.StatefulPropertySetting => 1,
            HierarchyType.Sound => 2,
            HierarchyType.EventAction => 3,
            HierarchyType.Event => 4,
            HierarchyType.SoundPlaylistContainer => 5,
            HierarchyType.SoundSwitchContainer => 6,
            HierarchyType.ActorMixer => 7,
            HierarchyType.AudioBus => 8,
            HierarchyType.SoundBlendContainer => 9,
            HierarchyType.MusicSegment => 10,
            HierarchyType.MusicTrack => 11,
            HierarchyType.MusicSwitchContainer => 12,
            HierarchyType.MusicPlaylistContainer => 13,
            HierarchyType.Attenuation => 14,
            HierarchyType.DialogueEvent => 15,
            HierarchyType.Effect => 16,
            HierarchyType.Source => 17,
            HierarchyType.AuxiliaryAudioBus => 18,
            HierarchyType.LowFrequencyOscillatorModulator => 19,
            HierarchyType.EnvelopeModulator => 20,
            HierarchyType.AudioDevice => 21,
            HierarchyType.Unknown => 255,
            _ => 0,
        } : version.Number >= 112 ? value switch
        {
            HierarchyType.StatefulPropertySetting => 1,
            HierarchyType.Sound => 2,
            HierarchyType.EventAction => 3,
            HierarchyType.Event => 4,
            HierarchyType.SoundPlaylistContainer => 5,
            HierarchyType.SoundSwitchContainer => 6,
            HierarchyType.ActorMixer => 7,
            HierarchyType.AudioBus => 8,
            HierarchyType.SoundBlendContainer => 9,
            HierarchyType.MusicSegment => 10,
            HierarchyType.MusicTrack => 11,
            HierarchyType.MusicSwitchContainer => 12,
            HierarchyType.MusicPlaylistContainer => 13,
            HierarchyType.Attenuation => 14,
            HierarchyType.DialogueEvent => 15,
            HierarchyType.Effect => 18,
            HierarchyType.Source => 19,
            HierarchyType.AuxiliaryAudioBus => 20,
            HierarchyType.LowFrequencyOscillatorModulator => 21,
            HierarchyType.EnvelopeModulator => 22,
            HierarchyType.Unknown => 255,
            _ => 0,
        } : value switch
        {
            HierarchyType.StatefulPropertySetting => 1,
            HierarchyType.Sound => 2,
            HierarchyType.EventAction => 3,
            HierarchyType.Event => 4,
            HierarchyType.SoundPlaylistContainer => 5,
            HierarchyType.SoundSwitchContainer => 6,
            HierarchyType.ActorMixer => 7,
            HierarchyType.AudioBus => 8,
            HierarchyType.SoundBlendContainer => 9,
            HierarchyType.MusicSegment => 10,
            HierarchyType.MusicTrack => 11,
            HierarchyType.MusicSwitchContainer => 12,
            HierarchyType.MusicPlaylistContainer => 13,
            HierarchyType.Attenuation => 14,
            HierarchyType.DialogueEvent => 15,
            HierarchyType.Effect => 18,
            HierarchyType.Source => 19,
            HierarchyType.AuxiliaryAudioBus => 20,
            HierarchyType.Unknown => 255,
            _ => 0,
        };
}

internal sealed class VoiceFilterBehaviorWire : IWireEnum<VoiceFilterBehavior>
{
    public static readonly VoiceFilterBehaviorWire Instance = new();

    public int Width(BankVersion version) => 1;
    public VoiceFilterBehavior FromRaw(BankVersion version, int raw) =>
        raw switch
        {
            0 => VoiceFilterBehavior.SumAllValue,
            1 => VoiceFilterBehavior.UseHighestValue,
            _ => default,
        };
    public int ToRaw(BankVersion version, VoiceFilterBehavior value) =>
        value switch
        {
            VoiceFilterBehavior.SumAllValue => 0,
            VoiceFilterBehavior.UseHighestValue => 1,
            _ => 0,
        };
}

internal sealed class EventActionCommonPropertyTypeWire : IWireEnum<EventActionCommonPropertyType>
{
    public static readonly EventActionCommonPropertyTypeWire Instance = new();

    public int Width(BankVersion version) => 8;
    public EventActionCommonPropertyType FromRaw(BankVersion version, int raw) =>
        raw switch
        {
            0 => EventActionCommonPropertyType.Delay,
            1 => EventActionCommonPropertyType.FadeTime,
            2 => EventActionCommonPropertyType.Probability,
            _ => default,
        };
    public int ToRaw(BankVersion version, EventActionCommonPropertyType value) =>
        value switch
        {
            EventActionCommonPropertyType.Delay => 0,
            EventActionCommonPropertyType.FadeTime => 1,
            EventActionCommonPropertyType.Probability => 2,
            _ => 0,
        };
}

internal sealed class ModulatorCommonPropertyTypeWire : IWireEnum<ModulatorCommonPropertyType>
{
    public static readonly ModulatorCommonPropertyTypeWire Instance = new();

    public int Width(BankVersion version) => 8;
    public ModulatorCommonPropertyType FromRaw(BankVersion version, int raw) =>
        raw switch
        {
            0 => ModulatorCommonPropertyType.Scope,
            1 => ModulatorCommonPropertyType.TriggerOn,
            2 => ModulatorCommonPropertyType.Depth,
            3 => ModulatorCommonPropertyType.Frequency,
            4 => ModulatorCommonPropertyType.Waveform,
            5 => ModulatorCommonPropertyType.Smoothing,
            6 => ModulatorCommonPropertyType.PulseWidthModulation,
            7 => ModulatorCommonPropertyType.Attack,
            8 => ModulatorCommonPropertyType.InitialPhaseOffset,
            9 => ModulatorCommonPropertyType.AttackTime,
            10 => ModulatorCommonPropertyType.AttackCurve,
            11 => ModulatorCommonPropertyType.DecayTime,
            12 => ModulatorCommonPropertyType.SustainLevel,
            13 => ModulatorCommonPropertyType.ReleaseTime,
            14 => ModulatorCommonPropertyType.SustainTime,
            15 => ModulatorCommonPropertyType.InitialDelay,
            16 => ModulatorCommonPropertyType.Duration,
            17 => ModulatorCommonPropertyType.Loop,
            18 => ModulatorCommonPropertyType.PlaybackRate,
            19 => ModulatorCommonPropertyType.StopPlayback,
            _ => default,
        };
    public int ToRaw(BankVersion version, ModulatorCommonPropertyType value) =>
        value switch
        {
            ModulatorCommonPropertyType.Scope => 0,
            ModulatorCommonPropertyType.TriggerOn => 1,
            ModulatorCommonPropertyType.Depth => 2,
            ModulatorCommonPropertyType.Frequency => 3,
            ModulatorCommonPropertyType.Waveform => 4,
            ModulatorCommonPropertyType.Smoothing => 5,
            ModulatorCommonPropertyType.PulseWidthModulation => 6,
            ModulatorCommonPropertyType.Attack => 7,
            ModulatorCommonPropertyType.InitialPhaseOffset => 8,
            ModulatorCommonPropertyType.AttackTime => 9,
            ModulatorCommonPropertyType.AttackCurve => 10,
            ModulatorCommonPropertyType.DecayTime => 11,
            ModulatorCommonPropertyType.SustainLevel => 12,
            ModulatorCommonPropertyType.ReleaseTime => 13,
            ModulatorCommonPropertyType.SustainTime => 14,
            ModulatorCommonPropertyType.InitialDelay => 15,
            ModulatorCommonPropertyType.Duration => 16,
            ModulatorCommonPropertyType.Loop => 17,
            ModulatorCommonPropertyType.PlaybackRate => 18,
            ModulatorCommonPropertyType.StopPlayback => 19,
            _ => 0,
        };
}

internal sealed class AudioCommonPropertyTypeWire : IWireEnum<AudioCommonPropertyType>
{
    public static readonly AudioCommonPropertyTypeWire Instance = new();

    public int Width(BankVersion version) => 8;
    public AudioCommonPropertyType FromRaw(BankVersion version, int raw) =>
        raw switch
        {
            0 => AudioCommonPropertyType.BusVolume,
            1 => AudioCommonPropertyType.OutputBusVolume,
            2 => AudioCommonPropertyType.OutputBusLowPassFilter,
            3 => AudioCommonPropertyType.VoiceVolume,
            4 => AudioCommonPropertyType.VoicePitch,
            5 => AudioCommonPropertyType.VoiceLowPassFilter,
            6 => AudioCommonPropertyType.GameDefinedAuxiliarySendVolume,
            7 => AudioCommonPropertyType.UserDefinedAuxiliarySendVolume0,
            8 => AudioCommonPropertyType.UserDefinedAuxiliarySendVolume1,
            9 => AudioCommonPropertyType.UserDefinedAuxiliarySendVolume2,
            10 => AudioCommonPropertyType.UserDefinedAuxiliarySendVolume3,
            11 => AudioCommonPropertyType.PositioningCenterPercent,
            12 => AudioCommonPropertyType.PositioningSpeakerPanningX,
            13 => AudioCommonPropertyType.PositioningSpeakerPanningY,
            14 => AudioCommonPropertyType.PlaybackPriorityValue,
            15 => AudioCommonPropertyType.PlaybackPriorityOffsetAtMaximumDistance,
            16 => AudioCommonPropertyType.PlaybackLoop,
            17 => AudioCommonPropertyType.MotionVolumeOffset,
            18 => AudioCommonPropertyType.MotionLowPassFilter,
            19 => AudioCommonPropertyType.VoiceVolumeMakeUpGain,
            20 => AudioCommonPropertyType.HdrThreshold,
            21 => AudioCommonPropertyType.HdrRatio,
            22 => AudioCommonPropertyType.HdrReleaseTime,
            23 => AudioCommonPropertyType.HdrWindowTapOutputGameParameterIdentifier,
            24 => AudioCommonPropertyType.HdrWindowTapOutputGameParameterMinimum,
            25 => AudioCommonPropertyType.HdrWindowTapOutputGameParameterMaximum,
            26 => AudioCommonPropertyType.HdrEnvelopeTrackingActiveRange,
            27 => AudioCommonPropertyType.PlaybackInitialDelay,
            28 => AudioCommonPropertyType.OutputBusHighPassFilter,
            29 => AudioCommonPropertyType.VoiceHighPassFilter,
            30 => AudioCommonPropertyType.MidiNoteTrackingRootNote,
            31 => AudioCommonPropertyType.MidiEventPlayOn,
            32 => AudioCommonPropertyType.MidiTransformationTransposition,
            33 => AudioCommonPropertyType.MidiTransformationVelocityOffset,
            34 => AudioCommonPropertyType.MidiFilterKeyRangeMinimum,
            35 => AudioCommonPropertyType.MidiFilterKeyRangeMaximum,
            36 => AudioCommonPropertyType.MidiFilterVelocityMinimum,
            37 => AudioCommonPropertyType.MidiFilterVelocityMaximum,
            38 => AudioCommonPropertyType.MidiFilterChannel,
            39 => AudioCommonPropertyType.MidiClipTempoSource,
            40 => AudioCommonPropertyType.MidiTargetIdentifier,
            41 => AudioCommonPropertyType.PlaybackSpeed,
            42 => AudioCommonPropertyType.MixerIdentifier,
            43 => AudioCommonPropertyType.GameDefinedAuxiliarySendLowPassFilter,
            44 => AudioCommonPropertyType.GameDefinedAuxiliarySendHighPassFilter,
            45 => AudioCommonPropertyType.UserDefinedAuxiliarySendLowPassFilter0,
            46 => AudioCommonPropertyType.UserDefinedAuxiliarySendLowPassFilter1,
            47 => AudioCommonPropertyType.UserDefinedAuxiliarySendLowPassFilter2,
            48 => AudioCommonPropertyType.UserDefinedAuxiliarySendLowPassFilter3,
            49 => AudioCommonPropertyType.UserDefinedAuxiliarySendHighPassFilter0,
            50 => AudioCommonPropertyType.UserDefinedAuxiliarySendHighPassFilter1,
            51 => AudioCommonPropertyType.UserDefinedAuxiliarySendHighPassFilter2,
            52 => AudioCommonPropertyType.UserDefinedAuxiliarySendHighPassFilter3,
            53 => AudioCommonPropertyType.PositioningListenerRoutingSpeakerPanningDivisionSpatializationMix,
            54 => AudioCommonPropertyType.PositioningListenerRoutingAttenuationIdentifier,
            55 => AudioCommonPropertyType.EarlyReflectionAuxiliarySendVolume,
            56 => AudioCommonPropertyType.PositioningSpeakerPanningZ,
            _ => default,
        };
    public int ToRaw(BankVersion version, AudioCommonPropertyType value) =>
        value switch
        {
            AudioCommonPropertyType.BusVolume => 0,
            AudioCommonPropertyType.OutputBusVolume => 1,
            AudioCommonPropertyType.OutputBusLowPassFilter => 2,
            AudioCommonPropertyType.VoiceVolume => 3,
            AudioCommonPropertyType.VoicePitch => 4,
            AudioCommonPropertyType.VoiceLowPassFilter => 5,
            AudioCommonPropertyType.GameDefinedAuxiliarySendVolume => 6,
            AudioCommonPropertyType.UserDefinedAuxiliarySendVolume0 => 7,
            AudioCommonPropertyType.UserDefinedAuxiliarySendVolume1 => 8,
            AudioCommonPropertyType.UserDefinedAuxiliarySendVolume2 => 9,
            AudioCommonPropertyType.UserDefinedAuxiliarySendVolume3 => 10,
            AudioCommonPropertyType.PositioningCenterPercent => 11,
            AudioCommonPropertyType.PositioningSpeakerPanningX => 12,
            AudioCommonPropertyType.PositioningSpeakerPanningY => 13,
            AudioCommonPropertyType.PlaybackPriorityValue => 14,
            AudioCommonPropertyType.PlaybackPriorityOffsetAtMaximumDistance => 15,
            AudioCommonPropertyType.PlaybackLoop => 16,
            AudioCommonPropertyType.MotionVolumeOffset => 17,
            AudioCommonPropertyType.MotionLowPassFilter => 18,
            AudioCommonPropertyType.VoiceVolumeMakeUpGain => 19,
            AudioCommonPropertyType.HdrThreshold => 20,
            AudioCommonPropertyType.HdrRatio => 21,
            AudioCommonPropertyType.HdrReleaseTime => 22,
            AudioCommonPropertyType.HdrWindowTapOutputGameParameterIdentifier => 23,
            AudioCommonPropertyType.HdrWindowTapOutputGameParameterMinimum => 24,
            AudioCommonPropertyType.HdrWindowTapOutputGameParameterMaximum => 25,
            AudioCommonPropertyType.HdrEnvelopeTrackingActiveRange => 26,
            AudioCommonPropertyType.PlaybackInitialDelay => 27,
            AudioCommonPropertyType.OutputBusHighPassFilter => 28,
            AudioCommonPropertyType.VoiceHighPassFilter => 29,
            AudioCommonPropertyType.MidiNoteTrackingRootNote => 30,
            AudioCommonPropertyType.MidiEventPlayOn => 31,
            AudioCommonPropertyType.MidiTransformationTransposition => 32,
            AudioCommonPropertyType.MidiTransformationVelocityOffset => 33,
            AudioCommonPropertyType.MidiFilterKeyRangeMinimum => 34,
            AudioCommonPropertyType.MidiFilterKeyRangeMaximum => 35,
            AudioCommonPropertyType.MidiFilterVelocityMinimum => 36,
            AudioCommonPropertyType.MidiFilterVelocityMaximum => 37,
            AudioCommonPropertyType.MidiFilterChannel => 38,
            AudioCommonPropertyType.MidiClipTempoSource => 39,
            AudioCommonPropertyType.MidiTargetIdentifier => 40,
            AudioCommonPropertyType.PlaybackSpeed => 41,
            AudioCommonPropertyType.MixerIdentifier => 42,
            AudioCommonPropertyType.GameDefinedAuxiliarySendLowPassFilter => 43,
            AudioCommonPropertyType.GameDefinedAuxiliarySendHighPassFilter => 44,
            AudioCommonPropertyType.UserDefinedAuxiliarySendLowPassFilter0 => 45,
            AudioCommonPropertyType.UserDefinedAuxiliarySendLowPassFilter1 => 46,
            AudioCommonPropertyType.UserDefinedAuxiliarySendLowPassFilter2 => 47,
            AudioCommonPropertyType.UserDefinedAuxiliarySendLowPassFilter3 => 48,
            AudioCommonPropertyType.UserDefinedAuxiliarySendHighPassFilter0 => 49,
            AudioCommonPropertyType.UserDefinedAuxiliarySendHighPassFilter1 => 50,
            AudioCommonPropertyType.UserDefinedAuxiliarySendHighPassFilter2 => 51,
            AudioCommonPropertyType.UserDefinedAuxiliarySendHighPassFilter3 => 52,
            AudioCommonPropertyType.PositioningListenerRoutingSpeakerPanningDivisionSpatializationMix => 53,
            AudioCommonPropertyType.PositioningListenerRoutingAttenuationIdentifier => 54,
            AudioCommonPropertyType.EarlyReflectionAuxiliarySendVolume => 55,
            AudioCommonPropertyType.PositioningSpeakerPanningZ => 56,
            _ => 0,
        };
}
