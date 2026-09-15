namespace LiarUtil.Core.Bnk.Codec;

using LiarUtil.Core.Bnk.Model;

internal static partial class BankCodecImpl
{
    public static void Section(BankContext c, StateGroup value)
    {
        c.Id(ref value.Identifier);
        if (c.Version.AtLeast(72))
        {
            c.U32(ref value.DefaultTransition);
        }
        if (c.Version.AtLeast(72))
        {
            c.List(value.CustomTransition, SizeKind.U32,
    static (BankContext ctx34, ref StateGroupCustomTransition item34) =>
            {
                if (ctx34.Version.AtLeast(72))
                {
                    ctx34.Id(ref item34.From);
                }
                if (ctx34.Version.AtLeast(72))
                {
                    ctx34.Id(ref item34.To);
                }
                if (ctx34.Version.AtLeast(72))
                {
                    ctx34.U32(ref item34.Time);
                }
            }
            );
        }
    }

    public static void Section(BankContext c, SwitchGroup value)
    {
        c.Id(ref value.Identifier);
        if (c.Version.AtLeast(72))
        {
            c.Id(ref value.Parameter.Identifier);
        }
        if (c.Version.AtLeast(112))
        {
            var bits114 = c.Bits8();
            bits114.Enum(ref value.Parameter.Category, ParameterCategoryWire.Instance);
            bits114.Done();
        }
        if (c.Version.AtLeast(72))
        {
            c.List(value.Point, SizeKind.U32,
    static (BankContext ctx35, ref CoordinateIdentifierPoint item35) =>
            {
                if (ctx35.Version.AtLeast(72))
                {
                    ctx35.F32(ref item35.Position.X);
                }
                if (ctx35.Version.AtLeast(72))
                {
                    ctx35.Id(ref item35.Position.Y);
                }
                if (ctx35.Version.AtLeast(72))
                {
                    var bits115 = ctx35.Bits32();
                    bits115.Enum(ref item35.Curve, CurveWire.Instance);
                    bits115.Done();
                }
            }
            );
        }
    }

    public static void Section(BankContext c, GameParameter value)
    {
        c.Id(ref value.Identifier);
        if (c.Version.AtLeast(72))
        {
            c.F32(ref value.RangeDefault);
        }
        if (c.Version.AtLeast(112))
        {
            var bits116 = c.Bits32();
            bits116.Enum(ref value.InterpolationMode, GameParameterInterpolationModeWire.Instance);
            bits116.Done();
        }
        if (c.Version.AtLeast(112))
        {
            c.F32(ref value.InterpolationAttack);
        }
        if (c.Version.AtLeast(112))
        {
            c.F32(ref value.InterpolationRelease);
        }
        if (c.Version.AtLeast(112))
        {
            var bits117 = c.Bits8();
            bits117.Enum(ref value.BindToBuiltInParameter, GameParameterBindToBuiltInParameterModeWire.Instance);
            bits117.Done();
        }
    }

    public static void Section(BankContext c, GameSynchronizationU1 value)
    {
        c.Id(ref value.Identifier);
        if (c.Version.AtLeast(140))
        {
            c.F32(ref value.U1);
        }
        if (c.Version.AtLeast(140))
        {
            c.F32(ref value.U2);
        }
        if (c.Version.AtLeast(140))
        {
            c.F32(ref value.U3);
        }
        if (c.Version.AtLeast(140))
        {
            c.F32(ref value.U4);
        }
        if (c.Version.AtLeast(140))
        {
            c.F32(ref value.U5);
        }
        if (c.Version.AtLeast(140))
        {
            c.F32(ref value.U6);
        }
    }

    public static void Section(BankContext c, StatefulPropertySetting value)
    {
        c.Id(ref value.Identifier);
        if (c.Version.In(72, 128))
        {
            c.List(value.Value, SizeKind.U8,
    static (BankContext ctx36, ref StatefulPropertySettingItem item36) =>
            {
                if (ctx36.Version.In(72, 128))
                {
                    ctx36.U8(ref item36.Type);
                }
                if (ctx36.Version.In(72, 128))
                {
                    ctx36.F32(ref item36.Value);
                }
            }
            );
        }
        if (c.Version.AtLeast(128))
        {
            c.List(value.Value, SizeKind.U16,
    static (BankContext ctx37, ref StatefulPropertySettingItem item37) =>
            {
                if (ctx37.Version.AtLeast(128))
                {
                    ctx37.U16(ref item37.Type);
                }
                if (ctx37.Version.AtLeast(128))
                {
                    ctx37.F32(ref item37.Value);
                }
            }
            );
        }
    }

    public static void Section(BankContext c, Event value)
    {
        c.Id(ref value.Identifier);
        if (c.Version.In(72, 125))
        {
            c.List(value.Child, SizeKind.U32, static (BankContext ctx, ref long item) => ctx.Id(ref item));
        }
        if (c.Version.AtLeast(125))
        {
            c.List(value.Child, SizeKind.U8, static (BankContext ctx, ref long item) => ctx.Id(ref item));
        }
    }

