namespace LiarUtil.Core.Bnk.Model;

internal sealed class AudioEffectSettingItem
{
    public long Index = 0L;
    public long Identifier = 0L;
    public bool UseShareSet = false;
    public bool U1 = false;
    public bool Bypass = false;
}

internal sealed class AudioEffectSetting
{
    public WireTuple<bool, bool, bool, bool, bool> Bypass = default;
    public List<AudioEffectSettingItem> Item = [];
}

internal sealed class AudioMetadataSettingItem
{
    public long Index = 0L;
    public long Identifier = 0L;
    public bool UseShareSet = false;
}

internal sealed class AudioMetadataSetting
{
    public List<AudioMetadataSettingItem> Item = [];
}
