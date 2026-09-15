namespace LiarUtil.Core.Bnk.Codec;

using LiarUtil.Core.Bnk.Model;

internal static partial class BankCodecImpl
{
    public static void Section(BankContext c, List<EffectU1> u1Value)
    {
        if (c.Version.AtLeast(112))
        {
            c.List(u1Value, SizeKind.U16,
    static (BankContext ctx8, ref EffectU1 item8) =>
            {
                if (ctx8.Version.AtLeast(112))
                {
                    ctx8.U8(ref item8.Type);
                }
                if (ctx8.Version.AtLeast(128))
                {
                    var bits8 = ctx8.Bits8();
                    bits8.Enum(ref item8.Mode, CoordinateModeWire.Instance);
                    bits8.Done();
                }
                if (ctx8.Version.AtLeast(112))
                {
                    ctx8.F32(ref item8.Value);
                }
            }
            );
        }
    }

    public static void Section(BankContext c, LowFrequencyOscillatorModulator value)
    {
        c.Id(ref value.Identifier);
        if (c.Version.AtLeast(112))
        {
            {
                var commonProperty = new CommonPropertyMap<ModulatorCommonPropertyType>();
                CommonProperty.Read(c, commonProperty, ModulatorCommonPropertyTypeTable.Instance, true);
                if (c.Version.AtLeast(112))
                {
                    value.Depth.Value = CommonProperty.Float(commonProperty, ModulatorCommonPropertyType.Depth, 100.0d);
                }
                if (c.Version.AtLeast(112))
                {
                    value.Frequency.Value = CommonProperty.Float(commonProperty, ModulatorCommonPropertyType.Frequency, 1.0d);
                }
                if (c.Version.AtLeast(112))
                {
                    value.Waveform = ModulatorWaveformWire.Instance.FromRaw(c.Version, CommonProperty.Enum(commonProperty, ModulatorCommonPropertyType.Waveform, (byte)ModulatorWaveformWire.Instance.ToRaw(c.Version, value.Waveform)));
                }
                if (c.Version.AtLeast(112))
                {
                    value.Smoothing.Value = CommonProperty.Float(commonProperty, ModulatorCommonPropertyType.Smoothing, 0.0d);
                }
                if (c.Version.AtLeast(112))
                {
                    value.PulseWidthModulation.Value = CommonProperty.Float(commonProperty, ModulatorCommonPropertyType.PulseWidthModulation, 50.0d);
                }
                if (c.Version.AtLeast(112))
                {
                    value.Attack.Value = CommonProperty.Float(commonProperty, ModulatorCommonPropertyType.Attack, 0.0d);
                }
                if (c.Version.AtLeast(112))
                {
                    value.InitialPhaseOffset.Value = CommonProperty.Float(commonProperty, ModulatorCommonPropertyType.InitialPhaseOffset, 0.0d);
                }
                if (c.Version.AtLeast(112))
                {
                    value.Scope = ModulatorScopeWire.Instance.FromRaw(c.Version, CommonProperty.Enum(commonProperty, ModulatorCommonPropertyType.Scope, (byte)ModulatorScopeWire.Instance.ToRaw(c.Version, value.Scope)));
                }
            }
        }
        if (c.Version.AtLeast(112))
        {
            Section(c, value.RealTimeParameterControl);
        }
    }

