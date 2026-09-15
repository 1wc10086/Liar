using LiarUtil.Core.Bnk.Model;

namespace LiarUtil.Core.Bnk.Codec;

internal static class EventActionCodec
{
    private static CommonPropertyMap<EventActionCommonPropertyType> Properties(BankContext c)
    {
        var map = new CommonPropertyMap<EventActionCommonPropertyType>();
        CommonProperty.Read(c, map, EventActionCommonPropertyTypeTable.Instance, true);
        return map;
    }

    private static void Exceptions(BankContext c, List<EventActionException> list)
    {
        var kind = c.Version.In(72, 125) ? SizeKind.U32 : SizeKind.U8;
        c.List(list, kind, static (BankContext ctx, ref EventActionException item) =>
        {
            ctx.Id(ref item.Identifier);
            var bits = ctx.Bits8();
            bits.Bit(ref item.U1);
            bits.Done();
        });
    }

    public static void Read(BankContext c, EventAction value)
    {
        c.Id(ref value.Identifier);
        var head = c.Bits8();
        head.Enum(ref value.Scope, EventActionScopeWire.Instance);
        head.Enum(ref value.Mode, EventActionModeWire.Instance);
        head.Done();
        var rawType = c.Reader.ReadUInt8();
        c.Id(ref value.Target);
        c.U8(ref value.U1);
        value.Property = new EventActionPropertyItem();
        switch (rawType)
        {
            case 4:
            {
                var p = new EventActionPropertyPlayAudio();
                var map = Properties(c);
                CommonProperty.RandomizableInt(map, EventActionCommonPropertyType.Delay, p.Delay, 0);
                CommonProperty.RandomizableInt(map, EventActionCommonPropertyType.FadeTime, p.FadeTime, 0);
                p.Probability = CommonProperty.Float(map, EventActionCommonPropertyType.Probability, 100f);
                var bits = c.Bits8();
                bits.Enum(ref p.FadeCurve, CurveWire.Instance);
                bits.Done();
                c.Id(ref p.SoundBank);
                if (c.Version.AtLeast(145))
                {
                    c.Const32(0);
                }
                value.Property.Type = EventActionPropertyType.PlayAudio;
                value.Property.Item = p;
                break;
            }
            case 1:
            {
                var p = new EventActionPropertyStopAudio();
                var map = Properties(c);
                CommonProperty.RandomizableInt(map, EventActionCommonPropertyType.Delay, p.Delay, 0);
                CommonProperty.RandomizableInt(map, EventActionCommonPropertyType.FadeTime, p.FadeTime, 0);
                var bits = c.Bits8();
                bits.Enum(ref p.FadeCurve, CurveWire.Instance);
                bits.Done();
                if (c.Version.AtLeast(125))
                {
                    var b1 = c.Bits8();
                    b1.Bit(ref p.ResumeStateTransition);
                    b1.Bit(ref p.ApplyToDynamicSequence);
                    b1.Done();
                }
                Exceptions(c, value.Exception);
                value.Property.Type = EventActionPropertyType.StopAudio;
                value.Property.Item = p;
                break;
            }
            case 2:
            {
                var p = new EventActionPropertyPauseAudio();
                var map = Properties(c);
                CommonProperty.RandomizableInt(map, EventActionCommonPropertyType.Delay, p.Delay, 0);
                CommonProperty.RandomizableInt(map, EventActionCommonPropertyType.FadeTime, p.FadeTime, 0);
                var bits = c.Bits8();
                bits.Enum(ref p.FadeCurve, CurveWire.Instance);
                bits.Done();
                if (c.Version.In(72, 125))
                {
                    var b1 = c.Bits8();
                    b1.Bit(ref p.IncludeDelayedResumeAction);
                    b1.Done();
                }
                else if (c.Version.AtLeast(125))
                {
                    var b1 = c.Bits8();
                    b1.Bit(ref p.IncludeDelayedResumeAction);
                    b1.Bit(ref p.ResumeStateTransition);
                    b1.Bit(ref p.ApplyToDynamicSequence);
                    b1.Done();
                }
                Exceptions(c, value.Exception);
                value.Property.Type = EventActionPropertyType.PauseAudio;
                value.Property.Item = p;
                break;
            }
            case 3:
            {
                var p = new EventActionPropertyResumeAudio();
                var map = Properties(c);
                CommonProperty.RandomizableInt(map, EventActionCommonPropertyType.Delay, p.Delay, 0);
                CommonProperty.RandomizableInt(map, EventActionCommonPropertyType.FadeTime, p.FadeTime, 0);
                var bits = c.Bits8();
                bits.Enum(ref p.FadeCurve, CurveWire.Instance);
                bits.Done();
                if (c.Version.In(72, 125))
                {
                    var b1 = c.Bits8();
                    b1.Bit(ref p.MasterResume);
                    b1.Done();
                }
                else if (c.Version.AtLeast(125))
                {
                    var b1 = c.Bits8();
                    b1.Bit(ref p.MasterResume);
                    b1.Bit(ref p.ResumeStateTransition);
                    b1.Bit(ref p.ApplyToDynamicSequence);
                    b1.Done();
                }
                Exceptions(c, value.Exception);
                value.Property.Type = EventActionPropertyType.ResumeAudio;
                value.Property.Item = p;
                break;
            }
            case 28:
            {
                var p = new EventActionPropertyBreakAudio();
                var map = Properties(c);
                CommonProperty.RandomizableInt(map, EventActionCommonPropertyType.Delay, p.Delay, 0);
                value.Property.Type = EventActionPropertyType.BreakAudio;
                value.Property.Item = p;
                break;
            }
            case 30:
            {
                var p = new EventActionPropertySeekAudio();
                var map = Properties(c);
                CommonProperty.RandomizableInt(map, EventActionCommonPropertyType.Delay, p.Delay, 0);
                var b0 = c.Bits8();
                b0.Enum(ref p.SeekType, EventActionPropertySeekTypeWire.Instance);
                b0.Done();
                c.F32(ref p.SeekValue.Value);
                c.F32(ref p.SeekValue.MinimumValue);
                c.F32(ref p.SeekValue.MaximumValue);
                var b1 = c.Bits8();
                b1.Bit(ref p.SeekToNearestMarker);
                b1.Done();
                Exceptions(c, value.Exception);
                value.Property.Type = EventActionPropertyType.SeekAudio;
                value.Property.Item = p;
                break;
            }
            case 33:
            {
                var p = new EventActionPropertyPostEvent();
                var map = Properties(c);
                CommonProperty.RandomizableInt(map, EventActionCommonPropertyType.Delay, p.Delay, 0);
                value.Property.Type = EventActionPropertyType.PostEvent;
                value.Property.Item = p;
                break;
            }
            case 8:
            case 9:
            {
                var p = new EventActionPropertySetVolumePitch();
                p.Reset = rawType == 9;
                var map = Properties(c);
                CommonProperty.RandomizableInt(map, EventActionCommonPropertyType.Delay, p.Delay, 0);
                CommonProperty.RandomizableInt(map, EventActionCommonPropertyType.FadeTime, p.FadeTime, 0);
                var b0 = c.Bits8();
                b0.Enum(ref p.FadeCurve, CurveWire.Instance);
                b0.Done();
                var b1 = c.Bits8();
                b1.Enum(ref p.ApplyMode, EventActionPropertyValueApplyModeWire.Instance);
                b1.Done();
                c.F32(ref p.Value.Value);
                c.F32(ref p.Value.MinimumValue);
                c.F32(ref p.Value.MaximumValue);
                Exceptions(c, value.Exception);
                value.Property.Type = EventActionPropertyType.SetVoicePitch;
                value.Property.Item = p;
                break;
            }
            case 10:
            case 11:
            {
                var p = new EventActionPropertySetVoiceVolume();
                p.Reset = rawType == 11;
                var map = Properties(c);
                CommonProperty.RandomizableInt(map, EventActionCommonPropertyType.Delay, p.Delay, 0);
                CommonProperty.RandomizableInt(map, EventActionCommonPropertyType.FadeTime, p.FadeTime, 0);
                var b0 = c.Bits8();
                b0.Enum(ref p.FadeCurve, CurveWire.Instance);
                b0.Done();
                var b1 = c.Bits8();
                b1.Enum(ref p.ApplyMode, EventActionPropertyValueApplyModeWire.Instance);
                b1.Done();
                c.F32(ref p.Value.Value);
                c.F32(ref p.Value.MinimumValue);
                c.F32(ref p.Value.MaximumValue);
                Exceptions(c, value.Exception);
                value.Property.Type = EventActionPropertyType.SetVoiceVolume;
                value.Property.Item = p;
                break;
            }
            case 12:
            case 13:
            {
                var p = new EventActionPropertySetBusVolume();
                p.Reset = rawType == 13;
                var map = Properties(c);
                CommonProperty.RandomizableInt(map, EventActionCommonPropertyType.Delay, p.Delay, 0);
                CommonProperty.RandomizableInt(map, EventActionCommonPropertyType.FadeTime, p.FadeTime, 0);
                var b0 = c.Bits8();
                b0.Enum(ref p.FadeCurve, CurveWire.Instance);
                b0.Done();
                var b1 = c.Bits8();
                b1.Enum(ref p.ApplyMode, EventActionPropertyValueApplyModeWire.Instance);
                b1.Done();
                c.F32(ref p.Value.Value);
                c.F32(ref p.Value.MinimumValue);
                c.F32(ref p.Value.MaximumValue);
                Exceptions(c, value.Exception);
                value.Property.Type = EventActionPropertyType.SetBusVolume;
                value.Property.Item = p;
                break;
            }
            case 14:
            case 15:
            {
                var p = new EventActionPropertySetVolumeLowPassFilter();
                p.Reset = rawType == 15;
                var map = Properties(c);
                CommonProperty.RandomizableInt(map, EventActionCommonPropertyType.Delay, p.Delay, 0);
                CommonProperty.RandomizableInt(map, EventActionCommonPropertyType.FadeTime, p.FadeTime, 0);
                var b0 = c.Bits8();
                b0.Enum(ref p.FadeCurve, CurveWire.Instance);
                b0.Done();
                var b1 = c.Bits8();
                b1.Enum(ref p.ApplyMode, EventActionPropertyValueApplyModeWire.Instance);
                b1.Done();
                c.F32(ref p.Value.Value);
                c.F32(ref p.Value.MinimumValue);
                c.F32(ref p.Value.MaximumValue);
                Exceptions(c, value.Exception);
                value.Property.Type = EventActionPropertyType.SetVoiceLowPassFilter;
                value.Property.Item = p;
                break;
            }
            case 32:
            case 48:
            {
                var p = new EventActionPropertySetVolumeHighPassFilter();
                p.Reset = rawType == 48;
                var map = Properties(c);
                CommonProperty.RandomizableInt(map, EventActionCommonPropertyType.Delay, p.Delay, 0);
                CommonProperty.RandomizableInt(map, EventActionCommonPropertyType.FadeTime, p.FadeTime, 0);
                var b0 = c.Bits8();
                b0.Enum(ref p.FadeCurve, CurveWire.Instance);
                b0.Done();
                var b1 = c.Bits8();
                b1.Enum(ref p.ApplyMode, EventActionPropertyValueApplyModeWire.Instance);
                b1.Done();
                c.F32(ref p.Value.Value);
                c.F32(ref p.Value.MinimumValue);
                c.F32(ref p.Value.MaximumValue);
                Exceptions(c, value.Exception);
                value.Property.Type = EventActionPropertyType.SetVoiceHighPassFilter;
                value.Property.Item = p;
                break;
            }
            case 6:
            case 7:
            {
                var p = new EventActionPropertySetMute();
                p.Reset = rawType == 7;
                var map = Properties(c);
                CommonProperty.RandomizableInt(map, EventActionCommonPropertyType.Delay, p.Delay, 0);
                CommonProperty.RandomizableInt(map, EventActionCommonPropertyType.FadeTime, p.FadeTime, 0);
                var b0 = c.Bits8();
                b0.Enum(ref p.FadeCurve, CurveWire.Instance);
                b0.Done();
                Exceptions(c, value.Exception);
                value.Property.Type = EventActionPropertyType.SetMute;
                value.Property.Item = p;
                break;
            }
            case 19:
            case 20:
            {
                var p = new EventActionPropertySetGameParameter();
                p.Reset = rawType == 20;
                var map = Properties(c);
                CommonProperty.RandomizableInt(map, EventActionCommonPropertyType.Delay, p.Delay, 0);
                CommonProperty.RandomizableInt(map, EventActionCommonPropertyType.FadeTime, p.FadeTime, 0);
                var b0 = c.Bits8();
                b0.Enum(ref p.FadeCurve, CurveWire.Instance);
                b0.Done();
                if (c.Version.AtLeast(112))
                {
                    var bb = c.Bits8();
                    bb.Bit(ref p.BypassGameParameterInterpolation);
                    bb.Done();
                }
                var b1 = c.Bits8();
                b1.Enum(ref p.ApplyMode, EventActionPropertyValueApplyModeWire.Instance);
                b1.Done();
                c.F32(ref p.Value.Value);
                c.F32(ref p.Value.MinimumValue);
                c.F32(ref p.Value.MaximumValue);
                Exceptions(c, value.Exception);
                value.Property.Type = EventActionPropertyType.SetGameParameter;
                value.Property.Item = p;
                break;
            }
            case 16:
            case 17:
            {
                var p = new EventActionPropertySetStateAvailability();
                p.Enable = rawType == 16;
                var map = Properties(c);
                CommonProperty.RandomizableInt(map, EventActionCommonPropertyType.Delay, p.Delay, 0);
                value.Property.Type = EventActionPropertyType.SetStateAvailability;
                value.Property.Item = p;
                break;
            }
            case 18:
            {
                var p = new EventActionPropertyActivateState();
                var map = Properties(c);
                CommonProperty.RandomizableInt(map, EventActionCommonPropertyType.Delay, p.Delay, 0);
                c.Id(ref p.Group);
                c.Id(ref p.Item);
                value.Property.Type = EventActionPropertyType.ActivateState;
                value.Property.Item = p;
                break;
            }
            case 25:
            {
                var p = new EventActionPropertyActivateSwitch();
                var map = Properties(c);
                CommonProperty.RandomizableInt(map, EventActionCommonPropertyType.Delay, p.Delay, 0);
                c.Id(ref p.Group);
                c.Id(ref p.Item);
                value.Property.Type = EventActionPropertyType.ActivateSwitch;
                value.Property.Item = p;
                break;
            }
            case 29:
            {
                var p = new EventActionPropertyActivateTrigger();
                var map = Properties(c);
                CommonProperty.RandomizableInt(map, EventActionCommonPropertyType.Delay, p.Delay, 0);
                value.Property.Type = EventActionPropertyType.ActivateTrigger;
                value.Property.Item = p;
                break;
            }
            case 26:
            case 27:
            {
                var p = new EventActionPropertySetBypassEffect();
                p.Reset = rawType == 27;
                var map = Properties(c);
                CommonProperty.RandomizableInt(map, EventActionCommonPropertyType.Delay, p.Delay, 0);
                var b0 = c.Bits8();
                b0.Bit(ref p.Enable);
                b0.Done();
                var b1 = c.Bits8();
                b1.Bit(ref p.Value.Item1);
                b1.Bit(ref p.Value.Item2);
                b1.Bit(ref p.Value.Item3);
                b1.Bit(ref p.Value.Item4);
                b1.Bit(ref p.Value.Item5);
                b1.Const(p.Reset);
                b1.Const(p.Reset);
                b1.Const(p.Reset);
                b1.Done();
                Exceptions(c, value.Exception);
                value.Property.Type = EventActionPropertyType.SetBypassEffect;
                value.Property.Item = p;
                break;
            }
            case 31:
            {
                var p = new EventActionPropertyReleaseEnvelope();
                var map = Properties(c);
                CommonProperty.RandomizableInt(map, EventActionCommonPropertyType.Delay, p.Delay, 0);
                value.Property.Type = EventActionPropertyType.ReleaseEnvelope;
                value.Property.Item = p;
                break;
            }
            case 34:
            {
                var p = new EventActionPropertyResetPlaylist();
                var map = Properties(c);
                CommonProperty.RandomizableInt(map, EventActionCommonPropertyType.Delay, p.Delay, 0);
                if (c.Version.AtLeast(113))
                {
                    c.Const8(4);
                }
                if (c.Version.In(113, 115))
                {
                    c.Const32(0);
                }
                if (c.Version.AtLeast(115))
                {
                    c.Const8(0);
                }
                value.Property.Type = EventActionPropertyType.ResetPlaylist;
                value.Property.Item = p;
                break;
            }
        }
    }

