namespace LiarUtil.Core.Bnk.Model;

internal sealed class LowFrequencyOscillatorModulator
{
    public long Identifier = 0L;
    public RandomizableValue<double> Depth = new();
    public RandomizableValue<double> Frequency = new();
    public ModulatorWaveform Waveform = default;
    public RandomizableValue<double> Smoothing = new();
    public RandomizableValue<double> PulseWidthModulation = new();
    public RandomizableValue<double> Attack = new();
    public RandomizableValue<double> InitialPhaseOffset = new();
    public ModulatorScope Scope = default;
    public RealTimeParameterControlSetting RealTimeParameterControl = new();
}

internal sealed class EnvelopeModulator
{
    public long Identifier = 0L;
    public RandomizableValue<double> AttackTime = new();
    public RandomizableValue<double> AttackCurve = new();
    public RandomizableValue<double> DecayTime = new();
    public RandomizableValue<double> SustainLevel = new();
    public RandomizableValue<double> ReleaseTime = new();
    public ModulatorScope Scope = default;
    public ModulatorTriggerOn TriggerOn = default;
    public RandomizableValue<double> SustainTime = new();
    public bool StopPlaybackAfterRelease = false;
    public RealTimeParameterControlSetting RealTimeParameterControl = new();
}

internal sealed class TimeModulator
{
    public long Identifier = 0L;
    public RandomizableValue<double> InitialDelay = new();
    public RegularValue<double> Duration = new();
    public RandomizableValue<long> Loop = new();
    public RandomizableValue<double> PlaybackRate = new();
    public ModulatorScope Scope = default;
    public ModulatorTriggerOn TriggerOn = default;
    public bool StopPlaybackAtEnd = false;
    public RealTimeParameterControlSetting RealTimeParameterControl = new();
}

internal sealed class EffectU1
{
    public long Type = 0L;
    public double Value = 0d;
    public CoordinateMode Mode = default;
}

internal class Effect
{
    public long Identifier = 0L;
    public long PlugIn = 0L;
    public byte[] Expand = [];
    public RealTimeParameterControlSetting RealTimeParameterControl = new();
    public List<EffectU1> U1 = [];
    public StateSetting State = new();
}
