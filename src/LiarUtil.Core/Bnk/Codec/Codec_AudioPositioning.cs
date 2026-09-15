namespace LiarUtil.Core.Bnk.Codec;

using LiarUtil.Core.Bnk.Model;

internal static partial class BankCodecImpl
{
    public static void Section(BankContext c, AudioMixerSetting mixerValue, ref bool mixerOverride)
    {
        if (c.Version.In(112, 150))
        {
            var bits14 = c.Bits8();
            bits14.Bit(ref mixerOverride);
            bits14.Done();
        }
    }

    public static void Section(BankContext c, AudioPositioningSetting positioningValue, ref bool positioningOverride)
    {
        if (c.Version.In(72, 112))
        {
            var bits21 = c.Bits8();
            bits21.Bit(ref positioningOverride);
            bits21.Done();
            if (positioningOverride)
            {
                var b1 = false;
                var b2 = false;
                var b3 = false;
                if (c.Version.In(88, 112))
                {
                    var bits22 = c.Bits8();
                    bits22.Bit(ref b3);
                    bits22.Done();
                }
                if (c.Version.In(72, 112))
                {
                    var bits23 = c.Bits8();
                    bits23.Enum(ref positioningValue.Type, AudioPositioningSettingTypeWire.Instance);
                    bits23.Done();
                }
                if (c.Version.In(72, 88))
                {
                    var bits24 = c.Bits8();
                    bits24.Bit(ref b1);
                    bits24.Bit(ref b2);
                    bits24.Done();
                }
                if (c.Version.In(88, 112))
                {
                    var bits25 = c.Bits8();
                    bits25.Bit(ref b1);
                    bits25.Done();
                }
                if (positioningValue.Type == AudioPositioningSettingType.TwoDimension)
                {
                    if (c.Version.In(72, 88))
                    {
                        // assert !(b2)
                    }
                    if (c.Version.In(88, 112))
                    {
                        // assert b3
                    }
                    positioningValue.SpeakerPanning.Enable = b1;
                }
                if (positioningValue.Type == AudioPositioningSettingType.ThreeDimension)
                {
                    if (c.Version.In(72, 88))
                    {
                        // assert b2
                    }
                    if (c.Version.In(88, 112))
                    {
                        // assert !(b3)
                    }
                    positioningValue.ListenerRouting.PositionSource.Mode = !b1 ? AudioPositioningSettingListenerRoutingPositionSourceMode.UserDefined : AudioPositioningSettingListenerRoutingPositionSourceMode.GameDefined;
                    c.Const8(0);
                    c.Const8(0);
                    c.Const8(0);
                    c.Id(ref positioningValue.ListenerRouting.Attenuation.Identifier);
                    var bits26 = c.Bits8();
                    bits26.Enum(ref positioningValue.ListenerRouting.Spatialization, AudioPositioningSettingListenerRoutingSpatializationWire.Instance);
                    bits26.Done();
                    if (positioningValue.ListenerRouting.PositionSource.Mode == AudioPositioningSettingListenerRoutingPositionSourceMode.GameDefined)
                    {
                        var bits27 = c.Bits8();
                        bits27.Bit(ref positioningValue.ListenerRouting.PositionSource.UpdateAtEachFrame);
                        bits27.Done();
                    }
                    if (positioningValue.ListenerRouting.PositionSource.Mode == AudioPositioningSettingListenerRoutingPositionSourceMode.UserDefined)
                    {
                        var bits28 = c.Bits8();
                        bits28.Enum(ref positioningValue.ListenerRouting.PositionSource.Automation.PlayType, AudioPlayTypeWire.Instance);
                        bits28.Enum(ref positioningValue.ListenerRouting.PositionSource.Automation.PlayMode, AudioPlayModeWire.Instance);
                        bits28.Bit(ref positioningValue.ListenerRouting.PositionSource.Automation.PickNewPathWhenSoundStart);
                        bits28.Done();
                        c.Const8(0);
                        c.Const8(0);
                        c.Const8(0);
                        var bits29 = c.Bits8();
                        bits29.Bit(ref positioningValue.ListenerRouting.PositionSource.Automation.Loop);
                        bits29.Done();
                        c.U32(ref positioningValue.ListenerRouting.PositionSource.Automation.TransitionTime);
                        var bits30 = c.Bits8();
                        bits30.Bit(ref positioningValue.ListenerRouting.PositionSource.HoldListenerOrientation);
                        bits30.Done();
                        c.List(positioningValue.ListenerRouting.PositionSource.Automation.Point, SizeKind.U32,
    static (BankContext ctx9, ref AudioPositioningSettingListenerRoutingPositionSourceAutomationPoint item9) =>
                        {
                            ctx9.F32(ref item9.Position.X);
                            ctx9.Const32(0u);
                            ctx9.F32(ref item9.Position.Y);
                            ctx9.U32(ref item9.Duration);
                        }
                        );
                        c.List(positioningValue.ListenerRouting.PositionSource.Automation.Path, SizeKind.U32,
    static (BankContext ctx10, ref AudioPositioningSettingListenerRoutingPositionSourceAutomationPath item10) =>
                        {
                            ctx10.U32(ref item10.Point.Begin);
                            ctx10.U32(ref item10.Point.Count);
                            ctx10.F32(ref item10.RandomRange.LeftRight);
                            ctx10.F32(ref item10.RandomRange.FrontBack);
                        }
                        );
                    }
                }
            }
        }
        if (c.Version.In(112, 132))
        {
            var b2 = false;
            if (c.Version.In(112, 125))
            {
                var bits31 = c.Bits8();
                bits31.Bit(ref positioningOverride);
                bits31.Bit(ref b2);
                bits31.Bit(ref positioningValue.SpeakerPanning.Enable);
                bits31.Enum(ref positioningValue.Type, AudioPositioningSettingTypeWire.Instance);
                bits31.Enum(ref positioningValue.ListenerRouting.Spatialization, AudioPositioningSettingListenerRoutingSpatializationWire.Instance);
                bits31.Bit(ref positioningValue.ListenerRouting.PositionSource.Automation.Loop);
                bits31.Bit(ref positioningValue.ListenerRouting.PositionSource.UpdateAtEachFrame);
                bits31.Bit(ref positioningValue.ListenerRouting.PositionSource.HoldListenerOrientation);
                bits31.Done();
            }
            if (c.Version.In(125, 132))
            {
                var bits32 = c.Bits8();
                bits32.Bit(ref positioningOverride);
                bits32.Bit(ref positioningValue.Enable);
                bits32.Bit(ref b2);
                bits32.Bit(ref positioningValue.SpeakerPanning.Enable);
                bits32.Enum(ref positioningValue.Type, AudioPositioningSettingTypeWire.Instance);
                bits32.Done();
            }
            if (positioningValue.Type == AudioPositioningSettingType.ThreeDimension)
            {
                if (c.Version.In(112, 125))
                {
                    var bits33 = c.Bits8();
                    bits33.Enum(ref positioningValue.ListenerRouting.PositionSource.Mode, AudioPositioningSettingListenerRoutingPositionSourceModeWire.Instance);
                    bits33.Done();
                }
                if (c.Version.In(125, 132))
                {
                    var bits34 = c.Bits8();
                    bits34.Enum(ref positioningValue.ListenerRouting.Spatialization, AudioPositioningSettingListenerRoutingSpatializationWire.Instance);
                    bits34.Bit(ref positioningValue.ListenerRouting.PositionSource.Automation.Loop);
                    bits34.Bit(ref positioningValue.ListenerRouting.PositionSource.UpdateAtEachFrame);
                    bits34.Bit(ref positioningValue.ListenerRouting.PositionSource.HoldListenerOrientation);
                    bits34.Enum(ref positioningValue.ListenerRouting.PositionSource.Mode, AudioPositioningSettingListenerRoutingPositionSourceModeWire.Instance);
                    bits34.Done();
                }
                if (c.Version.In(112, 132))
                {
                    c.Id(ref positioningValue.ListenerRouting.Attenuation.Identifier);
                }
                if (positioningValue.ListenerRouting.PositionSource.Mode == AudioPositioningSettingListenerRoutingPositionSourceMode.UserDefined)
                {
                    if (c.Version.In(112, 132))
                    {
                        var bits35 = c.Bits8();
                        bits35.Enum(ref positioningValue.ListenerRouting.PositionSource.Automation.PlayType, AudioPlayTypeWire.Instance);
                        bits35.Enum(ref positioningValue.ListenerRouting.PositionSource.Automation.PlayMode, AudioPlayModeWire.Instance);
                        bits35.Bit(ref positioningValue.ListenerRouting.PositionSource.Automation.PickNewPathWhenSoundStart);
                        bits35.Done();
                    }
                    if (c.Version.In(112, 132))
                    {
                        c.U32(ref positioningValue.ListenerRouting.PositionSource.Automation.TransitionTime);
                    }
                    if (c.Version.In(112, 132))
                    {
                        c.List(positioningValue.ListenerRouting.PositionSource.Automation.Point, SizeKind.U32,
    static (BankContext ctx11, ref AudioPositioningSettingListenerRoutingPositionSourceAutomationPoint item11) =>
                        {
                            if (ctx11.Version.In(112, 132))
                            {
                                ctx11.F32(ref item11.Position.X);
                            }
                            if (ctx11.Version.In(112, 132))
                            {
                                ctx11.F32(ref item11.Position.Z);
                            }
                            if (ctx11.Version.In(112, 132))
                            {
                                ctx11.F32(ref item11.Position.Y);
                            }
                            if (ctx11.Version.In(112, 132))
                            {
                                ctx11.U32(ref item11.Duration);
                            }
                        }
                        );
                    }
                    if (c.Version.In(112, 132))
                    {
                        c.List(positioningValue.ListenerRouting.PositionSource.Automation.Path, SizeKind.U32,
    static (BankContext ctx12, ref AudioPositioningSettingListenerRoutingPositionSourceAutomationPath item12) =>
                        {
                            if (ctx12.Version.In(112, 132))
                            {
                                ctx12.U32(ref item12.Point.Begin);
                                ctx12.U32(ref item12.Point.Count);
                                ctx12.F32(ref item12.RandomRange.LeftRight);
                                ctx12.F32(ref item12.RandomRange.FrontBack);
                                ctx12.F32(ref item12.RandomRange.UpDown);
                            }
                        }
                        );
                    }
                }
            }
        }
        if (c.Version.AtLeast(132))
        {
            if (c.Version.AtLeast(132))
            {
                var bits36 = c.Bits8();
                bits36.Bit(ref positioningOverride);
                bits36.Bit(ref positioningValue.ListenerRouting.Enable);
                bits36.Enum(ref positioningValue.SpeakerPanning.Mode, AudioPositioningSettingSpeakerPanningModeWire.Instance);
                bits36.Const(false);
                bits36.Enum(ref positioningValue.ListenerRouting.PositionSource.Mode, AudioPositioningSettingListenerRoutingPositionSourceModeWire.Instance);
                bits36.Const(false);
                bits36.Done();
            }
            if (positioningValue.ListenerRouting.Enable)
            {
                if (c.Version.In(132, 134))
                {
                    var bits37 = c.Bits8();
                    bits37.Enum(ref positioningValue.ListenerRouting.Spatialization, AudioPositioningSettingListenerRoutingSpatializationWire.Instance);
                    bits37.Bit(ref positioningValue.ListenerRouting.PositionSource.HoldEmitterPositionAndOrientation);
                    bits37.Bit(ref positioningValue.ListenerRouting.PositionSource.HoldListenerOrientation);
                    bits37.Bit(ref positioningValue.ListenerRouting.PositionSource.Automation.Loop);
                    bits37.Done();
                }
                if (c.Version.In(134, 140))
                {
                    var bits38 = c.Bits8();
                    bits38.Enum(ref positioningValue.ListenerRouting.Spatialization, AudioPositioningSettingListenerRoutingSpatializationWire.Instance);
                    bits38.Bit(ref positioningValue.ListenerRouting.Attenuation.Enable);
                    bits38.Bit(ref positioningValue.ListenerRouting.PositionSource.HoldEmitterPositionAndOrientation);
                    bits38.Bit(ref positioningValue.ListenerRouting.PositionSource.HoldListenerOrientation);
                    bits38.Bit(ref positioningValue.ListenerRouting.PositionSource.Automation.Loop);
                    bits38.Done();
                }
                if (c.Version.AtLeast(140))
                {
                    var bits39 = c.Bits8();
                    bits39.Enum(ref positioningValue.ListenerRouting.Spatialization, AudioPositioningSettingListenerRoutingSpatializationWire.Instance);
                    bits39.Bit(ref positioningValue.ListenerRouting.Attenuation.Enable);
                    bits39.Bit(ref positioningValue.ListenerRouting.PositionSource.HoldEmitterPositionAndOrientation);
                    bits39.Bit(ref positioningValue.ListenerRouting.PositionSource.HoldListenerOrientation);
                    bits39.Bit(ref positioningValue.ListenerRouting.PositionSource.Automation.Loop);
                    bits39.Bit(ref positioningValue.ListenerRouting.PositionSource.DiffractionAndTransmission);
                    bits39.Done();
                }
                if (positioningValue.ListenerRouting.PositionSource.Mode != AudioPositioningSettingListenerRoutingPositionSourceMode.Emitter)
                {
                    if (c.Version.AtLeast(132))
                    {
                        var bits40 = c.Bits8();
                        bits40.Enum(ref positioningValue.ListenerRouting.PositionSource.Automation.PlayType, AudioPlayTypeWire.Instance);
                        bits40.Enum(ref positioningValue.ListenerRouting.PositionSource.Automation.PlayMode, AudioPlayModeWire.Instance);
                        bits40.Bit(ref positioningValue.ListenerRouting.PositionSource.Automation.PickNewPathWhenSoundStart);
                        bits40.Done();
                    }
                    if (c.Version.AtLeast(132))
                    {
                        c.U32(ref positioningValue.ListenerRouting.PositionSource.Automation.TransitionTime);
                    }
                    if (c.Version.AtLeast(132))
                    {
                        c.List(positioningValue.ListenerRouting.PositionSource.Automation.Point, SizeKind.U32,
    static (BankContext ctx13, ref AudioPositioningSettingListenerRoutingPositionSourceAutomationPoint item13) =>
                        {
                            if (ctx13.Version.AtLeast(132))
                            {
                                ctx13.F32(ref item13.Position.X);
                            }
                            if (ctx13.Version.AtLeast(132))
                            {
                                ctx13.F32(ref item13.Position.Z);
                            }
                            if (ctx13.Version.AtLeast(132))
                            {
                                ctx13.F32(ref item13.Position.Y);
                            }
                            if (ctx13.Version.AtLeast(132))
                            {
                                ctx13.U32(ref item13.Duration);
                            }
                        }
                        );
                    }
                    if (c.Version.AtLeast(132))
                    {
                        c.List(positioningValue.ListenerRouting.PositionSource.Automation.Path, SizeKind.U32,
    static (BankContext ctx14, ref AudioPositioningSettingListenerRoutingPositionSourceAutomationPath item14) =>
                        {
                            if (ctx14.Version.AtLeast(132))
                            {
                                ctx14.U32(ref item14.Point.Begin);
                                ctx14.U32(ref item14.Point.Count);
                                ctx14.F32(ref item14.RandomRange.LeftRight);
                                ctx14.F32(ref item14.RandomRange.FrontBack);
                                ctx14.F32(ref item14.RandomRange.UpDown);
                            }
                        }
                        );
                    }
                }
            }
        }
    }

