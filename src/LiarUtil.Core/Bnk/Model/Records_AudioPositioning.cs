namespace LiarUtil.Core.Bnk.Model;

internal sealed class AudioPositioningSettingListenerRoutingPositionSourceAutomationPoint
{
    public Position3<double> Position = default;
    public long Duration = 0L;
}

internal sealed class AudioPositioningSettingListenerRoutingPositionSourceAutomationPathPoint
{
    public long Begin = 0L;
    public long Count = 0L;
}

internal sealed class AudioPositioningSettingListenerRoutingPositionSourceAutomationPathRandomRange
{
    public double LeftRight = 0d;
    public double FrontBack = 0d;
    public double UpDown = 0d;
}

internal sealed class AudioPositioningSettingListenerRoutingPositionSourceAutomationPath
{
    public AudioPositioningSettingListenerRoutingPositionSourceAutomationPathPoint Point = new();
    public AudioPositioningSettingListenerRoutingPositionSourceAutomationPathRandomRange RandomRange = new();
}

internal sealed class AudioPositioningSettingListenerRoutingPositionSourceAutomation
{
    public AudioPlayType PlayType = default;
    public AudioPlayMode PlayMode = default;
    public bool PickNewPathWhenSoundStart = false;
    public bool Loop = false;
    public long TransitionTime = 0L;
    public List<AudioPositioningSettingListenerRoutingPositionSourceAutomationPoint> Point = [];
    public List<AudioPositioningSettingListenerRoutingPositionSourceAutomationPath> Path = [];
}

internal sealed class AudioPositioningSettingListenerRoutingPositionSource
{
    public AudioPositioningSettingListenerRoutingPositionSourceMode Mode = default;
    public bool HoldListenerOrientation = false;
    public bool UpdateAtEachFrame = false;
    public AudioPositioningSettingListenerRoutingPositionSourceAutomation Automation = new();
    public bool HoldEmitterPositionAndOrientation = false;
    public bool DiffractionAndTransmission = false;
}

internal sealed class AudioPositioningSettingListenerRoutingAttenuation
{
    public long Identifier = 0L;
    public bool Enable = false;
}

internal sealed class AudioPositioningSettingListenerRouting
{
    public AudioPositioningSettingListenerRoutingSpatialization Spatialization = default;
    public AudioPositioningSettingListenerRoutingAttenuation Attenuation = new();
    public AudioPositioningSettingListenerRoutingPositionSource PositionSource = new();
    public bool Enable = false;
    public RegularValue<double> SpeakerPanningDivsionSpatializationMix = new();
}

internal sealed class AudioPositioningSettingSpeakerPanning
{
    public bool Enable = false;
    public Position3<double> Position = default;
    public AudioPositioningSettingSpeakerPanningMode Mode = default;
}

internal sealed class AudioPositioningSetting
{
    public AudioPositioningSettingType Type = default;
    public RegularValue<double> CenterPercent = new();
    public AudioPositioningSettingSpeakerPanning SpeakerPanning = new();
    public AudioPositioningSettingListenerRouting ListenerRouting = new();
    public bool Enable = false;
}

internal sealed class AudioMotionSetting
{
    public RandomizableValue<double> VolumeOffset = new();
    public RandomizableValue<double> LowPassFilter = new();
}

internal sealed class AudioMixerSetting
{
    public long Identifier = 0L;
}

internal sealed class BusAutomaticDuckingSettingBus
{
    public long Identifier = 0L;
    public double Volume = 0d;
    public long FadeOut = 0L;
    public long FadeIn = 0L;
    public Curve Curve = default;
    public BusAutomaticDuckingSettingBusTarget Target = default;
}

internal sealed class BusAutomaticDuckingSetting
{
    public long RecoveryTime = 0L;
    public double MaximumDuckingVolume = 0d;
    public List<BusAutomaticDuckingSettingBus> Bus = [];
}
