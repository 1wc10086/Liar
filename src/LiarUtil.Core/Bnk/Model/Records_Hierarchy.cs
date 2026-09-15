namespace LiarUtil.Core.Bnk.Model;

internal sealed class UnknownHierarchy
{
    public long Type = 0L;
    public byte[] Data = [];
}

internal sealed class StateGroupCustomTransition
{
    public long From = 0L;
    public long To = 0L;
    public long Time = 0L;
}

internal sealed class StateGroup
{
    public long Identifier = 0L;
    public long DefaultTransition = 0L;
    public List<StateGroupCustomTransition> CustomTransition = [];
}

internal sealed class SwitchGroup
{
    public long Identifier = 0L;
    public Parameter Parameter = new();
    public List<CoordinateIdentifierPoint> Point = [];
}

internal sealed class GameParameter
{
    public long Identifier = 0L;
    public double RangeDefault = 0d;
    public GameParameterInterpolationMode InterpolationMode = default;
    public double InterpolationAttack = 0d;
    public double InterpolationRelease = 0d;
    public GameParameterBindToBuiltInParameterMode BindToBuiltInParameter = default;
}

internal sealed class GameSynchronizationU1
{
    public long Identifier = 0L;
    public double U1 = 0d;
    public double U2 = 0d;
    public double U3 = 0d;
    public double U4 = 0d;
    public double U5 = 0d;
    public double U6 = 0d;
}

internal sealed class StatefulPropertySettingItem
{
    public long Type = 0L;
    public double Value = 0d;
}

internal sealed class StatefulPropertySetting
{
    public long Identifier = 0L;
    public List<StatefulPropertySettingItem> Value = [];
}

internal sealed class EventActionProperty
{
}

internal sealed class EventActionPropertyPlayAudio
{
    public RandomizableValue<long> Delay = new();
    public RandomizableValue<long> FadeTime = new();
    public Curve FadeCurve = default;
    public double Probability = 0d;
    public long SoundBank = 0L;
}

internal sealed class EventActionPropertyStopAudio
{
    public RandomizableValue<long> Delay = new();
    public RandomizableValue<long> FadeTime = new();
    public Curve FadeCurve = default;
    public bool ResumeStateTransition = false;
    public bool ApplyToDynamicSequence = false;
}

internal sealed class EventActionPropertyPauseAudio
{
    public RandomizableValue<long> Delay = new();
    public RandomizableValue<long> FadeTime = new();
    public Curve FadeCurve = default;
    public bool IncludeDelayedResumeAction = false;
    public bool ResumeStateTransition = false;
    public bool ApplyToDynamicSequence = false;
}

internal sealed class EventActionPropertyResumeAudio
{
    public RandomizableValue<long> Delay = new();
    public RandomizableValue<long> FadeTime = new();
    public Curve FadeCurve = default;
    public bool MasterResume = false;
    public bool ResumeStateTransition = false;
    public bool ApplyToDynamicSequence = false;
}

internal sealed class EventActionPropertyBreakAudio
{
    public RandomizableValue<long> Delay = new();
}

internal sealed class EventActionPropertySeekAudio
{
    public RandomizableValue<long> Delay = new();
    public EventActionPropertySeekType SeekType = default;
    public RandomizableValue<double> SeekValue = new();
    public bool SeekToNearestMarker = false;
}

internal sealed class EventActionPropertyPostEvent
{
    public RandomizableValue<long> Delay = new();
}

internal sealed class EventActionPropertySetBusVolume
{
    public bool Reset = false;
    public RandomizableValue<long> Delay = new();
    public RandomizableValue<long> FadeTime = new();
    public Curve FadeCurve = default;
    public EventActionPropertyValueApplyMode ApplyMode = default;
    public RandomizableValue<double> Value = new();
}

internal sealed class EventActionPropertySetVoiceVolume
{
    public bool Reset = false;
    public RandomizableValue<long> Delay = new();
    public RandomizableValue<long> FadeTime = new();
    public Curve FadeCurve = default;
    public EventActionPropertyValueApplyMode ApplyMode = default;
    public RandomizableValue<double> Value = new();
}

internal sealed class EventActionPropertySetVolumePitch
{
    public bool Reset = false;
    public RandomizableValue<long> Delay = new();
    public RandomizableValue<long> FadeTime = new();
    public Curve FadeCurve = default;
    public EventActionPropertyValueApplyMode ApplyMode = default;
    public RandomizableValue<double> Value = new();
}

internal sealed class EventActionPropertySetVolumeLowPassFilter
{
    public bool Reset = false;
    public RandomizableValue<long> Delay = new();
    public RandomizableValue<long> FadeTime = new();
    public Curve FadeCurve = default;
    public EventActionPropertyValueApplyMode ApplyMode = default;
    public RandomizableValue<double> Value = new();
}