    public static void Write(BankContext c, EventAction value)
    {
        c.Id(ref value.Identifier);
        var head = c.Bits8();
        head.Enum(ref value.Scope, EventActionScopeWire.Instance);
        head.Enum(ref value.Mode, EventActionModeWire.Instance);
        head.Done();
        c.Writer.WriteUInt8(RawType(value));
        c.Id(ref value.Target);
        c.U8(ref value.U1);
        switch (value.Property.Item)
        {
            case EventActionPropertyPlayAudio p:
            {
                var map = new CommonPropertyMap<EventActionCommonPropertyType>();
                CommonProperty.AddRandomizableInt(map, EventActionCommonPropertyType.Delay, p.Delay);
                CommonProperty.AddRandomizableInt(map, EventActionCommonPropertyType.FadeTime, p.FadeTime);
                CommonProperty.AddFloat(map, EventActionCommonPropertyType.Probability, p.Probability);
                CommonProperty.Read(c, map, EventActionCommonPropertyTypeTable.Instance, true);
                var bits = c.Bits8();
                bits.Enum(ref p.FadeCurve, CurveWire.Instance);
                bits.Done();
                c.Id(ref p.SoundBank);
                if (c.Version.AtLeast(145))
                {
                    c.Const32(0);
                }
                break;
            }
            case EventActionPropertyStopAudio p:
            {
                var map = new CommonPropertyMap<EventActionCommonPropertyType>();
                CommonProperty.AddRandomizableInt(map, EventActionCommonPropertyType.Delay, p.Delay);
                CommonProperty.AddRandomizableInt(map, EventActionCommonPropertyType.FadeTime, p.FadeTime);
                CommonProperty.Read(c, map, EventActionCommonPropertyTypeTable.Instance, true);
                var bits = c.Bits8();
                bits.Enum(ref p.FadeCurve, CurveWire.Instance);
                bits.Done();
                if (c.Version.AtLeast(125))
                {
                    var tail = c.Bits8();
                    tail.Bit(ref p.ResumeStateTransition);
                    tail.Bit(ref p.ApplyToDynamicSequence);
                    tail.Done();
                }
                Exceptions(c, value.Exception);
                break;
            }
            case EventActionPropertyPauseAudio p:
            {
                var map = new CommonPropertyMap<EventActionCommonPropertyType>();
                CommonProperty.AddRandomizableInt(map, EventActionCommonPropertyType.Delay, p.Delay);
                CommonProperty.AddRandomizableInt(map, EventActionCommonPropertyType.FadeTime, p.FadeTime);
                CommonProperty.Read(c, map, EventActionCommonPropertyTypeTable.Instance, true);
                var bits = c.Bits8();
                bits.Enum(ref p.FadeCurve, CurveWire.Instance);
                bits.Done();
                var tail = c.Bits8();
                tail.Bit(ref p.IncludeDelayedResumeAction);
                if (c.Version.AtLeast(125))
                {
                    tail.Bit(ref p.ResumeStateTransition);
                    tail.Bit(ref p.ApplyToDynamicSequence);
                }
                tail.Done();
                Exceptions(c, value.Exception);
                break;
            }
            case EventActionPropertyResumeAudio p:
            {
                var map = new CommonPropertyMap<EventActionCommonPropertyType>();
                CommonProperty.AddRandomizableInt(map, EventActionCommonPropertyType.Delay, p.Delay);
                CommonProperty.AddRandomizableInt(map, EventActionCommonPropertyType.FadeTime, p.FadeTime);
                CommonProperty.Read(c, map, EventActionCommonPropertyTypeTable.Instance, true);
                var bits = c.Bits8();
                bits.Enum(ref p.FadeCurve, CurveWire.Instance);
                bits.Done();
                var tail = c.Bits8();
                tail.Bit(ref p.MasterResume);
                if (c.Version.AtLeast(125))
                {
                    tail.Bit(ref p.ResumeStateTransition);
                    tail.Bit(ref p.ApplyToDynamicSequence);
                }
                tail.Done();
                Exceptions(c, value.Exception);
                break;
            }
            case EventActionPropertyBreakAudio p:
            {
                var map = new CommonPropertyMap<EventActionCommonPropertyType>();
                CommonProperty.AddRandomizableInt(map, EventActionCommonPropertyType.Delay, p.Delay);
                CommonProperty.Read(c, map, EventActionCommonPropertyTypeTable.Instance, true);
                break;
            }
            case EventActionPropertySeekAudio p:
            {
                var map = new CommonPropertyMap<EventActionCommonPropertyType>();
                CommonProperty.AddRandomizableInt(map, EventActionCommonPropertyType.Delay, p.Delay);
                CommonProperty.Read(c, map, EventActionCommonPropertyTypeTable.Instance, true);
                var b0 = c.Bits8();
                b0.Enum(ref p.SeekType, EventActionPropertySeekTypeWire.Instance);
                b0.Done();
                c.F32(ref p.SeekValue.Value);
                c.F32(ref p.SeekValue.MinimumValue);
                c.F32(ref p.SeekValue.MaximumValue);
                var b1 = c.Bits8();
                b1.Bit(ref p.SeekToNearestMarker);
                b1.Done();
                Exceptions(c, value.Exception);
                break;
            }
            case EventActionPropertyPostEvent p:
            {
                var map = new CommonPropertyMap<EventActionCommonPropertyType>();
                CommonProperty.AddRandomizableInt(map, EventActionCommonPropertyType.Delay, p.Delay);
                CommonProperty.Read(c, map, EventActionCommonPropertyTypeTable.Instance, true);
                break;
            }
            case EventActionPropertySetVolumePitch p:
            {
                var map = new CommonPropertyMap<EventActionCommonPropertyType>();
                CommonProperty.AddRandomizableInt(map, EventActionCommonPropertyType.Delay, p.Delay);
                CommonProperty.AddRandomizableInt(map, EventActionCommonPropertyType.FadeTime, p.FadeTime);
                CommonProperty.Read(c, map, EventActionCommonPropertyTypeTable.Instance, true);
                var b0 = c.Bits8();
                b0.Enum(ref p.FadeCurve, CurveWire.Instance);
                b0.Done();
                var b1 = c.Bits8();
                b1.Enum(ref p.ApplyMode, EventActionPropertyValueApplyModeWire.Instance);
                b1.Done();
                c.F32(ref p.Value.Value);
                c.F32(ref p.Value.MinimumValue);
                c.F32(ref p.Value.MaximumValue);
                Exceptions(c, value.Exception);
                break;
            }
            case EventActionPropertySetVoiceVolume p:
            {
                var map = new CommonPropertyMap<EventActionCommonPropertyType>();
                CommonProperty.AddRandomizableInt(map, EventActionCommonPropertyType.Delay, p.Delay);
                CommonProperty.AddRandomizableInt(map, EventActionCommonPropertyType.FadeTime, p.FadeTime);
                CommonProperty.Read(c, map, EventActionCommonPropertyTypeTable.Instance, true);
                var b0 = c.Bits8();
                b0.Enum(ref p.FadeCurve, CurveWire.Instance);
                b0.Done();
                var b1 = c.Bits8();
                b1.Enum(ref p.ApplyMode, EventActionPropertyValueApplyModeWire.Instance);
                b1.Done();
                c.F32(ref p.Value.Value);
                c.F32(ref p.Value.MinimumValue);
                c.F32(ref p.Value.MaximumValue);
                Exceptions(c, value.Exception);
                break;
            }
            case EventActionPropertySetBusVolume p:
            {
                var map = new CommonPropertyMap<EventActionCommonPropertyType>();
                CommonProperty.AddRandomizableInt(map, EventActionCommonPropertyType.Delay, p.Delay);
                CommonProperty.AddRandomizableInt(map, EventActionCommonPropertyType.FadeTime, p.FadeTime);
                CommonProperty.Read(c, map, EventActionCommonPropertyTypeTable.Instance, true);
                var b0 = c.Bits8();
                b0.Enum(ref p.FadeCurve, CurveWire.Instance);
                b0.Done();
                var b1 = c.Bits8();
                b1.Enum(ref p.ApplyMode, EventActionPropertyValueApplyModeWire.Instance);
                b1.Done();
                c.F32(ref p.Value.Value);
                c.F32(ref p.Value.MinimumValue);
                c.F32(ref p.Value.MaximumValue);
                Exceptions(c, value.Exception);
                break;
            }
            case EventActionPropertySetVolumeLowPassFilter p:
            {
                var map = new CommonPropertyMap<EventActionCommonPropertyType>();
                CommonProperty.AddRandomizableInt(map, EventActionCommonPropertyType.Delay, p.Delay);
                CommonProperty.AddRandomizableInt(map, EventActionCommonPropertyType.FadeTime, p.FadeTime);
                CommonProperty.Read(c, map, EventActionCommonPropertyTypeTable.Instance, true);
                var b0 = c.Bits8();
                b0.Enum(ref p.FadeCurve, CurveWire.Instance);
                b0.Done();
                var b1 = c.Bits8();
                b1.Enum(ref p.ApplyMode, EventActionPropertyValueApplyModeWire.Instance);
                b1.Done();
                c.F32(ref p.Value.Value);
                c.F32(ref p.Value.MinimumValue);
                c.F32(ref p.Value.MaximumValue);
                Exceptions(c, value.Exception);
                break;
            }
            case EventActionPropertySetVolumeHighPassFilter p:
            {
                var map = new CommonPropertyMap<EventActionCommonPropertyType>();
                CommonProperty.AddRandomizableInt(map, EventActionCommonPropertyType.Delay, p.Delay);
                CommonProperty.AddRandomizableInt(map, EventActionCommonPropertyType.FadeTime, p.FadeTime);
                CommonProperty.Read(c, map, EventActionCommonPropertyTypeTable.Instance, true);
                var b0 = c.Bits8();
                b0.Enum(ref p.FadeCurve, CurveWire.Instance);
                b0.Done();
                var b1 = c.Bits8();
                b1.Enum(ref p.ApplyMode, EventActionPropertyValueApplyModeWire.Instance);
                b1.Done();
                c.F32(ref p.Value.Value);
                c.F32(ref p.Value.MinimumValue);
                c.F32(ref p.Value.MaximumValue);
                Exceptions(c, value.Exception);
                break;
            }
            case EventActionPropertySetMute p:
            {
                var map = new CommonPropertyMap<EventActionCommonPropertyType>();
                CommonProperty.AddRandomizableInt(map, EventActionCommonPropertyType.Delay, p.Delay);
                CommonProperty.AddRandomizableInt(map, EventActionCommonPropertyType.FadeTime, p.FadeTime);
                CommonProperty.Read(c, map, EventActionCommonPropertyTypeTable.Instance, true);
                var b0 = c.Bits8();
                b0.Enum(ref p.FadeCurve, CurveWire.Instance);
                b0.Done();
                Exceptions(c, value.Exception);
                break;
            }
            case EventActionPropertySetGameParameter p:
            {
                var map = new CommonPropertyMap<EventActionCommonPropertyType>();
                CommonProperty.AddRandomizableInt(map, EventActionCommonPropertyType.Delay, p.Delay);
                CommonProperty.AddRandomizableInt(map, EventActionCommonPropertyType.FadeTime, p.FadeTime);
                CommonProperty.Read(c, map, EventActionCommonPropertyTypeTable.Instance, true);
                var b0 = c.Bits8();
                b0.Enum(ref p.FadeCurve, CurveWire.Instance);
                b0.Done();
                if (c.Version.AtLeast(112))
                {
                    var bb = c.Bits8();
                    bb.Bit(ref p.BypassGameParameterInterpolation);
                    bb.Done();
                }
                var b1 = c.Bits8();
                b1.Enum(ref p.ApplyMode, EventActionPropertyValueApplyModeWire.Instance);
                b1.Done();
                c.F32(ref p.Value.Value);
                c.F32(ref p.Value.MinimumValue);
                c.F32(ref p.Value.MaximumValue);
                Exceptions(c, value.Exception);
                break;
            }
            case EventActionPropertySetStateAvailability p:
            {
                var map = new CommonPropertyMap<EventActionCommonPropertyType>();
                CommonProperty.AddRandomizableInt(map, EventActionCommonPropertyType.Delay, p.Delay);
                CommonProperty.Read(c, map, EventActionCommonPropertyTypeTable.Instance, true);
                break;
            }
            case EventActionPropertyActivateState p:
            {
                var map = new CommonPropertyMap<EventActionCommonPropertyType>();
                CommonProperty.AddRandomizableInt(map, EventActionCommonPropertyType.Delay, p.Delay);
                CommonProperty.Read(c, map, EventActionCommonPropertyTypeTable.Instance, true);
                c.Id(ref p.Group);
                c.Id(ref p.Item);
                break;
            }
            case EventActionPropertyActivateSwitch p:
            {
                var map = new CommonPropertyMap<EventActionCommonPropertyType>();
                CommonProperty.AddRandomizableInt(map, EventActionCommonPropertyType.Delay, p.Delay);
                CommonProperty.Read(c, map, EventActionCommonPropertyTypeTable.Instance, true);
                c.Id(ref p.Group);
                c.Id(ref p.Item);
                break;
            }
            case EventActionPropertyActivateTrigger p:
            {
                var map = new CommonPropertyMap<EventActionCommonPropertyType>();
                CommonProperty.AddRandomizableInt(map, EventActionCommonPropertyType.Delay, p.Delay);
                CommonProperty.Read(c, map, EventActionCommonPropertyTypeTable.Instance, true);
                break;
            }
            case EventActionPropertySetBypassEffect p:
            {
                var map = new CommonPropertyMap<EventActionCommonPropertyType>();
                CommonProperty.AddRandomizableInt(map, EventActionCommonPropertyType.Delay, p.Delay);
                CommonProperty.Read(c, map, EventActionCommonPropertyTypeTable.Instance, true);
                var b0 = c.Bits8();
                b0.Bit(ref p.Enable);
                b0.Done();
                var b1 = c.Bits8();
                b1.Bit(ref p.Value.Item1);
                b1.Bit(ref p.Value.Item2);
                b1.Bit(ref p.Value.Item3);
                b1.Bit(ref p.Value.Item4);
                b1.Bit(ref p.Value.Item5);
                b1.Const(p.Reset);
                b1.Const(p.Reset);
                b1.Const(p.Reset);
                b1.Done();
                Exceptions(c, value.Exception);
                break;
            }
            case EventActionPropertyReleaseEnvelope p:
            {
                var map = new CommonPropertyMap<EventActionCommonPropertyType>();
                CommonProperty.AddRandomizableInt(map, EventActionCommonPropertyType.Delay, p.Delay);
                CommonProperty.Read(c, map, EventActionCommonPropertyTypeTable.Instance, true);
                break;
            }
            case EventActionPropertyResetPlaylist p:
            {
                var map = new CommonPropertyMap<EventActionCommonPropertyType>();
                CommonProperty.AddRandomizableInt(map, EventActionCommonPropertyType.Delay, p.Delay);
                CommonProperty.Read(c, map, EventActionCommonPropertyTypeTable.Instance, true);
                if (c.Version.AtLeast(113))
                {
                    c.Const8(4);
                }
                if (c.Version.In(113, 115))
                {
                    c.Const32(0);
                }
                if (c.Version.AtLeast(115))
                {
                    c.Const8(0);
                }
                break;
            }
        }
    }