    public static void Section(BankContext c, DialogueEvent value)
    {
        c.Id(ref value.Identifier);
        if (c.Version.AtLeast(88))
        {
            c.U8(ref value.Probability);
        }
        if (c.Version.AtLeast(72))
        {
            Section(c, value.Association);
        }
        if (c.Version.AtLeast(120))
        {
            c.Const16(0);
        }
    }

    public static void Section(BankContext c, Attenuation value)
    {
        c.Id(ref value.Identifier);
        if (c.Version.AtLeast(140))
        {
            var bits118 = c.Bits8();
            bits118.Bit(ref value.HeightSpread);
            bits118.Done();
        }
        if (c.Version.AtLeast(72))
        {
            var bits119 = c.Bits8();
            bits119.Bit(ref value.Cone.Enable);
            bits119.Done();
            if (value.Cone.Enable)
            {
                if (c.Version.AtLeast(72))
                {
                    c.F32(ref value.Cone.InnerAngle);
                }
                if (c.Version.AtLeast(72))
                {
                    c.F32(ref value.Cone.OuterAngle);
                }
                if (c.Version.AtLeast(72))
                {
                    c.F32(ref value.Cone.MaximumValue);
                }
                if (c.Version.AtLeast(72))
                {
                    c.F32(ref value.Cone.LowPassFilter);
                }
                if (c.Version.AtLeast(112))
                {
                    c.F32(ref value.Cone.HighPassFilter);
                }
            }
        }
        if (c.Version.In(72, 88))
        {
            c.U8(ref value.Apply.OutputBusVolume);
            c.U8(ref value.Apply.AuxiliarySendVolume);
            c.U8(ref value.Apply.LowPassFilter);
            c.U8(ref value.Apply.Spread);
        }
        if (c.Version.In(88, 112))
        {
            c.U8(ref value.Apply.OutputBusVolume);
            c.U8(ref value.Apply.GameDefinedAuxiliarySendVolume);
            c.U8(ref value.Apply.UserDefinedAuxiliarySendVolume);
            c.U8(ref value.Apply.LowPassFilter);
            c.U8(ref value.Apply.Spread);
        }
        if (c.Version.In(112, 145))
        {
            c.U8(ref value.Apply.OutputBusVolume);
            c.U8(ref value.Apply.GameDefinedAuxiliarySendVolume);
            c.U8(ref value.Apply.UserDefinedAuxiliarySendVolume);
            c.U8(ref value.Apply.LowPassFilter);
            c.U8(ref value.Apply.HighPassFilter);
            c.U8(ref value.Apply.Spread);
            c.U8(ref value.Apply.Focus);
        }
        if (c.Version.AtLeast(145))
        {
            c.U8(ref value.Apply.DistanceOutputBusVolume);
            c.U8(ref value.Apply.DistanceGameDefinedAuxiliarySendVolume);
            c.U8(ref value.Apply.DistanceUserDefinedAuxiliarySendVolume);
            c.U8(ref value.Apply.DistanceLowPassFilter);
            c.U8(ref value.Apply.DistanceHighPassFilter);
            c.U8(ref value.Apply.DistanceSpread);
            c.U8(ref value.Apply.DistanceFocus);
            c.U8(ref value.Apply.ObstructionVolume);
            c.U8(ref value.Apply.ObstructionLowPassFilter);
            c.U8(ref value.Apply.ObstructionHighPassFilter);
            c.U8(ref value.Apply.OcclusionVolume);
            c.U8(ref value.Apply.OcclusionLowPassFilter);
            c.U8(ref value.Apply.OcclusionHighPassFilter);
            c.U8(ref value.Apply.DiffractionVolume);
            c.U8(ref value.Apply.DiffractionLowPassFilter);
            c.U8(ref value.Apply.DiffractionHighPassFilter);
            c.U8(ref value.Apply.TransmissionVolume);
            c.U8(ref value.Apply.TransmissionLowPassFilter);
            c.U8(ref value.Apply.TransmissionHighPassFilter);
        }
        if (c.Version.AtLeast(72))
        {
            c.List(value.Curve, SizeKind.U8,
    static (BankContext ctx38, ref AttenuationCurve item38) =>
            {
                if (ctx38.Version.AtLeast(72))
                {
                    var bits120 = ctx38.Bits8();
                    bits120.Enum(ref item38.Mode, CoordinateModeWire.Instance);
                    bits120.Done();
                }
                if (ctx38.Version.AtLeast(72))
                {
                    ctx38.List(item38.Point, SizeKind.U16,
    static (BankContext ctx39, ref CoordinatePoint item39) =>
                    {
                        if (ctx39.Version.AtLeast(72))
                        {
                            ctx39.F32(ref item39.Position.X);
                        }
                        if (ctx39.Version.AtLeast(72))
                        {
                            ctx39.F32(ref item39.Position.Y);
                        }
                        if (ctx39.Version.AtLeast(72))
                        {
                            var bits121 = ctx39.Bits32();
                            bits121.Enum(ref item39.Curve, CurveWire.Instance);
                            bits121.Done();
                        }
                    }
                    );
                }
            }
            );
        }
        if (c.Version.AtLeast(72))
        {
            Section(c, value.RealTimeParameterControl);
        }
    }
}