    public static void Section(BankContext c, EnvelopeModulator value)
    {
        c.Id(ref value.Identifier);
        if (c.Version.AtLeast(112))
        {
            {
                var commonProperty = new CommonPropertyMap<ModulatorCommonPropertyType>();
                CommonProperty.Read(c, commonProperty, ModulatorCommonPropertyTypeTable.Instance, true);
                if (c.Version.AtLeast(112))
                {
                    value.AttackTime.Value = CommonProperty.Float(commonProperty, ModulatorCommonPropertyType.AttackTime, 0.2d);
                }
                if (c.Version.AtLeast(112))
                {
                    value.AttackCurve.Value = CommonProperty.Float(commonProperty, ModulatorCommonPropertyType.AttackCurve, 50.0d);
                }
                if (c.Version.AtLeast(112))
                {
                    value.DecayTime.Value = CommonProperty.Float(commonProperty, ModulatorCommonPropertyType.DecayTime, 0.2d);
                }
                if (c.Version.AtLeast(112))
                {
                    value.SustainLevel.Value = CommonProperty.Float(commonProperty, ModulatorCommonPropertyType.SustainLevel, 100.0d);
                }
                if (c.Version.AtLeast(112))
                {
                    value.ReleaseTime.Value = CommonProperty.Float(commonProperty, ModulatorCommonPropertyType.ReleaseTime, 0.5d);
                }
                if (c.Version.AtLeast(112))
                {
                    value.Scope = ModulatorScopeWire.Instance.FromRaw(c.Version, CommonProperty.Enum(commonProperty, ModulatorCommonPropertyType.Scope, (byte)ModulatorScopeWire.Instance.ToRaw(c.Version, value.Scope)));
                }
                if (c.Version.AtLeast(112))
                {
                    value.TriggerOn = ModulatorTriggerOnWire.Instance.FromRaw(c.Version, CommonProperty.Enum(commonProperty, ModulatorCommonPropertyType.TriggerOn, (byte)ModulatorTriggerOnWire.Instance.ToRaw(c.Version, value.TriggerOn)));
                }
                if (c.Version.AtLeast(112))
                {
                    value.SustainTime.Value = CommonProperty.Float(commonProperty, ModulatorCommonPropertyType.SustainTime, 0.0d);
                }
                if (c.Version.AtLeast(112))
                {
                    value.StopPlaybackAfterRelease = CommonProperty.Bool(commonProperty, ModulatorCommonPropertyType.StopPlayback, value.StopPlaybackAfterRelease);
                }
            }
        }
        if (c.Version.AtLeast(112))
        {
            Section(c, value.RealTimeParameterControl);
        }
    }

    public static void Section(BankContext c, TimeModulator value)
    {
        c.Id(ref value.Identifier);
        if (c.Version.AtLeast(132))
        {
            {
                var commonProperty = new CommonPropertyMap<ModulatorCommonPropertyType>();
                CommonProperty.Read(c, commonProperty, ModulatorCommonPropertyTypeTable.Instance, true);
                if (c.Version.AtLeast(132))
                {
                    value.InitialDelay.Value = CommonProperty.Float(commonProperty, ModulatorCommonPropertyType.InitialDelay, 0.0d);
                }
                if (c.Version.AtLeast(132))
                {
                    value.Duration.Value = CommonProperty.Float(commonProperty, ModulatorCommonPropertyType.Duration, 1.0d);
                }
                if (c.Version.AtLeast(132))
                {
                    value.Loop.Value = CommonProperty.Int(commonProperty, ModulatorCommonPropertyType.Loop, 1);
                }
                if (c.Version.AtLeast(132))
                {
                    value.PlaybackRate.Value = CommonProperty.Float(commonProperty, ModulatorCommonPropertyType.PlaybackRate, 1.0d);
                }
                if (c.Version.AtLeast(132))
                {
                    value.Scope = ModulatorScopeWire.Instance.FromRaw(c.Version, CommonProperty.Enum(commonProperty, ModulatorCommonPropertyType.Scope, (byte)ModulatorScopeWire.Instance.ToRaw(c.Version, value.Scope)));
                }
                if (c.Version.AtLeast(132))
                {
                    value.TriggerOn = ModulatorTriggerOnWire.Instance.FromRaw(c.Version, CommonProperty.Enum(commonProperty, ModulatorCommonPropertyType.TriggerOn, (byte)ModulatorTriggerOnWire.Instance.ToRaw(c.Version, value.TriggerOn)));
                }
                if (c.Version.AtLeast(132))
                {
                    value.StopPlaybackAtEnd = CommonProperty.Bool(commonProperty, ModulatorCommonPropertyType.StopPlayback, value.StopPlaybackAtEnd);
                }
            }
        }
        if (c.Version.AtLeast(132))
        {
            Section(c, value.RealTimeParameterControl);
        }
    }

    public static void Section(BankContext c, Effect value)
    {
        c.Id(ref value.Identifier);
        if (c.Version.AtLeast(72))
        {
            c.Id(ref value.PlugIn);
        }
        if (c.Version.AtLeast(72))
        {
            c.Data(ref value.Expand, SizeKind.U32);
        }
        if (c.Version.AtLeast(72))
        {
            c.Const8(0);
        }
        if (c.Version.AtLeast(72))
        {
            Section(c, value.RealTimeParameterControl);
        }
        if (c.Version.In(125, 128))
        {
            c.Const16(0);
        }
        if (c.Version.AtLeast(128))
        {
            Section(c, value.State);
        }
        if (c.Version.AtLeast(112))
        {
            Section(c, value.U1);
        }
    }
}