    private static byte RawType(EventAction value)
    {
        return value.Property.Item switch
        {
            EventActionPropertyPlayAudio => 4,
            EventActionPropertyStopAudio => 1,
            EventActionPropertyPauseAudio => 2,
            EventActionPropertyResumeAudio => 3,
            EventActionPropertyBreakAudio => 28,
            EventActionPropertySeekAudio => 30,
            EventActionPropertyPostEvent => 33,
            EventActionPropertySetVolumePitch p => p.Reset ? (byte)9 : (byte)8,
            EventActionPropertySetVoiceVolume p => p.Reset ? (byte)11 : (byte)10,
            EventActionPropertySetBusVolume p => p.Reset ? (byte)13 : (byte)12,
            EventActionPropertySetVolumeLowPassFilter p => p.Reset ? (byte)15 : (byte)14,
            EventActionPropertySetVolumeHighPassFilter p => p.Reset ? (byte)48 : (byte)32,
            EventActionPropertySetMute p => p.Reset ? (byte)7 : (byte)6,
            EventActionPropertySetGameParameter p => p.Reset ? (byte)20 : (byte)19,
            EventActionPropertySetStateAvailability p => p.Enable ? (byte)16 : (byte)17,
            EventActionPropertyActivateState => 18,
            EventActionPropertyActivateSwitch => 25,
            EventActionPropertyActivateTrigger => 29,
            EventActionPropertySetBypassEffect p => p.Reset ? (byte)27 : (byte)26,
            EventActionPropertyReleaseEnvelope => 31,
            EventActionPropertyResetPlaylist => 34,
            _ => 0,
        };
    }
}
