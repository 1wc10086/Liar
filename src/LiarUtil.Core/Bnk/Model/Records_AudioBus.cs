namespace LiarUtil.Core.Bnk.Model;

internal sealed class BusBusSetting
{
    public RegularValue<double> Volume = new();
}

internal sealed class BusOutputBusSetting
{
    public RegularValue<double> Volume = new();
    public RegularValue<double> LowPassFilter = new();
    public RegularValue<double> HighPassFilter = new();
}

internal sealed class AudioOutputBusSetting
{
    public long Bus = 0L;
    public RegularValue<double> Volume = new();
    public RegularValue<double> LowPassFilter = new();
    public RegularValue<double> HighPassFilter = new();
}

internal sealed class AudioGameDefinedAuxiliarySendSetting
{
    public bool Enable = false;
    public RegularValue<double> Volume = new();
    public RegularValue<double> LowPassFilter = new();
    public RegularValue<double> HighPassFilter = new();
}

internal sealed class AudioUserDefinedAuxiliarySendSettingItem
{
    public long Bus = 0L;
    public RegularValue<double> Volume = new();
    public RegularValue<double> LowPassFilter = new();
    public RegularValue<double> HighPassFilter = new();
}

internal sealed class AudioUserDefinedAuxiliarySendSetting
{
    public bool Enable = false;
    public AudioUserDefinedAuxiliarySendSettingItem Item1 = new();
    public AudioUserDefinedAuxiliarySendSettingItem Item2 = new();
    public AudioUserDefinedAuxiliarySendSettingItem Item3 = new();
    public AudioUserDefinedAuxiliarySendSettingItem Item4 = new();
}

internal sealed class AudioEarlyReflectionAuxiliarySendSetting
{
    public long Bus = 0L;
    public RegularValue<double> Volume = new();
}

internal sealed class AudioAuxiliarySendSetting
{
    public AudioGameDefinedAuxiliarySendSetting GameDefined = new();
    public AudioUserDefinedAuxiliarySendSetting UserDefined = new();
    public AudioEarlyReflectionAuxiliarySendSetting EarlyReflection = new();
}
