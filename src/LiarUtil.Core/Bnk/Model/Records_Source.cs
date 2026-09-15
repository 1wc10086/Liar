namespace LiarUtil.Core.Bnk.Model;

internal sealed class Source : Effect
{
}

internal sealed class AudioDevice
{
    public long Identifier = 0L;
    public long PlugIn = 0L;
    public byte[] Expand = [];
    public RealTimeParameterControlSetting RealTimeParameterControl = new();
    public StateSetting State = new();
    public List<EffectU1> U1 = [];
    public AudioEffectSetting Effect = new();
}

internal sealed class AudioBusMuteForBackgroundMusic
{
    public bool Enable = false;
}

internal sealed class AudioBusConfiguration
{
    public long U1 = 0L;
}

internal class AudioBus
{
    public long Identifier = 0L;
    public long Parent = 0L;
    public BusVoiceSetting Voice = new();
    public BusBusSetting Bus = new();
    public AudioEffectSetting Effect = new();
    public RealTimeParameterControlSetting RealTimeParameterControl = new();
    public StateSetting State = new();
    public BusAutomaticDuckingSetting AutomaticDucking = new();
    public AudioPlaybackLimitSetting PlaybackLimit = new();
    public bool OverridePlaybackLimit = false;
    public AudioBusConfiguration BusConfiguration = new();
    public AudioPositioningSetting Positioning = new();
    public BusHdrSetting Hdr = new();
    public bool OverridePositioning = false;
    public long Mixer = 0L;
    public AudioBusMuteForBackgroundMusic MuteForBackgroundMusic = new();
    public BusVoiceVolumeGainSetting VoiceVolumeGain = new();
    public AudioAuxiliarySendSetting AuxiliarySend = new();
    public long AudioDevice = 0L;
    public BusOutputBusSetting OutputBus = new();
    public AudioMetadataSetting Metadata = new();
}

internal sealed class AuxiliaryAudioBus : AudioBus
{
}