    public static void Section(BankContext c, BusAutomaticDuckingSetting automaticDuckingValue)
    {
        if (c.Version.AtLeast(72))
        {
            c.U32(ref automaticDuckingValue.RecoveryTime);
        }
        if (c.Version.AtLeast(72))
        {
            c.F32(ref automaticDuckingValue.MaximumDuckingVolume);
        }
        if (c.Version.AtLeast(72))
        {
            c.List(automaticDuckingValue.Bus, SizeKind.U32,
    static (BankContext ctx17, ref BusAutomaticDuckingSettingBus item17) =>
            {
                if (ctx17.Version.AtLeast(72))
                {
                    ctx17.Id(ref item17.Identifier);
                }
                if (ctx17.Version.AtLeast(72))
                {
                    ctx17.F32(ref item17.Volume);
                }
                if (ctx17.Version.AtLeast(72))
                {
                    ctx17.U32(ref item17.FadeOut);
                }
                if (ctx17.Version.AtLeast(72))
                {
                    ctx17.U32(ref item17.FadeIn);
                }
                if (ctx17.Version.AtLeast(72))
                {
                    var bits67 = ctx17.Bits8();
                    bits67.Enum(ref item17.Curve, CurveWire.Instance);
                    bits67.Done();
                }
                if (ctx17.Version.AtLeast(72))
                {
                    var bits68 = ctx17.Bits8();
                    bits68.Enum(ref item17.Target, BusAutomaticDuckingSettingBusTargetWire.Instance);
                    bits68.Done();
                }
            }
            );
        }
    }
}
