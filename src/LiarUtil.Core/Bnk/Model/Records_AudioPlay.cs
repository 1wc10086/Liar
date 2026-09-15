namespace LiarUtil.Core.Bnk.Model;

internal sealed class BusVoiceSetting
{
    public RegularValue<double> Volume = new();
    public RegularValue<double> Pitch = new();
    public RegularValue<double> LowPassFilter = new();
    public RegularValue<double> HighPassFilter = new();
}

internal sealed class AudioVoice
{
    public RandomizableValue<double> Volume = new();
    public RandomizableValue<double> Pitch = new();
    public RandomizableValue<double> LowPassFilter = new();
    public RandomizableValue<double> HighPassFilter = new();
}

internal sealed class BusVoiceVolumeGainSetting
{
    public RegularValue<double> MakeUp = new();
}

internal sealed class AudioVoiceVolumeGainSetting
{
    public bool Normalization = false;
    public RandomizableValue<double> MakeUp = new();
}
