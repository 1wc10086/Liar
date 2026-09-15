namespace LiarUtil.Core.Bnk.Model;

internal sealed class AudioPlaybackLimitSetting
{
    public RegularValue<long> Value = new();
    public AudioPlaybackLimitSettingScope Scope = default;
    public AudioPlaybackLimitSettingWhenLimitIsReached WhenLimitIsReached = default;
    public AudioPlaybackLimitSettingWhenPriorityIsEqual WhenPriorityIsEqual = default;
}

internal sealed class AudioVirtualVoiceSetting
{
    public AudioVirtualVoiceSettingBehavior Behavior = default;
    public AudioVirtualVoiceSettingOnReturnToPhysical OnReturnToPhysical = default;
}

internal sealed class AudioPlaybackPrioritySetting
{
    public RegularValue<double> Value = new();
    public bool UseDistanceFactor = false;
    public RegularValue<double> OffsetAtMaximumDistance = new();
}

internal sealed class AudioTimeSettingSignature
{
    public long First = 0L;
    public long Second = 0L;
}

internal sealed class AudioTimeSetting
{
    public double Time = 0d;
    public double Offset = 0d;
    public double Tempo = 0d;
    public AudioTimeSettingSignature Signature = new();
}

internal sealed class AudioSwitcherSetting
{
    public bool IsState = false;
    public long Group = 0L;
    public long DefaultItem = 0L;
}

internal sealed class AudioAssociationSettingArgument
{
    public long Identifier = 0L;
    public bool IsState = false;
}

internal sealed class AudioAssociationSettingPath
{
    public long U1 = 0L;
    public long Object = 0L;
    public long Weight = 0L;
    public long Probability = 0L;
}

internal sealed class AudioAssociationSetting
{
    public long Probability = 0L;
    public AudioAssociationSettingMode Mode = default;
    public List<AudioAssociationSettingArgument> Argument = [];
    public List<AudioAssociationSettingPath> Path = [];
}

internal sealed class AudioSourceSetting
{
    public long PlugIn = 0L;
    public AudioSourceType Type = default;
    public long Source = 0L;
    public long Resource = 0L;
    public long ResourceOffset = 0L;
    public long ResourceSize = 0L;
    public bool IsVoice = false;
    public bool NonCachableStream = false;
}

internal sealed class AudioPlayTypeRandomSetting
{
    public AudioPlayTypeRandomType Type = default;
    public long AvoidRepeat = 0L;
}

internal sealed class AudioPlayTypeSequenceSetting
{
    public AudioPlayTypeSequenceAtEndOfPlaylist AtEndOfPlaylist = default;
}

internal sealed class AudioPlayTypeSetting
{
    public AudioPlayTypeRandomSetting Random = new();
    public AudioPlayTypeSequenceSetting Sequence = new();
}

internal sealed class AudioPlayModeStepSetting
{
}

internal sealed class AudioPlayModeContinuousSetting
{
    public bool AlwaysResetPlaylist = false;
    public RandomizableValue<long> Loop = new();
    public AudioPlayModeContinuousTransitionType TransitionType = default;
    public RandomizableValue<double> TransitionDuration = new();
}

internal sealed class AudioPlayModeSetting
{
    public AudioPlayModeStepSetting Step = new();
    public AudioPlayModeContinuousSetting Continuous = new();
}
