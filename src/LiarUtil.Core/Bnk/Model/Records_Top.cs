namespace LiarUtil.Core.Bnk.Model;

internal sealed class SoundBankReference
{
    public long Identifier = 0L;
    public string Name = "";
}

internal sealed class PlugInReference
{
    public long Identifier = 0L;
    public string Library = "";
}

internal sealed class GameSynchronization
{
    public List<StateGroup> StateGroup = [];
    public List<SwitchGroup> SwitchGroup = [];
    public List<GameParameter> GameParameter = [];
    public List<GameSynchronizationU1> U1 = [];
}

internal sealed class ObstructionSetting
{
    public bool Enable = false;
    public CoordinateMode Mode = default;
    public List<CoordinatePoint> Point = [];
}

internal sealed class ObstructionSettingBundle
{
    public ObstructionSetting Volume = new();
    public ObstructionSetting LowPassFilter = new();
    public ObstructionSetting HighPassFilter = new();
}

internal sealed class Setting
{
    public double VolumeThreshold = 0d;
    public long MaximumVoiceInstance = 0L;
    public ObstructionSettingBundle Obstruction = new();
    public ObstructionSettingBundle Occlusion = new();
    public string Platform = "";
    public List<PlugInReference> PlugIn = [];
    public VoiceFilterBehavior VoiceFilterBehavior = default;
}

internal sealed class SoundBank
{
    public long Identifier = 0L;
    public long Language = 0L;
    public byte[] HeaderExpand = [];
    public List<long> EmbeddedMedia = [];
    public List<SoundBankReference> Reference = [];
    public Setting? Setting = null;
    public GameSynchronization? GameSynchronization = null;
    public List<Hierarchy> Hierarchy = [];
}