internal sealed class EventActionPropertySetVolumeHighPassFilter
{
    public bool Reset = false;
    public RandomizableValue<long> Delay = new();
    public RandomizableValue<long> FadeTime = new();
    public Curve FadeCurve = default;
    public EventActionPropertyValueApplyMode ApplyMode = default;
    public RandomizableValue<double> Value = new();
}

internal sealed class EventActionPropertySetMute
{
    public bool Reset = false;
    public RandomizableValue<long> Delay = new();
    public RandomizableValue<long> FadeTime = new();
    public Curve FadeCurve = default;
}

internal sealed class EventActionPropertySetGameParameter
{
    public bool Reset = false;
    public RandomizableValue<long> Delay = new();
    public RandomizableValue<long> FadeTime = new();
    public Curve FadeCurve = default;
    public EventActionPropertyValueApplyMode ApplyMode = default;
    public RandomizableValue<double> Value = new();
    public bool BypassGameParameterInterpolation = false;
}

internal sealed class EventActionPropertySetStateAvailability
{
    public bool Enable = false;
    public RandomizableValue<long> Delay = new();
}

internal sealed class EventActionPropertyActivateState
{
    public RandomizableValue<long> Delay = new();
    public long Group = 0L;
    public long Item = 0L;
}

internal sealed class EventActionPropertyActivateSwitch
{
    public RandomizableValue<long> Delay = new();
    public long Group = 0L;
    public long Item = 0L;
}

internal sealed class EventActionPropertyActivateTrigger
{
    public RandomizableValue<long> Delay = new();
}

internal sealed class EventActionPropertySetBypassEffect
{
    public bool Reset = false;
    public bool Enable = false;
    public RandomizableValue<long> Delay = new();
    public WireTuple<bool, bool, bool, bool, bool> Value = default;
}

internal sealed class EventActionPropertyReleaseEnvelope
{
    public RandomizableValue<long> Delay = new();
}

internal sealed class EventActionPropertyResetPlaylist
{
    public RandomizableValue<long> Delay = new();
}

internal sealed class EventActionException
{
    public long Identifier = 0L;
    public bool U1 = false;
}

internal sealed class EventAction
{
    public long Identifier = 0L;
    public long Target = 0L;
    public EventActionMode Mode = default;
    public List<EventActionException> Exception = [];
    public EventActionScope Scope = default;
    public long U1 = 0L;
    public EventActionPropertyItem Property = null!;
}

internal sealed class Event
{
    public long Identifier = 0L;
    public List<long> Child = [];
}

internal sealed class DialogueEvent
{
    public long Identifier = 0L;
    public AudioAssociationSetting Association = new();
    public long Probability = 0L;
}

internal sealed class AttenuationCurve
{
    public CoordinateMode Mode = default;
    public List<CoordinatePoint> Point = [];
}

internal sealed class AttenuationApplySetting
{
    public long OutputBusVolume = 0L;
    public long AuxiliarySendVolume = 0L;
    public long LowPassFilter = 0L;
    public long Spread = 0L;
    public long GameDefinedAuxiliarySendVolume = 0L;
    public long UserDefinedAuxiliarySendVolume = 0L;
    public long HighPassFilter = 0L;
    public long Focus = 0L;
    public long DistanceOutputBusVolume = 0L;
    public long DistanceGameDefinedAuxiliarySendVolume = 0L;
    public long DistanceUserDefinedAuxiliarySendVolume = 0L;
    public long DistanceLowPassFilter = 0L;
    public long DistanceHighPassFilter = 0L;
    public long DistanceSpread = 0L;
    public long DistanceFocus = 0L;
    public long ObstructionVolume = 0L;
    public long ObstructionLowPassFilter = 0L;
    public long ObstructionHighPassFilter = 0L;
    public long OcclusionVolume = 0L;
    public long OcclusionLowPassFilter = 0L;
    public long OcclusionHighPassFilter = 0L;
    public long DiffractionVolume = 0L;
    public long DiffractionLowPassFilter = 0L;
    public long DiffractionHighPassFilter = 0L;
    public long TransmissionVolume = 0L;
    public long TransmissionLowPassFilter = 0L;
    public long TransmissionHighPassFilter = 0L;
}

internal sealed class AttenuationCone
{
    public bool Enable = false;
    public double InnerAngle = 0d;
    public double OuterAngle = 0d;
    public double MaximumValue = 0d;
    public double LowPassFilter = 0d;
    public double HighPassFilter = 0d;
}

internal sealed class Attenuation
{
    public long Identifier = 0L;
    public AttenuationApplySetting Apply = new();
    public List<AttenuationCurve> Curve = [];
    public AttenuationCone Cone = new();
    public RealTimeParameterControlSetting RealTimeParameterControl = new();
    public bool HeightSpread = false;
}
